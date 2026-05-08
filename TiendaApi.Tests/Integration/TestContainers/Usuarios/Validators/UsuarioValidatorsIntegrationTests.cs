using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Validators;

/// <summary>
/// Tests de integración para Validators de Usuarios.
/// Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
[Category("Integration")]
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

    #region AvatarUpdateValidator Tests

    [Test]
    public async Task AvatarUpdateValidator_ConUrlHttpsValida_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlHttpValida_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "http://example.com/avatar.jpg" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlVacia_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlSinProtocolo_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "www.example.com/avatar.jpg" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlFtp_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "ftp://ftp.example.com/avatar.jpg" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlExcede500Caracteres_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var urlLarga = new string('a', 501);
        var dto = new AvatarUpdateDto { AvatarUrl = $"https://example.com/{urlLarga}.jpg" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    #endregion

    #region UserUpdateValidator Tests

    [Test]
    public async Task UserUpdateValidator_ConEmailValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Email = "nuevo@email.com" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConEmailInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Email = "email-invalido" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConPasswordValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Password = "NuevaPass123" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConPasswordCorto_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Password = "123" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_SinCampos_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto();
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    #endregion
}
