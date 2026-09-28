# ADR-0006: Validación en cascada con DataAnnotations y FluentValidation

|               |                                                                     |
| ------------- | ------------------------------------------------------------------- |
| **ADR**       | `ADR-0006`                                                          |
| **Título**    | Validación en cascada (DTO + servicios)                             |
| **Estado**    | Aceptado                                                            |
| **Fecha**     | 2026-01                                                             |
| **Decisores** | Equipo docente                                                       |

## Contexto

Toda petición que llega con datos del cliente es sospechosa: precios
negativos, emails mal formados, nombres duplicados. La validación debe
ocurrir siempre, sea cual sea el cliente (web, móvil, scripts).

## Decisión

- **`[ApiController]`** en todos los controladores: el `ModelState` se
  valida automáticamente y devuelve **400** con los errores de campo antes de
  ejecutar la acción.
- **DataAnnotations** en los DTOs para reglas simples y autoevidentes
  (`[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`).
- **FluentValidation** en `Validators/` para reglas de negocio que necesitan
  consultar datos o componerse (nombre duplicado, categoría existente...).
- La validación de negocio posterior se expresa con `Result` (ver
  `ADR-0001`), no con excepciones.

## Consecuencias

### Positivas

- Tres anillos de defensa: modelo → validador → reglas de dominio.
- Los errores de validación salen en el mismo formato en todos los endpoints.
- El cliente recibe mensajes por campo, útiles para formularios.

### Negativas o riesgos

- Reglas escritas dos veces si se mezclan DataAnnotations y FluentValidation
  sin criterio claro.
- Validar también en el cliente obliga a reflejar estas reglas fuera de .NET.

## Alternativas descartadas

| Alternativa                         | Por qué se descartó                                   |
| ----------------------------------- | ----------------------------------------------------- |
| Solo validación en el cliente       | Cualquier script puede saltársela.                    |
| Solo FluentValidation              | Añade dependencia y ceremony a reglas triviales.      |
| Validación imperativa en servicios  | Se olvida fácilmente; errores inconsistentes.         |

## Verificación

- E2E de errores de validación (400 con cuerpo de detalle).
- Tests unitarios de los validadores de FluentValidation.

## Referencias en el código y en la documentación

- `Validators/` — reglas de FluentValidation.
- `Dtos/` — DTOs con DataAnnotations.
- `doc/05-validacion-cascada.md` — explicación didáctica de la cascada.
