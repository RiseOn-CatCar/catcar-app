using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes;
using Aspire.Hosting.Kubernetes.Resources;

namespace CatCar.AppHost;

/// <summary>
/// Configures production Kubernetes workload policies, security contexts, probes, and HPA for CatCar services.
/// </summary>
public static class KubernetesPublishingExtensions
{
    public static IResourceBuilder<T> ConfigureCatCarKubernetesWorkload<T>(this IResourceBuilder<T> builder)
        where T : IComputeResource
    {
        return builder.PublishAsKubernetesService(k8sResource =>
        {
            if (k8sResource.Workload is not Deployment deployment)
            {
                throw new InvalidOperationException("Expected a Kubernetes Deployment workload for the API service.");
            }

            // Deployment replicas and pod security
            deployment.Spec.Replicas = 2;

            var podSpec = deployment.Spec.Template.Spec;
            podSpec.AutomountServiceAccountToken = false;
            podSpec.SecurityContext = new PodSecurityContextV1
            {
                RunAsNonRoot = true,
                SeccompProfile = new SeccompProfileV1
                {
                    Type = "RuntimeDefault"
                }
            };

            // Writable /tmp volume
            podSpec.Volumes.Add(new VolumeV1
            {
                Name = "tmp",
                EmptyDir = new EmptyDirVolumeSourceV1()
            });

            var container = podSpec.Containers.FirstOrDefault()
                ?? throw new InvalidOperationException("Expected at least one container in the API Deployment.");

            container.VolumeMounts.Add(new VolumeMountV1
            {
                Name = "tmp",
                MountPath = "/tmp"
            });

            container.SecurityContext = new SecurityContextV1
            {
                AllowPrivilegeEscalation = false,
                ReadOnlyRootFilesystem = true,
                Capabilities = new CapabilitiesV1
                {
                    Drop = { "ALL" }
                }
            };

            container.Resources = new ResourceRequirementsV1();
            container.Resources.Requests["cpu"] = "250m";
            container.Resources.Requests["memory"] = "512Mi";
            container.Resources.Requests["ephemeral-storage"] = "256Mi";
            container.Resources.Limits["cpu"] = "1000m";
            container.Resources.Limits["memory"] = "1Gi";
            container.Resources.Limits["ephemeral-storage"] = "1Gi";

            container.ReadinessProbe = new ProbeV1
            {
                HttpGet = new HttpGetActionV1
                {
                    Path = "/health/ready",
                    Port = new Int32OrStringV1(8080)
                },
                InitialDelaySeconds = 10,
                PeriodSeconds = 10,
                TimeoutSeconds = 3,
                FailureThreshold = 3
            };

            container.LivenessProbe = new ProbeV1
            {
                HttpGet = new HttpGetActionV1
                {
                    Path = "/health/live",
                    Port = new Int32OrStringV1(8080)
                },
                InitialDelaySeconds = 15,
                PeriodSeconds = 15,
                TimeoutSeconds = 3,
                FailureThreshold = 3
            };

            // Service exposure (LoadBalancer 80 -> 8080)
            if (k8sResource.Service is { } service)
            {
                service.Spec.Type = "LoadBalancer";
                service.Spec.Ports.Clear();
                service.Spec.Ports.Add(new ServicePortV1
                {
                    Name = "http",
                    Port = 80,
                    TargetPort = new Int32OrStringV1(8080),
                    Protocol = "TCP"
                });
            }

            // autoscaling/v2 HorizontalPodAutoscaler targeting CPU and Memory at 70%
            var targetName = string.IsNullOrWhiteSpace(deployment.Metadata?.Name) ? "api-deployment" : deployment.Metadata.Name;
            var hpa = new CatCarHorizontalPodAutoscaler
            {
                Metadata = new ObjectMetaV1
                {
                    Name = "catcar-api-hpa",
                    Labels = new Dictionary<string, string>
                    {
                        ["app.kubernetes.io/name"] = "catcar-api"
                    }
                },
                Spec = new CatCarHpaSpec
                {
                    MinReplicas = 2,
                    MaxReplicas = 10,
                    ScaleTargetRef = new CatCarCrossVersionObjectReference
                    {
                        ApiVersion = "apps/v1",
                        Kind = "Deployment",
                        Name = targetName
                    },
                    Metrics =
                    [
                        new CatCarMetricSpec
                        {
                            Type = "Resource",
                            Resource = new CatCarResourceMetricSource
                            {
                                Name = "cpu",
                                Target = new CatCarMetricTarget
                                {
                                    Type = "Utilization",
                                    AverageUtilization = 70
                                }
                            }
                        },
                        new CatCarMetricSpec
                        {
                            Type = "Resource",
                            Resource = new CatCarResourceMetricSource
                            {
                                Name = "memory",
                                Target = new CatCarMetricTarget
                                {
                                    Type = "Utilization",
                                    AverageUtilization = 70
                                }
                            }
                        }
                    ]
                }
            };

            k8sResource.AdditionalResources.Add(hpa);
        });
    }
}
public sealed class CatCarHorizontalPodAutoscaler : BaseKubernetesResource
{
    public CatCarHorizontalPodAutoscaler() : base("autoscaling/v2", "HorizontalPodAutoscaler")
    {
    }
    public CatCarHpaSpec Spec { get; set; } = new();
}

public sealed class CatCarHpaSpec
{
    public int MinReplicas { get; set; } = 2;
    public int MaxReplicas { get; set; } = 10;
    public CatCarCrossVersionObjectReference ScaleTargetRef { get; set; } = new();
    public List<CatCarMetricSpec> Metrics { get; set; } = [];
}

public sealed class CatCarCrossVersionObjectReference
{
    public string ApiVersion { get; set; } = "apps/v1";
    public string Kind { get; set; } = "Deployment";
    public string Name { get; set; } = "api-deployment";
}

public sealed class CatCarMetricSpec
{
    public string Type { get; set; } = "Resource";
    public CatCarResourceMetricSource Resource { get; set; } = new();
}

public sealed class CatCarResourceMetricSource
{
    public string Name { get; set; } = "cpu";
    public CatCarMetricTarget Target { get; set; } = new();
}

public sealed class CatCarMetricTarget
{
    public string Type { get; set; } = "Utilization";
    public int AverageUtilization { get; set; } = 70;
}
