using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateListings.Domain.Common;

namespace PrivateListings.Infrastructure.Persistence.Configurations;

internal static class StronglyTypedIdConverters
{
    public static readonly ValueConverter<UserProfileId, Guid> UserProfileIdConverter =
        new(id => id.Value, value => UserProfileId.From(value));

    public static readonly ValueConverter<UserProfileId?, Guid?> NullableUserProfileIdConverter =
        new(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? UserProfileId.From(value.Value) : null);

    public static readonly ValueConverter<ListingId, Guid> ListingIdConverter =
        new(id => id.Value, value => ListingId.From(value));

    public static readonly ValueConverter<ListingId?, Guid?> NullableListingIdConverter =
        new(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? ListingId.From(value.Value) : null);

    public static readonly ValueConverter<ListingCategoryId, Guid> ListingCategoryIdConverter =
        new(id => id.Value, value => ListingCategoryId.From(value));

    public static readonly ValueConverter<ListingImageId, Guid> ListingImageIdConverter =
        new(id => id.Value, value => ListingImageId.From(value));

    public static readonly ValueConverter<ConversationId, Guid> ConversationIdConverter =
        new(id => id.Value, value => ConversationId.From(value));

    public static readonly ValueConverter<MessageId, Guid> MessageIdConverter =
        new(id => id.Value, value => MessageId.From(value));

    public static readonly ValueConverter<ReportId, Guid> ReportIdConverter =
        new(id => id.Value, value => ReportId.From(value));

    public static readonly ValueConverter<RecommendationId, Guid> RecommendationIdConverter =
        new(id => id.Value, value => RecommendationId.From(value));

    public static readonly ValueConverter<AuditLogId, Guid> AuditLogIdConverter =
        new(id => id.Value, value => AuditLogId.From(value));

    public static readonly ValueConverter<ConsentRecordId, Guid> ConsentRecordIdConverter =
        new(id => id.Value, value => ConsentRecordId.From(value));

    public static readonly ValueConverter<DataExportRequestId, Guid> DataExportRequestIdConverter =
        new(id => id.Value, value => DataExportRequestId.From(value));

    public static readonly ValueConverter<List<UserProfileId>, Guid[]> UserProfileIdListConverter =
        new(ids => ids.Select(id => id.Value).ToArray(), values => values.Select(UserProfileId.From).ToList());

    public static readonly ValueComparer<List<UserProfileId>> UserProfileIdListComparer =
        new(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            ids => ids.ToList());
}
