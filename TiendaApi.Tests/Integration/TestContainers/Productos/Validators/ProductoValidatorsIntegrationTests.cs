using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Validators.Productos;

namespace TiendaApi.Tests.Integration.TestContainers.Productos.Validators;

/// <summary>
/// Tests de integración para Validators de Productos.
/// Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductoValidatorsIntegrationTests
{
    [Test]
    public async Task ProductoValidator_ConDtoValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 999.99m,
            Stock = 10,
            CategoriaId = 1,
            Descripcion = "Una laptop genial"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConPrecioNegativo_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = -100m,
            Stock = 10,
            CategoriaId = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConStockNegativo_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 100m,
            Stock = -5,
            CategoriaId = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConCategoriaIdInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 100m,
            Stock = 10,
            CategoriaId = 0
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConDescripcionVacia_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Mouse",
            Precio = 29.99m,
            Stock = 50,
            CategoriaId = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new ProductoFilterDto(null, null, null, null, null);

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }
}
