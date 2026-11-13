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
    Completed
}

/// <summary>One selectable answer on a multiple-choice question.</summary>
public sealed class AnswerOption
{
    public required int Index { get; init; }
    public required string Text { get; init; }
    public required bool IsCorrect { get; init; }
}

/// <summary>A question as the game loop sees it, independent of how it was authored.</summary>
public sealed class QuestionView
{
    public required int Index { get; init; }
    public required string Text { get; init; }
    public required IReadOnlyList<AnswerOption> Options { get; init; }

    /// <summary>Points for a fully-correct, instant answer.</summary>
    public double MaxScore { get; init; } = 1000;

    public int CorrectIndex => Options.First(o => o.IsCorrect).Index;
}

/// <summary>A deck of questions that can be hosted as a game.</summary>
public sealed class QuestionDeck
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int SecondsPerQuestion { get; init; }
    public required IReadOnlyList<QuestionView> Questions { get; init; }
}

/// <summary>What a player submitted for a single question.</summary>
public sealed class SubmittedAnswer
{
    public required int OptionIndex { get; init; }
    public required bool IsCorrect { get; init; }
    public required double Points { get; init; }
    public required TimeSpan Elapsed { get; init; }
}

/// <summary>A participant in a live game. A guest is simply a player with no UserId.</summary>
public sealed class LivePlayer
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Set only when the player was signed in when they joined.</summary>
    public string? UserId { get; init; }

    public bool IsHost { get; init; }
    public double Score { get; set; }

    /// <summary>Keyed by question index.</summary>
    public ConcurrentDictionary<int, SubmittedAnswer> Answers { get; } = new();

    public bool HasAnswered(int questionIndex) => Answers.ContainsKey(questionIndex);
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
    public required QuestionDeck Deck { get; init; }
    public required string HostPlayerId { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Guards every mutation of this game's phase, index and scores.</summary>
    public object Gate { get; } = new();

    public GamePhase Phase { get; set; } = GamePhase.Lobby;

    /// <summary>-1 until the first question starts.</summary>
    public int CurrentIndex { get; set; } = -1;

    /// <summary>When the current question was pushed. The basis for all scoring.</summary>
    public DateTime? QuestionStartedUtc { get; set; }

    /// <summary>When the current phase should end. Clients count down to this.</summary>
    public DateTime? PhaseEndsUtc { get; set; }

    public ConcurrentDictionary<string, LivePlayer> Players { get; } = new();

    public QuestionView? CurrentQuestion =>
        CurrentIndex >= 0 && CurrentIndex < Deck.Questions.Count ? Deck.Questions[CurrentIndex] : null;

    public int QuestionNumber => CurrentIndex + 1;
    public int QuestionCount => Deck.Questions.Count;

    /// <summary>Players, excluding the host when the host is not playing along.</summary>
    public IEnumerable<LivePlayer> Contestants => Players.Values;

    public int SecondsLeft(DateTime utcNow) =>
        PhaseEndsUtc is null ? 0 : Math.Max(0, (int)Math.Ceiling((PhaseEndsUtc.Value - utcNow).TotalSeconds));
}
