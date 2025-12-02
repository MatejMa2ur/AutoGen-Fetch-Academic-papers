using Autogen_research_paper_tool_calling_evaluation;
using Autogen_research_paper_tool_calling_evaluation.Agents;
using AutoGen.Core;

var mistralClient = LLMConfiguration.GetMistralNemo();

// Create Agents
var manager = Agents.CreateManagerAgent(mistralClient);
var fetcher = Agents.CreateFetcherAgent(mistralClient);
var analyzer = Agents.CreateAnalyzerAgent(mistralClient);
var critique = Agents.CreateCritiqueAgent(mistralClient);

// Create Workflow with transitions
// Transition 1: Manager always sends to Fetcher
var manager2fetcherTransition = Transition.Create(manager, fetcher);

// Transition 2: Fetcher always sends to Analyzer
var fetcher2analyzerTransition = Transition.Create(fetcher, analyzer);

// Transition 3: Analyzer to Critique (only if papers are RELEVANT)
var analyzer2critiqueTransition = Transition.Create(analyzer, critique);

// Transition 4: Analyzer to Manager (if papers are NOT RELEVANT - BAD RESULT)
var analyzer2managerNotRelevantTransition = Transition.Create(analyzer, manager, async (from, to, messages) =>
{
    var lastMessage = messages.Last();
    if (lastMessage.From != analyzer.Name)
        return false;

    var content = lastMessage.GetContent() ?? "";
    // Go back to manager if papers are not relevant (bad result from analyzer)
    return content.Contains("\"status\":\"not_relevant\"") || content.Contains("not_relevant");
});

// Transition 5: Critique to Manager (APPROVED - GOOD RESULT)
var critique2managerApprovedTransition = Transition.Create(critique, manager, async (from, to, messages) =>
{
    var lastMessage = messages.Last();
    if (lastMessage.From != critique.Name)
        return false;

    var content = lastMessage.GetContent() ?? "";
    // End conversation if critique approves
    return content.Contains("APPROVED", StringComparison.OrdinalIgnoreCase);
});

// Transition 6: Critique to Fetcher (REJECTED - BAD RESULT, retry search)
var critique2fetcherRejectedTransition = Transition.Create(critique, fetcher, async (from, to, messages) =>
{
    var lastMessage = messages.Last();
    if (lastMessage.From != critique.Name)
        return false;

    var content = lastMessage.GetContent() ?? "";
    // Go back to fetcher if critique rejects (needs new search)
    return content.Contains("REJECTED", StringComparison.OrdinalIgnoreCase) && !content.Contains("APPROVED", StringComparison.OrdinalIgnoreCase);
});

var workflow = new Graph([
    manager2fetcherTransition,
    fetcher2analyzerTransition,
    analyzer2critiqueTransition,
    analyzer2managerNotRelevantTransition,
    critique2managerApprovedTransition,
    critique2fetcherRejectedTransition
]);

// Create GroupChat
var groupChat = new GroupChat(
    admin: manager,
    workflow: workflow,
    members:
    [
        manager,
        fetcher,
        analyzer,
        critique
    ]
);

var task = "Please give me a research paper about machine learning in health and medicine with at least 50 citations and from year 2015+";

// Create initial message from manager
var taskMessage = new TextMessage(Role.User, task)
{
    From = manager.Name
};

await foreach (var message in groupChat.SendAsync([taskMessage], maxRound: 10))
{
    if (message.From == "critique" && (message.GetContent()?.Contains("APPROVED") ?? false))
    {
        Console.WriteLine($"{message.GetContent()}");
        break;
    }
}