using System.Text;
using Autogen_research_paper_tool_calling_evaluation;
using Autogen_research_paper_tool_calling_evaluation.Agents;
using AutoGen.Core;
using AutoGen.Mistral;
using AutoGen.Mistral.Extension;

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

// Capture conversation messages for evaluation
var conversationMessages = new List<IMessage>();
var taskCompleted = false;

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("STARTING MULTI-AGENT CONVERSATION");
Console.WriteLine(new string('=', 60) + "\n");

await foreach (var message in groupChat.SendAsync([taskMessage], maxRound: 10))
{
    conversationMessages.Add(message);
    if (message.From == "critique" && (message.GetContent()?.Contains("APPROVED") ?? false))
    {
        Console.WriteLine($"{message.GetContent()}");
        taskCompleted = true;
        break;
    }
}

// ==================== EXTERNAL EVALUATION ====================
Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("EXTERNAL EVALUATION - Assessing Agent Performance");
Console.WriteLine(new string('=', 60) + "\n");

// Build conversation summary for evaluator
var conversationSummary = new StringBuilder();
conversationSummary.AppendLine("=== CONVERSATION HISTORY ===");
conversationSummary.AppendLine($"Task: {task}");
conversationSummary.AppendLine($"Task Status: {(taskCompleted ? "COMPLETED" : "INCOMPLETE")}");
conversationSummary.AppendLine($"Total Rounds: {conversationMessages.Count}");
conversationSummary.AppendLine("\n=== DETAILED CONVERSATION ===\n");

foreach (var msg in conversationMessages)
{
    var content = msg.GetContent();
    if (!string.IsNullOrEmpty(content))
    {
        // Truncate very long messages for readability
        var displayContent = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
        conversationSummary.AppendLine($"[{msg.From}]: {displayContent}");
        conversationSummary.AppendLine();
    }
}

// Create evaluator agent
var evaluator = Agents.CreateEvaluatorAgent(mistralClient);

// Create evaluation initiator agent
var evaluationInitiator = new MistralClientAgent(
    client: mistralClient,
    name: "eval_initiator",
    model: "ministral-8b-2410",
    systemMessage: @"You are responsible for initiating the evaluation process.
You will ask the evaluator to assess the multi-agent system's performance.")
    .RegisterMessageConnector()
    .RegisterPrintMessage();

// Create a simple GroupChat for evaluation
var evaluationGroupChat = new GroupChat(
    admin: evaluationInitiator,
    members: [evaluationInitiator, evaluator]
);

// Send conversation summary to evaluator
var evaluationPrompt = $"""
Please evaluate the performance of this multi-agent research paper discovery system based on the conversation history below.

Provide a detailed evaluation using the JSON format specified in your system message.

{conversationSummary}
""";

var evaluationMessage = new TextMessage(Role.User, evaluationPrompt)
{
    From = evaluationInitiator.Name
};

Console.WriteLine("Evaluating agent performance...\n");

await foreach (var response in evaluationGroupChat.SendAsync([evaluationMessage], maxRound: 2))
{
    if (response.From == "evaluator")
    {
        Console.WriteLine("\n=== EVALUATION RESULTS ===\n");
        Console.WriteLine(response.GetContent());
        Console.WriteLine("\n" + new string('=', 60));
        break;
    }
}