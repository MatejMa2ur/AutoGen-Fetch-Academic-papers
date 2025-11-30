using System.Text.Json.Serialization;

namespace ResearchPaperAgent.Models;

public record EvaluationScore(
    [property: JsonPropertyName("correctness")] int Correctness,
    [property: JsonPropertyName("adherence")] int Adherence,
    [property: JsonPropertyName("completeness")] int Completeness,
    [property: JsonPropertyName("usefulness")] int Usefulness,
    [property: JsonPropertyName("comments")] string Comments = "")
{
    public double AverageScore => (Correctness + Adherence + Completeness + Usefulness) / 4.0;
    public int OverallScore => (int)(AverageScore * 20);
}

public record TestQuery(
    string Description,
    string Query,
    string? ExpectedTopic = null,
    int? ExpectedMinYear = null,
    int? ExpectedMaxYear = null,
    int? MinCitations = null);

public record EvaluationResult(
    TestQuery TestQuery,
    SearchResult SearchResult,
    EvaluationScore Score,
    long ElapsedMilliseconds,
    DateTime EvaluatedAt = default)
{
    public EvaluationResult(TestQuery q, SearchResult r, EvaluationScore s, long ms)
        : this(q, r, s, ms, DateTime.UtcNow) { }
}

public record EvaluationSummary(
    int TotalQueries,
    int SuccessfulQueries,
    double AverageCorrectness,
    double AverageAdherence,
    double AverageCompleteness,
    double AverageUsefulness,
    double OverallAverageScore,
    double TaskSuccessRate,
    List<EvaluationResult> Results,
    DateTime EvaluatedAt = default)
{
    public EvaluationSummary(int total, int successful, double correct, double adhere, double complete,
        double useful, double overall, double success, List<EvaluationResult> results)
        : this(total, successful, correct, adhere, complete, useful, overall, success, results, DateTime.UtcNow) { }
}

public record EvaluationScoreResponse(
    [property: JsonPropertyName("correctness")] int? Correctness = null,
    [property: JsonPropertyName("adherence")] int? Adherence = null,
    [property: JsonPropertyName("completeness")] int? Completeness = null,
    [property: JsonPropertyName("usefulness")] int? Usefulness = null,
    [property: JsonPropertyName("comment")] string? Comment = null);
