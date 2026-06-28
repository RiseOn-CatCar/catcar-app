namespace CatCar.Architecture.Tests;

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

// covers: CON-003 (SharedKernel does not reference Bounded Contexts)
public class SharedKernelPurityTests
{
    [Fact]
    public void SharedKernel_ShouldNot_DependOn_AnyBoundedContext()
    {
        var result = Types.InAssembly(typeof(CatCar.SharedKernel.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.ServiceOperations")
            .And().NotHaveDependencyOn("CatCar.Contexts.CatalogInventory")
            .And().NotHaveDependencyOn("CatCar.Contexts.Communication")
            .And().NotHaveDependencyOn("CatCar.Contexts.IdentityAccess")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
