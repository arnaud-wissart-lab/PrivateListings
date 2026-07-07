using Microsoft.EntityFrameworkCore;
using PrivateListings.Domain.Auditing;
using PrivateListings.Domain.Listings;
using PrivateListings.Domain.Messaging;
using PrivateListings.Domain.Moderation;
using PrivateListings.Domain.Privacy;
using PrivateListings.Domain.Recommendations;
using PrivateListings.Domain.Users;
using PrivateListings.Infrastructure.Persistence.Seeding;

namespace PrivateListings.Infrastructure.Persistence;

public sealed class PrivateListingsDbContext(DbContextOptions<PrivateListingsDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<ListingCategory> ListingCategories => Set<ListingCategory>();

    public DbSet<Listing> Listings => Set<Listing>();

    public DbSet<ListingImage> ListingImages => Set<ListingImage>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    public DbSet<DataExportRequest> DataExportRequests => Set<DataExportRequest>();

    public DbSet<DemoUserAccount> DemoUserAccounts => Set<DemoUserAccount>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrivateListingsDbContext).Assembly);
    }
}
