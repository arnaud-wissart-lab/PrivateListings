using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PrivateListings.Application.Authentication;
using PrivateListings.Application.BackOffice;
using PrivateListings.Application.Listings;
using PrivateListings.Application.Messaging;
using PrivateListings.Infrastructure.Configuration;
using PrivateListings.Infrastructure.BackOffice;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Messaging;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;
using PrivateListings.Infrastructure.Storage;

namespace PrivateListings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPrivateListingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddPrivateListingsInfrastructureCore(configuration, environment);
        services.AddPrivateListingsWebInfrastructure();

        return services;
    }

    public static IServiceCollection AddPrivateListingsWorkerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddPrivateListingsInfrastructureCore(configuration, environment);

        return services;
    }

    private static IServiceCollection AddPrivateListingsInfrastructureCore(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment)
    {
        services
            .Configure<PrivateListingsPersistenceOptions>(
                configuration.GetSection(PrivateListingsPersistenceOptions.SectionName))
            .Configure<PrivateListingsReferenceDataOptions>(
                configuration.GetSection(PrivateListingsReferenceDataOptions.SectionName))
            .Configure<PrivateListingsDemoSeedOptions>(
                configuration.GetSection(PrivateListingsDemoSeedOptions.SectionName))
            .Configure<PrivateListingsDataProtectionOptions>(
                configuration.GetSection(PrivateListingsDataProtectionOptions.SectionName))
            .Configure<PrivateListingsEmailOptions>(
                configuration.GetSection(PrivateListingsEmailOptions.SectionName))
            .Configure<ListingImageOptions>(
                configuration.GetSection(ListingImageOptions.SectionName))
            .Configure<ListingRateLimitOptions>(
                configuration.GetSection(ListingRateLimitOptions.SectionName))
            .Configure<MessagingRateLimitOptions>(
                configuration.GetSection(MessagingRateLimitOptions.SectionName))
            .Configure<MessagingSpamOptions>(
                configuration.GetSection(MessagingSpamOptions.SectionName));

        services.TryAddSingleton<IConfiguration>(configuration);
        services.TryAddSingleton(TimeProvider.System);
        ConfigureDataProtection(services, configuration);

        if (environment is not null)
        {
            services.AddSingleton(environment);
        }

        var persistenceOptions = configuration
            .GetSection(PrivateListingsPersistenceOptions.SectionName)
            .Get<PrivateListingsPersistenceOptions>()
            ?? new PrivateListingsPersistenceOptions();

        var connectionName = string.IsNullOrWhiteSpace(persistenceOptions.ConnectionName)
            ? "privatelistingsdb"
            : persistenceOptions.ConnectionName;

        var connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"La chaîne de connexion '{connectionName}' est introuvable.");

        services.AddDbContext<PrivateListingsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(PrivateListingsDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
                }));

        services.AddScoped<PrivateListingsReferenceDataSeeder>();
        services.AddScoped<AccountSecurityTokenStore>();
        services.AddScoped<IListingRepository, EfListingRepository>();
        services.AddScoped<IMessagingRepository, EfMessagingRepository>();
        services.AddScoped<IListingImageStorage, FileSystemListingImageStorage>();
        services.AddScoped<IMessageNotificationSender, SmtpMessageNotificationSender>();

        return services;
    }

    private static IServiceCollection AddPrivateListingsWebInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<PrivateListingsIdentitySeeder>();
        services.AddScoped<PrivateListingsDemoDataSeeder>();
        services.AddScoped<PrivateListingsDatabaseInitializer>();
        services.AddScoped<IUserAccountService, IdentityUserAccountService>();
        services.AddScoped<IBackOfficeService, EfBackOfficeService>();
        services.AddScoped<IEmailSender<PrivateListingsUser>, PrivateListingsIdentityEmailSender>();
        services.AddScoped<IPasswordValidator<PrivateListingsUser>, PrivateListingsPasswordValidator>();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(2);
        });

        services.AddIdentityCore<PrivateListingsUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = AccountPasswordPolicyOptions.DefaultRequiredLength;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<PrivateListingsRole>()
            .AddEntityFrameworkStores<PrivateListingsDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }

    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName("PrivateListings");

        var options = configuration
            .GetSection(PrivateListingsDataProtectionOptions.SectionName)
            .Get<PrivateListingsDataProtectionOptions>()
            ?? new PrivateListingsDataProtectionOptions();

        if (!string.IsNullOrWhiteSpace(options.KeysPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(options.KeysPath));
        }
    }
}
