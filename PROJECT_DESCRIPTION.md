# Project Description: Research Paper Discovery Agent

## What This Project Does (Simple Explanation)

This project is a **multi-agent AI system that finds research papers** based on complex search criteria. Instead of manually searching Google Scholar or arXiv yourself, you give the system a natural language request and it automatically searches multiple sources, validates the results, and returns papers that match what you asked for.

### Example Task
```
"Find a research paper on machine learning that was published after 2020 and has over 100 citations"
```

The system then:
1. Tries to find papers matching that criteria
2. Validates that the papers actually meet the requirements
3. Returns the best results it found

## How It Works (Conceptually)

### The Two-Strategy Approach

The system uses **two different search methods running in parallel**:

**Strategy 1: Structured Database Search**
- Queries Semantic Scholar API (a research paper database with metadata)
- Gets precise results with citation counts already included
- Fast and accurate for structured data

**Strategy 2: Web Search**
- Performs general web searches across the internet
- Scrapes web pages to extract paper details (title, authors, year, citations)
- Broader coverage but requires extraction and validation

### The Agent Team

Instead of one "super AI", the system uses multiple specialized AI agents that talk to each other:

1. **Search Agents** - Each uses one of the strategies above to search for papers
2. **Critic Agents** - Validate that found papers actually meet the task requirements
3. **Judge Agent** - Scores and compares results from both search strategies
4. **User Proxy Agent** - Acts like a "hands" agent that can run tools and execute searches

### The Conversation Flow

```
1. Human gives a task → "Find papers on X published after Y with Z citations"

2. Both search strategies run in parallel:
   - API Agent searches Semantic Scholar
   - Web Search Agent searches the internet

3. Each result is checked by a Critic:
   - "Does this paper actually match all the requirements?"
   - If not, go back to searching

4. Once both strategies have results:
   - Judge Agent evaluates both sets of results
   - Scores them on: completeness, relevance, honesty, clarity
   - Returns the best findings

5. Results are saved and reported
```

## Key Concepts to Understand for Your Rework

### 1. **Task Definition**
- Tasks are natural language strings with constraints
- Constraints might be: topic, year range, citation count, number of results needed
- The agents must parse these constraints and apply them

### 2. **Search Strategies**
- You could replace Semantic Scholar with another API (PubMed, IEEE, arXiv, etc.)
- You could replace web search with specialized domain searches
- The key is: each strategy should return papers with metadata (title, authors, year, citations, URL)

### 3. **Validation/Criticism**
- After each search, validate that results actually meet the constraints
- Common issues: papers found but wrong topic, or missing metadata like citation count
- Agents should iterate if results don't match requirements

### 4. **Evaluation**
- Multiple results should be compared and scored
- Scores should reflect: How well constraints were met, result quality, whether data was fabricated

### 5. **Multi-Agent Orchestration**
- Different agents specialize in different tasks (searching vs validating vs judging)
- They communicate by sending messages back and forth
- A "manager" controls who speaks next based on context

## What You Could Change/Rework

**Domain**: Instead of research papers, this could be used to find:
- Job postings that match your experience
- Real estate listings by multiple criteria
- Academic resources (books, courses)
- News articles by topic and date range

**Search Strategies**: Instead of Semantic Scholar + web search:
- Use different APIs (arXiv, IEEE Xplore, PubMed, etc.)
- Use specialized search engines
- Combine database searches with different data sources

**Validation Rules**: Instead of checking publication year and citations:
- Check salaries, locations, certifications for jobs
- Check price ranges, square footage, neighborhoods for real estate
- Check publication type, language, peer-review status for papers

**Evaluation Criteria**: Instead of scoring on relevance and honesty:
- Score on best match to criteria, value for money, accessibility
- Customize scoring based on what matters for your domain

## Tech Stack Used (You Can Replace These)

- **AutoGen**: Framework for multi-agent conversations (you could use: LangChain, LlamaIndex, custom Python)
- **Semantic Scholar API**: Research paper database (replace with domain-specific API)
- **DuckDuckGo Search**: Web search (replace with Google, Bing, or domain-specific search)
- **BeautifulSoup**: Web scraping (replace with other scraping libraries)
- **Multiple LLM Providers**: Mistral, Google Gemini, Cerebras (pick the one you prefer)
- **Python + UV**: Package management and virtual environments

## Key Files to Understand the Logic

- `main.py` - Main orchestration: sets up agents and runs the search tasks
- `agents/search_orchestrator.py` - Controls conversation between agents
- `agents/research_paper_agent.py` - Logic for API-based search
- `agents/web_search_agent.py` - Logic for web search
- `agents/internal_critic_agent.py` - Validation logic
- `agents/external_judge_agent.py` - Scoring logic
- `tools/` - The actual search functions
- `utils/task_prompts.py` - The tasks to test

## Next Steps for Your Rework

1. **Decide your domain**: What are you searching for? (Papers, jobs, products, etc.)
2. **Identify data sources**: What APIs or websites will you search?
3. **Define constraints**: What filters/criteria do users specify? (date range, category, price, location, etc.)
4. **Design validation**: How do you check if results meet the criteria?
5. **Create evaluation criteria**: How do you score/compare results?
6. **Implement agents**: Build specialized agents for each search strategy
7. **Build orchestration**: Connect agents so they work together
8. **Test with examples**: Run it with real search tasks to validate

## Simple Analogy

**Current System**: "I need a book on AI from 2023. The system has two friends: one checks the library database (Semantic Scholar), one checks all bookstores in the city (web search). Each friend brings back candidates, a critic checks if they're really about AI and from 2023, and a judge picks the best options."

**For Your Rework**: Same concept, but with different friends, different data sources, and different validation criteria tailored to your domain.
