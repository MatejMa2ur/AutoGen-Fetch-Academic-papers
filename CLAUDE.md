# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Commands

**Setup:**
```bash
# Using UV (recommended)
uv venv .venv
uv sync
source .venv/bin/activate

# Using pip
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

**Run the project:**
```bash
# Default (Mistral provider)
uv run main.py

# With specific LLM provider
uv run main.py --google
uv run main.py --mistral
uv run main.py --cerebras

# With output logging
uv run main.py 2>&1 | tee logs/output.log
```

**Environment:**
Create a `.env` file with API keys for your chosen LLM provider:
```
MISTRAL_API_KEY=...
GOOGLE_API_KEY=...
CEREBRAS_API_KEY=...
```

## Architecture Overview

This is a multi-agent research paper discovery system built on AutoGen. The system uses two parallel search agents that collaborate through a hierarchical orchestration pattern:

### Core Flow

1. **User Proxy Agent** - Entry point that receives research tasks and coordinates the multi-agent conversation
2. **Research Paper API Agent** - Searches Semantic Scholar API for papers with metadata (citations, publication year, etc.)
3. **Web Search Orchestrator** - Performs broader web searches and extracts data from web pages
4. **External Judge Agent** - Evaluates and scores the quality of results from both search agents
5. **Group Chat Manager** - Orchestrates conversation between agents

### Key Components

**SearchOrchestrator** (`agents/search_orchestrator.py`) - A custom ConversableAgent that manages an internal multi-agent group chat with:
- A search agent (either ResearchPaperAPIAgent or WebSearchAgent)
- An internal critic agent that validates results
- Its own user proxy agent for tool execution
- Custom speaker selection logic to control agent conversation flow

The SearchOrchestrator is wrapped by an external GroupChat that runs both orchestrators in parallel, with the ExternalJudgeAgent evaluating results from both.

### Agent Communication Pattern

```
Main GroupChat
├── User Proxy → initiates
├── ResearchPaperAPIAgent (wrapped in SearchOrchestrator)
│   └── Internal GroupChat
│       ├── ResearchPaperAPIAgent
│       ├── InternalCriticAgent
│       └── User Proxy (for tool execution)
├── WebSearchOrchestrator (wrapped in SearchOrchestrator)
│   └── Internal GroupChat
│       ├── WebSearchAgent
│       ├── InternalCriticAgent
│       └── User Proxy (for tool execution)
└── ExternalJudgeAgent → evaluates results
```

### Tools Available

- **research_api_tool.py** - `search_semantic_scholar()` searches the Semantic Scholar API with filters for year range and citation count
- **web_search_tool.py** - `search_web()` performs general web searches; `check_pages_for_relevance()` extracts metadata from web pages
- **simple_math_tool.py** - `is_greater()` utility for comparing integers (used by agents for constraint checking)

### Task Definition

Tasks are defined in `utils/task_prompts.py`. Each task is a natural language query like:
```
"Find a research paper on machine learning that was published after 2020 and has over 100 citations."
```

The agents parse these constraints and search accordingly. Results are evaluated by the judge agent on completeness, relevance, honesty, and clarity.

### Key Files to Understand Implementation

- `main.py` - Entry point; sets up all agents, group chat structure, and runs evaluation loop
- `agents/search_orchestrator.py` - Custom orchestration logic with internal speaker selection
- `agents/external_judge_agent.py` - Scoring mechanism for result evaluation
- `agents/internal_critic_agent.py` - Validates search results meet task constraints
- `tools/research_api_tool.py` - Semantic Scholar API integration
- `tools/web_search_tool.py` - Web search and page scraping logic
- `utils/utils.py` - LLM configuration, result extraction, and helper functions

## Development Notes

- **Docker Execution** - Code execution uses DockerCommandLineCodeExecutor. Ensure Docker is available when running.
- **LLM Configuration** - Different providers (Mistral, Google, Cerebras) have different rate limits and timeout settings configured in `utils/utils.py`.
- **Error Handling** - The SearchOrchestrator catches exceptions during inner conversations and returns error messages; these are propagated to the judge.
- **Message Parsing** - Results are extracted from agent messages by looking for "RESULT:" markers; see `utils/utils.py:extract_final_answer()`.
- **Termination Logic** - Conversations terminate when both orchestrators have returned results with "RESULT:" markers, or when max_turns is reached.
