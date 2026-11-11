using FluentValidation;
using TiendaApi.Api.Dtos.Proveedores;

namespace TiendaApi.Api.Validators.Proveedores;

/// <summary>
/// Validador FluentValidation para ProveedorRequestDto.
///
/// El código se valida SOLO por formato: si llega, debe ser un identificador de 11
/// caracteres Base64url (http 400 si no lo es). Que el código exista ya en base de datos
/// es una comprobación distinta que hace el servicio al guardar (http 409).
///
/// Reglas: Nombre(3-100, obligatorio), Descripcion(max 500), Contacto(max 200),
/// Codigo(opcional, formato de 11 caracteres).
/// </summary>
public class ProveedorRequestValidator : AbstractValidator<ProveedorRequestDto> {
    /// <summary>
    /// Define reglas de validación para ProveedorRequestDto.
    /// </summary>
    public ProveedorRequestValidator() {
        // El código solo se valida por FORMATO, y con tres estados bien distintos:
        //   null → se omite y el servidor genera uno
        //   ""   → sin sentido en el DTO: se rechaza (no es "omitir")
        //   valor → debe ser un ID de 11 caracteres Base64url
        // Esta regla protege también los caminos que no pasan por el model binding
        // HTTP, como el import de backups.
        // Regla 1: el vacío no es "omitir el campo".
        RuleFor(p => p.Codigo)
            .Must(codigo => codigo is null || codigo.Length > 0)
            .WithMessage("El código no puede estar vacío: omítalo y el servidor generará uno");

        // Regla 2: si viene, debe tener el formato. Va en un RuleFor aparte porque
        // .When() condiciona TODA la cadena donde se encadena, no solo la última regla.
        RuleFor(p => p.Codigo)
            .EsIdentificadorYouTube()
            .When(p => p.Codigo is not null && p.Codigo.Length > 0);

        RuleFor(p => p.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(p => p.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(p => p.Contacto)
            .MaximumLength(200).WithMessage("El contacto no puede exceder 200 caracteres");
    }
}
