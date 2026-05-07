namespace ClientBlazor.Cliente.Configuration;

/// <summary>
/// Contiene la configuración estática y constantes globales de la aplicación cliente.
/// </summary>
public static class AppConfig
{
    /// <summary>URL base donde se encuentra desplegada la API backend.</summary>
    public const string ApiBaseUrl = "http://localhost:5031";
    
    /// <summary>
    /// Contenedor de credenciales para usuarios de demostración administrativa.
    /// </summary>
public static class AdminUser
    {
        public const string Email = "admin";
        public const string Password = "admin";
        public const string Role = "ADMIN";
    }
    
    /// <summary>
    /// Contenedor de credenciales para usuarios de demostración estándar.
    /// </summary>
public static class RegularUser
    {
        public const string Email = "userdaw";
        public const string Password = "userdaw";
        public const string Role = "USER";
    }
}