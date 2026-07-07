namespace PrivateListings.Application.Authentication;

public sealed record AccountRateLimitRequest(
    AccountRateLimitAction Action,
    string? Identifier,
    string? RemoteIpAddress);
