namespace CatCar.Architecture.Tests;

using FluentAssertions;
using Xunit;

// covers: CON-004 (BC folder structure), CON-005 (Vertical Slice structure)
public class VerticalSliceStructureTests
{
    [Theory]
    [InlineData("CatCar.Contexts.ServiceOperations")]
    [InlineData("CatCar.Contexts.CatalogInventory")]
    [InlineData("CatCar.Contexts.Communication")]
    [InlineData("CatCar.Contexts.IdentityAccess")]
    public void BoundedContext_ShouldHave_RequiredFolderStructure(string markerNamespace)
    {
        var markerType = Type.GetType($"{markerNamespace}.Marker, {markerNamespace}")!;
        var assembly = markerType.Assembly;
        var requiredRoots = new[] { "Domain", "Features", "Infrastructure" };

        foreach (var folder in requiredRoots)
        {
            var expectedNamespace = $"{markerNamespace}.{folder}";
            var exists = assembly.GetTypes().Any(t => t.Namespace == expectedNamespace ||
                                                       (t.Namespace?.StartsWith(expectedNamespace + ".", StringComparison.Ordinal) ?? false));
            exists.Should().BeTrue($"Expected {expectedNamespace} namespace to exist");
        }
    }

    [Theory]
    [InlineData("CatCar.Contexts.ServiceOperations")]
    [InlineData("CatCar.Contexts.CatalogInventory")]
    [InlineData("CatCar.Contexts.Communication")]
    [InlineData("CatCar.Contexts.IdentityAccess")]
    public void BoundedContext_Root_ShouldNot_Have_TechnicalFolders(string markerNamespace)
    {
        var markerType = Type.GetType($"{markerNamespace}.Marker, {markerNamespace}")!;
        var assembly = markerType.Assembly;
        var forbiddenRoots = new[] { "Services", "Controllers", "Repositories" };

        foreach (var folder in forbiddenRoots)
        {
            var forbiddenNamespace = $"{markerNamespace}.{folder}";
            var exists = assembly.GetTypes().Any(t => t.Namespace == forbiddenNamespace ||
                                                       (t.Namespace?.StartsWith(forbiddenNamespace + ".", StringComparison.Ordinal) ?? false));
            exists.Should().BeFalse($"Unexpected technical namespace {forbiddenNamespace} found");
        }
    }
}
