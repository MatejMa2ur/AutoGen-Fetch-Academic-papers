"""
Configuration module for the research paper search agent.

This module centralizes all configuration settings for easy maintenance
and customization.
"""

import os
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

# API Configuration
MISTRAL_API_KEY = os.getenv("MISTRAL_API_KEY")
if not MISTRAL_API_KEY:
    raise ValueError("MISTRAL_API_KEY not found in environment variables. Please set it in your .env file.")

# Semantic Scholar API configuration
SEMANTIC_SCHOLAR_API = "https://api.semanticscholar.org/graph/v1/paper/search"
SEMANTIC_SCHOLAR_TIMEOUT = 10
SEMANTIC_SCHOLAR_RESULTS_LIMIT = 10

# LLM Configuration
LLM_CONFIG = {
    "config_list": [
        {
            "model": "mistral-small-2503",
            "api_type": "mistral",
            "api_key": MISTRAL_API_KEY,
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

# Agent Configuration
AGENT_CONFIG = {
    "max_consecutive_auto_reply": 15,
    "human_input_mode": "NEVER",
}

# Evaluation Configuration
EVALUATION_CONFIG = {
    "evaluation_model": "mistral-small-latest",
    "temperature": 0.0,
    "result_file": "evaluation_results.json",
}

# Search Parameters
SEARCH_CONFIG = {
    "max_results": 5,  # Maximum papers to return per search
    "default_fields": [
        "paperId",
        "title",
        "year",
        "citationCount",
        "authors",
        "venue",
        "openAccessPdf"
    ]
}
