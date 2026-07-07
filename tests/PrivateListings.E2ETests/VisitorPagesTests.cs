using PrivateListings.E2ETests.Support;

namespace PrivateListings.E2ETests;

[Trait("Category", "E2E")]
[Collection(E2ETestGroup.Name)]
public sealed class VisitorPagesTests(E2EApplicationFixture fixture) : E2ETestBase(fixture)
{
    [Fact(DisplayName = "Visiteur consulte la landing page")]
    public Task VisitorCanOpenLandingPage()
        => RunAsync(nameof(VisitorCanOpenLandingPage), async page =>
        {
            await page.GotoAsync("/").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Tableau de bord" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Voir les annonces" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Annonces locales"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Compte privé"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Visiteur consulte la page Données & sécurité")]
    public Task VisitorCanOpenDataSecurityPage()
        => RunAsync(nameof(VisitorCanOpenDataSecurityPage), async page =>
        {
            await page.GotoAsync("/").ConfigureAwait(false);
            await page.GetByRole(AriaRole.Link, new() { Name = "Données & sécurité" })
                .First
                .ClickAsync()
                .ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Données & sécurité" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Données minimisées"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Données fictives en mode démo"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });
}
