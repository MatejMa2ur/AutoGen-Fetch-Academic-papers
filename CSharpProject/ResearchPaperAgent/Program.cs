using AutoGen.Core;
using AutoGen.Mistral;
using AutoGen.Mistral.Extension;
using ResearchPaperAgent.Configuration;
using ResearchPaperAgent.Models;
using ResearchPaperAgent.Services;
using ResearchPaperAgent.Tools;
using System.Text.Json;
using System.Text.RegularExpressions;

const string SelectionSystemMessage = """
You are an expert research paper analyst with deep knowledge of academic research quality.

YOUR TASK - SELECT THE TOP 5 BEST PAPERS:
Analyze the provided 100 papers and select the TOP 5 BEST PAPERS based on these criteria:

SELECTION CRITERIA (in order of importance):
1. Topic Relevance: Does it directly address the research question?
2. Citation Impact: How influential/well-received is this paper? (500+ = landmark)
3. Publication Venue: Quality of publication (NeurIPS, ICML, Nature > others)
4. Recency: Is it recent enough to be relevant? (Balance with impact)
5. Author Credibility: Are these well-known researchers in the field?

RESPONSE FORMAT:
Provide your TOP 5 selection in this exact format:

TOP 5 PAPERS:
1. Title: [exact title from the data]
   Authors: [author list]
   Citations: [citation count]
   Venue: [publication venue]
   Why selected: [2-3 sentence explanation]

2. Title: [exact title]
   Authors: [author list]
   Citations: [citation count]
   Venue: [publication venue]
   Why selected: [2-3 sentence explanation]

(papers 3-5 in same format...)

Be rigorous in your analysis. Focus on quality over quantity.
""";

try
{
    var settings = ConfigurationLoader.LoadConfiguration();
    var service = new SemanticScholarService(settings.SemanticScholar);
    var tool = new SemanticScholarTool(service);

    var mistralClient = new MistralClient(apiKey: settings.MistralAI.ApiKey);
    var judge = new PaperSearchJudge(mistralClient, settings.MistralAI.Model);

    var agent = new MistralClientAgent(mistralClient, "PaperSearchAgent", settings.MistralAI.Model)
        .RegisterMessageConnector()
        .RegisterStreamingMiddleware(new FunctionCallMiddleware(
            functions: [tool.SearchPapersFunctionContract],
            functionMap: new Dictionary<string, Func<string, Task<string>>>
            {
                { tool.SearchPapersFunctionContract.Name, tool.SearchPapersWrapper }
            }));

    var runner = new EvaluationRunner(judge, service);
    var reportGen = new EvaluationReportGenerator();

    DisplayWelcome();

    var isFirstRun = true;
    while (true)
    {
        string query;
        if (isFirstRun)
        {
            query = "Find a paper on machine learning published after 2020";
            isFirstRun = false;
        }
        else
        {
            query = GetUserQuery();
        }

        if (string.IsNullOrWhiteSpace(query))
            continue;

        if (query.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nGoodbye!");
            break;
        }

        if (query.Equals("eval-full", StringComparison.OrdinalIgnoreCase))
        {
            var summary = await runner.EvaluateAsync(EvaluationTestSuite.GetAllTestQueries());
            var path = await reportGen.GenerateReportAsync(summary);
            Console.WriteLine($"\nReports:\n  - {path}.json\n  - {path}.txt\n  - {path}_detailed.txt\n");
            continue;
        }

        if (query.Equals("eval", StringComparison.OrdinalIgnoreCase))
        {
            var result = await service.SearchAsync("machine learning", 2020, "after", 50);
            var parsed = JsonSerializer.Deserialize<SearchResult>(result,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchResult();
            var score = await judge.EvaluateAsync("Find papers on machine learning published after 2020", parsed);

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("QUICK EVALUATION");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"\nCorrectness:   {score.Correctness}/5");
            Console.WriteLine($"Adherence:     {score.Adherence}/5");
            Console.WriteLine($"Completeness:  {score.Completeness}/5");
            Console.WriteLine($"Usefulness:    {score.Usefulness}/5");
            Console.WriteLine($"Overall Score: {score.OverallScore}/100");
            Console.WriteLine($"Comments:      {score.Comments}");
            Console.WriteLine(new string('=', 80) + "\n");
            continue;
        }

        Console.WriteLine("\n[STAGE 1] Searching for papers...\n");

        try
        {
            // STAGE 1: Extract constraints and fetch papers (single API call)
            var topic = ExtractTopicFromQuery(query);
            var (year, yearCondition, minCitations) = ExtractConstraintsFromQuery(query);

            Console.WriteLine($"Searching for: {topic} (year: {yearCondition} {year}, min citations: {minCitations ?? 0})...\n");

            var searchResult = await service.SearchAsync(topic, year, yearCondition, minCitations);
            var searchResultParsed = JsonSerializer.Deserialize<SearchResult>(searchResult,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SearchResult();

            if (searchResultParsed.Status != "success" || searchResultParsed.Papers.Count == 0)
            {
                Console.WriteLine($"Status: {searchResultParsed.Status}, Papers: {searchResultParsed.Papers.Count}\n");
                if (!string.IsNullOrEmpty(searchResultParsed.Message))
                    Console.WriteLine($"Message: {searchResultParsed.Message}\n");
                continue;
            }

            Console.WriteLine($"✓ Found {searchResultParsed.Papers.Count} papers\n");

            // STAGES 2 & 3: Analyze, select TOP 5, and evaluate (with auto-retry if score < 70)
            Console.WriteLine($"[STAGE 2] Analyzing {searchResultParsed.Papers.Count} papers...\n");

            var (selectedPapers, finalScore, retriesUsed) = await ProcessQueryWithRetryAsync(
                searchResultParsed.Papers,
                query,
                agent,
                judge,
                mistralClient,
                settings.MistralAI.Model,
                maxRetries: 3);

            if (selectedPapers != null && selectedPapers.Count > 0 && finalScore != null)
            {
                Console.WriteLine("Selected Papers:\n");
                var evaluationResult = new SearchResult(
                    Status: "success",
                    PapersFound: selectedPapers.Count,
                    Papers: selectedPapers,
                    Message: null);

                DisplayResults(JsonSerializer.Serialize(evaluationResult, new JsonSerializerOptions { WriteIndented = true }));

                Console.WriteLine($"\nFinal Evaluation of Agent's Selection:\n");
                Console.WriteLine($"Overall Score: {finalScore.OverallScore}/100");
                Console.WriteLine($"  Correctness:  {finalScore.Correctness}/5");
                Console.WriteLine($"  Adherence:    {finalScore.Adherence}/5");
                Console.WriteLine($"  Completeness: {finalScore.Completeness}/5");
                Console.WriteLine($"  Usefulness:   {finalScore.Usefulness}/5");
                Console.WriteLine($"  Comment:      {finalScore.Comments}");

                if (retriesUsed > 0)
                {
                    Console.WriteLine($"\n📊 Achieved score after {retriesUsed} retries\n");
                }
                else
                {
                    Console.WriteLine($"\n✓ Excellent score achieved on first attempt!\n");
                }
            }
            else
            {
                Console.WriteLine("⚠ Failed to get a valid selection after all retry attempts.\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}\n");
        }
    }
}
catch (InvalidOperationException ex) when (ex.Message.Contains("MISTRAL_API_KEY"))
{
    Console.WriteLine($"Configuration Error: {ex.Message}");
    Console.WriteLine("\nPlease set MISTRAL_API_KEY in appsettings.json or as an environment variable.");
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal Error: {ex.Message}");
    Environment.Exit(1);
}

static async Task<(List<Paper>, EvaluationScore, int)> ProcessQueryWithRetryAsync(
    List<Paper> allPapers,
    string query,
    MiddlewareStreamingAgent<MistralClientAgent> agent,
    PaperSearchJudge judge,
    MistralClient mistralClient,
    string model,
    int maxRetries = 3)
{
    int retryCount = 0;
    EvaluationScore currentScore = null;
    List<Paper> selectedPapers = null;
    string previousFeedback = "";

    while (retryCount < maxRetries)
    {
        // STAGE 2: Analyze papers and select TOP 5
        var papersJson = JsonSerializer.Serialize(allPapers,
            new JsonSerializerOptions { WriteIndented = false });

        // Adjust system message based on retry feedback
        var systemMsg = SelectionSystemMessage;
        if (retryCount > 0)
        {
            systemMsg += $"\n\n[RETRY ATTEMPT {retryCount}/{maxRetries - 1}]\n" +
                        $"Previous score: {currentScore.OverallScore}/100\n" +
                        previousFeedback +
                        "Please reconsider your selection with this feedback in mind.";
        }

        var selectionMessages = new List<IMessage>
        {
            new TextMessage(Role.System, systemMsg),
            new TextMessage(Role.User, $"Here are {allPapers.Count} papers to analyze:\n\n{papersJson}\n\nPlease select the TOP 5 BEST papers based on the criteria.")
        };

        var selectionResponse = await agent.SendAsync(chatHistory: selectionMessages);
        var agentAnalysis = selectionResponse.GetContent();

        if (retryCount == 0)
        {
            Console.WriteLine("Agent's Selection Analysis:\n");
            Console.WriteLine(agentAnalysis);
            Console.WriteLine("\n" + new string('=', 80) + "\n");
        }

        // Extract the top 5 papers from agent's response
        selectedPapers = ExtractTopFivePapersFromAnalysis(agentAnalysis, allPapers);

        if (selectedPapers == null || selectedPapers.Count == 0)
        {
            Console.WriteLine("⚠ Could not extract selected papers from agent's analysis.\n");
            retryCount++;

            if (retryCount < maxRetries)
            {
                previousFeedback = "⚠ Previous attempt failed to extract papers.\n→ Ensure output follows the exact format: \"Title: ...\", \"Why selected: ...\"";
                Console.WriteLine($"⚠ Retrying with format feedback... ({retryCount}/{maxRetries})\n");
                await Task.Delay(500);
            }
            continue;
        }

        // STAGE 3: Evaluate
        var evaluationResult = new SearchResult(
            Status: "success",
            PapersFound: selectedPapers.Count,
            Papers: selectedPapers,
            Message: null);

        currentScore = await judge.EvaluateAsync(query, evaluationResult);

        if (retryCount == 0)
        {
            Console.WriteLine($"[STAGE 3] Evaluating {selectedPapers.Count} selected papers...\n");
        }

        Console.WriteLine($"Attempt {retryCount + 1}: Overall Score: {currentScore.OverallScore}/100");
        Console.WriteLine($"  Correctness:  {currentScore.Correctness}/5");
        Console.WriteLine($"  Adherence:    {currentScore.Adherence}/5");
        Console.WriteLine($"  Completeness: {currentScore.Completeness}/5");
        Console.WriteLine($"  Usefulness:   {currentScore.Usefulness}/5");

        // Check if score is good enough
        if (currentScore.OverallScore >= 70)
        {
            Console.WriteLine($"✓ Good score achieved! ({currentScore.OverallScore}/100)\n");
            break;
        }

        retryCount++;

        if (retryCount < maxRetries)
        {
            // Generate feedback for next attempt using LLM
            previousFeedback = await GenerateLLMBasedFeedbackAsync(currentScore, selectedPapers, allPapers, mistralClient, model);
            Console.WriteLine($"⚠ Score too low. Retrying with feedback... ({retryCount}/{maxRetries})\n");
            await Task.Delay(500);  // Brief pause before retry
        }
    }

    return (selectedPapers ?? new(), currentScore ?? new EvaluationScore(0, 0, 0, 0, "Failed to evaluate"), retryCount);
}

static async Task<string> GenerateLLMBasedFeedbackAsync(
    EvaluationScore score,
    List<Paper> selected,
    List<Paper> available,
    MistralClient mistralClient,
    string model)
{
    try
    {
        var prompt = $"""
        You are an expert research paper evaluation coach. A paper selection algorithm selected these papers but received a low evaluation score.

        EVALUATION SCORES:
        - Correctness: {score.Correctness}/5 (Paper quality and relevance)
        - Adherence: {score.Adherence}/5 (Meeting constraints)
        - Completeness: {score.Completeness}/5 (Metadata quality)
        - Usefulness: {score.Usefulness}/5 (Impact and relevance)
        - Overall: {score.OverallScore}/100

        SELECTED PAPERS:
        {string.Join("\n", selected.Select((p, i) => $"{i+1}. \"{p.Title}\" ({p.CitationCount} citations, {p.Year}, {p.Venue})"))}

        AVAILABLE PAPERS (sample of top 10):
        {string.Join("\n", available.OrderByDescending(p => p.CitationCount).Take(10).Select((p, i) => $"{i+1}. \"{p.Title}\" ({p.CitationCount} citations, {p.Year}, {p.Venue})"))}

        Provide 2-3 specific, actionable suggestions to improve the next selection. Be brief and focus on what papers should have been selected instead.
        """;

        // Create a temporary agent for LLM feedback generation
        var feedbackAgent = new MistralClientAgent(mistralClient, "FeedbackAgent", model)
            .RegisterMessageConnector();

        var messages = new List<IMessage>
        {
            new TextMessage(Role.User, prompt)
        };

        // Get the feedback from the LLM
        var response = await feedbackAgent.GenerateReplyAsync(messages);
        var feedback = response.GetContent();

        if (!string.IsNullOrEmpty(feedback))
        {
            return "💡 AI-Generated Feedback:\n" + feedback;
        }
    }
    catch
    {
        // Fall back to heuristic if LLM fails
    }

    // Fallback to heuristic feedback
    return GenerateRetryFeedback(score, selected, available);
}

static string GenerateRetryFeedback(EvaluationScore score, List<Paper> selected, List<Paper> available)
{
    var feedback = new List<string>();

    if (score.Correctness < 3)
    {
        var avgCitations = selected.Average(p => p.CitationCount);
        feedback.Add($"❌ Correctness too low ({score.Correctness}/5): Papers have low citation impact (avg: {avgCitations:F0}).\n" +
                    "   → Look for papers with 500+ citations, especially landmark papers.");
    }

    if (score.Adherence < 3)
    {
        feedback.Add($"❌ Adherence too low ({score.Adherence}/5): Selected papers may not meet constraints.\n" +
                    "   → Verify all papers match the requested year and citation requirements.");
    }

    if (score.Completeness < 3)
    {
        feedback.Add($"❌ Completeness too low ({score.Completeness}/5): Missing author or venue information.\n" +
                    "   → Prioritize papers with complete author lists and publication venues.");
    }

    if (score.Usefulness < 3)
    {
        feedback.Add($"❌ Usefulness too low ({score.Usefulness}/5): Papers lack impact or relevance.\n" +
                    "   → Select papers from top-tier venues (Nature, NeurIPS, ICML, ICCV).");
    }

    if (feedback.Count == 0)
    {
        feedback.Add("⚠ Score is borderline. Try to find papers with slightly higher impact.\n");
    }

    return string.Join("\n", feedback);
}

static string ExtractTopicFromQuery(string query)
{
    // Extract topic from patterns like "Find papers on [topic]" or "Find [topic]"
    var match = Regex.Match(query, @"(?:on|about)\s+([^,]+?)(?:\s+published|\s+with|\s+that|$)", RegexOptions.IgnoreCase);
    if (match.Success)
        return match.Groups[1].Value.Trim();

    // Fallback: extract after "Find" or first meaningful phrase
    match = Regex.Match(query, @"[Ff]ind\s+(?:a\s+)?(?:paper\s+)?(?:on\s+)?(.+?)(?:\s+published|\s+with|$)");
    return match.Success ? match.Groups[1].Value.Trim() : query;
}

static (int? year, string yearCondition, int? minCitations) ExtractConstraintsFromQuery(string query)
{
    int? year = null;
    string yearCondition = "any";
    int? minCitations = null;

    // Extract year constraint
    var yearMatch = Regex.Match(query, @"(?:published\s+)?(?:after|since)\s+(\d{4})", RegexOptions.IgnoreCase);
    if (yearMatch.Success)
    {
        year = int.Parse(yearMatch.Groups[1].Value);
        yearCondition = "after";
    }
    else
    {
        yearMatch = Regex.Match(query, @"(?:published\s+)?(?:before)\s+(\d{4})", RegexOptions.IgnoreCase);
        if (yearMatch.Success)
        {
            year = int.Parse(yearMatch.Groups[1].Value);
            yearCondition = "before";
        }
    }

    // Extract citation constraint
    var citationMatch = Regex.Match(query, @"(?:has|with|over)\s+(\d+)\s+citation", RegexOptions.IgnoreCase);
    if (citationMatch.Success)
    {
        minCitations = int.Parse(citationMatch.Groups[1].Value);
    }

    return (year, yearCondition, minCitations);
}

static List<Paper> ExtractTopFivePapersFromAnalysis(string agentAnalysis, List<Paper> allPapers)
{
    var selected = new List<Paper>();

    // Extract title patterns from agent analysis (looking for "Title: " lines)
    var titleMatches = Regex.Matches(
        agentAnalysis,
        @"Title:\s*(.+?)(?=\n|Authors?:|Citations?:|Venue:|Why|$)",
        RegexOptions.IgnoreCase);

    foreach (Match match in titleMatches.Take(5))
    {
        var titleFromAgent = match.Groups[1].Value.Trim();

        // Find matching paper in allPapers list
        var matchingPaper = allPapers.FirstOrDefault(p =>
            p.Title.Contains(titleFromAgent, StringComparison.OrdinalIgnoreCase) ||
            titleFromAgent.Contains(p.Title, StringComparison.OrdinalIgnoreCase));

        if (matchingPaper != null)
            selected.Add(matchingPaper);
    }

    return selected;
}

static void DisplayWelcome()
{
    Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
    Console.WriteLine("  Research Paper Discovery Agent");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
    Console.WriteLine("\nFind research papers by topic, year, and citation count.");
    Console.WriteLine("Example: 'Find a paper on machine learning published after 2020'\n");
}

static string GetUserQuery()
{
    Console.Write("Your query: ");
    return Console.ReadLine() ?? "";
}

static void DisplayResults(string jsonResponse)
{
    try
    {
        var result = JsonSerializer.Deserialize<SearchResult>(jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result == null)
        {
            Console.WriteLine(jsonResponse);
            return;
        }

        if (result.Status == "success")
        {
            Console.WriteLine($"✓ Found {result.PapersFound} paper(s)\n");

            foreach (var (paper, i) in result.Papers.Select((p, idx) => (p, idx)))
            {
                Console.WriteLine($"Paper {i + 1}");
                Console.WriteLine(new string('─', 80));
                Console.WriteLine($"Title:     {paper.Title}");
                Console.WriteLine($"Year:      {paper.Year ?? 0}");
                Console.WriteLine($"Citations: {paper.CitationCount}");
                Console.WriteLine($"Venue:     {paper.Venue ?? "N/A"}");

                if (paper.Authors.Count > 0)
                {
                    var authors = string.Join(", ", paper.Authors.Take(3).Select(a => a.Name));
                    if (paper.Authors.Count > 3)
                        authors += $", +{paper.Authors.Count - 3} more";
                    Console.WriteLine($"Authors:   {authors}");
                }

                Console.WriteLine($"Paper ID:  {paper.PaperId}\n");
            }
        }
        else if (result.Status == "no_results")
        {
            Console.WriteLine($"✗ No results found");
            if (!string.IsNullOrEmpty(result.Message))
                Console.WriteLine($"  {result.Message}");
        }
        else if (result.Status == "error")
        {
            Console.WriteLine($"✗ Error: {result.Message}");
        }
        else
        {
            Console.WriteLine(jsonResponse);
        }
    }
    catch
    {
        Console.WriteLine(jsonResponse);
    }
}
