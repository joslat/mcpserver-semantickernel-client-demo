using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Transport;

namespace SemanticKernel_consuming_Aspire_mcp;

public static class skmcp
{
    public static async Task Execute()
    {
        var modelDeploymentName = "gpt-4o";
        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZUREOPENAI_ENDPOINT");
        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZUREOPENAI_APIKEY");

        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                modelDeploymentName,
                azureOpenAIEndpoint,
                azureOpenAIApiKey)
            .Build();

        // Create an MCPClient for our server
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new McpServerConfig()
            {
                Id = "UtcTimeTool",
                Name = "GetCurrentTime",
                TransportType = TransportTypes.Sse,
                // The transport type is set to Sse, which is a server-sent events transport.
                Location = "http://localhost:5554/sse"
            },
            new McpClientOptions()
            {
                ClientInfo = new Implementation()
                {
                    Name = "UtcTimeTool",
                    Version = "1.0.0"
                }
            }).ConfigureAwait(false);

        // Retrieve the list of tools available mcp server
        var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
        foreach (var tool in tools)
        {
            Console.WriteLine($"{tool.Name}: {tool.Description}");
        }

        kernel.Plugins.AddFromFunctions("UtcTimeTool", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

        string userPrompt = "I would like to know what date is it and 5 significant" +
            "things that happened on the past on this day.";

        OpenAIPromptExecutionSettings promptExecutionSettings = new()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true }),
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var result = await kernel.InvokePromptAsync(
            userPrompt,
            new(promptExecutionSettings));

        Console.WriteLine($"\n\n{userPrompt}\n{result}\n");
    }
}
