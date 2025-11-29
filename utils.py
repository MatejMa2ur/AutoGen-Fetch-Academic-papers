"""
Utility functions for the research paper search agent.

Handles formatting, logging, and other common operations.
"""

import json
from typing import Dict, Any, List
from datetime import datetime


def format_paper_results(results: str) -> str:
    """
    Parse and format JSON paper results for display.

    Takes raw JSON from search and returns nicely formatted output.
    """
    try:
        data = json.loads(results)
    except json.JSONDecodeError:
        return results  # Return as-is if not JSON

    if data.get("status") == "error":
        return f"Error: {data.get('message')}"

    if data.get("status") == "no_results":
        return f"No papers found matching: {data.get('message')}"

    # Format found papers
    output = []
    papers = data.get("papers", [])

    if papers:
        output.append(f"Found {len(papers)} matching papers:\n")

        for i, paper in enumerate(papers, 1):
            output.append(f"{i}. {paper.get('title')}")
            output.append(f"   Year: {paper.get('year')}")
            output.append(f"   Citations: {paper.get('citations')}")

            authors = paper.get("authors", [])
            if authors:
                author_str = ", ".join(authors)
                output.append(f"   Authors: {author_str}")

            venue = paper.get("venue")
            if venue:
                output.append(f"   Venue: {venue}")

            output.append("")

    return "\n".join(output)


def log_query(query: str, result: str) -> None:
    """Log a query and result to file for debugging."""
    timestamp = datetime.now().isoformat()

    log_entry = {
        "timestamp": timestamp,
        "query": query,
        "result_preview": result[:200] if result else "No result",
    }

    try:
        with open("query_log.jsonl", "a") as f:
            f.write(json.dumps(log_entry) + "\n")
    except Exception as e:
        print(f"Failed to log query: {e}")


def print_section(title: str) -> None:
    """Print a formatted section header."""
    print("\n" + "=" * 70)
    print(f" {title}")
    print("=" * 70 + "\n")


def print_error(message: str) -> None:
    """Print an error message with formatting."""
    print(f"\n❌ Error: {message}\n")


def print_success(message: str) -> None:
    """Print a success message with formatting."""
    print(f"\n✓ {message}\n")
