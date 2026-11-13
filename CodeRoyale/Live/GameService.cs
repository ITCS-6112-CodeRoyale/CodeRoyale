using System.Collections.Concurrent;
using CodeRoyale.Hubs;
using Microsoft.AspNetCore.SignalR;

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
public sealed class GameService
{
    /// <summary>How long the correct answer and standings stay on screen.</summary>
    public const int RevealSeconds = 5;

    /// <summary>A correct answer at the buzzer is worth this fraction of an instant one.</summary>
    private const double SlowestCorrectMultiplier = 0.5;

    private readonly ConcurrentDictionary<string, LiveGame> _games = new(StringComparer.OrdinalIgnoreCase);
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameService> _log;

    public GameService(IHubContext<GameHub> hub, ILogger<GameService> log)
    {
        _hub = hub;
        _log = log;
    }

    /// <summary>
    /// Raised with a join code whenever that game changed. Blazor components subscribe
    /// and re-render; the push reaches the browser over the circuit's own WebSocket.
    /// </summary>
    public event Action<string>? GameChanged;

    public LiveGame? Find(string? joinCode)
    {
        string code = JoinCode.Normalise(joinCode);
        return code.Length == 0 ? null : _games.GetValueOrDefault(code);
    }

    public IReadOnlyList<LiveGame> ActiveGames => _games.Values.ToList();

    /// <summary>Creates a game and seats the host as its first player.</summary>
    public (LiveGame Game, LivePlayer Host) CreateGame(string hostName, QuestionDeck deck, string? userId = null)
    {
        string code = NextFreeCode();
        string hostId = Guid.NewGuid().ToString("N");

        LiveGame game = new()
        {
            JoinCode = code,
            Deck = deck,
            HostPlayerId = hostId
        };

        LivePlayer host = new()
        {
            Id = hostId,
            Name = CleanName(hostName, "Host"),
            UserId = userId,
            IsHost = true
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
    public (LivePlayer? Player, string? Error) Join(string joinCode, string nickname, string? userId = null)
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

            LivePlayer player = new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                UserId = userId
            };

            game.Players[player.Id] = player;
            _log.LogInformation("{Name} joined game {JoinCode}", player.Name, game.JoinCode);

            NotifyChanged(game.JoinCode);
            return (player, null);
        }
    }

    /// <summary>Starts the game. Only the host may do this, and only from the lobby.</summary>
    public string? Start(string joinCode, string playerId)
    {
        LiveGame? game = Find(joinCode);
        if (game is null) return "No game found with that code.";

        lock (game.Gate)
        {
            if (game.HostPlayerId != playerId) return "Only the host can start the game.";
            if (game.Phase != GamePhase.Lobby) return "The game has already started.";
            if (game.Deck.Questions.Count == 0) return "That deck has no questions.";

            BeginQuestion(game, 0, DateTime.UtcNow);
        }

        NotifyChanged(game.JoinCode);
        return null;
    }

    /// <summary>
    /// Records an answer. Rejected if the game is not on that question, or if this
    /// player already answered it.
    /// </summary>
    public string? SubmitAnswer(string joinCode, string playerId, int questionIndex, int optionIndex)
    {
        LiveGame? game = Find(joinCode);
        if (game is null) return "No game found with that code.";

        lock (game.Gate)
        {
            if (game.Phase != GamePhase.Question) return "Answers are closed.";
            if (game.CurrentIndex != questionIndex) return "That question is no longer on screen.";
            if (!game.Players.TryGetValue(playerId, out LivePlayer? player)) return "You are not in this game.";
            if (player.HasAnswered(questionIndex)) return "You already answered this question.";

            QuestionView? question = game.CurrentQuestion;
            if (question is null) return "No question is active.";

            AnswerOption? chosen = question.Options.FirstOrDefault(o => o.Index == optionIndex);
            if (chosen is null) return "That is not one of the options.";

            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - (game.QuestionStartedUtc ?? now);
            double points = PointsFor(question, chosen.IsCorrect, elapsed, game.Deck.SecondsPerQuestion);

            player.Answers[questionIndex] = new SubmittedAnswer
            {
                OptionIndex = optionIndex,
                IsCorrect = chosen.IsCorrect,
                Points = points,
                Elapsed = elapsed
            };
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
    public static double PointsFor(QuestionView question, bool isCorrect, TimeSpan elapsed, int secondsPerQuestion)
    {
        if (!isCorrect) return 0;

        double limit = Math.Max(1, secondsPerQuestion);
        double fraction = Math.Clamp(elapsed.TotalSeconds / limit, 0, 1);
        double multiplier = 1 - (1 - SlowestCorrectMultiplier) * fraction;

        return Math.Round(question.MaxScore * multiplier);
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
                            _log.LogInformation("Game {JoinCode} completed", game.JoinCode);
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
                p.Answers.TryGetValue(lastIndex, out SubmittedAnswer? last);
                return new ScoreEntry(
                    Rank: i + 1,
                    PlayerId: p.Id,
                    Name: p.Name,
                    Score: p.Score,
                    LastPoints: last?.Points ?? 0,
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

            if (_games.TryRemove(game.JoinCode, out _))
            {
                removed++;
                _log.LogInformation("Pruned stale game {JoinCode}", game.JoinCode);
            }
        }
        return removed;
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
        return name.Length > 32 ? name[..32] : name;
    }

    /// <summary>
    /// Fans a change out twice: to in-process Blazor components, and to the SignalR
    /// group for this join code so any non-Blazor client sees the same events.
    /// </summary>
    private void NotifyChanged(string joinCode)
    {
        GameChanged?.Invoke(joinCode);

        // Fire and forget: a slow or dead hub client must never stall the game loop.
        _ = _hub.Clients.Group(GameHub.GroupName(joinCode))
                .SendAsync("GameChanged", joinCode)
                .ContinueWith(t => _log.LogWarning(t.Exception, "Hub broadcast failed for {JoinCode}", joinCode),
                              TaskContinuationOptions.OnlyOnFaulted);
    }
}
