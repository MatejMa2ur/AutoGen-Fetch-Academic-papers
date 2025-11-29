# Testing Guide - Research Paper Search Agent

Complete step-by-step guide to set up and test the agent.

---

## Prerequisites

### 1. Mistral AI API Key

The agent needs a free Mistral AI API key. Here's how to get it:

**Step 1: Create Account**
- Go to [mistral.ai](https://mistral.ai)
- Click "Sign Up"
- Create account with email

**Step 2: Get API Key**
- Login to console
- Go to API Keys section
- Click "Create API Key"
- Copy the key (starts with `sk-`)

**Step 3: Add to .env File**
```bash
cp .env.example .env
```

Then edit `.env` and add your key:
```env
MISTRAL_API_KEY=sk-your_actual_key_here
```

**Important**:
- Keep this key secret (don't commit to git)
- `.env` is in `.gitignore` so it won't be committed
- Free tier has generous limits (≈100 requests/month)

### 2. Python Environment

```bash
# Check Python version
python --version  # Should be 3.12+

# Create virtual environment
python -m venv .venv

# Activate it
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

---

## Quick Verification

Before running tests, verify everything is set up:

```bash
python check_setup.py
```

**Expected output:**
```
✓ Python 3.12 found
✓ autogen installed
✓ mistralai installed
✓ requests installed
✓ .env file configured
✓ main.py found
✓ tools.py found
✓ config.py found
✓ evaluation.py found
✓ utils.py found

✓ Setup is complete! You can now run:
  python main.py        (interactive mode)
  python demo.py         (run demo)
  python run_evaluation.py  (evaluate agent)
```

If you see errors, fix them before proceeding.

---

## Testing Strategies

### Level 1: Component Testing (Unit Tests)

Test individual functions independently.

#### Test 1: Query Parser

```python
# Create a test file: test_parser.py
from main import parse_search_query

# Test basic parsing
query = "Find a paper on machine learning after 2020 with 50 citations"
result = parse_search_query(query)

print(result)
# Expected output:
# {
#   'topic': 'machine learning',
#   'year': 2020,
#   'year_condition': 'after',
#   'min_citations': 50
# }
```

**Run it:**
```bash
python test_parser.py
```

**What to verify:**
- Topic extracted correctly
- Year parsed as integer
- year_condition is one of: 'exact', 'before', 'after', 'any'
- min_citations is integer or None

---

#### Test 2: Search Tool (Without Agent)

```python
# Create: test_search_tool.py
from tools import search_research_papers_api
import json

# Test basic search
result = search_research_papers_api(
    topic="machine learning",
    year=2023,
    year_condition="exact",
    min_citations=10
)

data = json.loads(result)
print(json.dumps(data, indent=2))

# Verify structure
assert data["status"] in ["success", "no_results", "error"]
if data["status"] == "success":
    assert isinstance(data["papers"], list)
    assert len(data["papers"]) > 0
    assert all("title" in p for p in data["papers"])
```

**Run it:**
```bash
python test_search_tool.py
```

**Expected output:**
```json
{
  "status": "success",
  "papers_found": 3,
  "papers": [
    {
      "title": "...",
      "year": 2023,
      "citations": 25,
      "authors": ["Author 1", "Author 2"],
      "venue": "Conference Name",
      "paperId": "..."
    }
  ]
}
```

**What to verify:**
- Status is "success" or "no_results" (not "error")
- Papers list contains expected fields
- Years match criteria
- Citation counts match minimum

---

### Level 2: Integration Testing (Agent Testing)

Test the full agent with real queries.

#### Test 3: Interactive Mode

**Simplest test - just run it:**
```bash
python main.py
```

**Test queries to try:**
```
1. Find a paper on machine learning published after 2020
2. Search for papers on deep learning from 2023 with 50+ citations
3. Look for research on transformers before 2023
4. Find a paper on neural networks
5. quit
```

**Expected behavior:**
- Agent processes query
- Searches Semantic Scholar
- Returns formatted results with paper titles, years, citations
- No errors or exceptions
- Exits cleanly when typing "quit"

**Timing:**
- First request: 3-5 seconds (Mistral initialization)
- Subsequent: 2-3 seconds

---

#### Test 4: Demo Script

```bash
python demo.py
```

**What it does:**
- Runs 3 predefined queries
- Shows agent in action
- Demonstrates various query types

**Expected output:**
```
========================================================================
 Research Paper Search Agent - Live Demo
========================================================================

Demo 1:
Query: Find a research paper on machine learning that was published
       after 2020 and has at least 50 citations.

Searching...

Found 3 matching papers:

1. Machine Learning for X
   Year: 2021
   Citations: 125
   Authors: Author 1, Author 2, Author 3
   Venue: Top Conference
...
```

---

### Level 3: Evaluation Testing

Test the agent's performance across multiple queries.

#### Test 5: Full Evaluation Suite

```bash
python run_evaluation.py
```

**What it does:**
1. Runs 5 predefined test queries
2. Uses LLM to score each response
3. Calculates statistics
4. Saves detailed results to `evaluation_results.json`

**Expected output:**
```
======================================================================
Research Paper Search Agent - Evaluation Suite
======================================================================

Running 5 test queries...

Evaluating query 1/5: Find a research paper on machine learning...
Evaluating query 2/5: Search for papers on transformer architectures...
...

======================================================================
Evaluation Summary
======================================================================

Total Queries Evaluated: 5
Average Score: 78.5/100
Best Score: 85/100
Worst Score: 72/100
Queries with score >= 70: 5/5

Detailed results saved to: evaluation_results.json
======================================================================
```

**Check the detailed results:**
```bash
cat evaluation_results.json | python -m json.tool
```

You'll see scores for:
- Clarity (0-100)
- Completeness (0-100)
- Accuracy (0-100)
- Overall quality (0-100)

---

## Testing Checklist

Use this checklist to verify each component:

### Setup
- [ ] Python 3.12+ installed
- [ ] Virtual environment created and activated
- [ ] Requirements installed: `pip install -r requirements.txt`
- [ ] `.env` file exists with MISTRAL_API_KEY
- [ ] `check_setup.py` runs without errors

### Component Tests
- [ ] `test_parser.py` extracts parameters correctly
- [ ] `test_search_tool.py` returns valid JSON with papers
- [ ] Papers have expected fields (title, year, citations, authors)
- [ ] Year filtering works (papers match year_condition)
- [ ] Citation filtering works (papers meet min_citations)

### Interactive Tests
- [ ] `python main.py` starts without errors
- [ ] Can type queries and get results
- [ ] Results are formatted nicely
- [ ] "quit" command exits cleanly
- [ ] Example queries work:
  - [ ] Query with just topic ("papers on machine learning")
  - [ ] Query with year ("published after 2020")
  - [ ] Query with citations ("50+ citations")
  - [ ] Query with all three criteria

### Demo Test
- [ ] `python demo.py` completes without errors
- [ ] Shows 3 different query types
- [ ] Results look reasonable

### Evaluation Test
- [ ] `python run_evaluation.py` completes (takes 2-3 minutes)
- [ ] `evaluation_results.json` created
- [ ] All 5 test queries have scores >= 70
- [ ] Summary statistics make sense

---

## Troubleshooting Common Issues

### Issue: "MISTRAL_API_KEY not found"

**Problem**: `.env` file doesn't have the API key.

**Solution**:
```bash
# Check if .env exists
ls -la .env

# If not, create it
cp .env.example .env

# Edit and add your key
nano .env  # or use your favorite editor
```

Make sure line looks like:
```
MISTRAL_API_KEY=sk-xxxxxxxxxxxxx
```

Not like:
```
MISTRAL_API_KEY=your_mistral_api_key_here  # ❌ Still placeholder
```

---

### Issue: "ModuleNotFoundError: No module named 'autogen'"

**Problem**: Dependencies not installed.

**Solution**:
```bash
# Make sure virtual environment is activated
source .venv/bin/activate

# Reinstall dependencies
pip install -r requirements.txt

# Verify
python -c "import autogen; print('OK')"
```

---

### Issue: Queries timeout or take 10+ seconds

**Problem**: Slow network or API issues.

**Diagnosis**:
```bash
# Test Mistral API directly
python -c "from mistralai import Mistral; print('Mistral OK')"

# Test Semantic Scholar
python -c "
import requests
r = requests.get('https://api.semanticscholar.org/graph/v1/paper/search',
                 params={'query': 'test', 'limit': 1},
                 timeout=5)
print('Semantic Scholar OK')
"
```

**Solutions**:
- Check internet connection
- Try again (API may be temporarily slow)
- Check if Mistral API is down: https://status.mistral.ai

---

### Issue: No papers found for any query

**Problem**: Search parameters too restrictive.

**Try these queries**:
```
1. "machine learning"  # No year or citation limits
2. "deep learning after 2020"  # Only year
3. "neural networks"  # Different topic
```

If still no results, the API might be having issues.

---

### Issue: "JSONDecodeError" when running evaluation

**Problem**: Response from LLM isn't valid JSON.

**Solution**:
- Usually temporary (Mistral response parsing issue)
- Re-run: `python run_evaluation.py`
- If persistent, check internet connection

---

## Manual Testing Examples

### Test Query 1: Basic Search

```bash
python main.py
> Find papers on machine learning
```

**Expected**: Returns 1-5 papers on machine learning with recent years

---

### Test Query 2: Year Filtering - Exact Year

```bash
python main.py
> Papers from 2023 on deep learning
```

**Expected**: Papers are mostly from 2023 (±1 year acceptable due to API precision)

---

### Test Query 3: Year Filtering - After Year

```bash
python main.py
> Find papers published after 2022 on artificial intelligence
```

**Expected**: All papers have year >= 2022

---

### Test Query 4: Year Filtering - Before Year

```bash
python main.py
> Search for papers before 2020 on machine learning
```

**Expected**: All papers have year <= 2020

---

### Test Query 5: Citation Filtering

```bash
python main.py
> Find papers on transformers with 100+ citations
```

**Expected**: All papers have citations >= 100 (and should be high-impact papers)

---

### Test Query 6: All Criteria Combined

```bash
python main.py
> Find a paper on computer vision published after 2021 with 50 citations
```

**Expected**:
- Topic matches computer vision
- Year >= 2021
- Citations >= 50

---

## Performance Testing

### Measure Response Time

```python
# Create: test_performance.py
import time
from main import run_paper_search_agent

queries = [
    "machine learning",
    "deep learning after 2020",
    "neural networks with 100+ citations",
]

for query in queries:
    start = time.time()
    result = run_paper_search_agent(query)
    elapsed = time.time() - start

    print(f"Query: {query[:40]}...")
    print(f"Time: {elapsed:.1f}s\n")
```

**Expected**: 2-4 seconds per query after first one

---

## What Success Looks Like

✓ **Setup verification passes**
```
✓ Python 3.12 found
✓ All modules imported successfully
✓ .env configured
```

✓ **Parser works**
```
{'topic': 'machine learning', 'year': 2020, 'year_condition': 'after', 'min_citations': 50}
```

✓ **Search returns papers**
```json
{
  "status": "success",
  "papers_found": 3,
  "papers": [...]
}
```

✓ **Interactive mode works**
```
Enter your search query: Find papers on machine learning
Searching for papers...

Found 3 matching papers:
1. Title: ...
   Year: 2023
   Citations: 150
...
```

✓ **Demo runs completely**
```
Demo 1: [query result]
Demo 2: [query result]
Demo 3: [query result]
Demo Complete
```

✓ **Evaluation completes**
```
Average Score: 78.5/100
Passed queries: 5/5
Detailed results saved to: evaluation_results.json
```

---

## Testing Timeline

Estimated time to complete all tests:

| Test | Time | Cumulative |
|------|------|-----------|
| check_setup.py | 10s | 10s |
| test_parser.py | 10s | 20s |
| test_search_tool.py | 5s | 25s |
| main.py (5 queries) | 15s | 40s |
| demo.py | 10s | 50s |
| run_evaluation.py | 2-3 min | 3-4 min |
| **Total** | | **~4 minutes** |

---

## Next Steps After Testing

If everything works:

1. **Customize evaluation queries** in `run_evaluation.py`
2. **Try your own queries** in interactive mode
3. **Check evaluation results** in `evaluation_results.json`
4. **Review logs** in `query_log.jsonl` (if generated)
5. **Extend the system** by adding more agents or tools

If issues occur:
1. Check troubleshooting section above
2. Re-read TECHNICAL_DOCUMENTATION.md
3. Check error messages carefully (usually very informative)
4. Try with simpler queries first
