using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using ClientBlazor.E2E.Extensions;

namespace ClientBlazor.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class UserFlowTests : PageTest
{
    // Configuración global para grabación de video y URL base
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            // URL base donde corre la app
            BaseURL = "http://localhost:5400",
            
            // Grabación de video
            RecordVideoDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "videos"),
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 },
            
            // Viewport estándar
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        };
    }

    [Test]
    public async Task Login_User_Flow_Should_Authenticate_Successfully()
    {
        // 1. Ir a la home
        await Page.GotoAsync("/");
        // Esperar a que el componente cargue
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.ScreenshotAsync(new() { Path = "screenshots/login-1-home.png" });

        // 2. Rellenar formulario de login (usando la extensión TestId)
        await Page.TestId("email-input").FillAsync("userdaw@tienda.com");
        await Page.TestId("password-input").FillAsync("userdaw");
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/login-2-filled.png" });

        // 3. Click en login
        await Page.TestId("login-btn").ClickAsync();

        // 4. Verificar resultado (debe aparecer el AuthPanel)
        // Aumentamos el timeout por si la animación o el proceso tarda
        var authPanel = Page.TestId("auth-panel");
        await Expect(authPanel).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Verificar que contiene el nombre del usuario
        await Expect(authPanel).ToContainTextAsync("userdaw");

        await Page.ScreenshotAsync(new() { Path = "screenshots/login-3-success.png" });
    }

    [Test]
    public async Task Navigation_To_Rest_Page_Should_Load_Content()
    {
        // 1. Ir a la home primero
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 2. Navegar usando el menú
        await Page.TestId("nav-rest").ClickAsync();

        // 3. Esperar a que la página cargue (verificando el contenedor principal)
        var restPage = Page.TestId("rest-page");
        await Expect(restPage).ToBeVisibleAsync();

        // 4. Verificar que hay contenido (buscando el h1 o h3 dentro de la sección de config o header)
        // Usamos restPage para acotar la búsqueda al componente, no al layout
        await Expect(restPage.Locator(".section-title")).ToContainTextAsync("REST API Client");

        await Page.ScreenshotAsync(new() { Path = "screenshots/nav-rest-success.png" });
    }

    [Test]
    public async Task Rest_Page_Interaction_Should_Fetch_Product_By_Id()
    {
        // 1. Ir directamente a REST
        await Page.GotoAsync("/rest");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 2. Seleccionar Operación "GET - Por ID"
        // Buscamos el select dentro del grupo que contiene el texto "Operación"
        await Page.Locator(".form-group")
            .Filter(new() { HasText = "Operación" })
            .Locator("select")
            .SelectOptionAsync(new[] { "get-by-id" });

        // 3. Introducir ID
        await Page.Locator(".form-group")
            .Filter(new() { HasText = "ID" })
            .Locator("input")
            .FillAsync("2");

        await Page.ScreenshotAsync(new() { Path = "screenshots/rest-1-setup.png" });

        // 4. Ejecutar
        // Buscamos el botón que dice "Ejecutar"
        await Page.GetByRole(AriaRole.Button, new() { Name = "Ejecutar" }).ClickAsync();

        // 5. Verificar Resultado
        // Esperamos que aparezca el área de respuesta con el JSON
        var responseArea = Page.Locator(".response-display");
        await Expect(responseArea).ToBeVisibleAsync();
        
        // Verificar contenido JSON (buscamos alguna propiedad típica de un producto)
        await Expect(responseArea).ToContainTextAsync("\"Id\": 2");
        await Expect(responseArea).ToContainTextAsync("Nombre");

        await Page.ScreenshotAsync(new() { Path = "screenshots/rest-2-result.png" });
    }

    [Test]
    public async Task WebSocket_Connection_Should_Receive_Events()
    {
        // 1. Ir a WebSocket
        await Page.GotoAsync("/websocket");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 2. Conectar a Productos (botón público)
        // Por defecto ya está seleccionado 'productos', así que solo pulsamos Conectar
        // Buscamos el botón que contiene "Conectar"
        await Page.GetByRole(AriaRole.Button, new() { Name = "▶️ Conectar" }).ClickAsync();

        // 3. Esperar confirmación de conexión en el log
        // Buscamos en el área de logs (response-display dentro de response-section)
        // El servicio de WS simulado dispara eventos tras un delay, pero el mensaje de conexión es inmediato
        // Buscamos el div específico de respuesta que muestra el estado
        await Expect(Page.Locator(".response-display").Filter(new() { HasText = "Estado: 🟢 Conectado" })).ToBeVisibleAsync(new() { Timeout = 10000 });

        await Page.ScreenshotAsync(new() { Path = "screenshots/ws-1-connected.png" });

        // 4. Esperar un evento automático
        // El log está en un <pre>
        var logsArea = Page.Locator("pre");
        await Expect(logsArea).ToContainTextAsync("\"tipo\":", new() { Timeout = 15000 });

        await Page.ScreenshotAsync(new() { Path = "screenshots/ws-2-event-received.png" });
    }
}