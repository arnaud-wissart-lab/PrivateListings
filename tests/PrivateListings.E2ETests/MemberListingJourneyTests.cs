using PrivateListings.E2ETests.Support;

namespace PrivateListings.E2ETests;

[Trait("Category", "E2E")]
[Collection(E2ETestGroup.Name)]
public sealed class MemberListingJourneyTests(E2EApplicationFixture fixture) : E2ETestBase(fixture)
{
    private const string SearchListingTitle = "Recherche coup de main fictif 03";
    private const string ReplyListingTitle = "Objet à donner pour atelier démo 02";
    private const string ModeratorOwnedListingTitle = "Atelier convivial démo 19";

    [Fact(DisplayName = "Utilisateur consulte, filtre et ouvre les annonces")]
    public Task MemberCanBrowseFilterAndOpenListings()
        => RunAsync(nameof(MemberCanBrowseFilterAndOpenListings), async page =>
        {
            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await page.GotoAsync("/annonces").ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Annonces" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Link, new() { Name = ReplyListingTitle }).First)
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            await page.Locator("#listing-filter-q").FillAsync("coup de main fictif 03").ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync().ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Link, new() { Name = SearchListingTitle }).First)
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            await page.GetByRole(AriaRole.Link, new() { Name = SearchListingTitle })
                .First
                .ClickAsync()
                .ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = SearchListingTitle }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText("Description"))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Utilisateur crée un brouillon, publie, répond et échange un message")]
    public Task MemberCanCreatePublishReplyAndSendMessage()
        => RunAsync(nameof(MemberCanCreatePublishReplyAndSendMessage), async page =>
        {
            var draftTitle = $"Annonce E2E {Guid.NewGuid():N}"[..21];
            var firstMessage = $"Premier message E2E {Guid.NewGuid():N}";
            var secondMessage = $"Relance E2E {Guid.NewGuid():N}";

            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await CreateDraftListingAsync(page, draftTitle).ConfigureAwait(false);

            await page.GetByRole(AriaRole.Button, new() { Name = "Publier" }).ClickAsync().ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Annonce publiée.")
                .ConfigureAwait(false);
            await Expect(page.Locator(".status-pill:has-text(\"Publiée\")").First)
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            await OpenListingAsync(page, ReplyListingTitle).ConfigureAwait(false);
            await page.GetByLabel("Message").FillAsync(firstMessage).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Répondre" }).ClickAsync().ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Message envoyé.")
                .ConfigureAwait(false);
            await Expect(page.GetByText(firstMessage))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            await page.GotoAsync("/messagerie").ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Messagerie" }))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);
            await page.Locator($"a.conversation-row-main:has(.conversation-listing:text-is(\"{ReplyListingTitle}\"))")
                .First
                .ClickAsync()
                .ConfigureAwait(false);

            await page.Locator("#message-body").FillAsync(secondMessage).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Envoyer" }).ClickAsync().ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Message envoyé.")
                .ConfigureAwait(false);
            await Expect(page.GetByText(secondMessage))
                .ToBeVisibleAsync()
                .ConfigureAwait(false);

            await Fixture.EmailServer.WaitForMessageAsync(_ => true, TimeSpan.FromSeconds(10))
                .ConfigureAwait(false);

            Assert.NotEmpty(Fixture.EmailServer.Messages);
            Assert.DoesNotContain(
                Fixture.EmailServer.Messages,
                email => email.RawContent.Contains(firstMessage, StringComparison.Ordinal));
            Assert.DoesNotContain(
                Fixture.EmailServer.Messages,
                email => email.RawContent.Contains(secondMessage, StringComparison.Ordinal));
        });

    [Fact(DisplayName = "Utilisateur signale un message")]
    public Task MemberCanReportMessage()
        => RunAsync(nameof(MemberCanReportMessage), async page =>
        {
            var memberMessage = $"Question E2E {Guid.NewGuid():N}";
            var moderatorMessage = $"Réponse modérateur E2E {Guid.NewGuid():N}";

            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await OpenListingAsync(page, ModeratorOwnedListingTitle).ConfigureAwait(false);
            await page.GetByLabel("Message").FillAsync(memberMessage).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Répondre" }).ClickAsync().ConfigureAwait(false);
            await Expect(page.GetByText(memberMessage)).ToBeVisibleAsync().ConfigureAwait(false);

            await LogoutAsync(page).ConfigureAwait(false);
            await LoginAsync(page, DemoAccounts.Moderator).ConfigureAwait(false);
            await page.GotoAsync("/messagerie").ConfigureAwait(false);
            await page.Locator($"a.conversation-row-main:has(.conversation-participant:text-is(\"Membre Démo\")):has(.conversation-listing:text-is(\"{ModeratorOwnedListingTitle}\"))")
                .First
                .ClickAsync()
                .ConfigureAwait(false);
            await page.Locator("#message-body").FillAsync(moderatorMessage).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Envoyer" }).ClickAsync().ConfigureAwait(false);
            await Expect(page.GetByText(moderatorMessage)).ToBeVisibleAsync().ConfigureAwait(false);

            await LogoutAsync(page).ConfigureAwait(false);
            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await page.GotoAsync("/messagerie").ConfigureAwait(false);
            await page.Locator($"a.conversation-row-main:has(.conversation-participant:text-is(\"Modérateur Démo\")):has(.conversation-listing:text-is(\"{ModeratorOwnedListingTitle}\"))")
                .First
                .ClickAsync()
                .ConfigureAwait(false);
            await Expect(page.GetByText(moderatorMessage)).ToBeVisibleAsync().ConfigureAwait(false);

            var reportForm = page.Locator(".message-report").First;
            await reportForm.Locator("summary").ClickAsync().ConfigureAwait(false);
            await reportForm.GetByLabel("Motif").FillAsync("Message à relire").ConfigureAwait(false);
            await reportForm.GetByLabel("Détails facultatifs").FillAsync("Signalement E2E sans donnée réelle.").ConfigureAwait(false);
            await reportForm.GetByRole(AriaRole.Button, new() { Name = "Envoyer le signalement" })
                .ClickAsync()
                .ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Signalement enregistré.")
                .ConfigureAwait(false);
        });

    [Fact(DisplayName = "Upload fichier invalide refusé")]
    public Task InvalidUploadIsRejected()
        => RunAsync(nameof(InvalidUploadIsRejected), async page =>
        {
            var draftTitle = $"Upload E2E {Guid.NewGuid():N}"[..18];
            var invalidFile = CreateArtifactPath($"invalid-upload-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(invalidFile, "ceci n'est pas une image").ConfigureAwait(false);

            await LoginAsync(page, DemoAccounts.Member).ConfigureAwait(false);
            await CreateDraftListingAsync(page, draftTitle).ConfigureAwait(false);

            await page.SetInputFilesAsync("#image", invalidFile).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Téléverser" }).ClickAsync().ConfigureAwait(false);

            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("Erreur")
                .ConfigureAwait(false);
            await Expect(page.GetByRole(AriaRole.Alert))
                .ToContainTextAsync("L'extension du fichier image est invalide.")
                .ConfigureAwait(false);
        });
}
