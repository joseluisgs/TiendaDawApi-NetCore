namespace ClientBlazor.Cliente.Configuration;

/// <summary>
/// Configuracion de la aplicacion cliente.
/// </summary>
public static class AppConfig
{
    /// <summary>URL base de la API.</summary>
    public const string ApiBaseUrl = "http://localhost:5000";
    
    /// <summary>Credenciales de usuario demo - Administrador.</summary>
    public static class AdminUser
    {
        public const string Email = "admin@tienda.com";
        public const string Password = "admin";
        public const string Role = "ADMIN";
    }
    
    /// <summary>Credenciales de usuario demo - Usuario normal.</summary>
    public static class RegularUser
    {
        public const string Email = "userdaw@tienda.com";
        public const string Password = "userdaw";
        public const string Role = "USER";
    }
}
