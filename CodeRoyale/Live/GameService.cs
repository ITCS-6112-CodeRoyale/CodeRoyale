using System.Collections.Concurrent;
using System.Globalization;
using CodeRoyale.Data;
using CodeRoyale.Data.Games;
using CodeRoyale.Data.Questions;
using CodeRoyale.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodeRoyale.Live;

/// <summary>
/// Owns every live game. Registered as a singleton: this is the single source of
/// truth for phase, timing and scores.
/// </summary>
/// <remarks>
/// Scoring is always computed from the server's own clock -- the gap between when
/// the server pushed the question and when the answer arrived. A client-reported
/// duration would be trivial to forge, and this is a competitive scoreboard.
/// </remarks>
public sealed class GameService : IAsyncDisposable
{
    /// <summary>How long the correct answer and standings stay on screen.</summary>
    public const int RevealSeconds = 5;

    /// <summary>A correct answer at the buzzer is worth this fraction of an instant one.</summary>
    private const double SlowestCorrectMultiplier = 0.5;

    private int _gamesEnding;
    private CancellationTokenRegistration _onApplicationStoppingListener;

    private readonly ConcurrentDictionary<string, LiveGame> _games = new(StringComparer.OrdinalIgnoreCase);
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameService> _log;
    private readonly IServiceProvider _serviceProvider;

    public GameService(IServiceProvider serviceProvider, IHubContext<GameHub> hub, ILogger<GameService> log, IHostApplicationLifetime? lifetime = null)
    {
        _serviceProvider = serviceProvider;
        _hub = hub;
        _log = log;
        if (lifetime != null)
        {
            _onApplicationStoppingListener = lifetime.ApplicationStopping.Register(OnApplicationClosing);
        }
    }

    /// <summary>
    /// Raised with a join code whenever that game changed. Blazor components subscribe
    /// and re-render; the push reaches the browser over the circuit's own WebSocket.
    /// </summary>
    public event Action<string>? GameChanged;

    public LiveGame? Find(string? joinCode)
    {
        if (joinCode != null)
            joinCode = JoinCode.Normalize(joinCode);
        return joinCode == null || joinCode.Length == 0 ? null : _games.GetValueOrDefault(joinCode);
    }

    private LiveGame? FindWellFormed(string? joinCode)
    {
        return joinCode == null || joinCode.Length == 0 ? null : _games.GetValueOrDefault(joinCode);
    }

    public IReadOnlyList<LiveGame> ActiveGames => _games.Values.ToList();

    /// <summary>Creates a game and seats the host as its first player.</summary>
    public (LiveGame Game, GamePlayer Host) CreateGame(string hostName, QuestionSet deck, ApplicationUser? hostUser)
    {
        string code = NextFreeCode();
        string hostId = hostUser?.Id ?? Guid.NewGuid().ToString("D");

        LiveGame game = new()
        {
            JoinCode = code,
            Deck = deck,
            HostPlayerId = hostId
        };

        GamePlayer host = new()
        {
            IntlGameId = hostId,
            Name = CleanName(hostName, "Host"),
            User = hostUser,
            UserId = hostUser?.Id,
            JoinedTime = DateTime.UtcNow,
            Results = [ ]
        };

        game.Players[hostId] = host;
        _games[code] = game;

        _log.LogInformation("Game {JoinCode} created by {HostName} on deck {Deck}", code, host.Name, deck.Name);
        return (game, host);
    }

    /// <summary>
    /// Seats a new player. Returns null when the code is unknown; returns a failure
    /// reason when the game has already started.
    /// </summary>
    public (GamePlayer? Player, string? Error) Join(string joinCode, string nickname, ApplicationUser? user)
    {
        LiveGame? game = Find(joinCode);
        if (game is null) return (null, "No game found with that code.");

        lock (game.Gate)
        {
            if (game.Phase != GamePhase.Lobby)
            {
                return (null, "That game has already started.");
            }

            string name = CleanName(nickname, "Player");

            // Nicknames are how players recognise themselves on the scoreboard, so keep them unique.
            if (game.Players.Values.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return (null, $"The name \"{name}\" is already taken in this game.");
            }

            GamePlayer player = new()
            {
                IntlGameId = user?.Id ?? Guid.NewGuid().ToString("N"),
                Name = name,
                UserId = user?.Id,
                User = user,
                JoinedTime = DateTime.UtcNow,
                Results = [ ]
            };

            game.Players[player.IntlGameId] = player;
            _log.LogInformation("{Name} joined game {JoinCode}", player.Name, game.JoinCode);

            NotifyChanged(game.JoinCode);
            return (player, null);
        }
    }

    /// <summary>Starts the game. Only the host may do this, and only from the lobby.</summary>
    public string? Start(string joinCode)
    {
        LiveGame? game = FindWellFormed(joinCode);
        if (game is null) return "No game found with that code.";

        lock (game.Gate)
        {
            if (game.Phase != GamePhase.Lobby) return "The game has already started.";
            if (game.Deck.Questions.Count == 0) return "That deck has no questions.";
            
            game.StartedUtc = DateTime.UtcNow;

            BeginQuestion(game, 0, DateTime.UtcNow);
        }

        NotifyChanged(game.JoinCode);
        return null;
    }

    /// <summary>
    /// Records an answer. Rejected if the game is not on that question, or if this
    /// player already answered it.
    /// </summary>
    public string? SubmitMultipleChoiceAnswer(string joinCode, string playerId, Question question, int optionIndex)
    {
        LiveGame? game = FindWellFormed(joinCode);
        if (game is null) return "No game found with that code.";

        lock (game.Gate)
        {
            int questionIndex = question.Index;
            if (game.Phase != GamePhase.Question) return "Answers are closed.";
            if (game.CurrentIndex != questionIndex) return "That question is no longer on screen.";
            if (!game.Players.TryGetValue(playerId, out GamePlayer? player)) return "You are not in this game.";
            if (player.HasAnswered(questionIndex)) return "You already answered this question.";

            if (question is not MultipleChoiceQuestion mcq) return "Unsupported question type.";

            MultipleChoiceAnswer? chosen = mcq.Answers.FirstOrDefault(o => o.Index == optionIndex);
            if (chosen is null) return "That is not one of the options.";

            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - (game.QuestionStartedUtc ?? now);
            int points = PointsFor(question, chosen.IsCorrect, elapsed, game.Deck.SecondsPerQuestion);

            GamePlayerQuestionResult result = new GamePlayerQuestionResult
            {
                Question = question,
                Score = points,
                Response = chosen.Id.ToString(CultureInfo.InvariantCulture),
                AnswerTime = elapsed
            };

            player.Answers[questionIndex] = result;
            player.Results.Add(result);

            player.Score += points;

            // Nobody left to wait for -- close early rather than stare at a dead timer.
            if (game.Players.Values.All(p => p.HasAnswered(questionIndex)))
            {
                CloseQuestion(game, now);
            }
        }

        NotifyChanged(game.JoinCode);
        return null;
    }

    /// <summary>
    /// Points for one answer: full value for an instant correct answer, falling linearly
    /// to half value at the deadline. Wrong answers score nothing at any speed.
    /// </summary>
    public static int PointsFor(Question question, bool isCorrect, TimeSpan elapsed, double secondsPerQuestion)
    {
        if (!isCorrect) return 0;

        double limit = Math.Max(1d, secondsPerQuestion);
        double fraction = Math.Clamp(elapsed.TotalSeconds / limit, 0, 1d);
        double multiplier = 1d - (1d - SlowestCorrectMultiplier) * fraction;

        return (int)Math.Round(question.Score * multiplier);
    }

    /// <summary>
    /// Advances any game whose phase has expired. Driven by <see cref="GameLoopService"/>.
    /// </summary>
    public void Tick(DateTime utcNow)
    {
        foreach (LiveGame game in _games.Values)
        {
            bool changed = false;

            lock (game.Gate)
            {
                if (game.PhaseEndsUtc is null || utcNow < game.PhaseEndsUtc) continue;

                switch (game.Phase)
                {
                    case GamePhase.Question:
                        CloseQuestion(game, utcNow);
                        changed = true;
                        break;

                    case GamePhase.Reveal:
                        int next = game.CurrentIndex + 1;
                        if (next < game.Deck.Questions.Count)
                        {
                            BeginQuestion(game, next, utcNow);
                        }
                        else
                        {
                            game.Phase = GamePhase.Completed;
                            game.PhaseEndsUtc = null;
                            _log.LogInformation("Game {JoinCode} completed fully", game.JoinCode);
                            StartEndingGame(game.JoinCode);
                        }
                        changed = true;
                        break;
                }
            }

            if (changed) NotifyChanged(game.JoinCode);
        }
    }

    /// <summary>Standings, highest first, with each player's result on the last question played.</summary>
    public IReadOnlyList<ScoreEntry> Scoreboard(LiveGame game)
    {
        int lastIndex = game.CurrentIndex;

        return game.Contestants
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select((p, i) =>
            {
                p.Answers.TryGetValue(lastIndex, out GamePlayerQuestionResult? last);
                return new ScoreEntry(
                    Rank: i + 1,
                    PlayerId: p.IntlGameId!,
                    Name: p.Name,
                    Score: p.Score,
                    LastPoints: last?.Score ?? 0,
                    AnsweredLast: last is not null,
                    LastWasCorrect: last?.IsCorrect ?? false);
            })
            .ToList();
    }

    /// <summary>Drops games nobody could still be playing, so memory does not grow forever.</summary>
    public int PruneStale(DateTime utcNow, TimeSpan maxAge)
    {
        int removed = 0;
        foreach (LiveGame game in _games.Values)
        {
            bool expired = utcNow - game.CreatedUtc > maxAge;
            if (!expired) continue;

            if (_games.TryRemove(game.JoinCode, out LiveGame? g) && g == game)
            {
                removed++;
                Interlocked.Increment(ref _gamesEnding);
                Task.Run(async () =>
                {
                    try
                    {
                        await OnGameRemoved(game);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Failed to apply game removal.");
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _gamesEnding);
                    }
                });
                _log.LogInformation("Pruned stale game {JoinCode}", game.JoinCode);
            }
        }
        return removed;
    }

    public void LeaveGame(string code, GamePlayer player)
    {
        LiveGame? game = Find(code);
        if (game == null)
            return;

        _log.LogInformation("Player {player} left game {code}.", player.Name, game.JoinCode);
        DateTime now = DateTime.UtcNow;
        if (now > game.EndedUtc)
        {
            return;
        }

        player.LeftTime = now;
        NotifyChanged(game.JoinCode);
    }

    public void StartEndingGame(string code)
    {
        if (!_games.TryRemove(code, out LiveGame? game))
            return;

        Interlocked.Increment(ref _gamesEnding);
        Task.Run(async () =>
        {
            try
            {
                await OnGameRemoved(game);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error ending game {code}.", game.JoinCode);
            }
            finally
            {
                Interlocked.Decrement(ref _gamesEnding);
            }
        });
    }

    public async Task<bool> EndGameAsync(string code)
    {
        if (!_games.TryRemove(code, out LiveGame? game))
            return false;

        Interlocked.Increment(ref _gamesEnding);
        try
        {
            await OnGameRemoved(game);
        }
        finally
        {
            Interlocked.Decrement(ref _gamesEnding);
        }
        return true;
    }

    internal async Task OnGameRemoved(LiveGame game)
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        DateTime endTime = DateTime.UtcNow;

        game.EndedUtc = endTime;
        if (game.Phase != GamePhase.Completed)
        {
            game.Phase = GamePhase.Ended;
        }

        List<GamePlayer> contestants = game.Contestants.ToList();

        QuestionSet? set = await dbContext.QuestionSets
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == game.Deck.Id && x.Revision == game.Deck.Revision);

        foreach (GamePlayer player in contestants)
        {
            player.LeftTime ??= endTime;
            // it tries to re-add the user to the database since its not from the same DbContext
            if (player.User != null)
                dbContext.Entry(player.User).State = EntityState.Unchanged;

            if (set == null)
                continue;

            for (int i = player.Results.Count - 1; i >= 0; i--)
            {
                GamePlayerQuestionResult result = player.Results[i];
                Question? q = set.Questions.FirstOrDefault(x => x.Id == result.Question.Id);
                if (q == null)
                    player.Results.RemoveAt(i);
                else
                    result.Question = q;
            }
        }

        dbContext.Set<Game>().Add(new Game
        {
            QuestionSet = set ?? game.Deck,
            CompletedTime = DateTime.UtcNow,
            StartedTime = game.StartedUtc,
            UserId = game.HostPlayerId,
            User = null!,
            Players = contestants
        });

        await dbContext.SaveChangesAsync();

        NotifyChanged(game.JoinCode);
        _log.LogInformation("Game {code} ended.", game.JoinCode);
    }

    // ---- internals; all callers already hold game.Gate ----

    private void BeginQuestion(LiveGame game, int index, DateTime now)
    {
        game.CurrentIndex = index;
        game.Phase = GamePhase.Question;
        game.QuestionStartedUtc = now;
        game.PhaseEndsUtc = now.AddSeconds(game.Deck.SecondsPerQuestion);
    }

    private void CloseQuestion(LiveGame game, DateTime now)
    {
        game.Phase = GamePhase.Reveal;
        game.PhaseEndsUtc = now.AddSeconds(RevealSeconds);
    }

    private string NextFreeCode()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            string code = JoinCode.Next();
            if (!_games.ContainsKey(code)) return code;
        }
        throw new InvalidOperationException("Could not allocate a free join code.");
    }

    private static string CleanName(string? raw, string fallback)
    {
        string name = (raw ?? string.Empty).Trim();
        if (name.Length == 0) return fallback;
        return name.Length > GamePlayer.MaxNameLength ? name[..GamePlayer.MaxNameLength] : name;
    }

    /// <summary>
    /// Fans a change out twice: to in-process Blazor components, and to the SignalR
    /// group for this join code so any non-Blazor client sees the same events.
    /// </summary>
    private void NotifyChanged(string joinCode)
    {
        try
        {
            GameChanged?.Invoke(joinCode);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error thrown by GameChanged listener.");
        }

        // Fire and forget: a slow or dead hub client must never stall the game loop.
        _ = _hub.Clients.Group(GameHub.GroupName(joinCode))
                .SendAsync("GameChanged", joinCode)
                .ContinueWith(t => _log.LogWarning(t.Exception, "Hub broadcast failed for {JoinCode}", joinCode),
                              TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnApplicationClosing()
    {
        Task.Run(async () =>
        {
            foreach (LiveGame game in _games.Values.ToList())
            {
                await EndGameAsync(game.JoinCode);
            }

            // wait for all running games to save
            DateTime startTime = DateTime.UtcNow;
            while (_gamesEnding > 0 && DateTime.UtcNow - startTime < TimeSpan.FromSeconds(10))
            {
                await Task.Delay(200);
            }
        }).Wait();
    }

    public async ValueTask DisposeAsync()
    {
        await _onApplicationStoppingListener.DisposeAsync();
    }
}
