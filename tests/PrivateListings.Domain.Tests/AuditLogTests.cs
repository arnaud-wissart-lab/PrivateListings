using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Tests;

public sealed class AuditLogTests
{
    [Fact]
    public void CreateBuildsAuditLogForSensitiveAction()
    {
        var actorId = UserProfileId.New();
        var listingId = ListingId.New();
        var occurredAt = new DateTimeOffset(2026, 7, 7, 11, 0, 0, TimeSpan.Zero);

        var auditLog = AuditLog.Create(
            actorId,
            "Listing.Suspended",
            AuditLogSubject.ForListing(listingId),
            occurredAt,
            "Motif: modération");

        Assert.NotEqual(default, auditLog.Id);
        Assert.Equal(actorId, auditLog.ActorUserProfileId);
        Assert.Equal("Listing.Suspended", auditLog.Action);
        Assert.Equal("Listing", auditLog.Subject.Type);
        Assert.Equal(listingId.Value, auditLog.Subject.EntityId);
        Assert.Equal(occurredAt, auditLog.OccurredAt);
    }
}
