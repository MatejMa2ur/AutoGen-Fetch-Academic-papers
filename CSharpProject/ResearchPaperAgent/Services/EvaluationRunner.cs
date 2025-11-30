namespace ResearchPaperAgent.Services;

using AutoGen.Core;
using ResearchPaperAgent.Models;

public class EvaluationRunner(PaperSearchJudge judge, SemanticScholarService searchService)
{
    public async Task<EvaluationSummary> EvaluateAsync(List<TestQuery> testQueries)
    {
        var results = new List<EvaluationResult>();

        Console.WriteLine($"\n{'='* 80}");
        Console.WriteLine("EVALUATION SUITE");
        Console.WriteLine($"{'='* 80}\n");

        for (int i = 0; i < testQueries.Count; i++)
        {
            var testQuery = testQueries[i];
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Console.Write($"[{i + 1}/{testQueries.Count}] {testQuery.Description}...");

                var searchResult = await searchService.SearchAsync(
                    testQuery.Query.Split(" on ").Last().Split(" published").First(),
                    testQuery.ExpectedMinYear,
                    testQuery.ExpectedMinYear.HasValue ? "after" : "any",
                    testQuery.MinCitations);

                var resultJson = System.Text.Json.JsonSerializer.Deserialize<SearchResult>(
                    searchResult,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new SearchResult();

                stopwatch.Stop();

                var score = await judge.EvaluateAsync(testQuery.Query, resultJson);
                var evalResult = new EvaluationResult(testQuery, resultJson, score, stopwatch.ElapsedMilliseconds);

                results.Add(evalResult);
                Console.WriteLine($" ✓ Score: {score.OverallScore}/100");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($" ✗ Error: {ex.Message}");
                var errorScore = new EvaluationScore(2, 2, 2, 2, ex.Message);
                results.Add(new EvaluationResult(testQuery,
                    new SearchResult("error", 0, [], ex.Message),
                    errorScore,
                    stopwatch.ElapsedMilliseconds));
            }
        }

        var summary = CalculateSummary(results);
        DisplaySummary(summary);
        return summary;
    }

    private EvaluationSummary CalculateSummary(List<EvaluationResult> results)
    {
        var successful = results.Where(r => r.SearchResult.Status == "success").ToList();

        return new EvaluationSummary(
            TotalQueries: results.Count,
            SuccessfulQueries: successful.Count,
            AverageCorrectness: successful.Any() ? successful.Average(r => r.Score.Correctness) : 0,
            AverageAdherence: successful.Any() ? successful.Average(r => r.Score.Adherence) : 0,
            AverageCompleteness: successful.Any() ? successful.Average(r => r.Score.Completeness) : 0,
            AverageUsefulness: successful.Any() ? successful.Average(r => r.Score.Usefulness) : 0,
            OverallAverageScore: successful.Any() ? successful.Average(r => r.Score.OverallScore) : 0,
            TaskSuccessRate: (successful.Count / (double)results.Count) * 100,
            Results: results);
    }

    private void DisplaySummary(EvaluationSummary summary)
    {
        Console.WriteLine($"\n{'='* 80}");
        Console.WriteLine("EVALUATION SUMMARY");
        Console.WriteLine($"{'='* 80}\n");
        Console.WriteLine($"Success Rate:    {summary.TaskSuccessRate:F1}% ({summary.SuccessfulQueries}/{summary.TotalQueries})");
        Console.WriteLine($"Correctness:     {summary.AverageCorrectness:F2}/5");
        Console.WriteLine($"Adherence:       {summary.AverageAdherence:F2}/5");
        Console.WriteLine($"Completeness:    {summary.AverageCompleteness:F2}/5");
        Console.WriteLine($"Usefulness:      {summary.AverageUsefulness:F2}/5");

        var grade = summary.OverallAverageScore switch
        {
            >= 80 => "(Excellent)",
            >= 70 => "(Good)",
            >= 60 => "(Satisfactory)",
            >= 50 => "(Needs Work)",
            _ => "(Poor)"
        };

        Console.WriteLine($"\nOverall Score:   {summary.OverallAverageScore:F1}/100 - {grade}");
        Console.WriteLine($"{'='* 80}\n");
    }
}
