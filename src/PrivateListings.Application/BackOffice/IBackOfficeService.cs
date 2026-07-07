using PrivateListings.Domain.Common;

namespace PrivateListings.Application.BackOffice;

public interface IBackOfficeService
{
    Task<BackOfficeDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<BackOfficePagedResult<BackOfficeUserSummary>> ListUsersAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default);

    Task<BackOfficeUserDetails?> GetUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> SuspendUserAsync(
        SuspendBackOfficeUserCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> ReactivateUserAsync(
        ReactivateBackOfficeUserCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> ChangeUserRoleAsync(
        ChangeBackOfficeUserRoleCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficePagedResult<BackOfficeListingSummary>> ListListingsAsync(
        BackOfficeListingQuery query,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> SuspendListingAsync(
        SuspendBackOfficeListingCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> DeleteListingAsync(
        DeleteBackOfficeListingCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficePagedResult<BackOfficeReportSummary>> ListReportsAsync(
        BackOfficeReportQuery query,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> MarkReportUnderReviewAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> ResolveReportAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> RejectReportAsync(
        ReviewBackOfficeReportCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficePagedResult<BackOfficeAuditLogSummary>> ListAuditLogsAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default);

    Task<BackOfficePagedResult<BackOfficeCategorySummary>> ListCategoriesAsync(
        BackOfficePagedQuery query,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> CreateCategoryAsync(
        CreateBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> UpdateCategoryAsync(
        UpdateBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> ActivateCategoryAsync(
        ToggleBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> DeactivateCategoryAsync(
        ToggleBackOfficeCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<BackOfficeOperationResult> RunDemoSeedAsync(
        RunBackOfficeDemoSeedCommand command,
        CancellationToken cancellationToken = default);
}
