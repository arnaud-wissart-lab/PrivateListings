namespace PrivateListings.E2ETests.Support;

public sealed record DemoAccount(string Email, string Password);

public static class DemoAccounts
{
    public static DemoAccount Admin { get; } = new("admin-demo@example.test", "DemoAdmin!2026");

    public static DemoAccount Moderator { get; } = new("moderateur-demo@example.test", "DemoModerateur!2026");

    public static DemoAccount Member { get; } = new("membre-demo@example.test", "DemoMembre!2026");
}
