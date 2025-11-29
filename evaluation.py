import json
from typing import Optional, Dict, List, Any
from main import run_paper_search_agent, LLM_CONFIG
from mistralai import Mistral
import os


class PaperSearchEvaluator:
    """Evaluate the performance of the paper search agent."""

    def __init__(self):
        api_key = os.getenv("MISTRAL_API_KEY")
        self.client = Mistral(api_key=api_key)
        self.llm_config = LLM_CONFIG

    def evaluate_paper_match(
        self,
        paper: Dict[str, Any],
        topic: str,
        year: Optional[int] = None,
        year_condition: str = "any",
        min_citations: Optional[int] = None,
    ) -> Dict[str, Any]:
        """
        Use an LLM to evaluate if a paper matches the search criteria.

        Returns a dict with match_score (0-100) and reasoning.
        """
        criteria = f"Topic: {topic}"
        if year:
            criteria += f", Year {year_condition}: {year}"
        if min_citations:
            criteria += f", Min Citations: {min_citations}"

        prompt = f"""Evaluate if this paper matches the search criteria:

Paper:
- Title: {paper.get('title')}
- Year: {paper.get('year')}
- Citations: {paper.get('citations')}
- Venue: {paper.get('venue')}

Criteria: {criteria}

Provide a JSON response with:
1. "match_score": 0-100 (how well does it match?)
2. "matches_topic": true/false
3. "matches_year": true/false
4. "matches_citations": true/false
5. "reasoning": brief explanation

Respond only with valid JSON."""

        # Use Mistral to evaluate
        response = self.client.messages.create(
            model="mistral-small-latest",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.0,
        )

        try:
            result = json.loads(response.content[0].text)
            return result
        except json.JSONDecodeError:
            # Fallback if LLM response isn't valid JSON
            return {
                "match_score": 0,
                "error": "Could not parse LLM response",
                "raw_response": response.content[0].text,
            }

    def evaluate_query_response(
        self,
        query: str,
        response_text: str,
        expected_criteria: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        """
        Evaluate how well the agent responded to a query.

        Uses the LLM to assess relevance, accuracy, and completeness.
        """
        prompt = f"""Evaluate this agent response to a research paper search query:

Query: {query}

Agent Response:
{response_text}

Provide a JSON evaluation with:
1. "clarity_score": 0-100 (how clear is the response?)
2. "completeness_score": 0-100 (did it answer all parts?)
3. "accuracy_score": 0-100 (does it seem accurate?)
4. "overall_score": 0-100 (overall quality)
5. "strengths": list of 2-3 strengths
6. "weaknesses": list of 2-3 weaknesses
7. "feedback": brief constructive feedback

Respond only with valid JSON."""

        response = self.client.messages.create(
            model="mistral-small-latest",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.0,
        )

        try:
            result = json.loads(response.content[0].text)
            return result
        except json.JSONDecodeError:
            return {
                "error": "Could not parse evaluation",
                "raw_response": response.content[0].text,
            }

    def run_evaluation_suite(
        self, test_queries: List[Dict[str, Any]]
    ) -> Dict[str, Any]:
        """
        Run a full evaluation suite on multiple test queries.

        test_queries should be a list of dicts with:
        - query: the search query
        - expected_topic: expected topic
        - expected_year: expected year
        - expected_year_condition: 'exact', 'before', 'after'
        - expected_min_citations: minimum citations expected
        """
        results = {
            "total_queries": len(test_queries),
            "evaluations": [],
            "summary": {},
        }

        scores = []

        for i, test in enumerate(test_queries):
            print(f"\nEvaluating query {i + 1}/{len(test_queries)}: {test['query'][:60]}...")

            # Run the agent
            response = run_paper_search_agent(test["query"])

            # Evaluate the response
            evaluation = self.evaluate_query_response(test["query"], response, test)

            results["evaluations"].append(
                {
                    "query": test["query"],
                    "agent_response": response[:500],  # Truncate for readability
                    "evaluation": evaluation,
                }
            )

            if "overall_score" in evaluation:
                scores.append(evaluation["overall_score"])

        # Calculate summary statistics
        if scores:
            results["summary"] = {
                "average_score": sum(scores) / len(scores),
                "max_score": max(scores),
                "min_score": min(scores),
                "passed_queries": sum(1 for s in scores if s >= 70),
                "total_queries": len(scores),
            }

        return results
