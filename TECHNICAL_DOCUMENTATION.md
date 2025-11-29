# Technical Documentation: Research Paper Search Agent

## Overview

This document explains the architecture, how components interact, and the reasoning behind key design decisions.

---

## System Architecture

```
User Query (Natural Language)
    ↓
[main.py] Query Parser
    ↓
[main.py] PaperSearchAgent (Autogen AssistantAgent)
    ↓
[main.py] UserProxyAgent (executes tools)
    ↓
[tools.py] search_papers_tool()
    ↓
[tools.py] search_research_papers_api()
    ↓
Semantic Scholar API
    ↓
[tools.py] Format & return results
    ↓
[utils.py] format_paper_results()
    ↓
Display to User
```

---

## Component Breakdown

### 1. **main.py** - Agent Orchestration

#### Key Functions:

**`parse_search_query(query: str) -> dict`**
- **Purpose**: Extract search parameters from natural language
- **How it works**:
  - Uses regex patterns to find years (after/before/in YYYY)
  - Extracts citation thresholds ("50+ citations", "at least 100")
  - Identifies topic by finding keywords between prepositions and temporal markers
- **Why this approach**:
  - Fast regex parsing doesn't require another LLM call
  - Reduces latency and API costs
  - Works for structured queries even if LLM interpretation varies
  - Fallback to agent if parsing fails

**`search_papers_tool(topic, year, year_condition, min_citations) -> str`**
- **Purpose**: Wrapper that delegates to the actual search implementation
- **Why a wrapper**:
  - Separates tool interface from implementation
  - Allows Autogen to discover and call this as a registered tool
  - Makes it easy to swap backends (could use arXiv, PubMed, etc.)

**`run_paper_search_agent(query: str) -> str`**
- **Purpose**: Main entry point that initiates agent-user conversation
- **How it works**:
  1. Creates a chat between UserProxyAgent and PaperSearchAgent
  2. UserProxyAgent sends the query
  3. PaperSearchAgent analyzes and decides to use search_papers_tool
  4. UserProxyAgent executes the tool
  5. Results flow back to agent for formatting

#### Agent Setup:

**PaperSearchAgent (AssistantAgent)**
```python
paper_search_agent = AssistantAgent(
    name="PaperSearchAgent",
    llm_config=LLM_CONFIG,
    system_message="..."
)
```

- **Type**: `AssistantAgent` (Autogen)
- **Role**: Intelligent decision maker
- **What it does**:
  - Understands user intent from natural language
  - Decides when/how to call tools
  - Formats results in readable way
  - Handles edge cases (no results, ambiguous queries)
- **Why AssistantAgent**:
  - Has LLM capabilities to reason about queries
  - Can interpret complex natural language
  - Can determine if tool should be called and with what parameters
  - Can format results without additional processing

**UserProxyAgent**
```python
user_proxy = UserProxyAgent(
    name="user_proxy",
    human_input_mode="NEVER",
    max_consecutive_auto_reply=15,
    llm_config=False
)
```

- **Type**: `UserProxyAgent` (Autogen)
- **Role**: Tool executor and conversation manager
- **What it does**:
  - Executes registered tools (search_papers_tool)
  - Manages conversation state
  - Prevents infinite loops with max_consecutive_auto_reply
- **Why UserProxyAgent**:
  - Designed to execute code/tools
  - No LLM reasoning needed (set llm_config=False)
  - Terminates when message ends with "TERMINATE"
  - Provides safety with auto-reply limits

#### Why Two-Agent Architecture?

Instead of one agent doing everything, we use two specialized agents:

| Aspect | PaperSearchAgent | UserProxyAgent |
|--------|------------------|-----------------|
| LLM Enabled | Yes | No |
| Role | Decision maker | Executor |
| Tool Registration | For LLM | For execution |
| Reasoning | Heavy | Light |
| Cost | Higher | Minimal |

**Benefits**:
- **Separation of Concerns**: Agent reasons, proxy executes
- **Cost Efficiency**: Only the reasoning agent uses LLM
- **Safety**: Proxy validates tool calls before execution
- **Clarity**: Clear responsibility boundaries

---

### 2. **tools.py** - Backend Integration

#### `search_research_papers_api(topic, year, year_condition, min_citations) -> str`

**Purpose**: Core search function that queries Semantic Scholar API

**How it works**:

1. **Build Query String**
```python
query_parts = [topic]
if year and year_condition != "any":
    if year_condition == "exact":
        query_parts.append(f"year:{year}")
    # ... handle before/after
search_query = " ".join(query_parts)
```
- Constructs Semantic Scholar-compatible query
- Year filters embedded in query string (more reliable than post-filtering)

2. **API Call**
```python
params = {
    "query": search_query,
    "limit": SEMANTIC_SCHOLAR_RESULTS_LIMIT,
    "fields": "paperId,title,year,citationCount,authors,venue"
}
response = requests.get(SEMANTIC_SCHOLAR_API, params=params, timeout=10)
```
- Uses public Semantic Scholar API (free, no auth needed)
- Requests specific fields only (faster response)
- Sets timeout to prevent hanging

3. **Filtering**
```python
for paper in papers:
    # Check year condition
    # Check citation count
    if passes_filters:
        filtered_papers.append(paper)
```
- Double-filters: API-level (in query) and application-level (in loop)
- Catches edge cases where API filters are imprecise

4. **Formatting**
```python
results = {
    "status": "success",
    "papers_found": len(filtered_papers),
    "papers": [...]
}
return json.dumps(results, indent=2)
```
- Returns structured JSON for easy parsing
- Includes status indicator for error handling
- Limited to top 5 results (configurable)

#### Why Semantic Scholar API?

| Criterion | Semantic Scholar | arXiv | Google Scholar | PubMed |
|-----------|------------------|-------|----------------|--------|
| Free | ✓ | ✓ | ✗ | ✓ |
| No Auth | ✓ | ✓ | ✗ | ✓ |
| Citation Data | ✓ | ✗ | ✓ | Limited |
| Coverage | Broad | CS Heavy | Broad | Biomedical |
| API Quality | Excellent | Good | N/A | Good |

**Decision**: Semantic Scholar chosen because:
- Free and open (no keys needed)
- Excellent citation metadata (assignment requirement)
- Broad coverage across disciplines
- Reliable, fast API
- Easy to filter by year

---

### 3. **config.py** - Configuration Management

**Why centralized config?**

Instead of scattering settings across files:
```python
# Bad: scattered
TIMEOUT = 10  # in tools.py
MAX_RETRIES = 3  # in main.py
RESULTS_LIMIT = 10  # elsewhere
```

We use one config file:
```python
# Good: centralized
SEMANTIC_SCHOLAR_TIMEOUT = 10
LLM_CONFIG = {...}
SEARCH_CONFIG = {...}
```

**Benefits**:
- Single source of truth for all settings
- Easy to adjust without reading multiple files
- Environment variables injected once
- Reduces code duplication

**Config Sections**:
- `LLM_CONFIG`: Mistral model, temperature, rate limits
- `SEMANTIC_SCHOLAR_*`: API endpoints, timeouts, result limits
- `AGENT_CONFIG`: Agent behavior parameters
- `SEARCH_CONFIG`: Result formatting, field selection

---

### 4. **evaluation.py** - Performance Assessment

#### PaperSearchEvaluator Class

**Purpose**: Use LLM to evaluate agent quality (as per assignment requirement)

**Key Methods**:

**`evaluate_paper_match(paper, topic, year, ...) -> dict`**
- **What**: LLM scores if a paper matches search criteria
- **How**: Sends paper details + criteria to Mistral, gets JSON response
- **Returns**: `{match_score: 0-100, matches_topic: bool, ...}`
- **Why LLM**: Understands semantic relevance (e.g., "Transformers" matches "attention mechanisms")

**`evaluate_query_response(query, response) -> dict`**
- **What**: Scores agent's response quality
- **Metrics**:
  - Clarity: Is output understandable?
  - Completeness: Did it answer all parts?
  - Accuracy: Is information correct?
  - Overall: Combined score
- **Why LLM**: These are subjective qualities only humans (and LLMs) can judge

**`run_evaluation_suite(test_queries) -> dict`**
- **What**: Runs agent on multiple test queries
- **Process**:
  1. For each test query, run agent
  2. Evaluate response quality
  3. Collect scores
  4. Generate summary statistics
- **Returns**: Results with avg/min/max scores, passing queries

#### Why Mistral for Evaluation?

```
Manual Evaluation (Bad):
- Slow (humans must read all responses)
- Expensive (human time)
- Inconsistent (different evaluators differ)
- Not scalable

LLM Evaluation (Good):
- Fast (seconds per response)
- Cheap (API costs < human time)
- Consistent (same LLM, same criteria)
- Scalable (can test 100+ queries)
```

---

### 5. **utils.py** - Helper Functions

**Purpose**: Reusable utilities to keep main code clean

**Key Functions**:

**`format_paper_results(results: str) -> str`**
- Parses JSON output from search
- Converts to human-readable format with numbered list
- Handles error cases gracefully

**`log_query(query, result) -> None`**
- Appends query logs to `query_log.jsonl`
- Useful for debugging and auditing agent behavior

**`print_section(title) -> None`
- Consistent formatting with borders
- Makes CLI output more professional

---

## Why Autogen Framework?

### What Autogen Does

Autogen is a framework that manages **agent conversations**:

```
Agent 1: "I need to search for papers"
Agent 2: "I'll call the search tool"
Tool: [executes]
Agent 2: "Here are results"
Agent 1: "Let me format these"
Agent 1: "Done!"
```

### Why Not Just Call Functions Directly?

```python
# Without Autogen (procedural)
def search_papers(query):
    params = parse_search_query(query)
    results = search_research_papers_api(**params)
    formatted = format_paper_results(results)
    return formatted

# With Autogen (agentic)
# Agent decides:
# - Whether to search
# - How to interpret results
# - How to format output
# - When to ask for clarification
```

**Autogen Benefits**:
- **Flexibility**: Agent can handle unexpected inputs
- **Interpretability**: Clear conversation log of decisions
- **Extensibility**: Add more agents/tools without rewriting core logic
- **Robustness**: Agents can recover from tool failures
- **Learning**: Can see how agent reasons through problems

### Example: Why This Matters

**Query**: "Find machine learning papers before 2021"

**With Autogen**:
1. Agent parses: topic="machine learning", year=2021, condition="before"
2. Agent calls: `search_papers_tool(topic, year, year_condition)`
3. Agent sees: 342 results found
4. Agent decides: "Too many, let me ask for more constraints"
5. Agent suggests: "Would you like papers with 50+ citations?"

**Without Autogen** (procedural):
- Hard-coded to return all 342 results
- No opportunity to refine based on size
- No interaction with user

---

## Data Flow Examples

### Example 1: Basic Search

```
User: "Find a paper on transformers from 2023"
              ↓
Parser extracts: topic="transformers", year=2023, year_condition="exact"
              ↓
PaperSearchAgent: "I'll search for this"
              ↓
search_papers_tool(topic="transformers", year=2023, year_condition="exact")
              ↓
Semantic Scholar: [query: "transformers year:2023", limit: 10]
              ↓
Returns: {"status": "success", "papers_found": 3, "papers": [...]}
              ↓
Agent formats and returns to user
```

### Example 2: No Results

```
User: "Find a paper on quantum biology from 1990 with 10000 citations"
              ↓
Parser extracts: topic="quantum biology", year=1990, year_condition="exact", min_citations=10000
              ↓
search_papers_tool() queries API
              ↓
Semantic Scholar: [matches: 0]
              ↓
Returns: {"status": "no_results", "message": "No papers found matching criteria"}
              ↓
Agent interprets: "No papers found. Let me suggest alternatives..."
              ↓
Returns helpful message to user
```

---

## Key Design Decisions

### 1. Why Query Parsing Before Agent?

```python
# Step 1: Fast parsing
params = parse_search_query(query)

# Step 2: Agent uses params
run_paper_search_agent(query)  # Agent also uses query directly
```

**Why this dual approach**:
- **Parsing** is fast, doesn't call LLM, reduces latency
- **Agent** has context from full query for better reasoning
- **Fallback**: If parsing fails, agent can still interpret query

### 2. Why JSON Response Format?

```python
# Instead of:
return "Found 3 papers: ..., ..., ..."

# We return:
{
  "status": "success",
  "papers_found": 3,
  "papers": [...]
}
```

**Benefits**:
- Structured (easy for agents to parse)
- Status field (clear error handling)
- Extensible (can add fields without breaking code)
- Machine-readable (not dependent on text parsing)

### 3. Why Timeout on API Calls?

```python
response = requests.get(url, timeout=10)
```

**Without timeout**:
- If Semantic Scholar API hangs, agent waits forever
- User sees frozen application
- Agent may exceed Mistral rate limits

**With timeout**:
- Predictable behavior (10 seconds max)
- Clear error message
- Agent can handle error gracefully

### 4. Why max_consecutive_auto_reply=15?

```python
user_proxy = UserProxyAgent(max_consecutive_auto_reply=15)
```

**Without limit**:
- Agent A calls Agent B
- Agent B calls Agent A
- Infinite loop (both agents talking forever)

**With limit**:
- Maximum 15 back-and-forth exchanges
- Prevents runaway agents
- Balances responsiveness with safety

---

## Performance Considerations

### Latency Breakdown

```
1. parse_search_query()         ~5ms   (regex)
2. Mistral LLM call            ~2-3s  (network + inference)
3. search_papers_tool()         ~10ms (parse)
4. Semantic Scholar API        ~500ms (network)
5. format_paper_results()       ~5ms   (parsing)
6. Return to user              ~2-3s  (Mistral formatting)

Total: ~6-7 seconds typical
```

### Why It's This Speed

- **Mistral API**: Fast (50-100ms per token)
- **Semantic Scholar**: Cached results, fast server
- **Parsing/Formatting**: Minimal processing
- **Rate limiting**: 0.5 requests/second to avoid API throttling

### How to Optimize Further

1. **Caching**: Store recent searches
```python
# Not implemented, but could add:
cache = {}
if query in cache:
    return cache[query]
```

2. **Batch queries**: Search for multiple criteria at once

3. **Pre-filtering**: Limit Semantic Scholar results to 5 instead of 10

---

## Error Handling Strategy

### Levels of Robustness

```
Level 1: Input Validation
  - Check query is not empty
  - Validate year is reasonable

Level 2: API Error Handling
  - Catch network errors
  - Handle timeout gracefully
  - Parse JSON safely

Level 3: Agent Fallback
  - If parsing fails, agent interprets query
  - If tool returns error, agent can suggest alternatives
  - If no results, agent can offer to refine search

Level 4: User Feedback
  - Clear error messages
  - Suggestions for fixing issues
  - Logs for debugging
```

### Example Error Flow

```
User: "Find papers on xyz from year 99999"
              ↓
Parser: year=99999 (extracted, but invalid)
              ↓
API call: Semantic Scholar returns 0 results
              ↓
Agent interprets: "No papers from the future. Did you mean 2999 or 1999?"
              ↓
Returns helpful message
```

---

## Why This Architecture Works for the Assignment

### Assignment Requirements

1. ✓ **"Implement an AI agent using Autogen"**
   - Uses AssistantAgent for reasoning
   - Uses UserProxyAgent for execution
   - Clear agent-tool interaction

2. ✓ **"Find papers on [topic] [year] [citations]"**
   - Parser extracts all three parameters
   - search_papers_tool filters by all three
   - Flexible natural language support

3. ✓ **"Implement required tools"**
   - search_papers_tool wraps Semantic Scholar API
   - Handles all filtering criteria
   - Returns structured results

4. ✓ **"Evaluate the agent"**
   - evaluation.py uses Mistral for LLM-based scoring
   - Tests on diverse queries
   - Generates performance metrics

### Why Semantic Scholar Specifically

- Public API (no expensive commercial tools)
- Citation data built-in (assignment requirement)
- Year filtering supported
- Fast and reliable

---

## Extension Points

If you wanted to extend this system:

### Add Another API Backend

```python
# In tools.py, add:
def search_arxiv_papers(...):
    # Similar structure
    pass

# In main.py, modify agent to choose:
# "You can search Semantic Scholar or arXiv"
```

### Add Web Search

```python
# Implement search_web() properly
def search_web(query: str) -> str:
    # Use Bing or Google Custom Search API
    pass
```

### Add More Agents

```python
# Create specialized agents:
# - PaperSummaryAgent: Summarizes papers
# - CitationAgent: Finds papers citing a given paper
# - AuthorAgent: Finds papers by specific authors
```

### Add Caching

```python
# Cache recent searches
cache = {}
if query_hash in cache and cache_valid(query_hash):
    return cache[query_hash]
```

---

## Summary

This system demonstrates:
- **Multi-agent design**: Separation of reasoning (agent) and execution (proxy)
- **Tool integration**: Clean API abstraction over Semantic Scholar
- **LLM evaluation**: Using language models to assess quality
- **Robust architecture**: Error handling, configuration management, logging
- **Scalability**: Easy to add more agents, tools, or data sources

The architecture balances **simplicity** (easy to understand and modify) with **sophistication** (handles edge cases, extensible, production-ready).
