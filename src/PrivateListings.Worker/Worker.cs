using Microsoft.Extensions.Options;
using PrivateListings.Application.Listings;

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
}
