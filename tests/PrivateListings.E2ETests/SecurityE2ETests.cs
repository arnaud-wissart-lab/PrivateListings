using Microsoft.EntityFrameworkCore;
using PrivateListings.E2ETests.Support;

namespace PrivateListings.E2ETests;

[Trait("Category", "E2E")]
[Collection(E2ETestGroup.Name)]
public sealed class SecurityE2ETests(E2EApplicationFixture fixture) : E2ETestBase(fixture)
{
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
