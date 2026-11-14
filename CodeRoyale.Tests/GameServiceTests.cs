using CodeRoyale.Data.Games;
using CodeRoyale.Data.Questions;
using CodeRoyale.Hubs;
using CodeRoyale.Live;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.ComponentModel.Design;

namespace CodeRoyale.Tests;

/// <summary>
/// The live game state machine. These are the rules that stop a player gaming the
/// scoreboard: answering twice, answering a closed question, or joining mid-game.
/// </summary>
public class GameServiceTests
{
    private static GameService NewService() =>
        new(new ServiceContainer(), Substitute.For<IHubContext<GameHub>>(), NullLogger<GameService>.Instance);

    private static QuestionSet Deck(int questions = 3, int seconds = 20) => new()
    {
        Id = 0,
        Name = "Test deck",
        SecondsPerQuestion = seconds,
        Revision = 0,
        Language = new Data.CodeLanguage { LanguageCode = "ts", DisplayName = "Test Language" },
        Questions = Enumerable.Range(0, questions).Select<int, Question>(i => new MultipleChoiceQuestion
        {
            Index = i,
            Text = $"Question {i}",
            Answers = new List<MultipleChoiceAnswer>
            {
                new() { Index = 0, Text = "right", IsCorrect = true },
                new() { Index = 1, Text = "wrong", IsCorrect = false }
            }
        }).ToList()
    };

    [Fact]
    public void Created_game_starts_in_the_lobby_with_the_host_seated()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Rahul", Deck(), null);

        Assert.Equal(GamePhase.Lobby, game.Phase);
        Assert.Equal(host.IntlGameId, game.HostPlayerId);
        Assert.Equal("Rahul", host.Name);
        Assert.Single(game.Players);
        Assert.Equal(-1, game.CurrentIndex);
    }

    [Fact]
    public void Game_is_findable_by_code_case_insensitively()
    {
        GameService svc = NewService();
        (LiveGame game, _) = svc.CreateGame("Host", Deck(), null);

        Assert.NotNull(svc.Find(game.JoinCode));
        Assert.NotNull(svc.Find(game.JoinCode.ToLowerInvariant()));
        Assert.Null(svc.Find("ZZZZZZ"));
    }

    [Fact]
    public void Guests_can_join_without_a_user_id()
    {
        GameService svc = NewService();
        (LiveGame game, _) = svc.CreateGame("Host", Deck(), null);

        (GamePlayer? guest, string? error) = svc.Join(game.JoinCode, "Ada", null);

        Assert.Null(error);
        Assert.NotNull(guest);
        Assert.Null(guest!.UserId);
        Assert.NotEqual(guest.IntlGameId, game.HostPlayerId);
        Assert.Equal(2, game.Players.Count);
    }

    [Fact]
    public void Duplicate_nicknames_are_rejected()
    {
        GameService svc = NewService();
        (LiveGame game, _) = svc.CreateGame("Host", Deck(), null);
        svc.Join(game.JoinCode, "Ada", null);

        (GamePlayer? second, string? error) = svc.Join(game.JoinCode, "ada", null);

        Assert.Null(second);
        Assert.Contains("already taken", error);
    }

    [Fact]
    public void Joining_an_unknown_code_fails_cleanly()
    {
        GameService svc = NewService();
        (GamePlayer? player, string? error) = svc.Join("ZZZZZZ", "Ada", null);

        Assert.Null(player);
        Assert.NotNull(error);
    }

    [Fact]
    public void Joining_after_the_game_starts_is_rejected()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        svc.Start(game.JoinCode);

        (GamePlayer? late, string? error) = svc.Join(game.JoinCode, "Latecomer", null);

        Assert.Null(late);
        Assert.Contains("already started", error);
    }

    [Fact]
    public void Answering_twice_is_rejected_and_does_not_double_score()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);
        svc.Start(game.JoinCode);

        Assert.Null(svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0));
        double afterFirst = ada.Score;
        Assert.True(afterFirst > 0);

        string? second = svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0);

        Assert.Contains("already answered", second);
        Assert.Equal(afterFirst, ada.Score);
    }

    [Fact]
    public void Answering_a_stale_question_index_is_rejected()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);
        svc.Start(game.JoinCode);

        string? error = svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[1], 0);

        Assert.Contains("no longer on screen", error);
        Assert.Equal(0, ada.Score);
    }

    [Fact]
    public void Answering_before_the_game_starts_is_rejected()
    {
        GameService svc = NewService();
        (LiveGame game, _) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);

        string? error = svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0);

        Assert.Contains("closed", error);
    }

    [Fact]
    public void A_wrong_answer_is_recorded_but_scores_zero()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);
        svc.Start(game.JoinCode);

        svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], optionIndex: 1);

        Assert.True(ada.HasAnswered(0));
        Assert.False(ada.Answers[0].IsCorrect);
        Assert.Equal(0, ada.Score);
    }

    [Fact]
    public void Question_closes_early_once_everyone_has_answered()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);
        svc.Start(game.JoinCode);

        svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0);
        Assert.Equal(GamePhase.Question, game.Phase);

        svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0);

        // Nobody left to wait for, so no one stares at a dead timer.
        Assert.Equal(GamePhase.Reveal, game.Phase);
    }

    [Fact]
    public void Tick_advances_question_then_reveal_then_completes()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(questions: 2, seconds: 20), null);
        svc.Start(game.JoinCode);

        DateTime t0 = DateTime.UtcNow;

        // Mid-question: nothing should move.
        svc.Tick(t0.AddSeconds(5));
        Assert.Equal(GamePhase.Question, game.Phase);
        Assert.Equal(0, game.CurrentIndex);

        // Past the deadline: the question closes into reveal.
        svc.Tick(t0.AddSeconds(21));
        Assert.Equal(GamePhase.Reveal, game.Phase);

        // Past the reveal window: on to question 2.
        svc.Tick(t0.AddSeconds(21 + GameService.RevealSeconds + 1));
        Assert.Equal(GamePhase.Question, game.Phase);
        Assert.Equal(1, game.CurrentIndex);

        // Last question closes, then the game is over rather than wrapping around.
        svc.Tick(t0.AddSeconds(100));
        Assert.Equal(GamePhase.Reveal, game.Phase);
        svc.Tick(t0.AddSeconds(200));
        Assert.Equal(GamePhase.Completed, game.Phase);

        // Completed is terminal.
        svc.Tick(t0.AddSeconds(300));
        Assert.Equal(GamePhase.Completed, game.Phase);
        Assert.Equal(1, game.CurrentIndex);
    }

    [Fact]
    public void Scoreboard_ranks_by_score_descending()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("Host", Deck(), null);
        (GamePlayer? ada, _) = svc.Join(game.JoinCode, "Ada", null);
        (GamePlayer? bob, _) = svc.Join(game.JoinCode, "Bob", null);

        svc.Start(game.JoinCode);
        svc.SubmitMultipleChoiceAnswer(game.JoinCode, ada!.IntlGameId!, game.Deck.Questions[0], 0);   // correct
        svc.SubmitMultipleChoiceAnswer(game.JoinCode, bob!.IntlGameId!, game.Deck.Questions[0], 1);   // wrong

        IReadOnlyList<ScoreEntry> board = svc.Scoreboard(game);

        Assert.Equal(3, board.Count);
        Assert.Equal("Ada", board[0].Name);
        Assert.Equal(1, board[0].Rank);
        Assert.True(board[0].Score > board[1].Score);
        Assert.True(board[0].LastWasCorrect);

        ScoreEntry bobRow = board.First(e => e.Name == "Bob");
        Assert.True(bobRow.AnsweredLast);
        Assert.False(bobRow.LastWasCorrect);

        ScoreEntry hostRow = board.First(e => e.Name == "Host");
        Assert.False(hostRow.AnsweredLast);
    }

    [Fact]
    public void Stale_games_are_pruned_and_fresh_ones_kept()
    {
        GameService svc = NewService();
        (LiveGame game, _) = svc.CreateGame("Host", Deck(), null);

        Assert.Equal(0, svc.PruneStale(DateTime.UtcNow, TimeSpan.FromHours(4)));
        Assert.NotNull(svc.Find(game.JoinCode));

        Assert.Equal(1, svc.PruneStale(DateTime.UtcNow.AddHours(5), TimeSpan.FromHours(4)));
        Assert.Null(svc.Find(game.JoinCode));
    }

    [Fact]
    public void Blank_nicknames_fall_back_rather_than_creating_an_unnamed_player()
    {
        GameService svc = NewService();
        (LiveGame game, GamePlayer host) = svc.CreateGame("   ", Deck(), null);

        Assert.Equal("Host", host.Name);

        (GamePlayer? guest, _) = svc.Join(game.JoinCode, "   ", null);
        Assert.Equal("Player", guest!.Name);
    }
}
