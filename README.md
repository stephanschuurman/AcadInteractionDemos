# AutoCAD Interaction Demos

Requires the .NET 10 SDK and Windows.

## Projects

| Project | Description |
|---|---|
| `AcadInteraction.Core` | Shared library with all AutoCAD interaction code. |
| `AcadInteraction.WinForms` | WinForms demo app, including an AI assistant (Azure OpenAI). |
| `AcadInteraction.McpServer` | MCP server (stdio) exposing AutoCAD tools to AI clients. |
| `AcadInteraction.Core.Tests` | xUnit tests for the Core library. |

### Core features

- AcShellEx, open drawings using the AutoCAD Drawing Launcher.
- DDE, old school uniform way to interact with AutoCAD.
- Interop, COM interface to a running AutoCAD (with retry while AutoCAD is busy).
- AcadCommands, shared drawing operations used by the MCP server and the AI assistant.
- AccoreConsoleRunner, run scripts headless with accoreconsole.exe (with timeout).
- Proc, list all running acad processes (PID, name).

## Configuration

| Environment variable | Purpose |
|---|---|
| `ACAD_ACCORECONSOLE` | Path to `accoreconsole.exe`. When not set, it is detected from the registry, falling back to AutoCAD 2025. |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint for the AI assistant. |
| `AZURE_OPENAI_DEPLOYMENT` | Deployment name (default `gpt-4o`). |

## MCP server

Tool failures (e.g. AutoCAD not running) are returned as MCP error results.