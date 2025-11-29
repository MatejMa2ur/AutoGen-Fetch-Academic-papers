# Research Paper Search Agent with Autogen

An AI agent powered by Autogen and Mistral AI that finds research papers based on specific criteria (topic, publication year, citation count).

## What It Does

The agent responds to queries like:
> "Find a research paper on machine learning that was published after 2020 and has at least 50 citations."

It parses the request, searches Semantic Scholar API, and returns matching papers with full details.

## Architecture

- **Agent**: Autogen `AssistantAgent` that orchestrates paper searches
- **Tool**: Semantic Scholar API integration for querying academic papers
- **LLM**: Mistral AI for natural language understanding
- **Evaluation**: LLM-based scoring of agent performance

## Setup

### Prerequisites

- Python 3.12+
- Mistral AI API key (free tier available at [mistral.ai](https://mistral.ai))

### Installation

#### Using UV (Recommended)

```sh
uv venv .venv
uv sync
source .venv/bin/activate
```

#### Using pip/venv

```sh
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

### Configuration

Create a `.env` file in the project root:

```env
MISTRAL_API_KEY=your_api_key_here
```

## Usage

### Interactive Mode

```sh
python main.py
```

This starts an interactive shell where you can enter queries like:
- "Find papers on deep learning from 2023 with 50+ citations"
- "Search for neural networks published after 2020"
- "Look for computer vision papers before 2023 with 100 citations"

### Programmatic Usage

```python
from main import run_paper_search_agent

result = run_paper_search_agent(
    "Find a paper on transformers published after 2023"
)
print(result)
```

### Run Evaluation Suite

```sh
python run_evaluation.py
```

This evaluates the agent on predefined test queries and saves results to `evaluation_results.json`.

## Project Structure

- **main.py**: Core agent logic and interactive interface
- **tools.py**: Semantic Scholar API integration
- **config.py**: Centralized configuration
- **evaluation.py**: LLM-based evaluation framework
- **run_evaluation.py**: Evaluation suite and reporting

## Configuration

All settings are managed in `config.py`. Key configurations:

- `MISTRAL_API_KEY`: Your Mistral API key
- `LLM_CONFIG`: LLM model and parameters
- `SEMANTIC_SCHOLAR_API`: API endpoint and limits
- `EVALUATION_CONFIG`: Evaluation model and output settings

## Performance Notes

- First request may take longer due to Mistral API initialization
- Semantic Scholar API is free and doesn't require authentication
- Results limited to top 5 papers per search (configurable)
- Agent respects rate limiting to avoid API throttling
