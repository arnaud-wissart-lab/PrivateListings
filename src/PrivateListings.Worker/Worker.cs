using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;
using PrivateListings.Infrastructure.Identity;

namespace PrivateListings.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<ListingExpirationWorkerOptions> options,
    ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExpireListingsAsync(stoppingToken).ConfigureAwait(false);
            await PurgeAccountSecurityTokensAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(options.Value.Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ExpireListingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var listingService = scope.ServiceProvider.GetRequiredService<IListingService>();
            var expiredCount = await listingService.ExpirePublishedAsync(cancellationToken).ConfigureAwait(false);

            WorkerLog.ListingExpirationCompleted(logger, expiredCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            WorkerLog.ListingExpirationFailed(logger, exception);
        }
    }

    private async Task PurgeAccountSecurityTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tokenStore = scope.ServiceProvider.GetRequiredService<AccountSecurityTokenStore>();
            var deletedCount = await tokenStore.PurgeExpiredAsync(cancellationToken).ConfigureAwait(false);

            WorkerLog.AccountSecurityTokenPurgeCompleted(logger, deletedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            WorkerLog.AccountSecurityTokenPurgeFailed(logger, exception);
        }
    }
}

public sealed class ListingExpirationWorkerOptions
{
    public const string SectionName = "PrivateListings:Worker:ListingExpiration";

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
}

internal static partial class WorkerLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Expiration des annonces terminée. Annonces expirées : {ExpiredCount}.")]
    public static partial void ListingExpirationCompleted(ILogger logger, int expiredCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "L'expiration automatique des annonces a échoué.")]
    public static partial void ListingExpirationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Purge des jetons de sécurité expirés terminée. Jetons supprimés : {DeletedCount}.")]
    public static partial void AccountSecurityTokenPurgeCompleted(ILogger logger, int deletedCount);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "La purge des jetons de sécurité expirés a échoué.")]
    public static partial void AccountSecurityTokenPurgeFailed(ILogger logger, Exception exception);
}
