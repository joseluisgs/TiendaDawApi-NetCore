namespace TiendaApi.Tests;

/// <summary>
/// Categorías de tests para filtrado y reporting.
/// </summary>
public static class TestCategories
{
    /// <summary>Tests unitarios - rápidos, sin dependencias externas</summary>
    public const string Unit = "Unit";

    /// <summary>Tests de integración - requieren servicios externos</summary>
    public const string Integration = "Integration";

    /// <summary>Tests de repositories</summary>
    public const string Repository = "Repository";

    /// <summary>Tests de servicios de dominio</summary>
    public const string Service = "Service";

    /// <summary>Tests de controladores API</summary>
    public const string Controller = "Controller";

    /// <summary>Tests de validaciones</summary>
    public const string Validator = "Validator";

    /// <summary>Tests de DTOs y mapeo</summary>
    public const string Dto = "Dto";

    /// <summary>Tests de GraphQL</summary>
    public const string GraphQL = "GraphQL";

    /// <summary>Tests de SignalR/Hubs</summary>
    public const string Realtime = "Realtime";

    /// <summary>Tests de background jobs</summary>
    public const string BackgroundJob = "BackgroundJob";

    /// <summary>Tests de caché</summary>
    public const string Cache = "Cache";

    /// <summary>Tests de email</summary>
    public const string Email = "Email";

    /// <summary>Tests de autenticación</summary>
    public const string Auth = "Auth";

    /// <summary>Tests de middleware</summary>
    public const string Middleware = "Middleware";

    /// <summary>Tests de infraestructura</summary>
    public const string Infrastructure = "Infrastructure";

    /// <summary>Tests que requieren Docker/TestContainers</summary>
    public const string Docker = "Docker";

    /// <summary>Tests de larga duración (timeout extendido)</summary>
    public const string Slow = "Slow";
}

/// <summary>
/// Atributo para tests unitarios.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class UnitTestAttribute : CategoryAttribute
{
    public UnitTestAttribute() : base(TestCategories.Unit) { }
}

/// <summary>
/// Atributo para tests de integración.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class IntegrationTestCategoryAttribute : CategoryAttribute
{
    public IntegrationTestCategoryAttribute() : base(TestCategories.Integration) { }
}
