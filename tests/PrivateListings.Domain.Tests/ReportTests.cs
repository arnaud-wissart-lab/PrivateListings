using PrivateListings.Domain.Common;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Domain.Tests;

public sealed class ReportTests
{
    [Fact]
    public void CreateBuildsOpenReportForListing()
    {
        var reporterId = UserProfileId.New();
        var listingId = ListingId.New();
        var createdAt = new DateTimeOffset(2026, 7, 7, 10, 0, 0, TimeSpan.Zero);

        var report = Report.Create(
            reporterId,
            ReportTarget.ForListing(listingId),
            "Contenu inapproprié",
            createdAt,
            "Le texte de l'annonce semble trompeur.");

        Assert.NotEqual(default, report.Id);
        Assert.Equal(reporterId, report.ReporterId);
        Assert.Equal(ReportTargetType.Listing, report.Target.Type);
        Assert.Equal(listingId.Value, report.Target.TargetId);
        Assert.Equal(ReportStatus.Open, report.Status);
    }
}
