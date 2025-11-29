from datetime import datetime
import os
from pathlib import Path
import dotenv
import json
import re
from tools import search_web, search_research_papers_api
from autogen import (
    AssistantAgent,
    UserProxyAgent,
    GroupChat,
    GroupChatManager,
)

dotenv.load_dotenv()
api_key = os.getenv("MISTRAL_API_KEY")
if not api_key:
    raise ValueError("MISTRAL_API_KEY not found in environment variables.")


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

LLM_CONFIG = {
    "config_list": [
        {
            "model": "mistral-small-2503",
            "api_type": "mistral",
            "api_key": api_key,
            "api_rate_limit": 0.5,
            "max_retries": 3,
            "timeout": 30,
            "num_predict": -1,
            "repeat_penalty": 1.1,
            "stream": False,
            "seed": 42,
            "native_tool_calls": False,
            "cache_seed": None,
            "timeout": 120,
        }
    ]
}

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


# Example usage when run directly
if __name__ == "__main__":
    query = "Find a research paper on speed bumps that was published after 2003 and has at least 10 citations."
    result = run_paper_search_agent(query)
