namespace PrivateListings.Infrastructure.Identity;

public sealed class PrivateListingsEmailOptions
{
    public const string SectionName = "PrivateListings:Email";

    public string FromAddress { get; set; } = "noreply@privatelistings.local";

    public string FromName { get; set; } = "PrivateListings";

    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; }
}
