# Visual Guide for 10-Minute Video Presentation

## Visual Elements to Create/Show

### 1. Title Slide (0:00-0:10)
```
┌──────────────────────────────────────────────┐
│                                              │
│     Research Paper Discovery Agent           │
│                                              │
│        An AI-Powered Search System           │
│                                              │
│                                              │
│             C# • AutoGen • Mistral AI        │
│                                              │
└──────────────────────────────────────────────┘
```

---

### 2. Problem Statement (0:10-0:30)
```
❌ Manual Paper Discovery:
   • Search database manually
   • Get 100s of results
   • Hard to evaluate quality
   • Time-consuming filtering
   • Inconsistent results

✅ Our Solution:
   • Single natural language query
   • AI analyzes automatically
   • Quality scoring (0-100)
   • Smart retry with feedback
   • Guaranteed good results
```

---

### 3. Architecture Diagram (0:30-2:30)

**Slide Title:** "Three-Stage Pipeline"

```
                    ┌─────────────┐
                    │   Input:    │
                    │   Query     │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ STAGE 1:    │
                    │  FETCH      │
                    │             │
                    │ Make 1 API  │
                    │ call to get │
                    │ 100 papers  │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ STAGE 2:    │
                    │  ANALYZE    │
                    │             │
                    │ AI agent    │
                    │ selects     │
                    │ TOP 5       │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ STAGE 3:    │
                    │  EVALUATE   │
                    │             │
                    │ Judge       │
                    │ scores 0-100│
                    └──────┬──────┘
                           │
                    ┌──────▼──────────────┐
                    │ Score >= 70?        │
                    └──────┬──────┬───────┘
                        YES│      │NO
                    ┌──────▼─┐  ┌─▼────────┐
                    │SUCCESS!│  │GENERATE  │
                    │ RETURN │  │FEEDBACK  │
                    │ RESULTS│  └──────┬───┘
                    └────────┘         │
                               ┌──────▼──────┐
                               │  RETRY LOOP │
                               │ (up to 3x)  │
                               └─────┬───────┘
                                     │
                            ┌────────▼───────┐
                            │ Go back to     │
                            │ STAGE 2 with   │
                            │ feedback msg   │
                            └────────────────┘
```

---

### 4. Four Evaluation Metrics (2:30-3:00)

**Slide Title:** "Quality Scoring System"

```
┌─────────────────────────────────────────────┐
│  CORRECTNESS (1-5)                          │
│  ████████████████░░  Are these highly cited?│
│  Score 5: 500+ citations per paper          │
│  Score 1: <100 citations per paper          │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  ADHERENCE (1-5)                            │
│  ████████████░░░░░░  Do they match query?   │
│  Score 5: All papers match year/citations   │
│  Score 1: Many papers don't match           │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  COMPLETENESS (1-5)                         │
│  ████████████████░░  Metadata complete?     │
│  Score 5: All have authors & venues         │
│  Score 1: Many missing info                 │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  USEFULNESS (1-5)                           │
│  ████████████░░░░░░  From top venues?       │
│  Score 5: NeurIPS, Nature, ICML papers      │
│  Score 1: Low-impact venues                 │
└─────────────────────────────────────────────┘

OVERALL SCORE = (Sum of 4 metrics / 4) × 20
Min: 0/100    Target: 70+/100    Max: 100/100
```

---

### 5. Auto-Retry Flow (3:00-5:30)

**Slide Title:** "The Innovation: Feedback-Guided Retry"

```
USER QUERY
"Find papers on transformers with 500+ citations
 published after 2023"
        │
        ▼
┌─────────────────────────┐
│ ATTEMPT 1               │
├─────────────────────────┤
│ Agent analyzes 100      │
│ papers                  │
│                         │
│ Selected TOP 5:         │
│ 1. Paper A (200 cite)   │
│ 2. Paper B (150 cite)   │
│ 3. Paper C (180 cite)   │
│ 4. Paper D (160 cite)   │
│ 5. Paper E (170 cite)   │
│                         │
│ SCORE: 65/100 ❌        │
│ Correctness: 2/5 (LOW)  │
│ Adherence: 4/5          │
│ Completeness: 3/5       │
│ Usefulness: 4/5         │
└─────────────────────────┘
        │
        ▼ Score < 70, Generate Feedback
┌─────────────────────────────────────┐
│ LLM FEEDBACK GENERATION             │
├─────────────────────────────────────┤
│ Mistral AI analyzes:                │
│ "Correctness too low (2/5).         │
│  These papers have low citations    │
│  (avg: 152).                        │
│                                     │
│  Suggestion: Look for papers with   │
│  500+ citations, especially from    │
│  top-tier venues like NeurIPS,      │
│  ICML, or Nature."                  │
└─────────────────────────────────────┘
        │
        ▼ Append feedback to system message
┌─────────────────────────┐
│ ATTEMPT 2               │
├─────────────────────────┤
│ Agent re-analyzes 100   │
│ papers WITH GUIDANCE    │
│                         │
│ Selected TOP 5:         │
│ 1. Paper F (1200 cite)  │
│ 2. Paper G (850 cite)   │
│ 3. Paper H (720 cite)   │
│ 4. Paper I (680 cite)   │
│ 5. Paper J (650 cite)   │
│                         │
│ SCORE: 82/100 ✅        │
│ Correctness: 5/5 (HIGH) │
│ Adherence: 4/5          │
│ Completeness: 4/5       │
│ Usefulness: 5/5         │
└─────────────────────────┘
        │
        ▼ Score >= 70, SUCCESS!
    RETURN RESULTS

Key Insight:
With feedback guidance, the agent understood
what was wanted and selected MUCH better
papers (average citation count: 1200 vs 152)
```

---

### 6. Code Highlights (5:30-8:00)

**Slide Title:** "Key Code Components"

```
Function 1: ProcessQueryWithRetryAsync()
Location: Program.cs, Lines 217-318
Purpose: Orchestrates the retry loop
Key Logic:
  • while (retryCount < maxRetries)
  • Run Stage 2 (Agent analysis)
  • Run Stage 3 (Judge evaluation)
  • if (score >= 70) break;
  • else generate feedback and continue

─────────────────────────────────────────

Function 2: GenerateLLMBasedFeedbackAsync()
Location: Program.cs, Lines 321-374
Purpose: Creates intelligent feedback
Key Logic:
  • Creates prompt with evaluation scores
  • Sends to Mistral AI
  • AI analyzes what went wrong
  • Returns specific suggestions
  • Falls back to heuristic if LLM fails

─────────────────────────────────────────

Class: PaperSearchJudge
Location: Services/PaperSearchJudge.cs
Purpose: Evaluates paper selections
Key Logic:
  • Calculate 4 metrics (Correctness, etc.)
  • Score each 1-5
  • Overall = (avg) × 20
  • Return detailed feedback

─────────────────────────────────────────

Class: SemanticScholarService
Location: Services/SemanticScholarService.cs
Purpose: Fetches papers from API
Key Features:
  • Single API call per query
  • API-side filtering (year, citations)
  • Exponential backoff for rate limiting
  • Returns up to 100 papers
```

---

### 7. Live Demo Output Example (8:00-8:45)

**Slide Title:** "Example Execution"

```
═════════════════════════════════════════════════
  Research Paper Discovery Agent
═════════════════════════════════════════════════

Your query: Find papers on machine learning
            published after 2020 with 50 citations

[STAGE 1] Searching for papers...
Searching for: machine learning (year: after 2020,
              min citations: 50)...
✓ Found 100 papers

[STAGE 2] Analyzing 100 papers...

Agent's Selection Analysis:

TOP 5 PAPERS:
1. Physics-informed machine learning
   Authors: G. Karniadakis, I. Kevrekidis, Lu Lu
   Citation Count: 4858
   Venue: Nature Reviews Physics
   Why selected: Landmark paper with exceptional
                 citation impact

[continuing for papers 2-5...]

════════════════════════════════════════════════

[STAGE 3] Evaluating 5 selected papers...

Attempt 1: Overall Score: 95/100
  Correctness:  5/5 ✓
  Adherence:    4/5
  Completeness: 5/5 ✓
  Usefulness:   5/5 ✓
✓ Good score achieved! (95/100)

Selected Papers:
✓ Found 5 paper(s)

Paper 1
────────────────────────────────────────
Title:     Physics-informed machine learning
Year:      2021
Citations: 4858
Venue:     Nature Reviews Physics
Authors:   G. Karniadakis, I. Kevrekidis, Lu Lu

[continuing for papers 2-5...]

Final Evaluation of Agent's Selection:
Overall Score: 95/100
  Correctness:  5/5
  Adherence:    4/5
  Completeness: 5/5
  Usefulness:   5/5
  Comment:      Excellent: 5 highly-cited papers;
                includes landmark papers

✓ Excellent score achieved on first attempt!
```

---

### 8. Key Features Summary (8:45-9:15)

**Slide Title:** "Key Features"

```
✅ Single API Call
   Only fetch papers once, analyze them multiple times

✅ AI-Powered Analysis
   Mistral AI intelligently selects based on quality

✅ Automatic Quality Control
   Score threshold (70) ensures good results

✅ Intelligent Feedback
   LLM generates context-aware suggestions

✅ Graceful Fallbacks
   If LLM fails, use heuristic feedback

✅ Retry Loop
   Up to 3 automatic improvement attempts

✅ Comprehensive Evaluation
   4 metrics ensure quality from multiple angles

✅ Efficient Design
   Respects API rate limits, handles failures

✅ Easy to Use
   Natural language queries, clear output
```

---

### 9. Technology Stack (9:15-9:30)

**Slide Title:** "Technology Stack"

```
┌──────────────────────────────────────────┐
│  LANGUAGE & FRAMEWORK                    │
│  C# 13                                   │
│  .NET 10                                 │
│  AutoGen for .NET (multi-agent framework)│
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│  AI & LANGUAGE MODELS                    │
│  Mistral AI                              │
│  Model: open-mistral-nemo                │
│  Uses: Analysis + Feedback generation    │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│  APIs & SERVICES                         │
│  Semantic Scholar API                    │
│  Uses: Paper metadata search/filtering   │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│  DESIGN PATTERNS                         │
│  Multi-Agent Architecture                │
│  Feedback Loops                          │
│  Graceful Degradation (fallback)         │
│  Exponential Backoff (retry)             │
└──────────────────────────────────────────┘
```

---

### 10. Conclusion Slide (9:30-10:00)

**Slide Title:** "Summary"

```
PROBLEM:
Finding quality research papers is difficult
and time-consuming.

SOLUTION:
AI-powered agent with quality control and
intelligent feedback loops.

INNOVATION:
Automatic retry with LLM-generated guidance
improves results from 65→82 scores.

IMPACT:
Researchers get better paper recommendations
faster, with transparent quality metrics.

Built with modern C#, AutoGen, and Mistral AI.

Thank you!
```

---

## Recording Tips for Each Section

### General Recording Tips
- **Audio**: Speak clearly, not too fast (about 140 words/min)
- **Screen**: Zoom IDE text 150-200% for visibility
- **Pace**: Take natural pauses between points
- **Visuals**: Let each slide/output sit for 3-5 seconds before continuing

### Specific Timing Tips

**Intro (1 min):**
- Show title slide 5 sec
- Talk about problem 20 sec
- Talk about solution 20 sec
- Transition to architecture 15 sec

**Architecture (2 min):**
- Show pipeline diagram 10 sec
- Explain each stage 30 sec each (2 min total)
- Explain retry decision 10 sec

**Auto-Retry (2.5 min):**
- Show flowchart 10 sec
- Explain retry concept 30 sec
- Show Score 65→78 improvement 20 sec
- Show feedback example 40 sec
- Why it works (conversational) 20 sec

**Demo (2.5 min):**
- Setup/input query 15 sec
- Stage 1 output 10 sec
- Stage 2 output 30 sec
- Stage 3 evaluation 25 sec
- Results display 40 sec
- Highlight key outputs 20 sec

**Features (1.5 min):**
- Show features list 30 sec
- Highlight each 4-5 features 60 sec

**Results (0.5 min):**
- Show example output 30 sec

**Conclusion (0.5 min):**
- Tech stack 15 sec
- Thank you 15 sec
