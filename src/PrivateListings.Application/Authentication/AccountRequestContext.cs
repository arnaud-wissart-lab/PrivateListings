namespace PrivateListings.Application.Authentication;

public sealed record AccountRequestContext(string? RemoteIpAddress, string? UserAgent);
