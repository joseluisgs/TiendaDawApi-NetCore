namespace TiendaApi.Api.Data.Seed;

/// <summary>
/// 🎓 Credenciales seed de una sola fuente de verdad.
///
/// ¿Por qué una clase y no literales repartidos por el seeder?
/// El sistema tiene DOS modos de autenticación (Custom e Identity) y ambos
/// deben comportarse de forma idéntica para quien las prueba: mismos usuarios,
/// mismas contraseñas, mismos roles. Cada modo solo cambia el ALGORITMO del
/// hash (BCrypt vs PBKDF2), nunca las credenciales. Al vivir aquí, el seeder,
/// el mensaje de arranque y las pruebas leen exactamente lo mismo y no pueden
/// divergir.
///
/// Requisito de las contraseñas: cumplir la política de Identity
/// (≥8 caracteres, mayúscula y dígito) para que sean válidas en AMBOS modos.
/// </summary>
public static class SeedCredentials
{
    /// <summary>Usuario administrador (rol ADMIN).</summary>
    public const string AdminUsername = "admin";

    /// <summary>Contraseña del administrador (cumple la política de Identity).</summary>
    public const string AdminPassword = "Admin123!";

    /// <summary>Usuario normal (rol USER).</summary>
    public const string UserUsername = "userdaw";

    /// <summary>Contraseña del usuario normal (cumple la política de Identity).</summary>
    public const string UserPassword = "Userdaw123!";
}
