using PrivateListings.E2ETests.Support;

namespace PrivateListings.E2ETests;

[Trait("Category", "E2E")]
[Collection(E2ETestGroup.Name)]
public sealed class BackOfficeE2ETests(E2EApplicationFixture fixture) : E2ETestBase(fixture)
{
    [Fact(DisplayName = "Modérateur consulte le back-office et traite un signalement")]
    public Task ModeratorCanResolveListingReport()
        => RunAsync(nameof(ModeratorCanResolveListingReport), async page =>
        {
            await LoginAsync(page, DemoAccounts.Moderator).ConfigureAwait(false);
            await page.GotoAsync("/admin/signalements?q=Cat%C3%A9gorie%20possiblement%20incorrecte").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Signalements" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Catégorie possiblement incorrecte"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            var row = page.Locator("tr:has-text(\"Catégorie possiblement incorrecte\")").First;
            var resolveForm = row.Locator("form[action*='/resolve']").First;
            await resolveForm.GetByPlaceholder("Note de résolution")
                .FillAsync("Signalement vérifié par le scénario E2E.")
                .ConfigureAwait(false);
            await resolveForm.Locator("input[name='confirm']").CheckAsync().ConfigureAwait(false);
            await resolveForm.GetByRole(AriaRole.Button, new() { Name = "Résoudre" })
                .ClickAsync()
                .ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Signalement résolu.")
                .ConfigureAwait(false);
            await Expect(page.Locator(".status-pill:has-text(\"Résolu\")").First)
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Admin consulte les AuditLogs")]
    public Task AdminCanOpenAuditLogs()
        => RunAsync(nameof(AdminCanOpenAuditLogs), async page =>
        {
            await LoginAsync(page, DemoAccounts.Admin).ConfigureAwait(false);
            await page.GotoAsync("/admin/audit").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Audit logs" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Demo.SeedExecuted"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Report.Resolved"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });
}
