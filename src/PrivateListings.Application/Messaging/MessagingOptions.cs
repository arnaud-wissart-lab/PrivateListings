namespace PrivateListings.Application.Messaging;

public sealed class MessagingRateLimitOptions
{
    public const string SectionName = "PrivateListings:MessagingRateLimits";

    public int SendPermitLimit { get; set; } = 20;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromMinutes(10);
}

public sealed class MessagingSpamOptions
{
    public const string SectionName = "PrivateListings:MessagingSpam";

    public TimeSpan DuplicateMessageWindow { get; set; } = TimeSpan.FromMinutes(2);
}
