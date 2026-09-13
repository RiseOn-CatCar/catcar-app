namespace CatCar.Contexts.ServiceOperations.Tests.Domain;

using CatCar.Contexts.ServiceOperations.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class DocumentNumberTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void Create_WithValidCpf_ShouldSucceed(string rawValue)
    {
        var result = DocumentNumber.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(DocumentType.Cpf);
        result.Value.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    public void Create_WithValidCnpj_ShouldSucceed(string rawValue)
    {
        var result = DocumentNumber.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(DocumentType.Cnpj);
        result.Value.Value.Should().Be("11222333000181");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutValue_ShouldFail(string? rawValue)
    {
        var result = DocumentNumber.Create(rawValue);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInvalidCpfCheckDigits_ShouldFail()
    {
        var result = DocumentNumber.Create("11111111111");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInvalidCnpjCheckDigits_ShouldFail()
    {
        var result = DocumentNumber.Create("11111111111111");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithWrongDigitCount_ShouldFail()
    {
        var result = DocumentNumber.Create("12345");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToString_ForCpf_ShouldFormatWithSeparators()
    {
        var document = DocumentNumber.Create("52998224725").Value;

        document.ToString().Should().Be("529.982.247-25");
    }

    [Fact]
    public void ToString_ForCnpj_ShouldFormatWithSeparators()
    {
        var document = DocumentNumber.Create("11222333000181").Value;

        document.ToString().Should().Be("11.222.333/0001-81");
    }
}
