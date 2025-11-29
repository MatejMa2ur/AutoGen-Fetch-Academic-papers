from datetime import datetime
import os
from pathlib import Path
import re
from config import LLM_CONFIG, AGENT_CONFIG
from tools import search_research_papers_api
from autogen import (
    AssistantAgent,
    UserProxyAgent,
)


def get_work_dir():
    timestamp = datetime.now().strftime("%Y-%m-%d-%H-%M")
    p = Path.cwd() / "coding" / timestamp
    p.mkdir(parents=True, exist_ok=True)
    return p


def search_papers_tool(topic: str, year: int = None, year_condition: str = "any", min_citations: int = None) -> str:
    """
    Tool wrapper for research paper search that Autogen agents can call.
    Handles parameter extraction and delegates to the main search function.
    """
    return search_research_papers_api(topic, year, year_condition, min_citations)

# Create the research paper search assistant
paper_search_agent = AssistantAgent(
    name="PaperSearchAgent",
    llm_config=LLM_CONFIG,
    system_message="""You are an expert research paper discovery assistant. Your task is to help users find relevant academic papers based on their criteria.

When a user asks for a paper, extract:
1. The research topic/keywords
2. The publication year constraint (if any) - exact year, before year, or after year
3. Minimum citation count (if specified)

Then use the search_papers_tool to find matching papers. Return results in a clear, readable format.""",
)

# Register the paper search tool with the agent
paper_search_agent.register_for_llm(
    name="search_papers_tool",
    description="Search for academic research papers by topic, publication year, and citation count. Parameters: topic (required), year (optional), year_condition ('exact', 'before', 'after'), min_citations (optional)"
)(search_papers_tool)

# Create the user proxy that will execute the tools
user_proxy = UserProxyAgent(
    name="user_proxy",
    human_input_mode="NEVER",
    max_consecutive_auto_reply=15,
    llm_config=False,
    is_termination_msg=lambda m: (m.get("content") or "")
    .rstrip()
    .endswith("TERMINATE"),
)

# Register tool execution with the user proxy
user_proxy.register_for_execution(
    name="search_papers_tool"
)(search_papers_tool)


def run_paper_search_agent(query: str) -> str:
    """
    Run the paper search agent with a given query.
    Returns the chat summary.
    """
    user_proxy.initiate_chat(
        paper_search_agent,
        message=query,
        summary_method="reflection_with_llm",
    )

    # Extract and return the final response
    if user_proxy.last_message():
        return user_proxy.last_message().get("content", "No results found")
    return "No results found"


def parse_search_query(query: str) -> dict:
    """
    Extract search parameters from a natural language query.

    Looks for patterns like:
    - Topic: any words describing the research area
    - Year: "in YYYY", "after YYYY", "before YYYY"
    - Citations: "X citations", "at least X citations"

    Returns a dict with extracted parameters.
    """
    params = {"topic": "", "year": None, "year_condition": "any", "min_citations": None}

    # Extract topic (simplified - everything before year/citations mentions)
    topic_match = query.lower()
    for keyword in ["on", "about", "regarding"]:
        if f" {keyword} " in topic_match:
            idx = topic_match.find(f" {keyword} ") + len(keyword) + 2
            # Get words until we hit temporal/citation keywords
            rest = topic_match[idx:]
            for end_phrase in [
                " that was",
                " published",
                " and has",
                " with",
                " containing",
            ]:
                if end_phrase in rest:
                    topic = rest[: rest.find(end_phrase)].strip()
                    if topic:
                        params["topic"] = topic
                        break
            if params["topic"]:
                break

    # Extract year and condition
    year_patterns = [
        (r"after (\d{4})", "after"),
        (r"before (\d{4})", "before"),
        (r"in (\d{4})", "exact"),
        (r"published (\d{4})", "exact"),
        (r"from (\d{4})", "exact"),
    ]

    for pattern, condition in year_patterns:
        match = re.search(pattern, query.lower())
        if match:
            params["year"] = int(match.group(1))
            params["year_condition"] = condition
            break

    # Extract citation count
    citation_patterns = [
        r"at least (\d+) citations?",
        r"(\d+)\+ citations?",
        r"more than (\d+) citations?",
        r"(\d+) citations?",
    ]

    for pattern in citation_patterns:
        match = re.search(pattern, query.lower())
        if match:
            params["min_citations"] = int(match.group(1))
            break

    return params


def search_papers_with_params(query: str) -> str:
    """
    Parse a natural language query and search for papers using extracted params.
    """
    params = parse_search_query(query)

    if not params["topic"]:
        return "Could not extract a research topic from your query. Please specify what you're looking for."

    # Call the agent with natural language - let it handle the actual search
    return run_paper_search_agent(query)


# Main entry point
if __name__ == "__main__":
    # Interactive mode - process queries from user
    print("=" * 70)
    print("Research Paper Search Agent")
    print("=" * 70)
    print("\nThis agent finds research papers based on your criteria.")
    print("Example: 'Find a paper on machine learning published after 2020 with 50+ citations'")
    print("\nType 'quit' to exit.\n")

    while True:
        try:
            query = input("Enter your search query: ").strip()

            if query.lower() in ["quit", "exit", "q"]:
                print("Goodbye!")
                break

            if not query:
                continue

            print("\nSearching for papers...\n")
            result = search_papers_with_params(query)
            print(f"\nAgent Response:\n{result}\n")

        except KeyboardInterrupt:
            print("\n\nGoodbye!")
            break
        except Exception as e:
            print(f"Error: {e}\n")
