namespace ResearchPaperAgent.Services;

using System.Text;
using System.Text.Json;
using ResearchPaperAgent.Models;

public class EvaluationReportGenerator(string reportDirectory = "evaluation-reports")
{
    private readonly string _reportDirectory = reportDirectory;

    public EvaluationReportGenerator() : this("evaluation-reports")
    {
        if (!Directory.Exists(_reportDirectory))
            Directory.CreateDirectory(_reportDirectory);
    }

    public async Task<string> GenerateReportAsync(EvaluationSummary summary)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reportBaseName = Path.Combine(_reportDirectory, $"evaluation_{timestamp}");

        await File.WriteAllTextAsync($"{reportBaseName}.json",
            JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

        await GenerateTextReportAsync(summary, $"{reportBaseName}.txt");
        await GenerateDetailedReportAsync(summary, $"{reportBaseName}_detailed.txt");

        return reportBaseName;
    }

    private Task GenerateTextReportAsync(EvaluationSummary summary, string filePath)
    {
        var sb = new StringBuilder();
        var divider = new string('=', 80);

        sb.AppendLine(divider);
        sb.AppendLine("RESEARCH PAPER DISCOVERY AGENT - EVALUATION REPORT");
        sb.AppendLine(divider);
        sb.AppendLine($"\nGenerated: {summary.EvaluatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"\nTotal Queries:   {summary.TotalQueries}");
        sb.AppendLine($"Success Rate:    {summary.TaskSuccessRate:F1}%");
        sb.AppendLine($"\nCorrectness:     {summary.AverageCorrectness:F2}/5");
        sb.AppendLine($"Adherence:       {summary.AverageAdherence:F2}/5");
        sb.AppendLine($"Completeness:    {summary.AverageCompleteness:F2}/5");
        sb.AppendLine($"Usefulness:      {summary.AverageUsefulness:F2}/5");

        var grade = summary.OverallAverageScore switch
        {
            >= 90 => "A (Excellent)",
            >= 80 => "B (Good)",
            >= 70 => "C (Satisfactory)",
            >= 60 => "D (Needs Work)",
            _ => "F (Poor)"
        };

        sb.AppendLine($"\nOverall Score:   {summary.OverallAverageScore:F1}/100");
        sb.AppendLine($"Grade:           {grade}");
        sb.AppendLine(divider);

        return File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private Task GenerateDetailedReportAsync(EvaluationSummary summary, string filePath)
    {
        var sb = new StringBuilder();
        var divider = new string('=', 80);

        sb.AppendLine(divider);
        sb.AppendLine("DETAILED EVALUATION RESULTS");
        sb.AppendLine(divider);

        foreach (var (result, index) in summary.Results.Select((r, i) => (r, i)))
        {
            sb.AppendLine($"\n[{index + 1}] {result.TestQuery.Description}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"Query:        {result.TestQuery.Query}");
            sb.AppendLine($"Status:       {result.SearchResult.Status}");
            sb.AppendLine($"Papers Found: {result.SearchResult.PapersFound}");
            sb.AppendLine($"Time:         {result.ElapsedMilliseconds}ms");
            sb.AppendLine($"Scores:       {result.Score.Correctness}/5, {result.Score.Adherence}/5, " +
                          $"{result.Score.Completeness}/5, {result.Score.Usefulness}/5");
            sb.AppendLine($"Overall:      {result.Score.OverallScore}/100");
            sb.AppendLine($"Comments:     {result.Score.Comments}");

            if (result.SearchResult.Status == "success" && result.SearchResult.Papers.Count > 0)
                sb.AppendLine($"Top Paper:    \"{result.SearchResult.Papers[0].Title}\"");
        }

        sb.AppendLine($"\n{divider}");

        return File.WriteAllTextAsync(filePath, sb.ToString());
    }
}
