using System.Text.Json.Serialization;

namespace ResearchPaperAgent.Models;

public record Author(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("authorId")] string? AuthorId = null);

