using Microsoft.Playwright;

namespace Iscuissatis.E2E.Tests;

/// <summary>
/// Sobe um Chromium headless e aponta para a aplicação real.
/// A URL vem de E2E_BASE_URL (o job de CI sobe a app e define a variável).
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5000";

    public string ArtifactsDirectory { get; } =
        Environment.GetEnvironmentVariable("E2E_ARTIFACTS") ?? "artifacts/e2e";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task<IPage> NewPageAsync()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            Locale = "pt-BR",
        });

        return await context.NewPageAsync();
    }

    public async Task<string?> ScreenshotAsync(IPage page, string name)
    {
        try
        {
            Directory.CreateDirectory(ArtifactsDirectory);
            var path = Path.Combine(ArtifactsDirectory, $"{name}-{DateTime.UtcNow:HHmmss}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
            return path;
        }
        catch
        {
            return null;
        }
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}

[CollectionDefinition("e2e")]
public class E2ECollection : ICollectionFixture<BrowserFixture>;
