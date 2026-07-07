using Microsoft.EntityFrameworkCore;
using PrivateListings.E2ETests.Support;

namespace PrivateListings.E2ETests;

[Trait("Category", "E2E")]
[Collection(E2ETestGroup.Name)]
public sealed class SecurityE2ETests(E2EApplicationFixture fixture) : E2ETestBase(fixture)
{
    [Fact(DisplayName = "Headers sécurité présents sur page publique")]
    public Task PublicPageIncludesSecurityHeaders()
        => RunAsync(nameof(PublicPageIncludesSecurityHeaders), async page =>
        {
            var response = await page.GotoAsync("/").ConfigureAwait(false);
            Assert.NotNull(response);

            var headers = await response.AllHeadersAsync().ConfigureAwait(false);
            Assert.Equal("nosniff", headers["x-content-type-options"]);
            Assert.Equal("DENY", headers["x-frame-options"]);
            Assert.Equal("strict-origin-when-cross-origin", headers["referrer-policy"]);
            Assert.Contains("camera=()", headers["permissions-policy"], StringComparison.Ordinal);
            Assert.Contains("geolocation=()", headers["permissions-policy"], StringComparison.Ordinal);
            Assert.Contains("microphone=()", headers["permissions-policy"], StringComparison.Ordinal);
        });

    [Fact(DisplayName = "Visiteur redirigé sur la liste des annonces")]
    public Task VisitorCannotOpenListings()
        => RunAsync(nameof(VisitorCannotOpenListings), async page =>
        {
            await page.GotoAsync("/annonces").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Connexion" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Annonces" }))
                .Not
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Visiteur redirigé sur les images d'annonces")]
    public Task VisitorCannotOpenListingImages()
        => RunAsync(nameof(VisitorCannotOpenListingImages), async page =>
        {
            var imageId = await Fixture.QueryAsync(async dbContext =>
            {
                return await dbContext.ListingImages
                    .AsNoTracking()
                    .Select(image => image.Id)
                    .FirstAsync()
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            await page.GotoAsync($"/annonces/images/{imageId.Value:D}").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Connexion" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Login refuse une returnUrl externe")]
    public Task LoginIgnoresExternalReturnUrl()
        => RunAsync(nameof(LoginIgnoresExternalReturnUrl), async page =>
        {
            await page.GotoAsync("/login?returnUrl=%2F%2Fevil.example").ConfigureAwait(false);
            await page.GetByLabel("Email").FillAsync(DemoAccounts.Member.Email).ConfigureAwait(false);
            await page.GetByLabel("Mot de passe").FillAsync(DemoAccounts.Member.Password).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" })
                .ClickAsync()
                .ConfigureAwait(false);

            await Expect(page.GetByText("Connecté"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            Assert.StartsWith(Fixture.BaseAddress.ToString(), page.Url, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("evil.example", page.Url, StringComparison.OrdinalIgnoreCase);
        });

    [Fact(DisplayName = "Logout refuse un POST sans antiforgery")]
    public Task LogoutRejectsMissingAntiforgeryToken()
        => RunAsync(nameof(LogoutRejectsMissingAntiforgeryToken), async page =>
        {
            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);

            var statusCode = await page.EvaluateAsync<int>(
                """
                async () => {
                    const response = await fetch('/account/logout', {
                        method: 'POST',
                        credentials: 'include',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: 'returnUrl='
                    });

                    return response.status;
                }
                """).ConfigureAwait(false);

            Assert.Equal(400, statusCode);

            await page.ReloadAsync().ConfigureAwait(false);
            await Expect(page.GetByText("Connecté"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Accès admin refusé à un membre")]
    public Task MemberCannotOpenAdminAuditLogs()
        => RunAsync(nameof(MemberCannotOpenAdminAuditLogs), async page =>
        {
            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await page.GotoAsync("/admin/audit").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Accès refusé" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Audit logs"))
                .Not
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Utilisateur ne peut pas consulter la conversation d'un autre")]
    public Task MemberCannotOpenAnotherUserConversation()
        => RunAsync(nameof(MemberCannotOpenAnotherUserConversation), async page =>
        {
            var conversationId = await Fixture.QueryAsync(async dbContext =>
            {
                var member = await dbContext.DemoUserAccounts
                    .SingleAsync(account => account.Email == DemoAccounts.Member.Email)
                    .ConfigureAwait(false);

                var conversations = await dbContext.Conversations
                    .AsNoTracking()
                    .ToListAsync()
                    .ConfigureAwait(false);

                return conversations
                    .Where(conversation => !conversation.ParticipantIds.Contains(member.UserProfileId))
                    .Select(conversation => conversation.Id)
                    .First();
            }).ConfigureAwait(false);

            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await page.GotoAsync($"/messagerie/{conversationId.Value:D}").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Conversation introuvable" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Bonjour, ce message fictif"))
                .Not
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });
}
