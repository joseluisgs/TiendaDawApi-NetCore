using Microsoft.AspNetCore.Mvc;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Services.Auth;

namespace TiendaApi.Controllers;

/*
 * CONTROLADOR LIMPIO - SIN LÓGICA DE NEGOCIO
 * 
 * Este controller solo se encarga de:
 * ✅ Recibir las peticiones HTTP
 * ✅ Delegar toda la lógica de negocio al AuthService
 * ✅ Convertir Result<T, AppError> a respuestas HTTP usando Match()
 * 
 * ❌ NO tiene validaciones (están en AuthService)
 * ❌ NO tiene lógica de BCrypt (está en AuthService)
 * ❌ NO accede a repositorios directamente (usa AuthService)
 * ❌ NO tiene verificación de duplicados (está en AuthService)
 * 
 * COMPARACIÓN CON JAVA/SPRING BOOT:
 * - Spring: Controller → Service → Repository
 * - Este patrón: Controller → Service → Repository (igual)
 * - Diferencia: Usamos Result Pattern en lugar de excepciones
 * - Spring: @ExceptionHandler maneja excepciones
 * - Aquí: Match() convierte Result a HTTP status codes
 * 
 * VENTAJAS:
 * - Controller delgado y fácil de mantener
 * - Lógica de negocio reutilizable (AuthService puede usarse desde GraphQL, gRPC, etc.)
 * - Testeo más fácil (solo testeamos conversión HTTP)
 * - Separación de responsabilidades clara
 */

/// <summary>
/// Authentication controller for user signup and signin
/// Java Spring Security equivalent: AuthenticationController
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// POST /v1/auth/signup
    /// 
    /// Railway Oriented Programming:
    /// 1. Llama al servicio (una sola línea)
    /// 2. El servicio retorna Result<AuthResponseDto, AppError>
    /// 3. Usa Match() para convertir a HTTP response
    /// 4. Switch expression mapea ErrorType a HTTP status codes
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto)
    {
        var resultado = await _authService.SignUpAsync(dto);
        
        return resultado.Match(
            onSuccess: response => CreatedAtAction(nameof(SignUp), response),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// POST /v1/auth/signin
    /// 
    /// Railway Oriented Programming:
    /// 1. Llama al servicio (una sola línea)
    /// 2. El servicio retorna Result<AuthResponseDto, AppError>
    /// 3. Usa Match() para convertir a HTTP response
    /// </summary>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        var resultado = await _authService.SignInAsync(dto);
        
        return resultado.Match(
            onSuccess: response => Ok(response),
            onFailure: error => error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}
