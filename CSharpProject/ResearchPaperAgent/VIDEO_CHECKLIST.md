# 10-Minute Video - Preparation Checklist

## Pre-Recording Setup (Do this today/evening)

### Technical Setup
- [ ] Test screen recording software (OBS, QuickTime, Zoom, etc.)
- [ ] Test microphone and audio levels
- [ ] Close all notifications (email, Slack, Discord, etc.)
- [ ] Close unnecessary browser tabs and applications
- [ ] Set IDE font size to 150-200% for visibility
- [ ] Open and test the application beforehand
- [ ] Have .env file configured with Mistral API key

### Content Preparation
- [ ] Read through VIDEO_SCRIPT.md
- [ ] Review PRESENTATION_VISUALS.md for diagrams
- [ ] Prepare or download slide software (PowerPoint, Google Slides, Keynote, etc.)
- [ ] Create 9-10 slides based on PRESENTATION_VISUALS.md
- [ ] Open README.md as a reference during recording
- [ ] Have a copy of the script visible (printed or second monitor)

### Optional Visuals to Create
- [ ] Slide 1: Title slide
- [ ] Slide 2: Problem vs Solution comparison
- [ ] Slide 3: Three-stage pipeline diagram (can copy from PRESENTATION_VISUALS.md)
- [ ] Slide 4: Four evaluation metrics with visual bars
- [ ] Slide 5: Retry loop flowchart
- [ ] Slide 6: Code highlights (screenshot or text)
- [ ] Slide 7: Key features checklist
- [ ] Slide 8: Technology stack
- [ ] Slide 9: Conclusion/Thank you

### Room & Environment
- [ ] Quiet environment (minimize background noise)
- [ ] Good lighting on your face (if using webcam)
- [ ] Clean desk/background
- [ ] Phone on silent
- [ ] Pets/family members asked not to interrupt

---

## Recording Sequence

### Part 1: Slides & Narration (6-7 minutes)
1. **Intro Slide** (1 min)
   - Title + narration about problem/solution

2. **Architecture Slides** (2 min)
   - Show three-stage pipeline
   - Explain each stage
   - Talk about the evaluation metrics

3. **Auto-Retry Innovation Slides** (2.5 min)
   - Show retry flowchart
   - Explain feedback loop concept
   - Show score improvement example (65→82)

4. **Key Features Slide** (1.5 min)
   - Highlight 8-10 key features
   - Explain why each matters

### Part 2: Live Demo or Code Walkthrough (2.5-3 minutes)

**Option A: Live Demo (Recommended)**
1. Show terminal/IDE
2. Run: `dotnet run`
3. Input query: "Find papers on machine learning published after 2020"
4. Walk through each output:
   - Stage 1 output (100 papers found)
   - Stage 2 output (Agent analysis)
   - Stage 3 output (Scores and metrics)
   - Final results display

**Option B: Code Walkthrough**
1. Show Program.cs in IDE
2. Highlight ProcessQueryWithRetryAsync() function
3. Highlight GenerateLLMBasedFeedbackAsync() function
4. Show PaperSearchJudge.cs
5. Explain how the three pieces work together

### Part 3: Conclusion (0.5-1 minute)
1. Show technology stack
2. Summary of key innovation (feedback loop)
3. Thank you message

---

## Recording Tips

### Audio
- [ ] Speak clearly and at natural pace (~140 words/min)
- [ ] Avoid "um" and "uh" - take pauses instead
- [ ] Smile while speaking (it comes through in your voice)
- [ ] If you mess up, pause 3 seconds and continue naturally (edit later)

### Screen Recording
- [ ] Zoom in text for readability
- [ ] Move mouse slowly and deliberately
- [ ] Don't click too quickly through code
- [ ] Let each output sit for 3-5 seconds before moving on
- [ ] Point out important lines/sections

### Pacing
- [ ] Follow the timing breakdown in VIDEO_SCRIPT.md
- [ ] Don't rush - you have 10 minutes, use them
- [ ] Pause between major points
- [ ] Let visuals "breathe" - don't cut between them too quickly

### Common Mistakes to Avoid
- [ ] Don't read directly from script - sound natural
- [ ] Don't show too much terminal output at once
- [ ] Don't go into implementation details unless necessary
- [ ] Don't skip the demo - it's what makes the project real
- [ ] Don't forget to explain WHY things matter, not just WHAT they do

---

## Post-Recording Editing

### Basic Editing (if needed)
- [ ] Cut out long pauses or mistakes
- [ ] Add title/conclusion screens
- [ ] Add background music (optional, keep it light)
- [ ] Normalize audio levels
- [ ] Export as MP4 or similar standard format

### File Management
- [ ] Save final video as `ResearchPaperAgent_Presentation.mp4`
- [ ] Save in project directory
- [ ] Test playback on different devices
- [ ] Keep original recording file as backup

---

## What to Actually Say (Quick Reference)

### When showing the application running:
**"As you can see, the system found 100 papers. Now the AI agent is analyzing all of them using criteria like citation impact, publication venue, and recency. Here it selects the TOP 5. Notice the evaluation shows each metric - Correctness, Adherence, Completeness, and Usefulness - all rated 1-5. The overall score of 95/100 means this is a high-quality selection."**

### When explaining auto-retry:
**"If this score had been below 70, the system would automatically generate feedback. It would say something like 'These papers don't have enough citations - look for papers with 500+ citations from top venues.' Then the agent would re-analyze and select different papers. This feedback loop is what makes our system smart - it's like having a conversation with the AI to improve the results."**

### When explaining why this matters:
**"Researchers typically spend hours searching databases and reading through results to find relevant papers. Our system automates the analysis and quality evaluation. The feedback loop means researchers don't get poor results - the system keeps improving until it finds papers that meet the quality threshold."**

---

## Time Management During Recording

**Don't worry about hitting exactly 10 minutes.** Anywhere from 9:30 to 10:30 is fine.

**If running long (> 11 min):**
- Cut the "Code Walkthrough" section
- Shorten feature explanations
- Keep the demo (it's most important)

**If running short (< 9 min):**
- Expand each section with more examples
- Show another demo query
- Explain evaluation metrics in more detail
- Add more context about why each feature matters

---

## Quick Content Outline (One-Page Reference)

```
[0:00-1:00]  INTRO
             • Problem: Finding papers is hard
             • Solution: AI agent with quality control

[1:00-3:00]  ARCHITECTURE
             • Stage 1: Fetch 100 papers (1 API call)
             • Stage 2: AI analyzes & selects TOP 5
             • Stage 3: Judge scores with 4 metrics

[3:00-5:30]  AUTO-RETRY INNOVATION
             • Score < 70? Generate feedback
             • LLM analyzes what went wrong
             • Agent re-analyzes with guidance
             • Score improves (65→82 example)

[5:30-8:00]  DEMO (Live or Code)
             • Show application running
             • Input: natural language query
             • Output: 5 papers + evaluation scores
             • Explain the results

[8:00-9:00]  KEY FEATURES
             • Single API call
             • Intelligent feedback
             • Quality control
             • Graceful fallbacks

[9:00-9:30]  TECHNOLOGY & IMPACT
             • Tech stack: C#, .NET, Mistral AI
             • Innovation: feedback loop
             • Impact: better, faster results

[9:30-10:00] CONCLUSION
             • Thank you
             • Questions?
```

---

## Backup Plan (If something goes wrong)

- [ ] Have the README open - can read key points directly from it
- [ ] Have test output screenshots ready to show
- [ ] Have the source code open as backup if demo fails
- [ ] Have the diagrams/visuals downloadable if presentation software fails
- [ ] Have a PDF version of the script as reference

---

## Final Checklist Before Clicking Record

- [ ] Microphone is working and levels are good
- [ ] Screen resolution is set appropriately
- [ ] IDE/Terminal is ready with good font size
- [ ] Application is built and tested
- [ ] Slides are prepared and visible
- [ ] Script is nearby for reference
- [ ] All notifications are disabled
- [ ] Recording software is set to high quality
- [ ] You're in a quiet, well-lit space
- [ ] You have water nearby (stay hydrated!)

**You're ready! Click record and speak naturally. This is a great project - let your enthusiasm show through!**
