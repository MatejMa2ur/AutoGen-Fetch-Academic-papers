using System.Text.Json.Serialization;

namespace ResearchPaperAgent.Models;

public record Paper(
    [property: JsonPropertyName("paperId")] string PaperId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("citationCount")] int CitationCount,
    [property: JsonPropertyName("authors")] List<Author> Authors,
    [property: JsonPropertyName("venue")] string? Venue);

