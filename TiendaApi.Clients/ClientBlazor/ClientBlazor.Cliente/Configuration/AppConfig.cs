namespace ClientBlazor.Cliente.Configuration;

/// <summary>
/// Contiene la configuración estática y constantes globales de la aplicación cliente.
/// </summary>
public static class AppConfig
{
    /// <summary>URL base donde se encuentra desplegada la API backend.</summary>
    public const string ApiBaseUrl = "http://localhost:5000";
    
    /// <summary>
    /// Contenedor de credenciales para usuarios de demostración administrativa.
    /// </summary>
    public static class AdminUser
    {
        /// <summary>Email predefinido del administrador.</summary>
        public const string Email = "admin@tienda.com";
        /// <summary>Contraseña predefinida del administrador.</summary>
        public const string Password = "admin";
        /// <summary>Rol asignado al administrador.</summary>
        public const string Role = "ADMIN";
    }
    
    /// <summary>
    /// Contenedor de credenciales para usuarios de demostración estándar.
    /// </summary>
    public static class RegularUser
    {
        /// <summary>Email predefinido del usuario normal.</summary>
        public const string Email = "userdaw@tienda.com";
        /// <summary>Contraseña predefinida del usuario normal.</summary>
        public const string Password = "userdaw";
        /// <summary>Rol asignado al usuario normal.</summary>
        public const string Role = "USER";
    }
}