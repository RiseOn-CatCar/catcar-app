namespace CatCar.Architecture.Tests;

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

// covers: CON-001 (no cross-BC imports)
public class CrossContextIsolationTests
{
    [Fact]
    public void ServiceOperations_ShouldNot_DependOn_CatalogInventory()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.CatalogInventory")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ServiceOperations_ShouldNot_DependOn_Communication()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.Communication")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void ServiceOperations_ShouldNot_DependOn_IdentityAccess()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.IdentityAccess")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CatalogInventory_ShouldNot_DependOn_ServiceOperations()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.CatalogInventory.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.ServiceOperations")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CatalogInventory_ShouldNot_DependOn_Communication()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.CatalogInventory.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.Communication")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CatalogInventory_ShouldNot_DependOn_IdentityAccess()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.CatalogInventory.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.IdentityAccess")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Communication_ShouldNot_DependOn_OtherBCs()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.Communication.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.ServiceOperations")
            .And().NotHaveDependencyOn("CatCar.Contexts.CatalogInventory")
            .And().NotHaveDependencyOn("CatCar.Contexts.IdentityAccess")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void IdentityAccess_ShouldNot_DependOn_OtherBCs()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.IdentityAccess.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.ServiceOperations")
            .And().NotHaveDependencyOn("CatCar.Contexts.CatalogInventory")
            .And().NotHaveDependencyOn("CatCar.Contexts.Communication")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
