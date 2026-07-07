using PrivateListings.Domain.Common;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Moderation;

namespace PrivateListings.Application.BackOffice;

public sealed record BackOfficeActor(
    UserProfileId UserProfileId,
    bool IsAdmin,
    bool IsModerator)
{
    public bool CanModerate => IsAdmin || IsModerator;
}

public sealed record BackOfficePagedQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed record BackOfficePagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}

public sealed record BackOfficeDashboard(
    int TotalUsers,
    int SuspendedUsers,
    int PublishedListings,
    int SuspendedListings,
    int OpenReports,
    int AuditLogsLastSevenDays);

public sealed record BackOfficeUserSummary(
    Guid UserId,
    UserProfileId UserProfileId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SuspendedAt,
    bool EmailConfirmed);

public sealed record BackOfficeUserDetails(
    Guid UserId,
    UserProfileId UserProfileId,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset? SuspendedAt,
    string? SuspensionReason,
    int ListingsCount,
    int OpenReportsCount,
    int AuditLogsCount);

public sealed record BackOfficeListingQuery(
    string? Search = null,
    ListingStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed record BackOfficeListingSummary(
    ListingId Id,
    string Title,
    ListingStatus Status,
    ListingType Type,
    string CategoryName,
    string OwnerDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? SuspendedAt,
    DateTimeOffset? DeletedAt);

public sealed record BackOfficeReportQuery(
    string? Search = null,
    ReportStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed record BackOfficeReportSummary(
    ReportId Id,
    ReportStatus Status,
    ReportTargetType TargetType,
    Guid TargetId,
    string ReporterDisplayName,
    string Reason,
    string? Details,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewedByDisplayName,
    string? ResolutionNote,
    string? TargetLabel);

public sealed record BackOfficeAuditLogSummary(
    AuditLogId Id,
    string Action,
    string SubjectType,
    Guid? SubjectId,
    string? ActorDisplayName,
    DateTimeOffset OccurredAt,
    string? Metadata);

public sealed record BackOfficeCategorySummary(
    ListingCategoryId Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int ListingsCount);

public sealed record SuspendBackOfficeUserCommand(
    BackOfficeActor Actor,
    Guid UserId,
    string Reason);

public sealed record ReactivateBackOfficeUserCommand(
    BackOfficeActor Actor,
    Guid UserId);

public sealed record ChangeBackOfficeUserRoleCommand(
    BackOfficeActor Actor,
    Guid UserId,
    string RoleName);

public sealed record SuspendBackOfficeListingCommand(
    BackOfficeActor Actor,
    ListingId ListingId,
    string Reason);

public sealed record DeleteBackOfficeListingCommand(
    BackOfficeActor Actor,
    ListingId ListingId,
    string Reason);

public sealed record ReviewBackOfficeReportCommand(
    BackOfficeActor Actor,
    ReportId ReportId,
    string Note);

public sealed record CreateBackOfficeCategoryCommand(
    BackOfficeActor Actor,
    string Name,
    string? Description);

public sealed record UpdateBackOfficeCategoryCommand(
    BackOfficeActor Actor,
    ListingCategoryId CategoryId,
    string Name,
    string? Description);

public sealed record ToggleBackOfficeCategoryCommand(
    BackOfficeActor Actor,
    ListingCategoryId CategoryId);

public sealed record RunBackOfficeDemoSeedCommand(BackOfficeActor Actor);

public enum BackOfficeOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    ValidationFailed
}

public sealed record BackOfficeOperationResult(
    BackOfficeOperationStatus Status,
    IReadOnlyList<string>? Errors = null)
{
    public bool Succeeded => Status == BackOfficeOperationStatus.Success;

    public static BackOfficeOperationResult Success()
        => new(BackOfficeOperationStatus.Success);

    public static BackOfficeOperationResult NotFound(string message)
        => new(BackOfficeOperationStatus.NotFound, [message]);

    public static BackOfficeOperationResult Forbidden(string message)
        => new(BackOfficeOperationStatus.Forbidden, [message]);

    public static BackOfficeOperationResult ValidationFailed(params string[] errors)
        => new(BackOfficeOperationStatus.ValidationFailed, errors);
}
