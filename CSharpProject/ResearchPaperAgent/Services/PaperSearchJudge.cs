namespace ResearchPaperAgent.Services;

using ResearchPaperAgent.Models;

public class PaperSearchJudge
{
    public Task<EvaluationScore> EvaluateAsync(string taskDescription, SearchResult result)
    {
        // No papers found - score fails
        if (result.Status != "success" || result.Papers.Count == 0)
        {
            return Task.FromResult(new EvaluationScore(
                Correctness: 1,
                Adherence: 2,
                Completeness: 0,
                Usefulness: 0,
                Comments: "No papers found matching criteria"));
        }

        // Evaluate using heuristics
        return Task.FromResult(EvaluateWithHeuristics(taskDescription, result));
    }

    private EvaluationScore EvaluateWithHeuristics(string taskDescription, SearchResult result)
    {
        var hasYearConstraint = taskDescription.Contains("published", StringComparison.OrdinalIgnoreCase);
        var hasCitationConstraint = taskDescription.Contains("citation", StringComparison.OrdinalIgnoreCase) ||
                                   taskDescription.Contains("cited", StringComparison.OrdinalIgnoreCase);

        var paperCount = result.Papers.Count;
        var avgCitations = result.Papers.Average(p => p.CitationCount);
        var maxCitations = result.Papers.Max(p => p.CitationCount);
        var minCitations = result.Papers.Min(p => p.CitationCount);
        var completeMetadata = result.Papers.Count(p => p.Authors.Count > 0 && !string.IsNullOrEmpty(p.Venue));

        var correctness = (paperCount, avgCitations, maxCitations) switch
        {
            (5, >= 100, >= 500) => 5,
            (5, >= 50, >= 200) => 5,
            (5, >= 30, >= 100) => 4,
            (4 or 5, >= 20, _) => 4,
            (>= 3, >= 10, _) => 3,
            (>= 1, _, _) => 2,
            _ => 1
        };

        var adherence = 4;
        if (minCitations < 50 && hasCitationConstraint)
            adherence = 3;
        var recentCount = result.Papers.Count(p => p.Year.HasValue && p.Year >= DateTime.Now.Year - 5);
        if (hasYearConstraint && recentCount < paperCount / 2)
            adherence = 2;

        var completeness = (completeMetadata, paperCount) switch
        {
            (5, 5) => 5,
            (>= 4, 5) => 4,
            (>= 3, >= 4) => 4,
            (>= 2, >= 3) => 3,
            (_, _) => 2
        };

        var usefulness = (maxCitations, avgCitations) switch
        {
            (>= 500, >= 100) => 5,
            (>= 300, >= 80) => 5,
            (>= 100, >= 60) => 5,
            (>= 100, >= 40) => 4,
            (>= 50, >= 30) => 4,
            (>= 20, >= 15) => 3,
            (_, _) => 2
        };

        var comments = BuildComments(paperCount, avgCitations, maxCitations, hasYearConstraint, hasCitationConstraint);

        return new EvaluationScore(
            Correctness: correctness,
            Adherence: adherence,
            Completeness: completeness,
            Usefulness: usefulness,
            Comments: comments);
    }

    private static string BuildComments(int paperCount, double avgCitations, int maxCitations, bool hasYear, bool hasCitations)
    {
        var parts = new List<string>();

        // Assess selection quality
        if (paperCount == 5 && avgCitations >= 100)
            parts.Add("Excellent: 5 highly-cited papers");
        else if (paperCount == 5 && avgCitations >= 50)
            parts.Add("Good: 5 well-cited papers");
        else if (paperCount >= 4 && avgCitations >= 30)
            parts.Add("Solid selection of relevant papers");
        else if (paperCount >= 3)
            parts.Add("Reasonable selection");
        else
            parts.Add("Limited selection");

        // Citation impact
        if (maxCitations >= 500)
            parts.Add("includes landmark papers");
        else if (maxCitations >= 200)
            parts.Add("high citation impact");
        else if (avgCitations >= 100)
            parts.Add("well-cited papers");

        // Constraint adherence warnings
        if (hasYear && paperCount < 5)
            parts.Add("⚠ Fewer papers than requested");
        if (hasCitations && avgCitations < 50)
            parts.Add("⚠ May not meet citation constraints");

        return string.Join("; ", parts);
    }
}
