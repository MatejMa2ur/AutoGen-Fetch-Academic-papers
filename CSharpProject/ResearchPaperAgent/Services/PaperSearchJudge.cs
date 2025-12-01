namespace ResearchPaperAgent.Services;

using AutoGen.Core;
using AutoGen.Mistral;
using ResearchPaperAgent.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

public class PaperSearchJudge
{
    private readonly MistralClient? _mistralClient;
    private readonly string? _model;

    public PaperSearchJudge() { }

    public PaperSearchJudge(MistralClient mistralClient, string model)
    {
        _mistralClient = mistralClient;
        _model = model;
    }

    public async Task<EvaluationScore> EvaluateAsync(string taskDescription, SearchResult result)
    {
        // No papers found - score fails
        if (result.Status != "success" || result.Papers.Count == 0)
        {
            return new EvaluationScore(
                Correctness: 1,
                Adherence: 2,
                Completeness: 0,
                Usefulness: 0,
                Comments: "No papers found matching criteria");
        }

        // Use LLM-based evaluation if client is available
        if (_mistralClient != null && !string.IsNullOrEmpty(_model))
        {
            Console.WriteLine("[LLM Evaluation] Using Mistral AI to evaluate papers...");
            return await EvaluateWithLLMAsync(taskDescription, result);
        }

        // Fallback to heuristic evaluation
        Console.WriteLine("[Heuristic Evaluation] Using fallback heuristic rules...");
        return EvaluateWithHeuristics(taskDescription, result);
    }

    private async Task<EvaluationScore> EvaluateWithLLMAsync(string taskDescription, SearchResult result)
    {
        try
        {
            var papersJson = string.Join("\n", result.Papers.Select((p, i) => $"""
                Paper {i + 1}:
                - Title: {p.Title}
                - Authors: {string.Join(", ", p.Authors.Select(a => a.Name))}
                - Year: {p.Year}
                - Citations: {p.CitationCount}
                - Venue: {p.Venue ?? "Unknown"}
                """));

            var evaluationPrompt = $"""
You are an expert research paper evaluator. Your task is to evaluate the following selected papers based on the user's research query.

USER QUERY: {taskDescription}

SELECTED PAPERS:
{papersJson}

Evaluate these papers on 4 dimensions (score each 1-5):

1. CORRECTNESS (Paper quality based on citations and impact)
   - 5: All papers are highly-cited landmarks (500+ citations)
   - 4: Papers have significant citations (100-500)
   - 3: Papers have moderate citations (50-100)
   - 2: Papers have few citations (<50)
   - 1: Papers have almost no citations

2. ADHERENCE (Meeting the query constraints - year, citations, topic)
   - 5: All papers perfectly match the query constraints
   - 4: Most papers match constraints
   - 3: Some papers match constraints
   - 2: Few papers match constraints
   - 1: Papers don't match constraints

3. COMPLETENESS (Metadata quality - have full author info, venue, etc.)
   - 5: All papers have complete metadata
   - 4: Most papers have complete metadata
   - 3: Some papers have complete metadata
   - 2: Few papers have complete metadata
   - 1: Papers are missing critical metadata

4. USEFULNESS (Relevance to the query and impact in the field)
   - 5: Papers are highly relevant and from top-tier venues (Nature, NeurIPS, ICML, Science)
   - 4: Papers are very relevant from good venues
   - 3: Papers are relevant from standard venues
   - 2: Papers have limited relevance
   - 1: Papers are not relevant to the query

Respond with ONLY a JSON object (no markdown, no explanation) with keys: correctness, adherence, completeness, usefulness, reasoning, strengths, weaknesses (each value must be either an integer 1-5 or a string).
""";

            var agent = new MistralClientAgent(_mistralClient, "EvaluatorAgent", _model);

            var messages = new List<IMessage>
            {
                new TextMessage(Role.User, evaluationPrompt)
            };

            var response = await agent.GenerateReplyAsync(messages);
            var responseText = response.GetContent();

            // Parse JSON response
            var jsonMatch = Regex.Match(responseText, @"\{[\s\S]*\}", RegexOptions.IgnoreCase);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                if (json.TryGetProperty("correctness", out var correctnessVal) &&
                    json.TryGetProperty("adherence", out var adherenceVal) &&
                    json.TryGetProperty("completeness", out var completenessVal) &&
                    json.TryGetProperty("usefulness", out var usefulnessVal) &&
                    json.TryGetProperty("strengths", out var strengthsVal) &&
                    json.TryGetProperty("weaknesses", out var weaknessesVal))
                {
                    int correctness = int.Clamp(correctnessVal.GetInt32(), 1, 5);
                    int adherence = int.Clamp(adherenceVal.GetInt32(), 1, 5);
                    int completeness = int.Clamp(completenessVal.GetInt32(), 1, 5);
                    int usefulness = int.Clamp(usefulnessVal.GetInt32(), 1, 5);

                    var strengths = strengthsVal.GetString() ?? "Good selection";
                    var weaknesses = weaknessesVal.GetString() ?? "Room for improvement";
                    var comments = $"✓ {strengths} | ⚠ {weaknesses}";

                    return new EvaluationScore(
                        Correctness: correctness,
                        Adherence: adherence,
                        Completeness: completeness,
                        Usefulness: usefulness,
                        Comments: comments);
                }
            }

            // Fallback if JSON parsing fails
            return EvaluateWithHeuristics(taskDescription, result);
        }
        catch
        {
            // Fallback to heuristic evaluation if LLM evaluation fails
            return EvaluateWithHeuristics(taskDescription, result);
        }
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
