using System.ComponentModel;
using AcadInteraction.Core;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AcadMcpServer
{
    /// <summary>
    /// MCP tools for AutoCAD. Failures are thrown as <see cref="McpException"/> so the client receives an error result.
    /// </summary>
    [McpServerToolType]
    public static class AcadTools
    {
        /// <summary>
        /// When true (started with --read-only), tools that modify drawings or execute code are refused.
        /// </summary>
        public static bool ReadOnlyMode { get; set; }

        private static T RunWrite<T>(string toolName, Func<T> action)
        {
            if (ReadOnlyMode)
                throw new McpException($"'{toolName}' is disabled: the server runs in read-only mode (--read-only).");
            return Run(action);
        }

        private static T Run<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (McpException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new McpException(ex.Message, ex);
            }
        }

        // ---- Proc ----

        [McpServerTool(Name = "list_acad_processes", ReadOnly = true),
         Description("Returns the list of running acad.exe processes (PID and name).")]
        public static string ListAcadProcesses() => Run(() =>
        {
            var procs = Proc.GetProcess();
            return procs.Count == 0
                ? "No acad.exe processes found."
                : string.Join("\n", procs.Select(p => $"PID {p.Key}: {p.Value}"));
        });

        // ---- Interop (COM) ----

        private const string DocDesc = "Optional document name (e.g. \"Drawing1.dwg\") or full path; uses the active document when empty. See list_documents.";

        [McpServerTool(Name = "list_documents", ReadOnly = true),
         Description("Lists the open AutoCAD drawings with name, path, modified state and which one is active.")]
        public static string ListDocuments() => Run(AcadCommands.ListDocuments);

        [McpServerTool(Name = "activate_document"),
         Description("Makes the given open drawing the active document.")]
        public static string ActivateDocument([Description("Document name or full path")] string document) =>
            Run(() => AcadCommands.ActivateDocument(document));

        [McpServerTool(Name = "get_acad_info", ReadOnly = true),
         Description("Returns the AutoCAD version, the name of the drawing and the number of objects in model space.")]
        public static string GetAcadInfo([Description(DocDesc)] string document = "") =>
            Run(() => AcadCommands.GetInfo(document));

        [McpServerTool(Name = "draw_line"),
         Description("Draws a line in model space of an AutoCAD drawing.")]
        public static string DrawLine(
            [Description("Start X")] double x1, [Description("Start Y")] double y1,
            [Description("End X")] double x2, [Description("End Y")] double y2,
            [Description(DocDesc)] string document = "") =>
            RunWrite("draw_line", () => AcadCommands.DrawLine(x1, y1, x2, y2, document));

        [McpServerTool(Name = "draw_circle"),
         Description("Draws a circle in model space of an AutoCAD drawing.")]
        public static string DrawCircle(
            [Description("Center X")] double x, [Description("Center Y")] double y,
            [Description("Radius")] double radius,
            [Description(DocDesc)] string document = "") =>
            RunWrite("draw_circle", () => AcadCommands.DrawCircle(x, y, radius, document));

        [McpServerTool(Name = "add_text"),
         Description("Places single-line text in model space of an AutoCAD drawing.")]
        public static string AddText(
            [Description("The text")] string text,
            [Description("Insertion point X")] double x, [Description("Insertion point Y")] double y,
            [Description("Text height")] double height,
            [Description(DocDesc)] string document = "") =>
            RunWrite("add_text", () => AcadCommands.AddText(text, x, y, height, document));

        [McpServerTool(Name = "zoom_extents"),
         Description("Zooms AutoCAD to the extents of the drawing (activates the document first if given).")]
        public static string ZoomExtents([Description(DocDesc)] string document = "") =>
            Run(() => AcadCommands.ZoomExtents(document));

        [McpServerTool(Name = "send_command", Destructive = true),
         Description("Sends a command-line string to an AutoCAD drawing (SendCommand). Does not return command output; use eval_lisp to inspect results. " +
                     "Terminate commands with a space or \\n, for example \"_REGEN \".")]
        public static string SendCommand(
            [Description("The command string")] string command,
            [Description(DocDesc)] string document = "") =>
            RunWrite("send_command", () => AcadCommands.SendCommand(command, document));

        [McpServerTool(Name = "eval_lisp", Destructive = true),
         Description("Evaluates an AutoLISP expression in AutoCAD and returns its result (printed with vl-prin1-to-string). " +
                     "Errors are returned as 'ERROR: <message>'. Example: (sslength (ssget \"X\")) or (getvar \"DWGNAME\").")]
        public static string EvalLisp(
            [Description("A single AutoLISP expression")] string expression,
            [Description(DocDesc)] string document = "",
            [Description("Timeout in milliseconds")] int timeoutMs = 30000) =>
            RunWrite("eval_lisp", () => AcadCommands.EvalLisp(expression, document, timeoutMs));

        // ---- DDE ----

        [McpServerTool(Name = "send_dde_commands", Destructive = true),
         Description("Sends one or more commands via DDE to the AutoCAD instance registered as the .dwg handler.")]
        public static string SendDdeCommands([Description("List of DDE commands, for example \"[_LINE 0,0 10,10 ]\"")] string[] commands) => RunWrite("send_dde_commands", () =>
        {
            new Dde().Execute(commands);
            return $"{commands.Length} command(s) sent via DDE.";
        });

        // ---- AcShellEx ----

        [McpServerTool(Name = "open_dwg"),
         Description("Opens a DWG file with the AutoCAD Drawing Launcher (AcLauncher.exe), which automatically selects the correct AutoCAD version.")]
        public static string OpenDwg([Description("Full path to the .dwg file")] string path) => Run(() =>
        {
            AcShellEx.OpenWithAcLauncher(path);
            return $"Opened: {path}";
        });

        // ---- AccoreConsole ----

        [McpServerTool(Name = "run_accoreconsole_script", Destructive = true),
         Description("Runs an AutoCAD script (.scr content) headless with accoreconsole.exe and returns the console output.")]
        public static string RunAccoreConsoleScript(
            [Description("Script content; one line per command")] string scriptContent,
            [Description("Optional path to a .dwg to open")] string drawingPath = "",
            [Description("Optional path to accoreconsole.exe; detected automatically when empty")] string accoreConsolePath = "") => RunWrite("run_accoreconsole_script", () =>
        {
            var runner = new AccoreConsoleRunner(accoreConsolePath, drawingPath);
            string script = runner.CreateTempScriptFile(scriptContent);
            try
            {
                return runner.RunScript(script);
            }
            finally
            {
                runner.DeleteTempFile(script);
            }
        });
    }
}
