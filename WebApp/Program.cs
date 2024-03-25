using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
#pragma warning disable SKEXP0050

var webbuilder = WebApplication.CreateBuilder(args);
// kernel build
var apiKey = webbuilder.Configuration["AI:OpenAI:APIKey"];
var kernelbuilder = Kernel.CreateBuilder();
    // .AddAzureOpenAITextEmbeddingGeneration("SKPromptFlowDeploy", apiKey!, "text-embedding-ada-002")
    // .AddAzureOpenAIChatCompletion("SKPromptFlowDeploy", apiKey!, "text-davinci-002");
kernelbuilder.Plugins.AddFromType<PolyglotPersistencePlugin>();
Kernel kernel = kernelbuilder.Build();
// bing build
using ILoggerFactory factory = LoggerFactory.Create(provider => provider.AddConsole());
var apiKeyBing = webbuilder.Configuration["AI:Bing:APIKey"];
var bingConnector = new BingConnector(apiKeyBing!, factory);
// memory build
var apiKeyHuggingFace = Environment.GetEnvironmentVariable("AI:HuggingFace:APIKey")!;
var db = new PolyglotPersistencePlugin();
db.Initialize(apiKeyHuggingFace);

var webapplogger = factory.CreateLogger("Program");
var webapp = webbuilder.Build();
webapp.MapGet("/", () => "Hello World!");
webapp.MapPost("/CreateTransportationPlan", () => {
    var result = new List<string>();
    if(string.IsNullOrEmpty(db.MilvusSERACH("JR九州出張").Result)){
        foreach (var item in bingConnector.SearchAsync("JR九州出張", 10).Result)
        {
            result.Add(item);
        }
    }
    return result;
});
webapp.Run();