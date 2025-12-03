using System.Text.Json;
using AutoGen.Core;
using AutoGen.Mistral;
using AutoGen.Mistral.Extension;

namespace Autogen_research_paper_tool_calling_evaluation.Agents;

public static class Agents
{
    public static IAgent CreateManagerAgent(MistralClient client)
    {
        var manager = new MistralClientAgent(
            client: client,
            name: "manager",
            model: "ministral-8b-2410")
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return manager;
    }
    
    public static IAgent CreateFetcherAgent(MistralClient client)
    {
        var tools = new Tools.Tools();

        var autoInvokeMiddleware = new FunctionCallMiddleware(
            functions: [tools.GetResearchPapersFunctionContract],
            functionMap: new Dictionary<string, Func<string, Task<string>>>()
            {
                { tools.GetResearchPapersFunctionContract.Name!, tools.GetResearchPapersWrapper },
            });

        var fetcher = new MistralClientAgent(
                client: client,
                name: "fetcher",
                model: "ministral-8b-2410",
                systemMessage: @"
Act as a research paper specialist.
Based on the input of what I am searching for, create a search query to fetch research papers.

Return the search results in a format that will be parsed by the system.")
            .RegisterMessageConnector()
            .RegisterMiddleware(autoInvokeMiddleware)
            .RegisterPrintMessage();

        return fetcher;
    }

    public static IAgent CreateAnalyzerAgent(MistralClient client)
    {
        var analyzer = new MistralClientAgent(
                client: client,
                name: "analyzer",
                model: "ministral-8b-2410",
                systemMessage: @"
You are a research paper analyzer.
Analyze the fetched research papers and select the best one based on citation count and recency.
Return the result as JSON with the selected paper details.
")
            .RegisterMessageConnector()
            .RegisterMiddleware(async (msgs, option, agent, _) =>
            {
                var lastMessage = msgs.LastOrDefault(m => m.From == "fetcher");
                if (lastMessage is null)
                {
                    return new TextMessage(Role.Assistant, "No papers retrieved from fetcher!", from: agent.Name);
                }
                var messageContent = lastMessage.GetContent();
                if (messageContent is null)
                {
                    return new TextMessage(Role.Assistant, "No papers retrieved from fetcher!", from: agent.Name);
                }
                var response = JsonSerializer.Deserialize<Records.SearchResult>(lastMessage.GetContent());

                if (response != null && response.Papers != null && response.Papers.Count > 0)
                {
                    // Check relevance: If papers contain irrelevant keywords, mark as not relevant
                    var allPaperTitles = string.Join(" ", response.Papers.Select(p => p.Title.ToLower()));
                    var isRelevant = CheckRelevance(allPaperTitles);

                    if (!isRelevant)
                    {
                        var notRelevantResult = JsonSerializer.Serialize(new
                        {
                            status = "not_relevant",
                            message = "The retrieved papers do not appear to be relevant to the search topic. The papers seem to focus on other subjects rather than machine learning in healthcare.",
                            total_papers_analyzed = response.Papers.Count
                        });
                        return new TextMessage(Role.Assistant, notRelevantResult, from: agent.Name);
                    }

                    var bestPaper = AnalyzePapersStatically(response.Papers);
                    if (bestPaper != null)
                    {
                        var analysisResult = JsonSerializer.Serialize(new
                        {
                            status = "success",
                            selected_paper = bestPaper,
                            total_papers_analyzed = response.Papers.Count
                        });
                        return new TextMessage(Role.Assistant, analysisResult, from: agent.Name);
                    }
                }
                return new TextMessage(Role.Assistant, "No papers retrieved from fetcher!", from: agent.Name);
            })
            .RegisterPrintMessage();

        return analyzer;
    }

    /// <summary>
    /// Analyzes papers statically and returns the best one based on citations and year.
    /// Papers are scored using a formula that prioritizes recent papers with high citations.
    /// </summary>
    private static Records.Paper? AnalyzePapersStatically(List<Records.Paper> papers)
    {
        // Get the range of years and citation counts for normalization
        var maxYear = papers.Max(p => p.Year.Value);
        var minYear = papers.Min(p => p.Year.Value);
        var maxCitations = papers.Max(p => p.CitationCount);
        var minCitations = papers.Min(p => p.CitationCount);

        // Score each paper: 60% weight on citations, 40% weight on year recency
        var scoredPapers = papers
            .Select(paper =>
            {
                // Normalize citations (0-1)
                var citationScore = maxCitations > minCitations
                    ? (double)(paper.CitationCount - minCitations) / (maxCitations - minCitations)
                    : 0.5;

                // Normalize year recency (0-1)
                var yearScore = maxYear > minYear
                    ? (double)(paper.Year.Value - minYear) / (maxYear - minYear)
                    : 0.5;

                // Combined score: 60% citations, 40% year
                var totalScore = (citationScore * 0.7) + (yearScore * 0.3);

                return new { Paper = paper, Score = totalScore };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return scoredPapers?.Paper;
    }

    /// <summary>
    /// Checks if papers are relevant to machine learning in healthcare.
    /// Returns false if papers appear to be about unrelated topics.
    /// </summary>
    private static bool CheckRelevance(string allTitles)
    {
        // Keywords that indicate relevance to ML in healthcare
        var relevantKeywords = new[] { "machine learning", "ml", "healthcare", "medicine", "medical", "health", "clinical", "disease", "diagnosis", "treatment", "patient", "hospital" };

        // Keywords that indicate irrelevance (papers about other topics)
        var irrelevantKeywords = new[] { "sports", "football", "soccer", "baseball", "gaming", "video game", "entertainment", "music", "cooking", "fashion", "automotive", "cars" };

        // Check if paper titles contain irrelevant keywords
        foreach (var keyword in irrelevantKeywords)
        {
            if (allTitles.Contains(keyword))
            {
                return false; // Not relevant
            }
        }

        // Check if paper titles contain relevant keywords
        var hasRelevantKeywords = relevantKeywords.Any(keyword => allTitles.Contains(keyword));

        return hasRelevantKeywords;
    }

    public static IAgent CreateCritiqueAgent(MistralClient client)
    {
        var critique = new MistralClientAgent(
            client: client,
            name: "critique",
            model: "ministral-8b-2410",
            systemMessage: @"
You are responsible for verifying if the retrieved research paper meets the requirements.

Requirements to check:
1. Publication year: 2015 or later
2. Citation count: At least 50 citations
3. Topic: Related to machine learning in health and medicine

Put your comment between ```review and ```, and set the result field to either APPROVED or REJECTED.

APPROVED: When the paper meets ALL three requirements.
REJECTED: When the paper fails to meet any of the requirements.

Make sure your assessment is clear and objective.

## Example 1 ##
```review
comment: Paper is from 2018 with 125 citations, directly related to ML in healthcare. Meets all requirements.
result: APPROVED
```

## Example 2 ##
```review
comment: Paper is from 2010, which is before the required year 2015. Does not meet requirements.
result: REJECTED
```
            ")
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return critique;
    }

    public static IAgent CreateEvaluatorAgent(MistralClient client)
    {
        var evaluator = new MistralClientAgent(
            client: client,
            name: "evaluator",
            model: "ministral-8b-2410",
            systemMessage: @"
You are an external evaluator responsible for assessing the performance of a multi-agent research paper discovery system.

Evaluate the conversation and agent interactions on the following criteria:

1. **Correctness (0-5)**: Did the system find a valid research paper that meets the requirements?
   - 5: Paper found and approved by critique, meets all requirements
   - 3-4: Paper found but with minor issues
   - 1-2: Paper found but questionable relevance
   - 0: No valid paper found or requirements not met

2. **Instruction Following (0-5)**: Did each agent follow its role and instructions?
   - 5: All agents executed their roles perfectly
   - 3-4: Most agents followed instructions with minor deviations
   - 1-2: Some agents didn't follow instructions
   - 0: Agents ignored their roles

3. **Efficiency (0-5)**: How many rounds/retries did it take to find a valid paper?
   - 5: Solved in 2-3 rounds
   - 4: Solved in 4-5 rounds
   - 3: Solved in 6-8 rounds
   - 2: Solved in 9-11 rounds
   - 1: Took 12+ rounds or failed
   - 0: No solution found

4. **Quality of Reasoning (0-5)**: Did agents provide clear explanations and justifications?
   - 5: All agents provided detailed reasoning
   - 3-4: Most agents explained their decisions
   - 1-2: Minimal explanation provided
   - 0: No reasoning provided

5. **Constraint Satisfaction (0-5)**: Were all task constraints satisfied?
   - Check if year requirement (2015+) was met
   - Check if citation count (50+) was met
   - Check if topic relevance was verified

Return ONLY a JSON object (no other text) with this structure:
{
  ""correctness"": <0-5>,
  ""instruction_following"": <0-5>,
  ""efficiency"": <0-5>,
  ""quality_of_reasoning"": <0-5>,
  ""constraint_satisfaction"": <0-5>,
  ""overall_score"": <0-5>,
  ""summary"": ""<brief explanation of scores>"",
  ""strengths"": [""strength1"", ""strength2""],
  ""improvements"": [""area1"", ""area2""]
}
")
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return evaluator;
    }

}