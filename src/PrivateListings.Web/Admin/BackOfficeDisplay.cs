using System.Globalization;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Moderation;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Web.Components.Listings;

namespace PrivateListings.Web.Admin;

public static class BackOfficeDisplay
{
    public static string RoleLabel(string roleName)
        => roleName switch
        {
            PrivateListingsRoleNames.Admin => "Admin",
            PrivateListingsRoleNames.Moderator => "Modérateur",
            PrivateListingsRoleNames.Member => "Membre",
            _ => roleName
        };

    public static string UserStatusLabel(DateTimeOffset? suspendedAt)
        => suspendedAt.HasValue ? "Suspendu" : "Actif";

    public static string UserStatusClass(DateTimeOffset? suspendedAt)
        => suspendedAt.HasValue ? "is-danger" : "is-success";

    public static string ListingStatusLabel(ListingStatus status)
        => ListingDisplay.StatusLabel(status);

    public static string ListingStatusClass(ListingStatus status)
        => ListingDisplay.StatusClass(status);

    public static string ReportStatusLabel(ReportStatus status)
        => status switch
        {
            ReportStatus.Open => "Ouvert",
            ReportStatus.UnderReview => "En revue",
            ReportStatus.Resolved => "Résolu",
            ReportStatus.Rejected => "Rejeté",
            _ => status.ToString()
        };

    public static string ReportStatusClass(ReportStatus status)
        => status switch
        {
            ReportStatus.Open => "is-warning",
            ReportStatus.UnderReview => "is-info",
            ReportStatus.Resolved => "is-success",
            ReportStatus.Rejected => "is-muted",
            _ => "is-muted"
        };

    public static string TargetTypeLabel(ReportTargetType targetType)
        => targetType switch
        {
            ReportTargetType.Listing => "Annonce",
            ReportTargetType.UserProfile => "Utilisateur",
            ReportTargetType.Conversation => "Conversation",
            ReportTargetType.Message => "Message",
            _ => targetType.ToString()
        };

    public static string FormatDate(DateTimeOffset? value)
        => value.HasValue
            ? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : "-";
}
