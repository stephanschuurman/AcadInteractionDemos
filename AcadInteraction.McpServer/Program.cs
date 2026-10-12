using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

string exePath = Environment.ProcessPath ?? "AcadInteraction.McpServer.exe";
string mcpConfig = System.Text.Json.JsonSerializer.Serialize(
    new { servers = new { acad = new { type = "stdio", command = exePath } } },
    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

if (args.Contains("--print-config"))
{
    Console.Out.WriteLine(mcpConfig);
    return;
}

AcadInteraction.McpServer.AcadTools.ReadOnlyMode = args.Contains("--read-only");

var builder = Host.CreateApplicationBuilder(args);

// stdout is reserved for the MCP protocol
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Every tool call is forwarded to the WinForms log console (named pipe) when it is open
var logPipe = new AcadInteraction.Core.LogPipeClient();
string pidTag = $"MCP {Environment.ProcessId}";
logPipe.Send($"[{pidTag}] server started{(AcadInteraction.McpServer.AcadTools.ReadOnlyMode ? " (read-only)" : "")}");

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (request, cancellationToken) =>
    {
        string name = request.Params?.Name ?? "?";
        string arguments = request.Params?.Arguments is { Count: > 0 } a
            ? string.Join(", ", a.Select(kv => $"{kv.Key}={kv.Value.GetRawText()}"))
            : "";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        logPipe.Send($"[{pidTag}] -> {name}({arguments})");
        try
        {
            var result = await next(request, cancellationToken);
            string text = string.Join(" ", result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));
            if (text.Length > 300)
                text = text[..300] + "...";
            logPipe.Send($"[{pidTag}] <- {name} {(result.IsError == true ? "ERROR" : "OK")} ({sw.ElapsedMilliseconds} ms): {text}");
            return result;
        }
        catch (Exception ex)
        {
            logPipe.Send($"[{pidTag}] <- {name} ERROR ({sw.ElapsedMilliseconds} ms): {ex.Message}");
            throw;
        }
    }));

var app = builder.Build();

// Startup info goes to stderr; stdout is reserved for the MCP protocol
Console.Error.WriteLine($"""
    AcadInteraction.McpServer started (stdio transport, PID {Environment.ProcessId}{(AcadInteraction.McpServer.AcadTools.ReadOnlyMode ? ", READ-ONLY mode" : "")}).
    Tips:
      - This server is normally launched by an MCP client (Copilot, Claude Desktop) via .mcp.json.
      - It is now waiting for MCP JSON-RPC messages on stdin. Press Ctrl+C to stop.
      - Test interactively: npx @modelcontextprotocol/inspector dotnet run --project AcadInteraction.McpServer
      - COM tools require a running AutoCAD instance with an open drawing.
      - Never write to stdout in server code; log to stderr instead.
      - Generate .mcp.json: AcadInteraction.McpServer.exe --print-config > .mcp.json
      - Start with --read-only to disable tools that modify drawings or execute code.

    .mcp.json for this build:
    {mcpConfig}
    """);

await app.RunAsync();
