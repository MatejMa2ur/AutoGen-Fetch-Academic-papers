namespace ResearchPaperAgent.Configuration;

public record MistralAISettings(
    string ApiKey = "",
    string Model = "open-mistral-nemo",
    int Timeout = 120,
    int MaxRetries = 2);

public record SemanticScholarSettings(
    string ApiUrl = "https://api.semanticscholar.org/graph/v1/paper/search",
    int Timeout = 10,
    int ResultsLimit = 100,
    int MaxResults = 100,
    List<string>? DefaultFields = null)
{
    public List<string> Fields => DefaultFields ?? ["paperId", "title", "year", "citationCount", "authors", "venue"];
}

public record AppSettings(
    MistralAISettings MistralAI = default!,
    SemanticScholarSettings SemanticScholar = default!)
{
    public AppSettings() : this(new MistralAISettings(), new SemanticScholarSettings()) { }
}
