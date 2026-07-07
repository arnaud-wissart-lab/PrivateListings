namespace PrivateListings.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WorkerLog.ScheduledTick(logger, DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

internal static partial class WorkerLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Exécution planifiée du worker à {Timestamp}.")]
    public static partial void ScheduledTick(ILogger logger, DateTimeOffset timestamp);
}
