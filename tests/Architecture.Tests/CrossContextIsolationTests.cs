namespace CatCar.Architecture.Tests;

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

// covers: CON-001 (no cross-BC imports)
public class CrossContextIsolationTests
{
    [Fact]
    public void ServiceOperations_ShouldNot_DependOn_IdentityAccess()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.ServiceOperations.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.IdentityAccess")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }


    [Fact]
    public void IdentityAccess_ShouldNot_DependOn_OtherBCs()
    {
        var result = Types.InAssembly(typeof(CatCar.Contexts.IdentityAccess.Marker).Assembly)
            .ShouldNot().HaveDependencyOn("CatCar.Contexts.ServiceOperations")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
