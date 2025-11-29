#!/usr/bin/env python3
"""
Run the evaluation suite for the research paper search agent.

This script tests the agent on various queries and generates an evaluation report.
"""

import json
import sys
from pathlib import Path
from evaluation import PaperSearchEvaluator


def main():
    # Define test queries with expected criteria
    test_queries = [
        {
            "query": "Find a research paper on machine learning that was published after 2020 and has at least 50 citations.",
            "expected_topic": "machine learning",
            "expected_year": 2020,
            "expected_year_condition": "after",
            "expected_min_citations": 50,
        },
        {
            "query": "Search for papers on neural networks published in 2023 with more than 10 citations.",
            "expected_topic": "neural networks",
            "expected_year": 2023,
            "expected_year_condition": "exact",
            "expected_min_citations": 10,
        },
        {
            "query": "Find a paper about transformers that was published before 2023.",
            "expected_topic": "transformers",
            "expected_year": 2023,
            "expected_year_condition": "before",
            "expected_min_citations": None,
        },
        {
            "query": "Look for research on deep learning from 2022 with at least 25 citations.",
            "expected_topic": "deep learning",
            "expected_year": 2022,
            "expected_year_condition": "exact",
            "expected_min_citations": 25,
        },
        {
            "query": "Find papers on computer vision published after 2019 with more than 100 citations.",
            "expected_topic": "computer vision",
            "expected_year": 2019,
            "expected_year_condition": "after",
            "expected_min_citations": 100,
        },
    ]

    print("=" * 70)
    print("Research Paper Search Agent - Evaluation Suite")
    print("=" * 70)
    print(f"\nRunning {len(test_queries)} test queries...\n")

    evaluator = PaperSearchEvaluator()

    try:
        results = evaluator.run_evaluation_suite(test_queries)

        # Save results to file
        output_file = Path("evaluation_results.json")
        with open(output_file, "w") as f:
            json.dump(results, f, indent=2)

        print("\n" + "=" * 70)
        print("Evaluation Summary")
        print("=" * 70)

        if results["summary"]:
            summary = results["summary"]
            print(f"\nTotal Queries Evaluated: {summary['total_queries']}")
            print(f"Average Score: {summary['average_score']:.1f}/100")
            print(f"Best Score: {summary['max_score']}/100")
            print(f"Worst Score: {summary['min_score']}/100")
            print(f"Queries with score >= 70: {summary['passed_queries']}/{summary['total_queries']}")

        print(f"\nDetailed results saved to: {output_file}")
        print("=" * 70)

        return 0

    except Exception as e:
        print(f"\nError during evaluation: {e}", file=sys.stderr)
        import traceback

        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
