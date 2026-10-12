using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace AcadInteraction.Core
{
    /// <summary>
    /// AI assistant (Azure OpenAI) that controls AutoCAD through a fixed set of tools.
    /// Configuration via environment variables:
    ///   AZURE_OPENAI_ENDPOINT   e.g. https://my-resource.openai.azure.com/
    ///   AZURE_OPENAI_DEPLOYMENT e.g. gpt-4o
    /// Authentication: company account via DefaultAzureCredential (Visual Studio / Azure CLI / Windows login).
    /// </summary>
    public class AcadAssistant
    {
        private readonly ChatClient _chat;
        private readonly List<ChatMessage> _history = new();
        private readonly ChatCompletionOptions _options;

        private static readonly BinaryData PointSchema = BinaryData.FromString("""
            { "type":"object",
              "properties":{ "x1":{"type":"number"},"y1":{"type":"number"},
                             "x2":{"type":"number"},"y2":{"type":"number"} },
              "required":["x1","y1","x2","y2"] }
            """);

        public AcadAssistant()
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new InvalidOperationException("Environment variable AZURE_OPENAI_ENDPOINT is not set.");
            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o";

            var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
            _chat = client.GetChatClient(deployment);

            _options = new ChatCompletionOptions
            {
                Tools =
                {
                    ChatTool.CreateFunctionTool("draw_line",
                        "Draws a line in model space of the active drawing.", PointSchema),
                    ChatTool.CreateFunctionTool("draw_circle",
                        "Draws a circle in model space.",
                        BinaryData.FromString("""
                        { "type":"object",
                          "properties":{ "x":{"type":"number"},"y":{"type":"number"},"radius":{"type":"number"} },
                          "required":["x","y","radius"] }
                        """)),
                    ChatTool.CreateFunctionTool("add_text",
                        "Places single-line text in model space.",
                        BinaryData.FromString("""
                        { "type":"object",
                          "properties":{ "text":{"type":"string"},"x":{"type":"number"},"y":{"type":"number"},"height":{"type":"number"} },
                          "required":["text","x","y","height"] }
                        """)),
                    ChatTool.CreateFunctionTool("zoom_extents",
                        "Zooms to the extents of the drawing."),
                    ChatTool.CreateFunctionTool("get_drawing_info",
                        "Returns the name of the active drawing and the number of objects in model space.")
                }
            };

            _history.Add(new SystemChatMessage(
                "You are an assistant that controls AutoCAD through the available tools. " +
                "Only use the tools; do not make up results. Answer briefly in English."));
        }

        public async Task<string> AskAsync(string prompt)
        {
            _history.Add(new UserChatMessage(prompt));

            for (int i = 0; i < 10; i++) // guard against endless tool loops
            {
                ChatCompletion result = await _chat.CompleteChatAsync(_history, _options);

                if (result.FinishReason != ChatFinishReason.ToolCalls)
                {
                    var answer = result.Content.Count > 0 ? result.Content[0].Text : "";
                    _history.Add(new AssistantChatMessage(answer));
                    return answer;
                }

                _history.Add(new AssistantChatMessage(result));
                foreach (var call in result.ToolCalls)
                    _history.Add(new ToolChatMessage(call.Id, Execute(call)));
            }

            return "Stopped: too many consecutive tool calls.";
        }

        private static string Execute(ChatToolCall call)
        {
            try
            {
                using var args = JsonDocument.Parse(call.FunctionArguments.ToString() is { Length: > 0 } s ? s : "{}");
                var r = args.RootElement;
                double D(string name) => r.GetProperty(name).GetDouble();

                return call.FunctionName switch
                {
                    "draw_line" => AcadCommands.DrawLine(D("x1"), D("y1"), D("x2"), D("y2")),
                    "draw_circle" => AcadCommands.DrawCircle(D("x"), D("y"), D("radius")),
                    "add_text" => AcadCommands.AddText(r.GetProperty("text").GetString() ?? "", D("x"), D("y"), D("height")),
                    "zoom_extents" => AcadCommands.ZoomExtents(),
                    "get_drawing_info" => AcadCommands.GetInfo(),
                    _ => $"Unknown function: {call.FunctionName}"
                };
            }
            catch (Exception ex)
            {
                return $"Error in {call.FunctionName}: {ex.Message}";
            }
        }
    }
}
