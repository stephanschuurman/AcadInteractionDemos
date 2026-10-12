# AcadInteraction.McpServer

A [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that exposes AutoCAD automation as tools for AI assistants such as GitHub Copilot (Visual Studio / VS Code) and Claude Desktop.

The server uses the **stdio** transport: the MCP client launches the executable and communicates via stdin/stdout. You normally do not start it yourself.

## Requirements

- Windows with AutoCAD installed
- .NET 10 SDK
- For COM tools: a running AutoCAD instance with an open drawing

## Build

```powershell
dotnet build AcadInteraction.McpServer -c Release
```

## Configure a client

### Generate `.mcp.json` automatically

The server can print a ready-to-use config with the correct path to the executable:

```powershell
.\AcadInteraction.McpServer\bin\Debug\net10.0\AcadInteraction.McpServer.exe --print-config > .mcp.json
```

The same config is also shown on stderr each time the server starts.

### Visual Studio / VS Code (GitHub Copilot)

Create `.mcp.json` in the solution root:

```json
{
  "servers": {
	"acad": {
	  "type": "stdio",
	  "command": "dotnet",
	  "args": [ "run", "--project", "C:\\path\\to\\AcadInteraction.McpServer\\AcadInteraction.McpServer.csproj" ]
	}
  }
}
```

Open Copilot Chat in **Agent** mode and enable the `acad` tools via the tools icon.

### Claude Desktop

Add to `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
	"acad": {
	  "command": "C:\\path\\to\\AcadInteraction.McpServer\\bin\\Release\\net10.0\\AcadInteraction.McpServer.exe"
	}
  }
}
```

Pointing to the built `.exe` gives faster startup than `dotnet run`.

## Tools

| Tool | Description | Needs running AutoCAD |
|------|-------------|:---:|
| `list_acad_processes` | Lists running `acad.exe` processes | No |
| `list_documents` | Open drawings with name, path, modified state and which is active | Yes |
| `activate_document` | Makes an open drawing the active document | Yes |
| `get_acad_info` | AutoCAD version, drawing, model space object count | Yes |
| `draw_line` | Draws a line in model space | Yes |
| `draw_circle` | Draws a circle in model space | Yes |
| `add_text` | Places single-line text | Yes |
| `zoom_extents` | Zooms to drawing extents | Yes |
| `send_command` | Sends a command string (`SendCommand`); returns no output | Yes |
| `eval_lisp` | Evaluates an AutoLISP expression and returns the result | Yes |
| `send_dde_commands` | Sends commands via DDE | Yes |
| `open_dwg` | Opens a DWG via AcLauncher | No |
| `run_accoreconsole_script` | Runs a script headless with `accoreconsole.exe` | No |

All drawing tools (`get_acad_info`, `draw_*`, `add_text`, `zoom_extents`, `send_command`, `eval_lisp`) accept an optional
`document` parameter (name such as `Drawing1.dwg`, with or without extension, or full path). When empty, the active drawing is used.
`zoom_extents`, `send_command` and `eval_lisp` activate the given drawing first.

## Example prompts

- "Which AutoCAD processes are running?"
- "Which drawings are open?"
- "How many objects are in Drawing1? Use (sslength (ssget \"X\"))."
- "Draw a circle at 0,0 with radius 50 and zoom extents."
- "Place the text 'Hello' at 10,10 with height 2.5."
- "Open C:\temp\test.dwg."
- "Run this script headless on C:\temp\test.dwg: `_-LAYER _N Walls ` ..."

## Tips & troubleshooting

- **Never write to stdout** (`Console.WriteLine`) in server code: stdout is reserved for the MCP protocol. All logging goes to stderr.
- Starting the executable manually shows startup info on stderr and then waits for MCP messages on stdin; press `Ctrl+C` to stop.
- To test interactively, use the MCP Inspector:
  ```powershell
  npx @modelcontextprotocol/inspector dotnet run --project AcadInteraction.McpServer
  ```
- If the client does not show the tools, check the client's output/log window for startup errors.
- COM errors such as `MK_E_UNAVAILABLE` usually mean AutoCAD is not running or is busy (e.g. a modal dialog is open).
- `send_command`, `eval_lisp`, `send_dde_commands` and `run_accoreconsole_script` are marked **destructive**: they can execute arbitrary AutoCAD commands or AutoLISP. Review before approving.
- **Read-only mode**: start with `--read-only` (add `"args": ["--read-only"]` to the config) to refuse `draw_*`, `add_text`, `send_command`, `eval_lisp`, `send_dde_commands` and `run_accoreconsole_script`.
- `eval_lisp` returns errors as `ERROR: <message>`
- **Copilot home** (`~/.copilot/mcp-config.json`, used by VS Code after migration): the server can only start after a chat message has been sent. Starting it earlier from *MCP: List Servers* fails with "Cannot start an MCP server before the session runtime has been created".
