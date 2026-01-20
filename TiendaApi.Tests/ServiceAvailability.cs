using System.Runtime.CompilerServices;
using MongoDB.Driver;
using NUnit.Framework;
using StackExchange.Redis;

namespace TiendaApi.Tests;

/// <summary>
/// Proporciona métodos para verificar disponibilidad de servicios externos.
/// </summary>
public static class ServiceAvailability
{
    /// <summary>
    /// Verifica si MongoDB está disponible.
    /// </summary>
    public static bool IsMongoDbAvailable()
    {
        var envSkip = Environment.GetEnvironmentVariable("SKIP_INTEGRATION_TESTS");
        if (envSkip?.ToLower() == "true")
            return false;

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
                ?? "mongodb://localhost:27017";

            var client = new MongoClient(connectionString);
            client.ListDatabaseNames().FirstOrDefaultAsync().Wait();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica si Redis está disponible.
    /// </summary>
    public static bool IsRedisAvailable()
    {
        var envSkip = Environment.GetEnvironmentVariable("SKIP_INTEGRATION_TESTS");
        if (envSkip?.ToLower() == "true")
            return false;

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
                ?? "localhost:6379";

            using var connection = ConnectionMultiplexer.Connect(connectionString);
            return connection.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Omite el test si MongoDB no está disponible.
    /// </summary>
    public static void AssumeMongoDbAvailable([CallerMemberName] string? testName = null)
    {
        if (!IsMongoDbAvailable())
            Assert.Ignore($"MongoDB no disponible. Saltando test: {testName}");
    }

    /// <summary>
    /// Omite el test si Redis no está disponible.
    /// </summary>
    public static void AssumeRedisAvailable([CallerMemberName] string? testName = null)
    {
        if (!IsRedisAvailable())
            Assert.Ignore($"Redis no disponible. Saltando test: {testName}");
    }

    /// <summary>
    /// Omite todos los tests de integración si no hay servicios.
    /// </summary>
    public static void AssumeServicesAvailable()
    {
        var envSkip = Environment.GetEnvironmentVariable("SKIP_INTEGRATION_TESTS");
        if (envSkip?.ToLower() == "true")
            Assert.Ignore("Tests de integración desactivados por variable SKIP_INTEGRATION_TESTS");
    }
}

/// <summary>
/// Atributo para marcar tests que requieren MongoDB.
/// Se saltan automáticamente si MongoDB no está disponible.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequiresMongoDbAttribute : NUnitAttribute
{
}

/// <summary>
/// Atributo para marcar tests que requieren Redis.
/// Se saltan automáticamente si Redis no está disponible.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequiresRedisAttribute : NUnitAttribute
{
}

/// <summary>
/// Atributo para marcar tests de integración.
/// Se pueden desactivar con la variable SKIP_INTEGRATION_TESTS.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IntegrationTestAttribute : NUnitAttribute
{
}
