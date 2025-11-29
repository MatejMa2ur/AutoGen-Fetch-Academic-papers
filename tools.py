import requests
import json
from typing import Optional
from datetime import datetime

# Semantic Scholar API endpoint
SEMANTIC_SCHOLAR_API = "https://api.semanticscholar.org/graph/v1/paper/search"


def search_web(query: str) -> str:
    """Basic web search placeholder - could be extended with Bing/Google API"""
    return f"Web search results for '{query}' would be retrieved from a search engine"


def search_research_papers_api(
    topic: str,
    year: Optional[int] = None,
    year_condition: str = "any",
    min_citations: Optional[int] = None,
) -> str:
    """
    Search for research papers using Semantic Scholar API.

    Args:
        topic: The research topic to search for
        year: The publication year to filter by
        year_condition: 'exact', 'before', 'after', or 'any'
        min_citations: Minimum number of citations

    Returns:
        JSON string with paper details or error message
    """

    # Build the query string
    query_parts = [topic]

    # Add year constraint to query if specified
    if year and year_condition != "any":
        if year_condition == "exact":
            query_parts.append(f"year:{year}")
        elif year_condition == "before":
            query_parts.append(f"year:<{year}")
        elif year_condition == "after":
            query_parts.append(f"year:>{year}")

    search_query = " ".join(query_parts)

    try:
        # Query Semantic Scholar API
        params = {
            "query": search_query,
            "limit": 10,
            "fields": "paperId,title,year,citationCount,authors,venue,openAccessPdf"
        }

        response = requests.get(SEMANTIC_SCHOLAR_API, params=params, timeout=10)
        response.raise_for_status()
        data = response.json()

        papers = data.get("data", [])

        # Filter by criteria if needed
        filtered_papers = []
        for paper in papers:
            # Check year condition
            if year and year_condition != "any":
                paper_year = paper.get("year")
                if year_condition == "exact" and paper_year != year:
                    continue
                elif year_condition == "before" and (not paper_year or paper_year >= year):
                    continue
                elif year_condition == "after" and (not paper_year or paper_year <= year):
                    continue

            # Check citation count
            if min_citations and paper.get("citationCount", 0) < min_citations:
                continue

            filtered_papers.append(paper)

        if not filtered_papers:
            return json.dumps({
                "status": "no_results",
                "message": f"No papers found matching criteria: {topic}",
                "query": search_query
            })

        # Format results
        results = {
            "status": "success",
            "papers_found": len(filtered_papers),
            "papers": []
        }

        for paper in filtered_papers[:5]:  # Return top 5 results
            results["papers"].append({
                "title": paper.get("title"),
                "year": paper.get("year"),
                "citations": paper.get("citationCount", 0),
                "authors": [a.get("name", "Unknown") for a in paper.get("authors", [])[:3]],
                "venue": paper.get("venue", "N/A"),
                "paperId": paper.get("paperId")
            })

        return json.dumps(results, indent=2)

    except requests.exceptions.RequestException as e:
        return json.dumps({
            "status": "error",
            "message": f"Failed to search papers: {str(e)}"
        })
