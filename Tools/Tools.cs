using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoGen.Core;

namespace Autogen_research_paper_tool_calling_evaluation.Tools;

public partial class Tools
{
    [Function]
    public async Task<string> GetWeather(string city)
    {
        return $"The weather in {city} is sunny.";
    }

    [Function]
    public async Task<string> GetResearchPapers(string query, int citationsCount, string year)
    {
        try
        {
            var fields = "paperId,title,year,citationCount,authors,venue";
            var url = $"https://api.semanticscholar.org/graph/v1/paper/search?query={Uri.EscapeDataString(query)}&limit=10&fields={fields}";

            if (!string.IsNullOrEmpty(year) && year != "any")
            {
                url += $"&year={Uri.EscapeDataString(year)}";
            }

            if (citationsCount > 0)
            {
                url += $"&minCitationCount={citationsCount}";
            }

            var httpClient = new HttpClient();

            var response = await httpClient.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<Records.SemanticScholarResponse>(response, options);

            return JsonSerializer.Serialize(new Records.SearchResult("success", result?.Data?.Count ?? 0, result?.Data ?? []));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new Records.SearchResult("error", 0, [], ex.Message));
        }
    }
}
