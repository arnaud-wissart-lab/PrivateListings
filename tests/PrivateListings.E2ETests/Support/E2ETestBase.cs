using System.Text.RegularExpressions;

namespace PrivateListings.E2ETests.Support;

public abstract partial class E2ETestBase(E2EApplicationFixture fixture) : IAsyncLifetime
{
    protected E2EApplicationFixture Fixture { get; } = fixture;

    public Task InitializeAsync()
        => Fixture.ResetAsync();

    public Task DisposeAsync()
        => Task.CompletedTask;

    protected async Task RunAsync(string scenarioName, Func<IPage, Task> scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var context = await Fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.ToString(),
            IgnoreHTTPSErrors = true,
            Locale = "fr-FR",
            TimezoneId = "Europe/Paris",
            ViewportSize = new ViewportSize
            {
                Width = 1440,
                Height = 1000
            }
        }).ConfigureAwait(false);

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);

        try
        {
            await scenario(page).ConfigureAwait(false);
            await context.Tracing.StopAsync().ConfigureAwait(false);
        }
        catch
        {
            await CaptureFailureArtifactsAsync(context, page, scenarioName).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await context.CloseAsync().ConfigureAwait(false);
        }
    }

    protected static async Task LoginAsync(IPage page, DemoAccount account)
    {
        await page.GotoAsync("/login").ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Connexion" }))
            .ToBeVisibleAsync()
            .ConfigureAwait(false);

        await page.GetByLabel("Email").FillAsync(account.Email).ConfigureAwait(false);
        await page.GetByLabel("Mot de passe").FillAsync(account.Password).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" })
            .ClickAsync()
            .ConfigureAwait(false);

        await Expect(page.GetByText("Connecté"))
            .ToBeVisibleAsync()
            .ConfigureAwait(false);
    }

    protected static async Task LogoutAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Déconnexion" })
            .ClickAsync()
            .ConfigureAwait(false);

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Connexion" }))
            .ToBeVisibleAsync()
            .ConfigureAwait(false);
    }

    protected static async Task CreateDraftListingAsync(IPage page, string title)
    {
        await page.GotoAsync("/annonces/nouvelle").ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Nouvelle annonce" }))
            .ToBeVisibleAsync()
            .ConfigureAwait(false);

        await page.GetByLabel("Titre").FillAsync(title).ConfigureAwait(false);
        await page.GetByLabel("Description").FillAsync(
            "Annonce créée par un scénario E2E avec des données fictives et sans contact réel.")
            .ConfigureAwait(false);
        await page.GetByLabel("Type").SelectOptionAsync(new SelectOptionValue { Label = "Service" }).ConfigureAwait(false);
        await page.GetByLabel("Catégorie").SelectOptionAsync(new SelectOptionValue { Label = "Services" }).ConfigureAwait(false);
        await page.GetByLabel("Localisation approximative").FillAsync("Secteur E2E").ConfigureAwait(false);
        await page.GetByLabel("Prix facultatif").FillAsync("12").ConfigureAwait(false);

        await page.GetByRole(AriaRole.Button, new() { Name = "Créer le brouillon" })
            .ClickAsync()
            .ConfigureAwait(false);

        await Expect(page.GetByRole(AriaRole.Alert))
            .ToContainTextAsync("Annonce créée en brouillon.")
            .ConfigureAwait(false);
        await Expect(page.Locator(".status-pill:has-text(\"Brouillon\")").First)
            .ToBeVisibleAsync()
            .ConfigureAwait(false);
    }

    protected static async Task OpenListingAsync(IPage page, string title)
    {
        var query = Uri.EscapeDataString(title);
        await page.GotoAsync($"/annonces?q={query}").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Link, new() { Name = title })
            .First
            .ClickAsync()
            .ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = title }))
            .ToBeVisibleAsync()
            .ConfigureAwait(false);
    }

    protected string CreateArtifactPath(string fileName)
        => Fixture.CreateArtifactPath(fileName);

    private async Task CaptureFailureArtifactsAsync(IBrowserContext context, IPage page, string scenarioName)
    {
        var safeName = SafeArtifactName(scenarioName);
        var screenshotPath = Fixture.CreateArtifactPath($"{safeName}.png");
        var tracePath = Fixture.CreateArtifactPath($"{safeName}.zip");
        var webOutputPath = Fixture.CreateArtifactPath($"{safeName}.web-output.txt");

        try
        {
            await File.WriteAllTextAsync(webOutputPath, Fixture.ReadWebOutput()).ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        try
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            }).ConfigureAwait(false);
        }
        catch (PlaywrightException)
        {
        }

        try
        {
            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = tracePath
            }).ConfigureAwait(false);
        }
        catch (PlaywrightException)
        {
        }
    }

    private static string SafeArtifactName(string value)
        => UnsafeArtifactCharactersRegex().Replace(value, "_");

    [GeneratedRegex("[^a-zA-Z0-9_.-]+")]
    private static partial Regex UnsafeArtifactCharactersRegex();
}
