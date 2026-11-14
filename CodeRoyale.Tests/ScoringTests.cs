using CodeRoyale.Data.Questions;
using CodeRoyale.Live;

namespace CodeRoyale.Tests;

/// <summary>
/// The scoring formula is the competitive core of the game: if it drifts, games are
/// unfair and nothing throws. These pin the curve down at both ends and in the middle.
/// </summary>
public class ScoringTests
{
    private static Question Question(double maxScore = 1000) => new MultipleChoiceQuestion()
    {
        Index = 0,
        Text = "q",
        Score = maxScore,
        Answers = new List<MultipleChoiceAnswer>
        {
            new() { Index = 0, Text = "right", IsCorrect = true },
            new() { Index = 1, Text = "wrong", IsCorrect = false }
        }
    };

    [Fact]
    public void Instant_correct_answer_scores_full_marks()
    {
        double points = GameService.PointsFor(Question(), isCorrect: true, TimeSpan.Zero, secondsPerQuestion: 20);
        Assert.Equal(1000, points);
    }

    [Fact]
    public void Correct_answer_at_the_buzzer_scores_half_marks()
    {
        double points = GameService.PointsFor(Question(), isCorrect: true, TimeSpan.FromSeconds(20), secondsPerQuestion: 20);
        Assert.Equal(500, points);
    }

    [Fact]
    public void Correct_answer_halfway_scores_three_quarters()
    {
        // Linear from 1.0 down to 0.5, so the midpoint is 0.75.
        double points = GameService.PointsFor(Question(), isCorrect: true, TimeSpan.FromSeconds(10), secondsPerQuestion: 20);
        Assert.Equal(750, points);
    }

    [Fact]
    public void Wrong_answer_scores_nothing_however_fast()
    {
        Assert.Equal(0, GameService.PointsFor(Question(), isCorrect: false, TimeSpan.Zero, 20));
        Assert.Equal(0, GameService.PointsFor(Question(), isCorrect: false, TimeSpan.FromSeconds(5), 20));
    }

    [Fact]
    public void Answer_arriving_after_the_deadline_is_clamped_not_negative()
    {
        // A late answer should never score below the slowest-correct floor.
        double points = GameService.PointsFor(Question(), isCorrect: true, TimeSpan.FromSeconds(999), secondsPerQuestion: 20);
        Assert.Equal(500, points);
    }

    [Fact]
    public void Faster_correct_answers_always_beat_slower_ones()
    {
        double fast = GameService.PointsFor(Question(), true, TimeSpan.FromSeconds(2), 20);
        double mid = GameService.PointsFor(Question(), true, TimeSpan.FromSeconds(10), 20);
        double slow = GameService.PointsFor(Question(), true, TimeSpan.FromSeconds(18), 20);

        Assert.True(fast > mid, $"expected {fast} > {mid}");
        Assert.True(mid > slow, $"expected {mid} > {slow}");
    }

    [Fact]
    public void Per_question_max_score_is_respected()
    {
        double points = GameService.PointsFor(Question(maxScore: 500), isCorrect: true, TimeSpan.Zero, 20);
        Assert.Equal(500, points);
    }

    [Fact]
    public void A_zero_second_limit_does_not_divide_by_zero()
    {
        double points = GameService.PointsFor(Question(), isCorrect: true, TimeSpan.FromSeconds(1), secondsPerQuestion: 0);
        Assert.InRange(points, 0, 1000);
    }
}
