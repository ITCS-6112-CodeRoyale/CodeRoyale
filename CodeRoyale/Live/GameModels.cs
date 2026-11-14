using CodeRoyale.Data.Games;
using CodeRoyale.Data.Questions;
using System.Collections.Concurrent;

namespace CodeRoyale.Live;

/// <summary>
/// Phase of a live game. The server owns this; clients only render it.
/// </summary>
public enum GamePhase
{
    /// <summary>Players are joining. Only the host can leave this phase.</summary>
    Lobby,

    /// <summary>A question is on screen and answers are being accepted.</summary>
    Question,

    /// <summary>The question closed; the correct answer and standings are shown.</summary>
    Reveal,

    /// <summary>Every question has been played.</summary>
    Completed,

    /// <summary>The game ended early because the host closed the window.</summary>
    Ended
}

/// <summary>One row of the scoreboard.</summary>
public sealed record ScoreEntry(
    int Rank,
    string PlayerId,
    string Name,
    double Score,
    double LastPoints,
    bool AnsweredLast,
    bool LastWasCorrect);

/// <summary>
/// A game in progress. Lives in memory only: the database stores authoring and
/// history, never per-tick state.
/// </summary>
public sealed class LiveGame
{
    public required string JoinCode { get; init; }
    public required QuestionSet Deck { get; init; }
    public required string HostPlayerId { get; init; }
    public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    public DateTime StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }

    /// <summary>Guards every mutation of this game's phase, index and scores.</summary>
    public Lock Gate { get; } = new();

    public GamePhase Phase { get; set; } = GamePhase.Lobby;

    /// <summary>-1 until the first question starts.</summary>
    public int CurrentIndex { get; set; } = -1;

    /// <summary>When the current question was pushed. The basis for all scoring.</summary>
    public DateTime? QuestionStartedUtc { get; set; }

    /// <summary>When the current phase should end. Clients count down to this.</summary>
    public DateTime? PhaseEndsUtc { get; set; }

    public ConcurrentDictionary<string, GamePlayer> Players { get; } = new();

    public Question? CurrentQuestion =>
        CurrentIndex >= 0 && CurrentIndex < Deck.Questions.Count ? Deck.Questions[CurrentIndex] : null;

    public int QuestionNumber => CurrentIndex + 1;
    public int QuestionCount => Deck.Questions.Count;

    /// <summary>Players, excluding the host when the host is not playing along.</summary>
    public IEnumerable<GamePlayer> Contestants => Players.Values;

    public int SecondsLeft(DateTime utcNow) =>
        PhaseEndsUtc is null ? 0 : Math.Max(0, (int)Math.Ceiling((PhaseEndsUtc.Value - utcNow).TotalSeconds));
}
