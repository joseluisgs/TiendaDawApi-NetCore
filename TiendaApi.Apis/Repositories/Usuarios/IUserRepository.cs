using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Usuarios;

/// <summary>
/// Define el contrato para el repositorio de usuarios.
/// 
/// El repositorio de usuarios implementa el patrón Repository para gestionar
/// la entidad User, encapsulando todas las operaciones de acceso a datos
/// relacionadas con la autenticación y gestión de usuarios del sistema.
/// 
/// Características específicas del repositorio de usuarios:
/// 
/// 1. **Búsquedas alternativas**: Proporciona múltiples formas de encontrar usuarios
///    (por ID, nombre de usuario, correo electrónico) para soportar diferentes
///    flujos de autenticación.
/// 
/// 2. **Consultas para autenticación**: Los métodos FindByUsernameAsync y
///    FindByEmailAsync están optimizados para los flujos de login.
/// 
/// 3. **Gestión de credenciales**: El repositorio no maneja directamente el
///    hashing de contraseñas, pero proporciona acceso a los datos necesarios
///    para validación en el servicio de autenticación.
/// 
/// 4. **Auditoría**: Los métodos de escritura (Save, Update, Delete) facilitan
///    el registro de cambios para auditoría de seguridad.
/// 
/// Consideraciones de seguridad:
/// - Las contraseñas nunca deben almacenarse en texto plano.
/// - Los métodos de búsqueda no revelan si un usuario existe o no en flujos de login
///   para evitar enumeración de usuarios.
/// - Considere usar consultas que no distingan mayúsculas/minúsculas para usernames/emails.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario por su identificador único.
    /// 
    /// <remarks>
    /// Este método recupera un usuario específico usando su clave primaria.
    /// Es el método más directo para obtener datos de un usuario conocido.
    /// 
    /// Útil para:
    /// - Obtener datos del usuario autenticado.
    /// - Cargar usuario para edición de perfil.
    /// - Validar permisos y autorizaciones.
    /// 
    /// No use este método para autenticación; use FindByUsernameAsync o FindByEmailAsync.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Obtener perfil de usuario
    /// var usuario = await _userRepository.FindByIdAsync(userId);
    /// if (usuario == null)
    /// {
    ///     return NotFound();
    /// }
    /// return View(usuario);
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador único del usuario.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByIdAsync(long id);

    /// <summary>
    /// Busca un usuario por su nombre de usuario (username).
    /// 
    /// <remarks>
    /// Este método es fundamental para el flujo de autenticación tradicional
    /// por nombre de usuario y contraseña.
    /// 
    /// Consideraciones de seguridad:
    /// - La comparación debe ser case-insensitive (insensible a mayúsculas/minúsculas).
    /// - No revele si el usuario existe o no en mensajes de error públicos.
    /// - Considere implementar rate limiting para prevenir ataques de fuerza bruta.
    /// 
    /// Rendimiento:
    /// - El campo username debe tener un índice único para consultas eficientes.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Flujo de autenticación
    /// var usuario = await _userRepository.FindByUsernameAsync(username);
    /// if (usuario == null)
    /// {
    ///     // Mensaje genérico para no revelar información
    ///     throw new AuthenticationException("Credenciales inválidas");
    /// }
    /// 
    /// if (!VerifyPassword(usuario.PasswordHash, password))
    /// {
    ///     throw new AuthenticationException("Credenciales inválidas");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="username">Nombre de usuario a buscar.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByUsernameAsync(string username);

    /// <summary>
    /// Busca un usuario por su correo electrónico.
    /// 
    /// <remarks>
    /// Utilizado para autenticación por email y funcionalidades de recuperación
    /// de contraseña.
    /// 
    /// La búsqueda debe ser:
    /// - Case-insensitive para la parte local del email.
    /// - Case-sensitive para el dominio (según RFC 5321), pero típicamente
    ///   se trata como case-insensitive para mejor experiencia de usuario.
    /// 
    /// Útil para:
    /// - Login con email.
    /// - Envío de emails de recuperación de contraseña.
    /// - Verificación de email único al registrarse.
    /// - Búsqueda de usuario para soporte técnico.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Verificar si email ya está registrado
    /// var existeEmail = await _userRepository.FindByEmailAsync(email) != null;
    /// if (existeEmail)
    /// {
    ///     return BadRequest("El email ya está registrado");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> FindByEmailAsync(string email);

    /// <summary>
    /// Recupera todos los usuarios del sistema.
    /// 
    /// <remarks>
    /// Este método carga todos los usuarios en memoria. Úselo con precaución
    /// en sistemas con muchos usuarios.
    /// 
    /// Casos de uso apropiados:
    /// - Paneles de administración con pocos usuarios.
    /// - Exportaciones completas.
    /// - Sincronización con sistemas externos.
    /// 
    /// Para paneles de administración con muchos usuarios,
    /// use <see cref="FindAllPagedAsync"/> en su lugar.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Panel de administración pequeño
    /// var usuarios = await _userRepository.FindAllAsync();
    /// foreach (var usuario in usuarios)
    /// {
    ///     Console.WriteLine($"{usuario.Nombre} ({usuario.Email})");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <returns>Colección de todos los usuarios.</returns>
    Task<IEnumerable<User>> FindAllAsync();

    /// <summary>
    /// Recupera usuarios de forma paginada con filtros.
    /// 
    /// <remarks>
    /// Este método es esencial para paneles de administración que gestionan
    /// grandes cantidades de usuarios. El filtro UserFilterDto permite:
    /// 
    /// - Búsqueda por nombre, email o username.
    /// - Filtrado por rol o estado (activo/inactivo).
    /// - Filtrado por fecha de registro.
    /// - Ordenación por diferentes campos.
    /// 
    /// El total de registros permite calcular el número de páginas
    /// para la UI de paginación.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Panel de administración con paginación
    /// var filter = new UserFilterDto
    /// {
    ///     Page = pageNumber,
    ///     Size = pageSize,
    ///     Search = searchTerm,
    ///     Role = "Admin",
    ///     Activo = true
    /// };
    /// 
    /// var (usuarios, total) = await _userRepository.FindAllPagedAsync(filter);
    /// var totalPages = (int)Math.Ceiling(total / (double)pageSize);
    /// </code>
    /// </example>
    /// 
    /// <param name="filter">Objeto con criterios de filtrado, paginación y ordenación.</param>
    /// <returns>Tupla con usuarios de la página y total de registros.</returns>
    Task<(IEnumerable<User> Items, int TotalCount)> FindAllPagedAsync(UserFilterDto filter);

    /// <summary>
    /// Persiste un nuevo usuario en la base de datos.
    /// 
    /// <remarks>
    /// Crea un nuevo registro de usuario. El objeto retornado contendrá
    /// el ID asignado y cualquier valor generado por la base de datos.
    /// 
    /// Antes de guardar, debe:
    /// - Hashear la contraseña (no guardar en texto plano).
    /// - Validar formato de email.
    /// - Verificar username y email únicos.
    /// - Asignar rol por defecto si no se especifica.
    /// 
    /// El servicio de usuario típicamente maneja la validación y hashing
    /// antes de llamar a este método.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Registro de nuevo usuario
    /// var nuevoUsuario = new User
    /// {
    ///     Username = "juan.perez",
    ///     Email = "juan@example.com",
    ///     PasswordHash = HashPassword(password),
    ///     Nombre = "Juan Pérez",
    ///     Rol = "Usuario"
    /// };
    /// 
    /// var usuarioGuardado = await _userRepository.SaveAsync(nuevoUsuario);
    /// </code>
    /// </example>
    /// 
    /// <param name="user">Usuario a persistir. Username y email deben ser únicos.</param>
    /// <returns>El usuario guardado con datos actualizados.</returns>
    Task<User> SaveAsync(User user);

    /// <summary>
    /// Actualiza un usuario existente.
    /// 
    /// <remarks>
    /// Actualiza los datos de un usuario ya registrado.
    /// 
    /// Campos actualizables comunes:
    /// - Información de perfil (nombre, apellido, etc.).
    /// - Configuraciones y preferencias.
    /// 
    /// Campos que requieren manejo especial:
    /// - Password: requiere hashing y posiblemente verificación de contraseña actual.
    /// - Email: requiere verificación de unicidad y potencialmente verificación de email.
    /// - Username: típicamente no debería cambiarse después del registro.
    /// 
    /// El repositorio no debe manejar lógica de negocio como verificación de contraseña.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Actualizar perfil de usuario
    /// var usuario = await _userRepository.FindByIdAsync(userId);
    /// usuario.Nombre = "Juan Pérez Actualizado";
    /// usuario.Telefono = "123456789";
    /// await _userRepository.UpdateAsync(usuario);
    /// </code>
    /// </example>
    /// 
    /// <param name="user">Usuario con datos actualizados.</param>
    /// <returns>El usuario actualizado.</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Elimina un usuario de forma suave (soft delete).
    /// 
    /// <remarks>
    /// La eliminación suave desactiva el usuario sin eliminarlo físicamente.
    /// Esto es preferible porque:
    /// 
    /// - Mantiene integridad en pedidos y transacciones históricas.
    /// - Permite auditoría de actividad.
    /// - Posibilita restauración si el usuario se elimina accidentalmente.
    /// - Cumple con regulaciones de retención de datos.
    /// 
    /// Un usuario eliminado suavemente no puede autenticarse pero sus
    /// datos históricos permanecen disponibles para reportes.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Desactivar usuario (no eliminar)
    /// await _userRepository.DeleteAsync(userId);
    /// 
    /// // El usuario ya no puede iniciar sesión
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador del usuario a eliminar.</param>
    /// <returns>Tarea asíncrona completada tras la eliminación.</returns>
    Task DeleteAsync(long id);

    /// <summary>
    /// Recupera solo los usuarios activos del sistema.
    /// 
    /// <remarks>
    /// Este método retorna usuarios donde IsDeleted es false.
    /// Es útil para operaciones de mailing masivo como newsletters
    /// o notificaciones a usuarios activos.
    /// 
    /// Los resultados se ordenan por Email para facilitar operaciones batch.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Enviar newsletter a usuarios activos
    /// var usuariosActivos = await _userRepository.GetActiveUsersAsync();
    /// foreach (var usuario in usuariosActivos)
    /// {
    ///     await _emailService.SendEmailAsync(new EmailMessage
    ///     {
    ///         To = usuario.Email,
    ///         Subject = "Novedades",
    ///         Body = GetNewsletterHtml()
    ///     });
    /// }
    /// </code>
    /// </example>
    /// 
    /// <returns>Colección de usuarios activos ordenados por Email.</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync();
}
