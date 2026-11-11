using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Validators;

/// <summary>
///     Tests de integración para Validators de Usuarios.
///     Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
[Category("Integration")]
public class UsuarioValidatorsIntegrationTests {
    [Test]
    public async Task RegisterValidator_ConDtoValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConEmailInvalido_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "juanperez",
            Email = "email-invalido",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConPasswordCorto_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConUsernameVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "",
            Email = "juan@test.com",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConEmailVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "juanperez",
            Email = "",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConPasswordVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<RegisterDto> validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = ""
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConDtoValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<LoginDto> validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto {
            Username = "juanperez",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConUsernameVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<LoginDto> validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto {
            Username = "",
            Password = "Password123"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginValidator_ConPasswordVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<LoginDto> validator = provider.GetRequiredService<IValidator<LoginDto>>();

        var dto = new LoginDto {
            Username = "juanperez",
            Password = ""
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserFilterDto_DefaultValues_AreCorrect() {
        var filter = new UserFilterDto(null, null, null);

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlHttpsValida_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlHttpValida_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "http://example.com/avatar.jpg" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlVacia_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlSinProtocolo_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "www.example.com/avatar.jpg" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlFtp_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        var dto = new AvatarUpdateDto { AvatarUrl = "ftp://ftp.example.com/avatar.jpg" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AvatarUpdateValidator_ConUrlExcede500Caracteres_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<AvatarUpdateDto>, AvatarUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<AvatarUpdateDto> validator = provider.GetRequiredService<IValidator<AvatarUpdateDto>>();

        string urlLarga = new('a', 501);
        var dto = new AvatarUpdateDto { AvatarUrl = $"https://example.com/{urlLarga}.jpg" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConEmailValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<UserUpdateDto> validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Email = "nuevo@email.com" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConEmailInvalido_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<UserUpdateDto> validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Email = "email-invalido" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConPasswordValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<UserUpdateDto> validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Password = "NuevaPass123" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_ConPasswordCorto_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<UserUpdateDto> validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto { Password = "123" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserUpdateValidator_SinCampos_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<UserUpdateDto> validator = provider.GetRequiredService<IValidator<UserUpdateDto>>();

        var dto = new UserUpdateDto();
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }
}
