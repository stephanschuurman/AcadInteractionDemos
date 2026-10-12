namespace AcadInteraction.Core
{
    public class AcadNotRunningException : InvalidOperationException
    {
        public AcadNotRunningException() : base("AutoCAD is not running.") { }
    }

    /// <summary>
    /// Shared AutoCAD operations used by the MCP server and the WinForms demo.
    /// Each call attaches to the running AutoCAD, retries while it is busy and releases COM references.
    /// </summary>
    public static class AcadCommands
    {
        private static string Run(Func<dynamic, dynamic, string> action, string? document = null, bool activate = false)
        {
            return ComRetry.Invoke(() =>
            {
                dynamic? app = Interop.GetAcadApp() ?? throw new AcadNotRunningException();
                dynamic? doc = null;
                try
                {
                    doc = string.IsNullOrWhiteSpace(document) ? app.ActiveDocument : FindDocument(app, document);
                    if (activate && !string.IsNullOrWhiteSpace(document))
                        doc.Activate();
                    return action(app, doc);
                }
                finally
                {
                    ComRetry.Release((object?)doc);
                    ComRetry.Release((object?)app);
                }
            });
        }

        /// <summary>
        /// True when <paramref name="document"/> matches the document name (with or without extension) or full path.
        /// </summary>
        public static bool MatchesDocument(string document, string name, string? fullName) =>
            string.Equals(name, document, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(name), document, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(fullName) && string.Equals(fullName, document, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Builds the AutoLISP command that evaluates <paramref name="expression"/> and writes the result to <paramref name="resultFile"/>.
        /// </summary>
        public static string BuildEvalLispCommand(string expression, string resultFile)
        {
            string lispPath = resultFile.Replace('\\', '/');
            string expr = expression.Replace("\r", " ").Replace("\n", " ");

            return
                "(progn (vl-load-com)" +
                $"(setq *mcp-r* (vl-catch-all-apply (function (lambda () {expr}))))" +
                $"(setq *mcp-f* (open \"{lispPath}\" \"w\"))" +
                "(write-line (if (vl-catch-all-error-p *mcp-r*) (strcat \"ERROR: \" (vl-catch-all-error-message *mcp-r*)) (vl-prin1-to-string *mcp-r*)) *mcp-f*)" +
                "(close *mcp-f*)(setq *mcp-r* nil *mcp-f* nil)(princ))\n";
        }

        /// <summary>
        /// Finds an open document by name
        /// </summary>
        private static dynamic FindDocument(dynamic app, string document)
        {
            dynamic docs = app.Documents;
            try
            {
                var names = new List<string>();
                int count = docs.Count;
                for (int i = 0; i < count; i++)
                {
                    dynamic d = docs.Item(i);
                    string name = d.Name;
                    string fullName = d.FullName;
                    if (MatchesDocument(document, name, fullName))
                        return d;
                    names.Add(name);
                    ComRetry.Release((object)d);
                }
                throw new InvalidOperationException($"Document '{document}' is not open. Open documents: {string.Join(", ", names)}");
            }
            finally
            {
                ComRetry.Release((object)docs);
            }
        }

        private static string AddToModelSpace(Func<dynamic, object> add, string result, string? document) => Run((_, doc) =>
        {
            dynamic modelSpace = doc.ModelSpace;
            try
            {
                ComRetry.Release(add(modelSpace));
                return $"{result} ({doc.Name})";
            }
            finally
            {
                ComRetry.Release((object)modelSpace);
            }
        }, document);

        public static string GetInfo(string? document = null) => Run((app, doc) =>
        {
            dynamic modelSpace = doc.ModelSpace;
            try
            {
                return $"AutoCAD version: {app.Version}, drawing: {doc.FullName}, objects in model space: {modelSpace.Count}";
            }
            finally
            {
                ComRetry.Release((object)modelSpace);
            }
        }, document);

        public static string ListDocuments() => Run((app, active) =>
        {
            string activeName = active.Name;
            dynamic docs = app.Documents;
            try
            {
                var lines = new List<string>();
                int count = docs.Count;
                for (int i = 0; i < count; i++)
                {
                    dynamic d = docs.Item(i);
                    try
                    {
                        string name = d.Name;
                        string fullName = d.FullName;
                        bool saved = d.Saved;
                        string marker = name == activeName ? "* " : "  ";
                        lines.Add($"{marker}{name} | path: {(string.IsNullOrEmpty(fullName) ? "(not saved)" : fullName)} | modified: {(saved ? "no" : "yes")}");
                    }
                    finally
                    {
                        ComRetry.Release((object)d);
                    }
                }
                return lines.Count == 0 ? "No documents open." : "(* = active)\n" + string.Join("\n", lines);
            }
            finally
            {
                ComRetry.Release((object)docs);
            }
        });

        public static string ActivateDocument(string document) =>
            Run((_, doc) => $"Active document: {doc.Name}", document, activate: true);

        public static string DrawLine(double x1, double y1, double x2, double y2, string? document = null) =>
            AddToModelSpace(ms => ms.AddLine(new double[] { x1, y1, 0 }, new double[] { x2, y2, 0 }), "Line drawn.", document);

        public static string DrawCircle(double x, double y, double radius, string? document = null) =>
            AddToModelSpace(ms => ms.AddCircle(new double[] { x, y, 0 }, radius), "Circle drawn.", document);

        public static string AddText(string text, double x, double y, double height, string? document = null) =>
            AddToModelSpace(ms => ms.AddText(text, new double[] { x, y, 0 }, height), "Text placed.", document);

        public static string ZoomExtents(string? document = null) => Run((app, _) =>
        {
            app.ZoomExtents();
            return "Zoomed to extents.";
        }, document, activate: true);

        public static string SendCommand(string command, string? document = null) => Run((_, doc) =>
        {
            doc.SendCommand(command);
            return $"Sent to {doc.Name}: {command}";
        }, document, activate: true);

        /// <summary>
        /// Evaluates an AutoLISP expression and returns its printed result.
        /// The result is written by AutoLISP to a temp file because COM offers no direct eval.
        /// </summary>
        public static string EvalLisp(string expression, string? document = null, int timeoutMs = 30000)
        {
            string resultFile = Path.Combine(Path.GetTempPath(), $"acadmcp_{Guid.NewGuid():N}.txt");

            SendCommand(BuildEvalLispCommand(expression, resultFile), document);

            try
            {
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < deadline)
                {
                    if (File.Exists(resultFile))
                    {
                        try
                        {
                            return File.ReadAllText(resultFile).TrimEnd();
                        }
                        catch (IOException)
                        {
                            // still being written by AutoCAD
                        }
                    }
                    Thread.Sleep(100);
                }
                throw new TimeoutException($"No result from AutoLISP within {timeoutMs} ms. Check the expression for unbalanced parentheses or a pending command/prompt in AutoCAD.");
            }
            finally
            {
                try { File.Delete(resultFile); } catch (IOException) { }
            }
        }
    }
}
