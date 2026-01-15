using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Clase estática que proporciona métodos de extensión para el mapeo entre entidades de dominio y DTOs (Data Transfer Objects)
/// del modelo de Usuario.
///
/// <para><b>Patrón Mapper:</b></para>
/// Este mapper implementa el patrón de diseño "Mapper" o "Data Mapper", cuyo propósito es transferir datos entre
/// objetos en memoria y una base de datos, aislando la capa de dominio de los detalles de representación de datos.
///
/// <para><b>Por qué no se usa AutoMapper (con fines educativos):</b></para>
/// <list type="number">
///   <item>
///     <term>Comprensión profunda del mapeo</term>
///     <description>Al escribir los mapeos manualmente, los desarrolladores entienden exactamente cómo se transforman
///     los datos, qué campos se mapean y cuáles se ignoran. Esto es crucial para el aprendizaje.</description>
///   </item>
///   <item>
///     <term>Control total sobre la transformación</term>
///     <description>AutoMapper puede ocultar lógica de negocio importante. Al escribir mapeos explícitos, se hace
///     visible qué transformaciones se realizan (conversiones de tipos, formateo de fechas, cálculos).</description>
///   </item>
///   <item>
///     <term>性能 (Rendimiento)</term>
///     <description>AutoMapper usa reflexión y generación dinámica de IL, lo cual tiene overhead.
///     Los mapeos manuales son más eficientes en escenarios de alto rendimiento.</description>
///   </item>
///   <item>
///     <term>Flexibilidad para casos complejos</term>
///     <description>Cuando los mapeos no son simples copias de propiedades (flattening, renaming, condicionales,
///    计算 de campos derivados), AutoMapper puede resultar limitante o confuso.</description>
///   </item>
///   <item>
///     <term>Menor acoplamiento</term>
///     <description>No depender de una librería externa reduce las dependencias del proyecto y facilita el mantenimiento
///     a largo plazo.</description>
///   </item>
///   <item>
///     <term>Facilita las pruebas</term>
///     <description>Al ser métodos simples y explícitos, son más fáciles de probar y depurar.</description>
///   </item>
/// </list>
///
/// <para><b>Casos de uso apropiados para AutoMapper:</b></para>
/// En proyectos grandes con muchos mapeos simples y boilerplate repetitivo, AutoMapper puede acelerar el desarrollo.
/// Sin embargo, en esta API académica, se prioriza el aprendizaje de los fundamentos.
///
/// <para><b>Características especiales del UserMapper:</b></para>
/// Este mapper maneja información sensible como contraseñas (hasheadas antes del mapeo),
/// roles de usuario yavatars. Incluye dos variantes de UpdateEntity para PUT (actualización completa)
/// y PATCH (actualización parcial), permitiendo diferentes estrategias de actualización.
///
/// <para><b>Ejemplo de uso general:</b></para>
/// <code>
/// // Convertir entidad a DTO para respuesta API (excluye datos sensibles)
/// var userDto = user.ToDto();
/// 
/// // Convertir lista de entidades a lista de DTOs
/// var usersDto = users.ToDtoList();
/// 
/// // Crear usuario desde DTO de registro (recibe password ya hasheada)
/// var user = dto.ToEntity(passwordHash);
/// 
/// // Actualizar usuario completo (PUT)
/// dto.UpdateEntity(user);
/// 
/// // Actualizar usuario parcialmente (PATCH)
/// patchDto.UpdateEntity(user);
/// </code>
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Convierte una entidad de dominio <see cref="User"/> a un DTO de respuesta <see cref="UserDto"/>
    /// para ser retornado en las respuestas de la API.
    /// </summary>
    /// <param name="user">La entidad de usuario a convertir.</param>
    /// <returns>Un nuevo objeto <see cref="UserDto"/> con los datos públicos del usuario.</returns>
    /// <remarks>
    /// Este método excluye información sensible como el hash de la contraseña.
    /// Utiliza el método de extensión GetAvatarUrl() del modelo User para construir la URL completa del avatar.
    /// El rol se incluye para que el cliente pueda aplicar lógica de autorización en el frontend.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint GET para obtener perfil del usuario actual
    /// [HttpGet("me")]
    /// public ActionResult&lt;UserDto&gt; GetPerfil()
    /// {
    ///     var user = GetCurrentUser();
    ///     return Ok(user.ToDto());
    /// }
    /// 
    /// // En un endpoint GET para obtener usuario por ID (admin)
    /// [HttpGet("{id}")]
    /// [Authorize(Roles = "Admin")]
    /// public ActionResult&lt;UserDto&gt; GetUsuario(long id)
    /// {
    ///     var user = _repo.GetById(id);
    ///     if (user == null) return NotFound();
    ///     return Ok(user.ToDto());
    /// }
    /// </code>
    /// </example>
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.GetAvatarUrl(),
            user.Role,
            user.CreatedAt
        );
    }

    /// <summary>
    /// Convierte una colección de entidades de dominio <see cref="User"/> a una colección de DTOs
    /// <see cref="UserDto"/> para ser retornados en las respuestas de la API.
    /// </summary>
    /// <param name="users">La colección de entidades de usuario a convertir.</param>
    /// <returns>Una colección enumerable de objetos <see cref="UserDto"/>.</returns>
    /// <remarks>
    /// Utiliza LINQ Select internamente para transformar cada elemento.
    /// Devuelve un IEnumerable&lt;UserDto&gt; que se evalúa de forma diferida (lazy evaluation).
    /// Útil para endpoints de administración que listan usuarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint GET para listar todos los usuarios (admin)
    /// [HttpGet]
    /// [Authorize(Roles = "Admin")]
    /// public ActionResult&lt;IEnumerable&lt;UserDto&gt;&gt; GetUsuarios()
    /// {
    ///     var usuarios = _repo.GetAll();
    ///     return Ok(usuarios.ToDtoList());
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<UserDto> ToDtoList(this IEnumerable<User> users)
    {
        return users.Select(u => u.ToDto());
    }

    /// <summary>
    /// Convierte un DTO de registro <see cref="RegisterDto"/> a una entidad de dominio <see cref="User"/>
    /// para ser persistida en la base de datos.
    /// </summary>
    /// <param name="dto">El DTO de registro que contiene los datos proporcionados por el nuevo usuario.</param>
    /// <param name="passwordHash">El hash de la contraseña ya procesada (no almacenar contraseña en texto plano).</param>
    /// <returns>Una nueva entidad <see cref="User"/> con los datos del registro.</returns>
    /// <remarks>
    /// La contraseña NO debe pasarse en texto plano. Debe hashearse antes de llamar este método
    /// usando BCrypt.Net.BCrypt.HashPassword con un work factor apropiado (11-12).
    /// Inicializa automáticamente el rol como USER (no admin).
    /// Establece IsDeleted en false para usuarios activos.
    /// Inicializa las propiedades de auditoría CreatedAt y UpdatedAt con la fecha UTC actual.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint POST de registro
    /// [HttpPost("register")]
    /// public ActionResult&lt;UserDto&gt; Register([FromBody] RegisterDto dto)
    /// {
    ///     // Validar que el email y username no existan
    ///     if (_repo.ExistsByEmail(dto.Email))
    ///         return BadRequest("El email ya está registrado");
    ///     
    ///     // Hashear la contraseña (NUNCA guardar en texto plano)
    ///     var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
    ///     
    ///     // Crear la entidad usuario
    ///     var user = dto.ToEntity(passwordHash);
    ///     
    ///     _repo.Add(user);
    ///     _repo.SaveChanges();
    ///     
    ///     return CreatedAtAction(nameof(GetPerfil), user.ToDto());
    /// }
    /// </code>
    /// </example>
    public static User ToEntity(this RegisterDto dto, string passwordHash)
    {
        return new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Actualiza una entidad de dominio <see cref="User"/> existente con los datos de un DTO de actualización
    /// <see cref="UserUpdateDto"/>. Este método modifica directamente el objeto proporcionado.
    /// </summary>
    /// <param name="dto">El DTO de actualización que contiene los nuevos datos del usuario.</param>
    /// <param name="user">La entidad de usuario existente a actualizar.</param>
    /// <remarks>
    /// Este método implementa una actualización "parcial inteligente": solo actualiza campos no vacíos.
    /// La contraseña se hashea antes de guardarla usando BCrypt con work factor 11.
    /// Este método está diseñado para operaciones PUT donde se espera actualización completa
    /// pero con validaciones de campos opcionales.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint PUT para actualizar perfil completo
    /// [HttpPut("profile")]
    /// public ActionResult&lt;UserDto&gt; UpdatePerfil([FromBody] UserUpdateDto dto)
    /// {
    ///     var user = GetCurrentUser();
    ///     
    ///     // Actualizar solo si se proporcionan nuevos valores
    ///     dto.UpdateEntity(user);
    ///     
    ///     _repo.Update(user);
    ///     _repo.SaveChanges();
    ///     
    ///     return Ok(user.ToDto());
    /// }
    /// </code>
    /// </example>
    public static void UpdateEntity(this UserUpdateDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
    }

    /// <summary>
    /// Actualiza una entidad de dominio <see cref="User"/> existente con los datos de un DTO de actualización
    /// parcial (PATCH) <see cref="UserPatchDto"/>. Este método modifica directamente el objeto proporcionado.
    /// </summary>
    /// <param name="dto">El DTO de actualización parcial que contiene los campos a modificar.</param>
    /// <param name="user">La entidad de usuario existente a actualizar.</param>
    /// <remarks>
    /// Este método está diseñado específicamente para operaciones PATCH donde solo algunos campos
    /// pueden ser actualizados. Además de email y contraseña, permite actualizar el avatar.
    /// Si el avatar es null, no se modifica (comportamiento esperado para PATCH).
    /// La contraseña se hashea antes de guardarla usando BCrypt con work factor 11.
    /// </remarks>
    /// <example>
    /// <code>
    /// // En un endpoint PATCH para actualización parcial del perfil
    /// [HttpPatch("profile")]
    /// public ActionResult&lt;UserDto&gt; PatchPerfil([FromForm] UserPatchDto dto)
    /// {
    ///     var user = GetCurrentUser();
    ///     
    ///     // Actualizar solo los campos proporcionados
    ///     dto.UpdateEntity(user);
    ///     
    ///     _repo.Update(user);
    ///     _repo.SaveChanges();
    ///     
    ///     return Ok(user.ToDto());
    /// }
    /// 
    /// // Ejemplo de llamada con curl para PATCH:
    /// // curl -X PATCH -H "Authorization: Bearer token" \
    /// //      -F "avatar=@avatar.png" https://api.com/profile
    /// </code>
    /// </example>
    public static void UpdateEntity(this UserPatchDto dto, User user)
    {
        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        if (dto.Avatar != null)
            user.Avatar = dto.Avatar;
    }
}
