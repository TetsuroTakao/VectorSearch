using Microsoft.SemanticKernel;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Milvus;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.HuggingFace;
using Microsoft.SemanticKernel.Embeddings;

public class PolyglotPersistencePlugin
{
    #pragma warning disable SKEXP0001, SKEXP0011, SKEXP0020, SKEXP0025
    ITextEmbeddingGenerationService embeddingGenerator {get; set;} = null!;
    IMemoryStore memoryStore {get; set;} = null!;
    Milvus.Client.MilvusClient milvusClient {get; set;} = null!;
    SemanticTextMemory textMemory {get; set;} = null!;
    public async void Initialize(string apiKey)
    {
        await MilvusCheck();
        memoryStore = new MilvusMemoryStore(milvusClient, "book",1536,Milvus.Client.SimilarityMetricType.Ip);
        milvusClient = new Milvus.Client.MilvusClient(host: "localhost", port: 19530, ssl:false);
        if(apiKey == Environment.GetEnvironmentVariable("AI:HuggingFace:APIKey")!)
        {
            embeddingGenerator = new HuggingFaceTextEmbeddingGenerationService("intfloat/multilingual-e5-large", new Uri(@"https://huggingface.co/intfloat/multilingual-e5-large"), apiKey);
        }
        else
        {
            embeddingGenerator = new OpenAITextEmbeddingGenerationService("text-embedding-ada-002", apiKey!);
        }
        textMemory = new(memoryStore, embeddingGenerator);
    }
    public async Task MilvusCheck()
    {
        var databases = await milvusClient.ListDatabasesAsync();
        if(databases.Where(x => x == "book").Count() == 0 ) await milvusClient.CreateDatabaseAsync("book");
    }
    public bool IsDefault { get; set; } = false;
    [KernelFunction]
    [Description("Gets the Kernel memory type.")]
    public string GetState() => IsDefault ? "Azure OpenAI" : "Third Party Milvus";
    [KernelFunction]
    [Description("Changes the state of memory type.")]
    public string ChangeState(bool newState)
    {
        this.IsDefault = newState;
        var state = GetState();
        return state;
    }
    public async Task<string> MilvusINPUT(string input)
    {
        await textMemory.SaveInformationAsync("PromptFlowSample", id: "info2", text: input);
        return "regist database";
    }
    public async Task<string> MilvusOUTPUT(string index)
    {
        MemoryQueryResult? lookup = await textMemory.GetAsync("PromptFlowSample", index);
        return lookup?.Metadata.Text ?? "No data found";
    }
    public async Task<string> MilvusSERACH(string inquery)
    {
        var ans = string.Empty;
        await foreach (var answer in textMemory.SearchAsync(
            collection: "PromptFlowSample",
            query: inquery,
            limit: 2,
            minRelevanceScore: 0.79,
            withEmbeddings: true))
        {
            ans = $"Answer: {answer.Metadata.Text}";
        }
        return ans == string.Empty ? "No answer found" : ans;
    }
}