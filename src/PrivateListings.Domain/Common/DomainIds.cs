namespace PrivateListings.Domain.Common;

public readonly record struct UserProfileId
{
    private UserProfileId(Guid value) => Value = value;

    public Guid Value { get; }

    public static UserProfileId New() => new(Guid.NewGuid());

    public static UserProfileId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new UserProfileId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ListingId
{
    private ListingId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ListingId New() => new(Guid.NewGuid());

    public static ListingId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ListingId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ListingCategoryId
{
    private ListingCategoryId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ListingCategoryId New() => new(Guid.NewGuid());

    public static ListingCategoryId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ListingCategoryId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ListingImageId
{
    private ListingImageId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ListingImageId New() => new(Guid.NewGuid());

    public static ListingImageId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ListingImageId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ConversationId
{
    private ConversationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ConversationId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct MessageId
{
    private MessageId(Guid value) => Value = value;

    public Guid Value { get; }

    public static MessageId New() => new(Guid.NewGuid());

    public static MessageId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new MessageId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ReportId
{
    private ReportId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ReportId New() => new(Guid.NewGuid());

    public static ReportId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ReportId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct RecommendationId
{
    private RecommendationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RecommendationId New() => new(Guid.NewGuid());

    public static RecommendationId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new RecommendationId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct AuditLogId
{
    private AuditLogId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AuditLogId New() => new(Guid.NewGuid());

    public static AuditLogId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new AuditLogId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct ConsentRecordId
{
    private ConsentRecordId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ConsentRecordId New() => new(Guid.NewGuid());

    public static ConsentRecordId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new ConsentRecordId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct DataExportRequestId
{
    private DataExportRequestId(Guid value) => Value = value;

    public Guid Value { get; }

    public static DataExportRequestId New() => new(Guid.NewGuid());

    public static DataExportRequestId From(Guid value)
    {
        Guard.AgainstEmpty(value, nameof(value));
        return new DataExportRequestId(value);
    }

    public override string ToString() => Value.ToString("D");
}
