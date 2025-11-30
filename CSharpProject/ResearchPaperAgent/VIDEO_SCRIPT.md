# 10-Minute Video Presentation Script
## Research Paper Discovery Agent

**Total Duration:** ~10 minutes
**Segments:** 5 major sections with visuals

---

## 1. INTRODUCTION & PROBLEM STATEMENT (1 minute)

**What to show on screen:**
- Title slide: "Research Paper Discovery Agent"
- Show a research paper database image or Semantic Scholar screenshot

**What to say:**
"Hi, I'm presenting the Research Paper Discovery Agent - a C# application that uses AI to help researchers find the best papers for their topics.

The problem we're solving: Finding quality research papers is hard. You have to search manually, read through hundreds of results, and often end up with papers that don't meet your needs.

Our solution: An intelligent agent that searches for papers, analyzes them automatically using AI, evaluates their quality, and even retries if the results aren't good enough."

---

## 2. ARCHITECTURE OVERVIEW (2 minutes)

**What to show on screen:**
- Display the three-stage pipeline diagram (from README)
```
[Stage 1: Fetch]  → 100 papers from Semantic Scholar API
      ↓
[Stage 2: Analyze] → AI agent selects TOP 5
      ↓
[Stage 3: Evaluate] → Judge scores 0-100
      ↓
   Score >= 70? → Success OR Retry with feedback
```

**What to say:**
"The system has three main stages:

**Stage 1 - Fetch:** We make a single API call to Semantic Scholar to get up to 100 papers. The query constraints like year and citation count are applied at the API level for efficiency.

**Stage 2 - Analyze & Select:** A Mistral AI agent analyzes all 100 papers and intelligently selects the TOP 5 based on citation impact, publication venue quality, recency, and relevance.

**Stage 3 - Evaluate:** A judge component scores the selection from 0 to 100 using 4 metrics:
- Correctness: Are these highly-cited papers?
- Adherence: Do they meet the query constraints?
- Completeness: Do they have full metadata?
- Usefulness: Are they from top venues?

If the score is 70 or above, great! We display the results. If it's below 70, the system automatically retries with feedback."

---

## 3. KEY INNOVATION: AUTO-RETRY WITH LLM FEEDBACK (2.5 minutes)

**What to show on screen:**
- Show a flowchart of the retry loop:
```
Attempt 1: Score = 65/100 (TOO LOW)
    ↓
Generate LLM Feedback:
  "Correctness too low (2/5): Look for papers with 500+ citations"
    ↓
Attempt 2: Same 100 papers + feedback message to agent
    ↓
Agent Re-analyzes with guidance → Selects DIFFERENT TOP 5
    ↓
Score = 78/100 (SUCCESS!)
```

**What to say:**
"The innovation here is the automatic retry with intelligent feedback. This is what sets our system apart.

When the initial selection scores below 70, instead of just failing, the system:

1. Analyzes which metrics are low
2. Uses Mistral AI to generate context-aware feedback
3. Tells the AI agent something like: 'The papers you selected have low citation impact. Look for papers with 500+ citations from top venues like NeurIPS or Nature.'
4. The agent re-analyzes the same 100 papers but with this guidance
5. Selects a completely different TOP 5 that better matches what we're looking for

This happens automatically, up to 3 times. We've tested this - on the first attempt we might get 65/100, but after the feedback-guided retry, it jumps to 78/100 or higher.

It's like having a conversation with the AI: 'That didn't work well, try this approach instead.'"

---

## 4. LIVE DEMO OR CODE WALKTHROUGH (2.5 minutes)

**Option A: Live Demo (Preferred if time/internet allows)**

**What to show:**
1. Run the application in terminal
2. Input a query: "Find papers on transformer models with 300 citations published after 2023"
3. Show it finding 100 papers
4. Show it analyzing and selecting TOP 5
5. Show the evaluation scores
6. If it triggers a retry, show the feedback message
7. Show the improved score on attempt 2

**What to say:**
"Let me show you how it works in practice. I'll search for papers on transformer models with strict constraints - 300+ citations after 2023.

[Run the application]

Notice it found 100 papers. Now the AI is analyzing them... The agent is looking at citation counts, publication venues, recency. Here are the TOP 5 selected papers.

Now the judge is evaluating... We got a score of 65/100. That's below our 70 threshold, so the system automatically generates feedback and retries.

[If feedback shows]

See this feedback? The AI is saying the correctness metric is low - these papers don't have enough citations. It suggests looking for papers with 500+ citations. The agent receives this and re-analyzes.

[Attempt 2 runs]

Now we got 78/100 - much better! The feedback worked. The agent understood the guidance and selected more impactful papers."

**Option B: Code Walkthrough (If live demo isn't possible)**

**What to show:**
- Open Program.cs in an IDE
- Highlight the three functions:
  - `ProcessQueryWithRetryAsync()` (lines 217-318) - The retry loop
  - `GenerateLLMBasedFeedbackAsync()` (lines 321-374) - Feedback generation
  - `GenerateRetryFeedback()` (lines 376-410) - Heuristic fallback

**What to say:**
"Here's the core retry logic in Program.cs. The `ProcessQueryWithRetryAsync()` function orchestrates the entire flow.

It runs a loop up to 3 times:
1. Calls the AI agent to analyze papers
2. Extracts the TOP 5 papers from the response
3. Calls the judge to evaluate them
4. If score >= 70, done. If not, generate feedback and loop again.

The feedback is generated by `GenerateLLMBasedFeedbackAsync()` which sends the evaluation metrics and paper lists to Mistral AI. The AI analyzes what went wrong and suggests improvements.

If the LLM feedback fails for any reason, we gracefully fall back to heuristic feedback - simple rules like 'if correctness is low, look for high-citation papers.'"

---

## 5. KEY FEATURES & TECHNICAL DETAILS (1.5 minutes)

**What to show on screen:**
- Table/list of key features:

| Feature | Benefit |
|---------|---------|
| Single API Call | Efficient, only 100 papers fetched once |
| LLM-Based Feedback | Context-aware, intelligent suggestions |
| Heuristic Fallback | Robust - works even if LLM fails |
| Score Thresholds | Automatic quality control (70/100 target) |
| Max 3 Retries | Prevents infinite loops, respects API rate limits |
| 4-Metric Evaluation | Comprehensive quality assessment |

**What to say:**
"Here are the technical highlights:

**Efficiency:** We fetch 100 papers once from Semantic Scholar. The agent analyzes all of them, so we're not making multiple API calls for each retry. This saves time and respects API rate limits.

**Intelligent Feedback:** When the score is low, Mistral AI doesn't just say 'try again.' It analyzes the metrics and generates specific guidance. For example, if 'Completeness' is low, it suggests looking for papers with full author information and venue data.

**Robust Design:** If the LLM feedback fails (maybe due to API issues), we fall back to heuristic feedback. The system keeps working.

**Quality Control:** We have a clear threshold - 70/100. This ensures we don't return poor results to users. The system keeps trying until it meets this standard.

**Rate Limiting Respect:** We cap at 3 retries to balance quality with practicality. We also implement exponential backoff for the Semantic Scholar API to handle rate limiting gracefully."

---

## 6. RESULTS & TESTING (0.5 minutes)

**What to show on screen:**
- Example output showing:
  - Attempt 1: 65/100 score
  - Attempt 2: 78/100 score with "Achieved score after 1 retry" message
  - The 5 selected papers with metadata

**What to say:**
"In testing, we've seen the system consistently improve selections through retries. A query that scores 65/100 on the first attempt often reaches 78-85/100 after feedback-guided retries.

The selected papers come with full metadata - titles, authors, citation counts, publication venues - everything a researcher needs to decide if they want to read each paper."

---

## 7. CONCLUSION (0.5 minutes)

**What to show on screen:**
- Technology stack icons/list:
  - C# / .NET 10
  - Mistral AI
  - Semantic Scholar API
  - AutoGen Framework

**What to say:**
"This project demonstrates how AI can make the research paper discovery process smarter and more efficient. Instead of manually searching and filtering, researchers can now ask for papers and get intelligent, quality-assured results.

The key innovation is the feedback loop - the system doesn't just accept its first attempt. It evaluates quality, understands what went wrong, and improves through AI-guided iteration.

Built with modern C#, AutoGen multi-agent framework, and Mistral AI, the system is efficient, robust, and user-friendly.

Thank you!"

---

# Video Production Tips

## What to Record
- [ ] Desktop screen showing the application running
- [ ] Code editor showing key functions
- [ ] IDE with syntax highlighting for visibility
- [ ] Terminal showing query execution
- [ ] Diagrams (can be screenshots from README or draw them)

## Setup Checklist
- [ ] Zoom in on text (150-200% on IDE)
- [ ] Close notifications and distracting windows
- [ ] Have the README open as reference
- [ ] Test the application before recording
- [ ] Record in a quiet environment
- [ ] Use screen recording tool (OBS, Quicktime, etc.)

## Timing Breakdown
- Intro: 1 min
- Architecture: 2 min
- Auto-Retry Innovation: 2.5 min
- Demo: 2.5 min
- Features: 1.5 min
- Results: 0.5 min
- Conclusion: 0.5 min
- **Total: 10 minutes**

## Suggested Slides/Visuals

**Slide 1:** Title + Project Name
**Slide 2:** Problem Statement (research paper discovery is hard)
**Slide 3:** Three-Stage Pipeline Diagram
**Slide 4:** Retry Loop Flowchart
**Slide 5:** Code walkthrough OR Live demo
**Slide 6:** Key Features Table
**Slide 7:** Example Results/Output
**Slide 8:** Technology Stack
**Slide 9:** Conclusion/Thank You

---

# Quick Reference from README

- **Overview:** Lines 5-18 in README
- **Architecture:** Lines 20-50 in README
- **Key Features:** Lines 151-204 in README
- **Evaluation System:** Lines 334-373 in README
- **Testing Examples:** Lines 218-256 in README

All the information you need is already documented. This script just organizes it for a 10-minute presentation!
