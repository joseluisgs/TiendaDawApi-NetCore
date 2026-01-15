using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Validators.Usuarios;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Validators;

/// <summary>
/// Tests de integración para Validators de Usuarios.
/// Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
public class UsuarioValidatorsIntegrationTests
{
    [Test]
    public async Task RegisterValidator_ConDtoValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConEmailInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "email-invalido",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConPasswordCorto_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConUsernameVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "",
            Email = "juan@test.com",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConEmailVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConPasswordVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = ""
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConDtoValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto
        {
            Username = "juanperez",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConUsernameVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto
        {
            Username = "",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConPasswordVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto
        {
            Username = "juanperez",
            Password = ""
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }
}
