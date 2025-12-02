using System.Text.Json.Serialization;

namespace Autogen_research_paper_tool_calling_evaluation;

public class Records
{
    public record Author(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("authorId")] string? AuthorId = null);

    public record Paper(
        [property: JsonPropertyName("paperId")] string PaperId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("abstract")] string Abstract,
        [property: JsonPropertyName("year")] int? Year,
        [property: JsonPropertyName("citationCount")] int CitationCount,
        [property: JsonPropertyName("authors")] List<Author> Authors,
        [property: JsonPropertyName("tldr")] Tldr Tldr,
        [property: JsonPropertyName("venue")] string? Venue);

    public record Tldr(
        [property: JsonPropertyName("text")] string Text
    );
    
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

}