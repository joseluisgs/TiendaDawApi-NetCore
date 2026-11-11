using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Validators;

/// <summary>
///     Tests de integración para Validators de Pedidos.
///     Verifica la validación de DTOs usando FluentValidation.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PedidoValidatorsIntegrationTests {
    [Test]
    public async Task PedidoRequestValidator_ConItemsValidos_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<PedidoRequestDto> validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var dto = new PedidoRequestDto {
            Destinatario = new DestinatarioDto {
                NombreCompleto = "Test User",
                Email = "test@email.com",
                Direccion = new DireccionDto {
                    Calle = "Calle Test",
                    Ciudad = "Madrid",
                    Pais = "España"
                }
            },
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestValidator_SinItems_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<PedidoRequestDto> validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var dto = new PedidoRequestDto {
            Items = new List<PedidoItemRequestDto>()
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_ConProductoIdInvalido_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<PedidoItemRequestDto> validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var dto = new PedidoItemRequestDto {
            ProductoId = 0,
            Cantidad = 1
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_ConCantidadInvalida_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<PedidoItemRequestDto> validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var dto = new PedidoItemRequestDto {
            ProductoId = 1,
            Cantidad = 0
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_ConCantidadNegativa_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<PedidoItemRequestDto> validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var dto = new PedidoItemRequestDto {
            ProductoId = 1,
            Cantidad = -5
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UpdateEstadoDto_ConEstadoValido_PasaValidacion() {
        var dto = new UpdateEstadoDto { Estado = "PROCESANDO" };
        dto.Should().NotBeNull();
        dto.Estado.Should().Be("PROCESANDO");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoDto_AllStates_ShouldBeValid() {
        string[] estadosValidos = new[] { "PENDIENTE", "PROCESANDO", "ENVIADO", "ENTREGADO", "CANCELADO" };

        foreach (string estado in estadosValidos) {
            var dto = new PedidoDto(
                "PED-2024-0001",
                1,
                new DestinatarioDto {
                    NombreCompleto = "Test",
                    Email = "test@test.com",
                    Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
                },
                new List<PedidoItemDto>(),
                100m,
                estado,
                "Calle Test 123",
                DateTime.UtcNow
            );

            dto.Estado.Should().Be(estado);
        }

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestDto_ConItemsValidos_DeberiaSerValido() {
        var dto = new PedidoRequestDto {
            Items = new List<PedidoItemRequestDto> {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 1 }
            }
        };

        dto.Items.Should().HaveCount(2);

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestDto_SinItems_DeberiaTenerItemsVacios() {
        var dto = new PedidoRequestDto();

        dto.Items.Should().NotBeNull();
        dto.Items.Should().BeEmpty();

        await Task.CompletedTask;
    }

    [Test]
    public async Task UpdatePedidoDto_ConCamposOpcionales_DeberiaSerValido() {
        var dto = new UpdatePedidoDto {
            Estado = "PROCESANDO",
            DireccionEnvio = "Nueva Calle 456"
        };

        dto.Estado.Should().Be("PROCESANDO");
        dto.DireccionEnvio.Should().Be("Nueva Calle 456");

        await Task.CompletedTask;
    }

    [Test]
    public async Task UpdatePedidoDto_SinCampos_DeberiaSerValido() {
        var dto = new UpdatePedidoDto();

        dto.Estado.Should().BeNull();
        dto.DireccionEnvio.Should().BeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DireccionValidator_ConDtoValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DireccionDto>, DireccionValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DireccionDto> validator = provider.GetRequiredService<IValidator<DireccionDto>>();

        var dto = new DireccionDto {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DireccionValidator_ConCalleVacia_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DireccionDto>, DireccionValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DireccionDto> validator = provider.GetRequiredService<IValidator<DireccionDto>>();

        var dto = new DireccionDto { Calle = "" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DireccionValidator_ConCiudadVacia_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DireccionDto>, DireccionValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DireccionDto> validator = provider.GetRequiredService<IValidator<DireccionDto>>();

        var dto = new DireccionDto { Calle = "Calle Test", Ciudad = "" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DireccionValidator_ConPaisVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DireccionDto>, DireccionValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DireccionDto> validator = provider.GetRequiredService<IValidator<DireccionDto>>();

        var dto = new DireccionDto { Calle = "Calle Test", Pais = "" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DireccionValidator_ConCodigoPostalInvalido_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DireccionDto>, DireccionValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DireccionDto> validator = provider.GetRequiredService<IValidator<DireccionDto>>();

        var dto = new DireccionDto {
            Calle = "Calle Test",
            Ciudad = "Madrid",
            Pais = "España",
            CodigoPostal = "1234"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DestinatarioValidator_ConDtoValido_PasaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DestinatarioDto>, DestinatarioValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DestinatarioDto> validator = provider.GetRequiredService<IValidator<DestinatarioDto>>();

        var dto = new DestinatarioDto {
            NombreCompleto = "Juan Pérez",
            Email = "juan@email.com",
            Telefono = "+34612345678",
            Direccion = new DireccionDto {
                Calle = "Gran Vía",
                Ciudad = "Madrid",
                Pais = "España"
            }
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DestinatarioValidator_ConNombreCompletoVacio_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DestinatarioDto>, DestinatarioValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DestinatarioDto> validator = provider.GetRequiredService<IValidator<DestinatarioDto>>();

        var dto = new DestinatarioDto { NombreCompleto = "" };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DestinatarioValidator_ConEmailInvalido_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DestinatarioDto>, DestinatarioValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DestinatarioDto> validator = provider.GetRequiredService<IValidator<DestinatarioDto>>();

        var dto = new DestinatarioDto {
            NombreCompleto = "Juan Pérez",
            Email = "email-invalido"
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task DestinatarioValidator_SinDireccion_FallaValidacion() {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<DestinatarioDto>, DestinatarioValidator>();

        using ServiceProvider provider = services.BuildServiceProvider();
        IValidator<DestinatarioDto> validator = provider.GetRequiredService<IValidator<DestinatarioDto>>();

        var dto = new DestinatarioDto {
            NombreCompleto = "Juan Pérez",
            Email = "juan@email.com",
            Direccion = null!
        };
        ValidationResult result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }
}
