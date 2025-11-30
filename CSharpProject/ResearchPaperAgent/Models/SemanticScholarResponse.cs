using System.Text.Json.Serialization;

namespace ResearchPaperAgent.Models;

public record SearchResult(
    [property: JsonPropertyName("status")] string Status = "error",
    [property: JsonPropertyName("papers_found")] int PapersFound = 0,
    [property: JsonPropertyName("papers")] List<Paper> Papers = default!,
    [property: JsonPropertyName("message")] string? Message = null)
{
    public SearchResult() : this("error", 0, new List<Paper>(), null) { }
}

public record SemanticScholarResponse(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("offset")] int Offset,
    [property: JsonPropertyName("next")] int? Next,
    [property: JsonPropertyName("data")] List<Paper> Data);

