using PrivateListings.Domain.Common;

namespace PrivateListings.Domain.Privacy;

public sealed class DataExportRequest
{
    private DataExportRequest(
        DataExportRequestId id,
        UserProfileId userProfileId,
        DateTimeOffset requestedAt)
    {
        Guard.AgainstEmpty(id.Value, nameof(id));
        Guard.AgainstEmpty(userProfileId.Value, nameof(userProfileId));

        Id = id;
        UserProfileId = userProfileId;
        RequestedAt = Guard.AgainstDefault(requestedAt, nameof(requestedAt));
        Status = DataExportRequestStatus.Requested;
    }

    public DataExportRequestId Id { get; }

    public UserProfileId UserProfileId { get; }

    public DataExportRequestStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; }

    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? StorageKey { get; private set; }

    public DateTimeOffset? DownloadExpiresAt { get; private set; }

    public string? FailureReason { get; private set; }

    public static DataExportRequest Create(UserProfileId userProfileId, DateTimeOffset requestedAt)
        => new(DataExportRequestId.New(), userProfileId, requestedAt);

    public void MarkProcessing(DateTimeOffset startedAt)
    {
        EnsureStatus(DataExportRequestStatus.Requested);

        Status = DataExportRequestStatus.Processing;
        ProcessingStartedAt = Guard.AgainstDefault(startedAt, nameof(startedAt));
    }

    public void Complete(string storageKey, DateTimeOffset completedAt, DateTimeOffset downloadExpiresAt)
    {
        if (Status is not (DataExportRequestStatus.Requested or DataExportRequestStatus.Processing))
        {
            throw new DomainException("Cette demande d'export ne peut plus être finalisée.");
        }

        var completionDate = Guard.AgainstDefault(completedAt, nameof(completedAt));
        var expirationDate = Guard.AgainstDefault(downloadExpiresAt, nameof(downloadExpiresAt));
        if (expirationDate <= completionDate)
        {
            throw new ArgumentException("La date d'expiration du téléchargement doit être postérieure à la finalisation.", nameof(downloadExpiresAt));
        }

        Status = DataExportRequestStatus.Completed;
        CompletedAt = completionDate;
        StorageKey = Guard.AgainstBlank(storageKey, nameof(storageKey), 260);
        DownloadExpiresAt = expirationDate;
    }

    public void Fail(string reason, DateTimeOffset failedAt)
    {
        if (Status is DataExportRequestStatus.Completed or DataExportRequestStatus.Cancelled)
        {
            throw new DomainException("Cette demande d'export ne peut plus échouer.");
        }

        Status = DataExportRequestStatus.Failed;
        CompletedAt = Guard.AgainstDefault(failedAt, nameof(failedAt));
        FailureReason = Guard.AgainstBlank(reason, nameof(reason), 1_000);
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is DataExportRequestStatus.Completed or DataExportRequestStatus.Failed)
        {
            throw new DomainException("Cette demande d'export ne peut plus être annulée.");
        }

        Status = DataExportRequestStatus.Cancelled;
        CancelledAt = Guard.AgainstDefault(cancelledAt, nameof(cancelledAt));
    }

    private void EnsureStatus(DataExportRequestStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException("La demande d'export n'est pas dans l'état attendu.");
        }
    }
}
