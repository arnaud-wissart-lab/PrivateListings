namespace PrivateListings.E2ETests.Support;

public sealed record ReceivedEmail(string RawContent, DateTimeOffset ReceivedAt);
