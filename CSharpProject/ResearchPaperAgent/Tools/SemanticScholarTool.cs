namespace ResearchPaperAgent.Tools;

using AutoGen.Core;
using ResearchPaperAgent.Services;

/// <summary>
/// Tool for searching academic research papers using Semantic Scholar API.
/// This class is partial to enable AutoGen source generator for [Function] attribute.
/// </summary>
public partial class SemanticScholarTool
{
    private readonly SemanticScholarService _service;

    public SemanticScholarTool(SemanticScholarService service)
    {
        _service = service;
    }

    /// <summary>
    /// Search for academic research papers by topic, publication year, and citation count.
    /// </summary>
    /// <param name="topic">The research topic or keywords to search for (required)</param>
    /// <param name="year">The publication year to filter by (optional)</param>
    /// <param name="yearCondition">Year filter condition: 'exact', 'before', 'after', or 'any' (default: 'any')</param>
    /// <param name="minCitations">Minimum number of citations required (optional)</param>
    /// <returns>JSON string containing search results with paper details</returns>
    [Function]
    public async Task<string> SearchPapers(
        string topic,
        int? year = null,
        string yearCondition = "any",
        int? minCitations = null)
    {
        return await _service.SearchAsync(topic, year, yearCondition, minCitations);
    }
}
