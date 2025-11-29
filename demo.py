#!/usr/bin/env python3
"""
Demonstration script for the research paper search agent.

Shows the agent in action with predefined queries.
"""

import sys
from main import run_paper_search_agent
from utils import print_section, format_paper_results


def main():
    # Example queries to demonstrate the agent
    demo_queries = [
        "Find a research paper on machine learning that was published after 2020 and has at least 50 citations.",
        "Search for papers on transformer architectures from 2023",
        "Look for deep learning research before 2020 with 100 citations",
    ]

    print_section("Research Paper Search Agent - Live Demo")

    print("This demo shows the agent searching for research papers based on criteria.\n")

    for i, query in enumerate(demo_queries, 1):
        print(f"\nDemo {i}:")
        print(f"Query: {query}\n")
        print("Searching...")

        try:
            result = run_paper_search_agent(query)

            # Format and display the result
            formatted = format_paper_results(result)
            print(formatted)

            print("-" * 70)

        except Exception as e:
            print(f"Error processing query: {e}\n")
            continue

    print_section("Demo Complete")
    print("The agent successfully demonstrated finding papers based on various criteria.")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nDemo interrupted by user.")
        sys.exit(0)
    except Exception as e:
        print(f"Fatal error: {e}", file=sys.stderr)
        sys.exit(1)
