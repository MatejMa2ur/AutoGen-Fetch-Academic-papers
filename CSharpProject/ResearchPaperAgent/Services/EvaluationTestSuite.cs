namespace ResearchPaperAgent.Services;

using ResearchPaperAgent.Models;

public static class EvaluationTestSuite
{
    /// <summary>
    /// Get essential test queries for evaluation
    /// </summary>
    public static List<TestQuery> GetAllTestQueries() =>
    [
        new("Simple topic search",
            "Find papers on machine learning",
            ExpectedTopic: "machine learning"),

        new("Topic with year constraint",
            "Find papers on deep learning published after 2020",
            ExpectedTopic: "deep learning",
            ExpectedMinYear: 2020),

        new("Topic with citation requirement",
            "Find papers on neural networks with 100+ citations",
            ExpectedTopic: "neural networks",
            MinCitations: 100),

        new("Multiple constraints",
            "Find papers on transformers published after 2021 with 50+ citations",
            ExpectedTopic: "transformers",
            ExpectedMinYear: 2021,
            MinCitations: 50),

        new("Recent high-impact papers",
            "Find papers on attention mechanisms published after 2021 with 200+ citations",
            ExpectedTopic: "attention mechanisms",
            ExpectedMinYear: 2021,
            MinCitations: 200)
    ];

    public static TestQuery GetDemoQuery() =>
        new("Demo evaluation",
            "Find papers on machine learning published after 2020",
            ExpectedTopic: "machine learning",
            ExpectedMinYear: 2020);
}
