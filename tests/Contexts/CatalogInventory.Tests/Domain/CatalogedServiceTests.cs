namespace CatCar.Contexts.CatalogInventory.Tests.Domain;

using CatCar.Contexts.CatalogInventory.Domain.CatalogedServices;
using CatCar.SharedKernel;
using FluentAssertions;
using Xunit;

public class CatalogedServiceTests
{
    [Fact]
    public void Register_WithValidData_ShouldSucceedAndBeActive()
    {
        var result = CatalogedService.Register("Troca de óleo", "Troca de óleo do motor e filtro", 60, 150m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Troca de óleo");
        result.Value.Description.Should().Be("Troca de óleo do motor e filtro");
        result.Value.EstimatedDurationMinutes.Should().Be(60);
        result.Value.Price.Amount.Should().Be(150m);
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutName_ShouldFail(string? name)
    {
        var result = CatalogedService.Register(name, "Descrição válida", 30, 100m);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Register_WithoutDescription_ShouldFail(string? description)
    {
        var result = CatalogedService.Register("Alinhamento", description, 30, 100m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithZeroDuration_ShouldFail()
    {
        var result = CatalogedService.Register("Alinhamento", "Alinhamento e balanceamento", 0, 100m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_WithNegativePrice_ShouldFail()
    {
        var result = CatalogedService.Register("Alinhamento", "Alinhamento e balanceamento", 30, -1m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateFields()
    {
        var service = CatalogedService.Register("Alinhamento", "Alinhamento simples", 30, 80m).Value;

        var result = service.UpdateDetails("Alinhamento 3D", "Alinhamento 3D computadorizado", 45, 120m);

        result.IsSuccess.Should().BeTrue();
        service.Name.Should().Be("Alinhamento 3D");
        service.Description.Should().Be("Alinhamento 3D computadorizado");
        service.EstimatedDurationMinutes.Should().Be(45);
        service.Price.Amount.Should().Be(120m);
    }

    [Fact]
    public void UpdateDetails_WithInvalidData_ShouldFailAndKeepOriginalState()
    {
        var service = CatalogedService.Register("Alinhamento", "Alinhamento simples", 30, 80m).Value;

        var result = service.UpdateDetails(string.Empty, "Nova descrição", 45, 120m);

        result.IsFailure.Should().BeTrue();
        service.Name.Should().Be("Alinhamento");
        service.Price.Amount.Should().Be(80m);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldBecomeInactive()
    {
        var service = CatalogedService.Register("Troca de óleo", "Descrição", 60, 150m).Value;

        service.Deactivate();

        service.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrow()
    {
        var service = CatalogedService.Register("Troca de óleo", "Descrição", 60, 150m).Value;
        service.Deactivate();

        var act = () => service.Deactivate();

        act.Should().Throw<BusinessRuleViolatedException>();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        var service = CatalogedService.Register("Troca de óleo", "Descrição", 60, 150m).Value;

        var act = () => service.Activate();

        act.Should().Throw<BusinessRuleViolatedException>();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldBecomeActive()
    {
        var service = CatalogedService.Register("Troca de óleo", "Descrição", 60, 150m).Value;
        service.Deactivate();

        service.Activate();

        service.IsActive.Should().BeTrue();
    }
}
