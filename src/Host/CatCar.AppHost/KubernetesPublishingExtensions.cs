using System.Net;
using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes;
using Aspire.Hosting.Kubernetes.Resources;
using YamlDotNet.Serialization;

namespace CatCar.AppHost;

/// <summary>
/// Configures production Kubernetes workload policies, security contexts, probes, and HPA for CatCar services.
/// </summary>
public static class KubernetesPublishingExtensions
{
    public static IResourceBuilder<T> ConfigureCatCarKubernetesWorkload<T>(this IResourceBuilder<T> builder, bool useKindDefaults = false, string? apiPrivateIp = null, string? image = null)
        where T : IComputeResource
    {
        if (!useKindDefaults && (!IPAddress.TryParse(apiPrivateIp, out var address) || address.AddressFamily != AddressFamily.InterNetwork))
        {
            throw new InvalidOperationException("A valid private IPv4 address is required for the cloud API service.");
        }

        return builder.PublishAsKubernetesService(k8sResource =>
        {
            if (k8sResource.Workload is not Deployment deployment)
            {
                throw new InvalidOperationException("Expected a Kubernetes Deployment workload for the API service.");
            }
            // Keep Aspire's default Deployment name ("api-deployment"): chart file names derive from metadata.name,
            // so a Deployment named "catcar-api" would overwrite the "catcar-api" Service template file.

            // Deployment replicas and pod security
            deployment.Spec.Replicas = useKindDefaults ? 1 : 2;
            deployment.Spec.Selector.MatchLabels["catcar.io/workload"] = "api";
            deployment.Spec.Template.Metadata.Labels["catcar.io/workload"] = "api";
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

            // Writable temporary volume
            podSpec.Volumes.Add(new VolumeV1
            {
                Name = "tmp",
                EmptyDir = new EmptyDirVolumeSourceV1()
            });

            var container = podSpec.Containers.FirstOrDefault()
                ?? throw new InvalidOperationException("Expected at least one container in the API Deployment.");

            if (image is not null)
            {
                container.Image = RequireDigest(image);
            }
            if (useKindDefaults)
            {
                AddLocalRuntimeEnvironment(container, authFunction: false);
            }

            container.VolumeMounts.Add(new VolumeMountV1
            {
                Name = "tmp",
                MountPath = "/app/tmp"
            });
            container.Env.Add(new EnvVarV1
            {
                Name = "TMPDIR",
                Value = "/app/tmp"
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

            // Service exposure (LoadBalancer in production, NodePort in Kind)
            if (k8sResource.Service is { } service)
            {
                service.Spec.Type = useKindDefaults ? "NodePort" : "LoadBalancer";
                service.Metadata.Name = "catcar-api";
                service.Spec.Selector["catcar.io/workload"] = "api";
                service.Spec.Ports.Clear();
                var servicePort = new ServicePortV1
                {
                    Name = "http",
                    Port = 80,
                    TargetPort = new Int32OrStringV1(8080),
                    Protocol = "TCP"
                };
                if (useKindDefaults)
                {
                    servicePort.NodePort = 30080;
                }
                service.Spec.Ports.Add(servicePort);

                if (!useKindDefaults)
                {
                    service.Metadata.Annotations["service.beta.kubernetes.io/azure-load-balancer-internal"] = "true";
                    service.Metadata.Annotations["service.beta.kubernetes.io/azure-load-balancer-ipv4"] = apiPrivateIp!;
                }
            }

            if (!useKindDefaults)
            {
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
            }
        });
    }

    public static IResourceBuilder<T> ConfigureLocalPostgres<T>(this IResourceBuilder<T> builder)
        where T : IComputeResource
    {
        return builder.PublishAsKubernetesService(resource =>
        {
            if (resource.Workload is not StatefulSet statefulSet)
                throw new InvalidOperationException("Expected a StatefulSet for persistent PostgreSQL.");
            var container = statefulSet.Spec.Template.Spec.Containers.First();
            container.Env.Add(Secret("POSTGRES_PASSWORD", "POSTGRES_PASSWORD"));
            container.Env.Add(new EnvVarV1 { Name = "PGDATA", Value = "/var/lib/postgresql/data/pgdata" });
            if (resource.Service is { } service)
                service.Metadata.Name = "postgres";
        });
    }

    public static IResourceBuilder<T> ConfigureLocalAzurite<T>(this IResourceBuilder<T> builder)
        where T : IComputeResource
    {
        return builder.PublishAsKubernetesService(resource =>
        {
            if (resource.Service is not { } service)
                throw new InvalidOperationException("Expected an Azurite service.");
            service.Metadata.Name = "azurite";
            service.Spec.Type = "ClusterIP";
            service.Spec.Ports.Clear();
            foreach (var (name, port) in new[] { ("blob", 10000), ("queue", 10001), ("table", 10002) })
                service.Spec.Ports.Add(new ServicePortV1
                {
                    Name = name,
                    Port = port,
                    TargetPort = new Int32OrStringV1(port),
                    Protocol = "TCP"
                });
        });
    }

    public static IResourceBuilder<T> ConfigureLocalService<T>(this IResourceBuilder<T> builder, string name, int port, string? image = null)
        where T : IComputeResource
    {
        return builder.PublishAsKubernetesService(resource =>
        {
            if (resource.Workload is not Deployment deployment)
                throw new InvalidOperationException($"Expected a Deployment for {name}.");
            var container = deployment.Spec.Template.Spec.Containers.First();
            if (image is not null)
            {
                container.Image = RequireDigest(image);
                AddLocalRuntimeEnvironment(container, authFunction: true);
            }
            if (resource.Service is { } service)
            {
                service.Metadata.Name = name;
                service.Spec.Type = "ClusterIP";
                service.Spec.Ports.Clear();
                service.Spec.Ports.Add(new ServicePortV1
                {
                    Name = "http",
                    Port = port,
                    TargetPort = new Int32OrStringV1(port),
                    Protocol = "TCP"
                });
            }
        });
    }

    private static void AddLocalRuntimeEnvironment(ContainerV1 container, bool authFunction)
    {
        container.Env.Add(Secret("POSTGRES_PASSWORD", authFunction ? "POSTGRES_AUTH_READONLY_PASSWORD" : "POSTGRES_PASSWORD"));
        container.Env.Add(Secret("CustomerJwt__SigningKey", "CUSTOMER_JWT_SIGNING_KEY"));
        if (!authFunction)
            container.Env.Add(Secret("Jwt__Secret", "JWT_SECRET"));
        container.Env.Add(new EnvVarV1
        {
            Name = authFunction ? "ConnectionStrings__catcar-auth" : "ConnectionStrings__catcar",
            Value = $"Host=postgres;Port=5432;Database=catcar;Username={(authFunction ? "catcar_auth_readonly" : "postgres")};Password=$(POSTGRES_PASSWORD)"
        });
    }

    private static EnvVarV1 Secret(string name, string key) => new()
    {
        Name = name,
        ValueFrom = new EnvVarSourceV1
        {
            SecretKeyRef = new SecretKeySelectorV1 { Name = "catcar-local-runtime", Key = key }
        }
    };

    private static string RequireDigest(string image)
    {
        var separator = image.LastIndexOf("@sha256:", StringComparison.Ordinal);
        if (separator <= 0 || image.Length != separator + 8 + 64 ||
            !image[(separator + 8)..].All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            throw new InvalidOperationException("An immutable sha256 image reference is required.");
        return image;
    }
}
public sealed class CatCarHorizontalPodAutoscaler : BaseKubernetesResource
{
    public CatCarHorizontalPodAutoscaler() : base("autoscaling/v2", "HorizontalPodAutoscaler")
    {
    }
    [YamlMember(Alias = "spec")]
    public CatCarHpaSpec Spec { get; set; } = new();
}

public sealed class CatCarHpaSpec
{
    [YamlMember(Alias = "minReplicas")]
    public int MinReplicas { get; set; } = 2;
    [YamlMember(Alias = "maxReplicas")]
    public int MaxReplicas { get; set; } = 10;
    [YamlMember(Alias = "scaleTargetRef")]
    public CatCarCrossVersionObjectReference ScaleTargetRef { get; set; } = new();
    [YamlMember(Alias = "metrics")]
    public List<CatCarMetricSpec> Metrics { get; set; } = [];
}

public sealed class CatCarCrossVersionObjectReference
{
    [YamlMember(Alias = "apiVersion")]
    public string ApiVersion { get; set; } = "apps/v1";
    [YamlMember(Alias = "kind")]
    public string Kind { get; set; } = "Deployment";
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "api-deployment";
}

public sealed class CatCarMetricSpec
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "Resource";
    [YamlMember(Alias = "resource")]
    public CatCarResourceMetricSource Resource { get; set; } = new();
}

public sealed class CatCarResourceMetricSource
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "cpu";
    [YamlMember(Alias = "target")]
    public CatCarMetricTarget Target { get; set; } = new();
}

public sealed class CatCarMetricTarget
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "Utilization";
    [YamlMember(Alias = "averageUtilization")]
    public int AverageUtilization { get; set; } = 70;
}
