namespace CatCar.Architecture.Tests;

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

// covers: CON-002 (EF Core only in Infrastructure/Integrations)
public class DependencyDirectionTests
{
    [Fact]
    // covers: B-07, CON-002
    public void BCRoot_ShouldNot_Reference_EFCore()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespaceMatching(@".*\.Contexts\.\w+$")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DomainFolders_ShouldNot_Reference_EFCore()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespaceMatching(@".*\.Contexts\.\w+\.Domain($|\..*)")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void FeaturesFolders_ShouldNot_Reference_EFCore()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespaceMatching(@".*\.Contexts\.\w+\.Features($|\..*)")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
