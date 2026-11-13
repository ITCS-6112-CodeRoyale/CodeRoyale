namespace CodeRoyale.Live;

/// <summary>
/// The authoritative clock. One background loop advances every live game, rather
/// than a timer per game, so question delivery is driven from a single place.
/// </summary>
/// <remarks>
/// This is what makes question timing consistent for everyone in a lobby, which the
/// fairness requirement asks for: the deadline is decided server-side and pushed,
/// never negotiated per client.
/// </remarks>
public sealed class GameLoopService : BackgroundService
{
    /// <summary>Fine enough that a 15s question never visibly overruns.</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan PruneInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxGameAge = TimeSpan.FromHours(4);

    private readonly GameService _games;
    private readonly ILogger<GameLoopService> _log;

    public GameLoopService(GameService games, ILogger<GameLoopService> log)
    {
        _games = games;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Game loop started, ticking every {Ms}ms", TickInterval.TotalMilliseconds);

        using PeriodicTimer timer = new(TickInterval);
        DateTime lastPrune = DateTime.UtcNow;

        while (await SafeWaitAsync(timer, stoppingToken))
        {
            DateTime now = DateTime.UtcNow;

            try
            {
                _games.Tick(now);

                if (now - lastPrune > PruneInterval)
                {
                    _games.PruneStale(now, MaxGameAge);
                    lastPrune = now;
                }
            }
            catch (Exception ex)
            {
                // One bad game must not take the loop down for everyone else.
                _log.LogError(ex, "Game loop tick failed");
            }
        }

        _log.LogInformation("Game loop stopped");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
