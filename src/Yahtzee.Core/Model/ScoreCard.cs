using Yahtzee.Core.Rules;

namespace Yahtzee.Core.Model;

/// <summary>
/// One player's scorecard: the 13 category boxes, each empty or holding a recorded score.
/// </summary>
public sealed class ScoreCard
{
    private readonly Dictionary<Category, int> _scores = [];

    /// <summary>Creates an empty scorecard.</summary>
    public ScoreCard()
    {
    }

    /// <summary>
    /// Creates a scorecard restored from previously recorded scores, for example when loading a saved game.
    /// </summary>
    /// <param name="scores">Recorded scores; categories that are absent are empty.</param>
    /// <exception cref="ArgumentException">A score is negative.</exception>
    public ScoreCard(IReadOnlyDictionary<Category, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        foreach (var (category, score) in scores)
        {
            if (!Enum.IsDefined(category))
                throw new ArgumentException($"Unknown category {category}.", nameof(scores));
            if (score < 0)
                throw new ArgumentException($"Score for {category} cannot be negative.", nameof(scores));
            _scores[category] = score;
        }
    }

    /// <summary>The recorded scores, keyed by category. Empty categories are absent.</summary>
    public IReadOnlyDictionary<Category, int> Scores => _scores;

    /// <summary>The categories that have not been scored yet, in scorecard order.</summary>
    public IEnumerable<Category> AvailableCategories =>
        CategoryExtensions.All.Where(category => !_scores.ContainsKey(category));

    /// <summary>Whether all 13 categories have been scored.</summary>
    public bool IsComplete => _scores.Count == CategoryExtensions.All.Count;

    /// <summary>The total score: the sum of all recorded boxes.</summary>
    public int Total => _scores.Values.Sum();

    /// <summary>Whether <paramref name="category"/> has already been scored.</summary>
    public bool IsFilled(Category category) => _scores.ContainsKey(category);

    /// <summary>The score recorded in <paramref name="category"/>, or <see langword="null"/> if empty.</summary>
    public int? GetScore(Category category) => _scores.TryGetValue(category, out int score) ? score : null;

    /// <summary>
    /// Records the score for <paramref name="dice"/> in <paramref name="category"/>.
    /// Dice that do not match the category score 0.
    /// </summary>
    /// <returns>The score that was recorded.</returns>
    /// <exception cref="InvalidOperationException">The category has already been scored.</exception>
    public int Record(Category category, IReadOnlyList<int> dice)
    {
        if (IsFilled(category))
            throw new InvalidOperationException($"{category.DisplayName()} has already been scored.");

        int score = Scorer.Score(category, dice);
        _scores[category] = score;
        return score;
    }

    /// <summary>Previews the score <paramref name="dice"/> would earn in each available category.</summary>
    public IReadOnlyDictionary<Category, int> Preview(IReadOnlyList<int> dice) =>
        Scorer.Preview(dice, AvailableCategories);
}
