# Research Paper Discovery Agent

A C# application using AutoGen for .NET and Mistral AI to search for academic research papers from Semantic Scholar.

## Overview

This agent helps users find relevant academic papers based on their search criteria:
- Research topic/keywords
- Publication year (exact, before, or after)
- Minimum citation count

The system uses a three-stage pipeline:
1. **Fetch**: Retrieves up to 100 papers from Semantic Scholar API with filtering
2. **Analyze & Select**: AI agent analyzes papers and intelligently selects TOP 5
3. **Evaluate**: Judge scores the selection and automatically retries if score < 70
4. **Feedback Loop**: Mistral AI generates intelligent feedback to guide next attempt

Key feature: **Automatic retry with LLM-based feedback** (up to 3 attempts) for intelligent paper selection improvement.

## Architecture

**Three-Stage Pipeline:**

```
[Stage 1: Fetch]  → Semantic Scholar API (100 papers, single call)
      ↓
[Stage 2: Analyze] → MistralClientAgent selects TOP 5 papers
      ↓
[Stage 3: Evaluate] → PaperSearchJudge scores selection (1-100)
      ↓
   Score >= 70? YES → Success! Display results
      ↓
   NO → GenerateLLMBasedFeedback (up to 3 retries)
      ↓
[Retry Loop] → Agent re-analyzes with feedback (repeat Stage 2-3)
```

**Components:**
- **MistralClientAgent**: LLM-powered agent for paper analysis and selection
- **PaperSearchJudge**: Heuristic evaluation engine scoring paper quality (4 metrics: Correctness, Adherence, Completeness, Usefulness)
- **SemanticScholarService**: HTTP client with exponential backoff for Semantic Scholar API
- **SemanticScholarTool**: Tool for searching papers with natural language query parsing
- **FunctionCallMiddleware**: Manages tool invocation
- **GenerateLLMBasedFeedbackAsync**: Creates intelligent feedback using Mistral AI when score < 70

**Technology Stack:**
- .NET 10
- AutoGen for .NET 0.2.3
- Mistral AI API (dual use: agent analysis + feedback generation)
- Semantic Scholar API

## Setup

### Prerequisites
- .NET 10 SDK installed
- Mistral AI API key

### Installation

1. Copy the `.env.example` file to `.env`:
```bash
cp .env.example .env
```

2. Add your Mistral API key to `.env`:
```
MISTRAL_API_KEY=your_mistral_api_key_here
```

3. Restore dependencies:
```bash
dotnet restore
```

## Running the Application

```bash
dotnet run
```

The application will start an interactive loop where you can ask for papers:

```
Your query: Find a paper on machine learning published after 2020 with 50+ citations
```

### Example Queries

- "Find papers on deep learning"
- "Search for machine learning papers from 2023"
- "Find papers on neural networks with at least 100 citations"
- "Show me papers on transformers published after 2021"

## Configuration

### appsettings.json

Non-sensitive configuration:

```json
{
  "MistralAI": {
    "Model": "open-mistral-nemo",
    "Timeout": 120,
    "MaxRetries": 2
  },
  "SemanticScholar": {
    "ApiUrl": "https://api.semanticscholar.org/graph/v1/paper/search",
    "Timeout": 10,
    "ResultsLimit": 10,
    "MaxResults": 5,
    "DefaultFields": ["paperId", "title", "year", "citationCount", "authors", "venue"]
  }
}
```

### Environment Variables

- `MISTRAL_API_KEY`: Your Mistral AI API key (required)

## Project Structure

```
ResearchPaperAgent/
├── Configuration/
│   ├── AppSettings.cs               # Configuration models
│   └── ConfigurationLoader.cs       # Loads from appsettings.json + env vars
├── Models/
│   ├── Author.cs
│   ├── Paper.cs                     # Research paper data model
│   ├── SearchResult.cs              # API response wrapper
│   ├── EvaluationModels.cs          # EvaluationScore (metrics + overall score)
│   └── SemanticScholarResponse.cs   # API response mapping
├── Services/
│   ├── SemanticScholarService.cs    # Stage 1: Fetches papers (with retry logic)
│   ├── PaperSearchJudge.cs          # Stage 3: Evaluates selections (4 metrics)
│   ├── EvaluationRunner.cs          # Batch evaluation runner
│   └── EvaluationReportGenerator.cs # Report generation
├── Tools/
│   └── SemanticScholarTool.cs       # [Function] decorated tool for agent
├── Program.cs                        # Main entry point
│   ├── Lines 10-53:   System messages (Search & Selection prompts)
│   ├── Lines 155-195: Main query loop & retry orchestration
│   ├── Lines 217-318: ProcessQueryWithRetryAsync() - retry wrapper
│   ├── Lines 321-374: GenerateLLMBasedFeedbackAsync() - LLM feedback
│   └── Lines 376-410: GenerateRetryFeedback() - heuristic feedback
├── appsettings.json                 # Configuration defaults
└── ResearchPaperAgent.csproj
```

## Key Features

### Three-Stage Pipeline with Auto-Retry

**Stage 1 - Fetch (Single API Call)**
- Queries Semantic Scholar API once for up to 100 papers
- API-side filtering by year and citation count
- Results cached for Stages 2-3

**Stage 2 - Analyze & Select**
- MistralClientAgent intelligently analyzes 100 papers
- Selects TOP 5 based on: citation impact, venue quality, recency, relevance
- On retry: receives feedback appended to system message for improvement

**Stage 3 - Evaluate**
- PaperSearchJudge scores selection with 4 metrics (0-5 each):
  - **Correctness**: Paper quality based on citations
  - **Adherence**: Meeting query constraints (year, citations)
  - **Completeness**: Metadata quality (authors, venues present)
  - **Usefulness**: Impact and relevance (landmark papers)
- Overall Score: (avg of 4 metrics) × 20 = 0-100 scale

**Automatic Retry Loop**
- If score < 70: Automatically retry (up to 3 attempts)
- Mistral AI generates intelligent, context-aware feedback
- Feedback analyzes which metrics are low and suggests improvements
- Example feedback: "Correctness too low (2/5): Look for papers with 500+ citations"
- On retry: Agent re-analyzes with feedback + updated system message
- Falls back to heuristic feedback if LLM feedback generation fails

### Intelligent Feedback Generation

**LLM-Based Feedback** (when score < 70)
- Mistral AI evaluates why selection scored poorly
- Provides 2-3 specific, actionable suggestions
- Considers both selected papers and available papers
- Guides agent toward better selections

**Heuristic Feedback** (fallback)
- Rule-based feedback for quick retry guidance
- Triggers based on which metric is lowest
- Provides specific targets (e.g., "look for 500+ citations")

### API Integration & Resilience
- Exponential backoff retry logic for rate limiting (HTTP 429)
- Maximum 3 retry attempts on API failures
- Configurable timeouts
- Natural language query parsing with regex extraction

### Error Handling
- Graceful fallbacks (LLM feedback → heuristic feedback)
- Clear error messages for configuration issues
- JSON-formatted responses for all scenarios
- Null safety checks throughout retry loop

## Testing

### Build
```bash
dotnet build
```

### Run the Application
```bash
dotnet run
```

### Manual Test Scenarios

The application supports interactive testing with various query types:

1. **Basic search** (likely high score on first attempt):
   ```
   Your query: Find papers on machine learning
   → Expected: Score >= 70 on first attempt
   → Output: "✓ Excellent score achieved on first attempt!"
   ```

2. **Specific query with constraints** (may trigger retry):
   ```
   Your query: Find papers on transformer models with 500 citations published after 2023
   → Expected: May score < 70 on first attempt due to strict constraints
   → Output: "Attempt 1: Overall Score: 65/100"
   → Then: "⚠ Score too low. Retrying with feedback... (1/3)"
   → See: "💡 AI-Generated Feedback: [LLM suggestions]"
   → Then: "Attempt 2: Overall Score: 78/100" (improved!)
   ```

3. **Broad search** (usually succeeds quickly):
   ```
   Your query: Find papers on deep learning
   → Expected: High-quality results on first attempt
   → Output: Score 80-95/100
   ```

4. **Special commands**:
   ```
   Your query: eval
   → Quick evaluation of a single query

   Your query: eval-full
   → Batch evaluation of 5 test queries with report generation

   Your query: quit
   → Exit application
   ```

### What to Look For

**Successful Execution (Score >= 70):**
- ✓ Excellent score achieved on first attempt!
- Shows: 5 papers with full metadata (title, authors, citations, venue)
- Evaluation metrics all >= 3/5

**Retry Triggered (Score < 70):**
- ⚠ Score too low. Retrying with feedback...
- Shows: "💡 AI-Generated Feedback:" followed by LLM suggestions
- Falls back to heuristic feedback if LLM fails
- Shows attempt counter: "Attempt 1", "Attempt 2", etc.
- Final message: "📊 Achieved score after X retries"

**Evaluation Metrics Interpretation:**
- Score >= 80: Excellent (5+ landmark papers)
- Score 70-79: Good (meets requirements)
- Score 60-69: Fair (needs improvement, triggers retry)
- Score < 60: Poor (likely triggers retry)

## API Integration

### Semantic Scholar API
- **Endpoint**: `https://api.semanticscholar.org/graph/v1/paper/search`
- **Fields**: paperId, title, year, citationCount, authors, venue, openAccessPdf
- **Rate Limit**: Max 3 retries with exponential backoff

### Mistral AI
- **Model**: open-mistral-nemo
- **Timeout**: 120 seconds
- **Max Retries**: 2

## Troubleshooting

### "MISTRAL_API_KEY is required"
Ensure you have created a `.env` file and added your Mistral API key.

### Tool not being called
- Ensure the class is `partial`
- Check that `RegisterMessageConnector()` is called on the agent
- Verify the `[Function]` attribute is present on the method

### No results found
- Try a simpler query (e.g., just a topic)
- Check if the Semantic Scholar API is responding
- Verify the filters aren't too restrictive

## Development Notes

### AutoGen for .NET Specifics
- Tools must return `Task<string>`
- Classes with `[Function]` attributes must be `partial`
- Source generator creates `FunctionContract` and `Wrapper` members automatically
- Call `RegisterMessageConnector()` for proper tool calling

### Current Capabilities (v2.0)
- ✅ Auto-retry with score-based thresholds (70/100)
- ✅ LLM-based intelligent feedback generation
- ✅ Heuristic feedback fallback system
- ✅ Four-metric evaluation system (Correctness, Adherence, Completeness, Usefulness)
- ✅ API-side filtering (year ranges, citation counts)
- ✅ Batch evaluation with report generation
- ✅ Exponential backoff retry for API rate limiting

### Future Enhancements
- [ ] Make maxRetries configurable per query
- [ ] Add advanced query parsing (boolean operators, complex constraints)
- [ ] Cache previous searches to avoid redundant API calls
- [ ] Store retry statistics for learning and optimization
- [ ] Add adaptive retry strategy (increase difficulty each attempt)
- [ ] Implement weighted feedback (prioritize high-impact improvements)
- [ ] Build REST API wrapper for external consumption
- [ ] Add unit tests for service layer
- [ ] Support for additional paper sources (arXiv, IEEE Xplore, etc.)
- [ ] User preference learning (remember preferred venues, authors)

## Evaluation System Details

### Scoring Metrics (PaperSearchJudge)

The judge evaluates each paper selection on 4 dimensions (each 1-5):

| Metric | Measures | High Score (5) | Low Score (1) |
|--------|----------|---|---|
| **Correctness** | Paper quality based on citations | 5+ highly-cited papers (500+) | Papers with <100 citations |
| **Adherence** | Meeting query constraints | All papers match year/citation filters | Papers don't match constraints |
| **Completeness** | Metadata quality | All papers have authors + venues | Missing author/venue info |
| **Usefulness** | Impact and relevance | Papers from top venues (NeurIPS, Nature) | Papers from low-impact venues |

**Overall Score Calculation:**
```
AverageScore = (Correctness + Adherence + Completeness + Usefulness) / 4
OverallScore = AverageScore × 20   (scale to 0-100)
```

### Retry Loop Flow

```
Attempt 1:
├─ Agent analyzes 100 papers
├─ Selects TOP 5
├─ Judge evaluates → Score = 65/100
└─ Score < 70? YES → Generate feedback

LLM Feedback Generation:
├─ Analyze which metrics are low (e.g., Correctness: 2/5)
├─ Suggest improvements (e.g., "Look for papers with 500+ citations")
└─ Append to system message for next attempt

Attempt 2:
├─ Agent receives feedback + updated system message
├─ Re-analyzes same 100 papers with guidance
├─ Selects different TOP 5
├─ Judge evaluates → Score = 78/100
└─ Score >= 70? YES → Success! Display results
```

## License

This project is part of an educational assignment.
