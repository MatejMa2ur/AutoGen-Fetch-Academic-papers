namespace ResearchPaperAgent.Configuration;

using Microsoft.Extensions.Configuration;

public static class ConfigurationLoader
{
    public static AppSettings LoadConfiguration()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = config["MistralAI:ApiKey"] ?? Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? "";

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException(
                "MISTRAL_API_KEY is required. Please set it in your environment or appsettings.json");

        var mistral = new MistralAISettings(
            ApiKey: apiKey,
            Model: config["MistralAI:Model"] ?? "open-mistral-nemo",
            Timeout: int.TryParse(config["MistralAI:Timeout"], out var t) ? t : 120,
            MaxRetries: int.TryParse(config["MistralAI:MaxRetries"], out var r) ? r : 2);

        var fields = config.GetSection("SemanticScholar:DefaultFields").GetChildren()
            .Select(x => x.Value).Where(x => x != null).Cast<string>().ToList();

        var scholar = new SemanticScholarSettings(
            ApiUrl: config["SemanticScholar:ApiUrl"] ?? "https://api.semanticscholar.org/graph/v1/paper/search",
            Timeout: int.TryParse(config["SemanticScholar:Timeout"], out var st) ? st : 10,
            ResultsLimit: int.TryParse(config["SemanticScholar:ResultsLimit"], out var rl) ? rl : 10,
            MaxResults: int.TryParse(config["SemanticScholar:MaxResults"], out var mr) ? mr : 5,
            DefaultFields: fields.Count > 0 ? fields : null);

        return new AppSettings(mistral, scholar);
    }
}
