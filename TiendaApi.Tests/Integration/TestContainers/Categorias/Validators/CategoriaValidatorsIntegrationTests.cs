using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Validators.Categorias;

namespace TiendaApi.Tests.Integration.TestContainers.Categorias.Validators;

/// <summary>
/// Tests de integración para Validators de Categorías.
/// Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CategoriaValidatorsIntegrationTests
{
    [Test]
    public async Task CategoriaValidator_ConNombreValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreCorto_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "AB" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreLargo_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = new string('A', 50) };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new CategoriaFilterDto();

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }
}
