using System.Text.Json;
using ResearchPaperAgent.Configuration;
using ResearchPaperAgent.Models;

namespace ResearchPaperAgent.Services;

public class SemanticScholarService(SemanticScholarSettings settings)
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(settings.Timeout)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<string> SearchAsync(
        string topic,
        int? year = null,
        string yearCondition = "any",
        int? minCitations = null)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "query", topic },
                { "limit", "100"},
                { "fields", string.Join(",", settings.Fields) }
            };

            // Add year filter to API query
            if (year.HasValue && yearCondition != "any")
            {
                var yearParam = yearCondition switch
                {
                    "exact" => year.ToString(),
                    "after" => $"{year}-",
                    "before" => $"-{year}",
                    _ => null
                };
                if (yearParam != null)
                    parameters["year"] = yearParam;
            }

            // Add citation filter to API query
            if (minCitations.HasValue)
                parameters["minCitationCount"] = minCitations!.Value.ToString();

            var queryString = string.Join("&", parameters
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var url = $"{settings.ApiUrl}?{queryString}";
            var response = await GetWithRetryAsync(url);

            if (!response.IsSuccessStatusCode)
                return CreateErrorResponse($"API returned {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<SemanticScholarResponse>(content, JsonOptions);

            if (apiResponse?.Data.Count == 0)
                return CreateNoResultsResponse(topic);

            Console.WriteLine($"Length: {apiResponse!.Data.Count}");
            // Return all results (up to 100) for the agent to select from
            return CreateSuccessResponse(apiResponse!.Data);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Search failed: {ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> GetWithRetryAsync(string url)
    {
        const int maxRetries = 3;
        const int retryDelay = 2;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return response;

                if ((int)response.StatusCode == 429 && attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay * (1 << attempt)));
                    continue;
                }

                return response;
            }
            catch (TaskCanceledException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(retryDelay * (1 << attempt)));
            }
        }

        throw new InvalidOperationException("Max retries exceeded");
    }


    private static string CreateSuccessResponse(List<Paper> papers) =>
        JsonSerializer.Serialize(new SearchResult("success", papers.Count, papers), JsonOptions);

    private static string CreateNoResultsResponse(string topic) =>
        JsonSerializer.Serialize(new SearchResult("no_results", 0, [], $"No papers found: {topic}"), JsonOptions);

    private static string CreateErrorResponse(string message) =>
        JsonSerializer.Serialize(new SearchResult("error", 0, [], message), JsonOptions);
}
