using Microsoft.Playwright;

namespace ClientBlazor.E2E.Extensions;

public static class PlaywrightExtensions
{
    public static ILocator TestId(this IPage page, string id) => page.GetByTestId(id);
    public static ILocator TestId(this ILocator locator, string id) => locator.GetByTestId(id);
}