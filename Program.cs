using Azure.AI.Language.Conversations;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.Identity;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

var command = GetCommand();
command.Handler = CommandHandler.Create<Options>(async options =>
{
    DefaultAzureCredential credential = new();
    ConversationAnalysisClient conversationClient = new(options.Endpoint, credential);

    var request = new
    {
        analysisInput = new
        {
            conversationItem = new
            {
                text = options.Question,
                id = "1",
                participantId = "1",
            }
        },
        parameters = new
        {
            projectName = options.Project,
            deploymentName = options.Deployment,
            stringIndexType = "Utf16CodeUnit",
        },
        kind = "Conversation",
    };

    // Use Conversation Analysis to recognize entities.
    var response = await conversationClient.AnalyzeConversationAsync(RequestContent.Create(request));

    Console.WriteLine("Conversation Analysis recognized:");
    dynamic json = JsonData.FromStream(response.ContentStream);
    foreach (var entity in json.result.prediction.entities)
    {
        Console.WriteLine($"{entity.category}: {entity.text}");
    }
    Console.WriteLine();

    TextAnalyticsClient textAnalyticsClient = new(options.Endpoint, credential);

    // Use Text Analytics to recognize entities.
    CategorizedEntityCollection entities = await textAnalyticsClient.RecognizeEntitiesAsync(options.Question);

    Console.WriteLine("Text Analytics recognized:");
    foreach (var entity in entities)
    {
        Console.WriteLine($"{entity.Category}: {entity.Text}");
    }
    Console.WriteLine();
});
await command.InvokeAsync(args);

#region Helpers
static Command GetCommand() => new RootCommand("Cognitive Services - Language Sample")
{
    new Option<Uri>("--endpoint", getDefaultValue: () => Uri.TryCreate(Environment.GetEnvironmentVariable("CONVERSATIONS_ENDPOINT"), UriKind.Absolute, out var endpoint) ? endpoint : null) { IsRequired = true },
    new Option<string>("--project", getDefaultValue: () => Environment.GetEnvironmentVariable("CONVERSATIONS_PROJECT_NAME")) { IsRequired = true },
    new Option<string>("--deployment", getDefaultValue: () => Environment.GetEnvironmentVariable("CONVERSATIONS_DEPLOYMENT_NAME") ?? "production") { IsRequired = true },
    new Option<string>(new[] {"--question", "-q" }, getDefaultValue: () => "Send an email to Carol about tomorrow's demo") { IsRequired = true },
};

class Options
{
    public Uri Endpoint { get; set; }
    public string Project { get; set; }
    public string Deployment { get; set; }
    public string Question { get; set; }
}
#endregion
