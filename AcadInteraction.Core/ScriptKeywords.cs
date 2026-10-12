namespace AcadInteraction.Core
{
    /// <summary>
    /// ScriptPro-compatible script keywords, replaced per drawing before the script runs.
    /// Example for C:\Work\Plan.dwg: cFolderName=C:\Work, cBaseName=Plan, cExtension=.dwg,
    /// cFileName=Plan.dwg, cFullFileName=C:\Work\Plan.dwg.
    /// </summary>
    public static class ScriptKeywords
    {
        public static readonly IReadOnlyList<string> Names =
            new[] { "cFolderName", "cBaseName", "cExtension", "cFileName", "cFullFileName" };

        public static bool ContainsKeywords(string script) =>
            script.Contains("<acet:", StringComparison.OrdinalIgnoreCase);

        public static string Expand(string script, string drawingPath)
        {
            return script
                .Replace("<acet:cFolderName>", Path.GetDirectoryName(drawingPath) ?? "", StringComparison.OrdinalIgnoreCase)
                .Replace("<acet:cBaseName>", Path.GetFileNameWithoutExtension(drawingPath), StringComparison.OrdinalIgnoreCase)
                .Replace("<acet:cExtension>", Path.GetExtension(drawingPath), StringComparison.OrdinalIgnoreCase)
                .Replace("<acet:cFileName>", Path.GetFileName(drawingPath), StringComparison.OrdinalIgnoreCase)
                .Replace("<acet:cFullFileName>", drawingPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Reads legacy ScriptPro 1.x project files (.scp).
    /// Sections: [General_Start] key=value lines [General_End], [DWGList_Start] one drawing per line [DWGList_End].
    /// </summary>
    public static class ScpProject
    {
        public sealed record Result(string? ScriptFile, int? TimeoutSeconds, IReadOnlyList<string> Drawings);

        public static Result Parse(IEnumerable<string> lines)
        {
            string? script = null;
            int? timeout = null;
            var drawings = new List<string>();
            string section = "";

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    section = line.ToUpperInvariant() switch
                    {
                        "[GENERAL_START]" => "general",
                        "[DWGLIST_START]" or "[FILELIST]" => "dwg",
                        _ => ""
                    };
                    continue;
                }

                if (section == "general")
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    var key = line[..eq].Trim();
                    var value = line[(eq + 1)..].Trim().Trim('"');

                    if (key.Contains("script", StringComparison.OrdinalIgnoreCase)
                        && !key.Contains("startup", StringComparison.OrdinalIgnoreCase)
                        && value.EndsWith(".scr", StringComparison.OrdinalIgnoreCase))
                        script ??= value;
                    else if (key.Contains("timeout", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var t) && t > 0)
                        timeout = t;
                }
                else if (section == "dwg")
                {
                    var path = ExtractDrawingPath(line);
                    if (path != null)
                        drawings.Add(path);
                }
            }

            return new Result(script, timeout, drawings);
        }

        /// <summary>
        /// Takes the drawing path from a list line, ignoring optional status/flags around it (e.g. "C:\a.dwg,1").
        /// </summary>
        public static string? ExtractDrawingPath(string line)
        {
            foreach (var ext in new[] { ".dwg", ".dxf" })
            {
                int i = line.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                if (i > 0)
                {
                    var path = line[..(i + ext.Length)].Trim().Trim('"');
                    int eq = path.IndexOf('=');
                    if (eq >= 0)
                        path = path[(eq + 1)..].Trim().Trim('"');
                    return path;
                }
            }
            return null;
        }
    }
}
