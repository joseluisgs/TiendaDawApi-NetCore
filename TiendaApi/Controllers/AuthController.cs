using Microsoft.AspNetCore.Mvc;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services.Auth;
using BCrypt.Net;

namespace TiendaApi.Controllers;

/// <summary>
/// Authentication controller for user signup and signin
/// Java Spring Security equivalent: AuthenticationController
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// POST /v1/auth/signup
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto)
    {
        // Sanitize username for logging to prevent log forging
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("Signup request for username: {Username}", sanitizedUsername);

        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
        {
            return BadRequest(new { message = "Username must be at least 3 characters" });
        }

        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
        {
            return BadRequest(new { message = "Valid email is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters" });
        }

        // Check if username already exists
        var existingUser = await _userRepository.FindByUsernameAsync(dto.Username);
        if (existingUser != null)
        {
            return Conflict(new { message = "Username already exists" });
        }

        // Check if email already exists
        var existingEmail = await _userRepository.FindByEmailAsync(dto.Email);
        if (existingEmail != null)
        {
            return Conflict(new { message = "Email already exists" });
        }

        // Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        // Create new user
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = UserRoles.USER, // Default role
            IsDeleted = false
        };

        var savedUser = await _userRepository.SaveAsync(user);

        // Generate JWT token
        var token = _jwtService.GenerateToken(savedUser);

        var userDto = new UserDto
        {
            Id = savedUser.Id,
            Username = savedUser.Username,
            Email = savedUser.Email,
            Role = savedUser.Role,
            CreatedAt = savedUser.CreatedAt
        };

        var response = new AuthResponseDto
        {
            Token = token,
            User = userDto
        };

        _logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return CreatedAtAction(nameof(SignUp), response);
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// POST /v1/auth/signin
    /// </summary>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        // Sanitize username for logging to prevent log forging
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("Signin request for username: {Username}", sanitizedUsername);

        // Find user by username
        var user = await _userRepository.FindByUsernameAsync(dto.Username);
        if (user == null)
        {
            _logger.LogWarning("Signin failed: User not found - {Username}", sanitizedUsername);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Verify password
        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            _logger.LogWarning("Signin failed: Invalid password - {Username}", sanitizedUsername);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

        var response = new AuthResponseDto
        {
            Token = token,
            User = userDto
        };

        _logger.LogInformation("User signed in successfully: {Username}", sanitizedUsername);

        return Ok(response);
    }
}
