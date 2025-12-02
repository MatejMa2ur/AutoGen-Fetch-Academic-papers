using System;
using AutoGen.Mistral;

namespace Autogen_research_paper_tool_calling_evaluation;

public static class LLMConfiguration
{
    public static MistralClient GetMistralNemo()
    {
        var mistralApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") ?? throw new Exception("Please set MISTRAL_API_KEY environment variable.");

        return new MistralClient(mistralApiKey);
    }
}