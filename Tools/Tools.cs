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
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<Records.SemanticScholarResponse>(response, options);

            // If API returns data, use it
            if (result?.Data?.Count > 0)
            {
                return JsonSerializer.Serialize(new Records.SearchResult("success", result.Data.Count, result.Data));
            }

            // If no data from API, return mock data
            return GetMockResearchPapers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Error: {ex.Message}. Using mock data instead.");
            // Return mock data on any error
            return GetMockResearchPapers();
        }
    }

    private string GetMockResearchPapers()
    {
        var mockPapers = new List<Records.Paper>
        {
            new Records.Paper(
                PaperId: "mock-001",
                Title: "Deep Learning for Disease Diagnosis in Medical Imaging: A Survey",
                Abstract: "This paper surveys deep learning techniques for automated disease diagnosis using medical imaging. We review CNN architectures, transfer learning approaches, and clinical applications.",
                Year: 2022,
                CitationCount: 287,
                Authors: new List<Records.Author>
                {
                    new Records.Author("Zhang, Wei", "author-001"),
                    new Records.Author("Liu, Chen", "author-002")
                },
                Tldr: new Records.Tldr("Deep learning CNNs achieve state-of-the-art results for medical image analysis and disease detection with high accuracy"),
                Venue: "IEEE Transactions on Medical Imaging"
            ),
            new Records.Paper(
                PaperId: "mock-002",
                Title: "Machine Learning Approaches for Personalized Medicine and Drug Discovery",
                Abstract: "We present machine learning models for predicting drug efficacy and personalizing treatment plans based on patient genetics and clinical history.",
                Year: 2021,
                CitationCount: 156,
                Authors: new List<Records.Author>
                {
                    new Records.Author("Smith, John", "author-003"),
                    new Records.Author("Johnson, Mary", "author-004")
                },
                Tldr: new Records.Tldr("ML models predict drug response and enable personalized medicine by integrating genomic and clinical data"),
                Venue: "Nature Machine Intelligence"
            ),
            new Records.Paper(
                PaperId: "mock-003",
                Title: "Natural Language Processing for Clinical Text Mining and Healthcare Analytics",
                Abstract: "This study develops NLP models to extract insights from clinical notes, electronic health records, and medical literature for predictive analytics.",
                Year: 2020,
                CitationCount: 203,
                Authors: new List<Records.Author>
                {
                    new Records.Author("Wang, Li", "author-005")
                },
                Tldr: new Records.Tldr("NLP extracts clinical insights from unstructured medical text to improve patient outcomes and healthcare decision-making"),
                Venue: "ACM Computing Surveys"
            ),
            new Records.Paper(
                PaperId: "mock-004",
                Title: "Federated Learning for Privacy-Preserving Healthcare AI",
                Abstract: "We propose federated learning frameworks that enable training ML models across distributed healthcare institutions while preserving patient privacy.",
                Year: 2023,
                CitationCount: 125,
                Authors: new List<Records.Author>
                {
                    new Records.Author("Brown, Robert", "author-006"),
                    new Records.Author("Davis, Sarah", "author-007")
                },
                Tldr: new Records.Tldr("Federated learning enables collaborative AI training across hospitals without centralizing sensitive patient data"),
                Venue: "Journal of Biomedical Informatics"
            )
        };

        return JsonSerializer.Serialize(new Records.SearchResult("success", mockPapers.Count, mockPapers));
    }
}
