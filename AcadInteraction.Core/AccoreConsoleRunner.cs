using System.Diagnostics;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Runs AutoCAD scripts headless with accoreconsole.exe.
    /// </summary>
    public class AccoreConsoleRunner
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private readonly string accoreConsolePath;
        private readonly string drawingFilePath;

        public AccoreConsoleRunner(string? accoreConsolePath = null, string drawingFilePath = "")
        {
            this.accoreConsolePath = string.IsNullOrWhiteSpace(accoreConsolePath)
                ? AcadPaths.FindAccoreConsole()
                : accoreConsolePath;
            this.drawingFilePath = drawingFilePath ?? "";
        }

        public string CreateTempScriptFile(string scriptContent)
        {
            string tempFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".scr");
            File.WriteAllText(tempFileName, scriptContent);
            Debug.WriteLine($"Temporary script file created at: {tempFileName}");
            return tempFileName;
        }

        /// <summary>
        /// Builds the accoreconsole.exe command-line arguments.
        /// The drawing is only passed when it is specified and exists.
        /// </summary>
        public static string BuildArguments(string scriptFilePath, string? drawingFilePath)
        {
            return string.IsNullOrWhiteSpace(drawingFilePath) || !File.Exists(drawingFilePath)
                ? $"/s \"{scriptFilePath}\""
                : $"/i \"{drawingFilePath}\" /s \"{scriptFilePath}\"";
        }

        public string RunScript(string scriptFilePath, TimeSpan? timeout = null)
        {
            if (!File.Exists(scriptFilePath))
                throw new FileNotFoundException("Script file not found.", scriptFilePath);
            if (!File.Exists(accoreConsolePath))
                throw new FileNotFoundException($"accoreconsole.exe not found at: '{accoreConsolePath}'.", accoreConsolePath);

            string arguments = BuildArguments(scriptFilePath, drawingFilePath);
            Debug.WriteLine($"Running: {arguments}");

            using var process = new Process
            {
                StartInfo =
                {
                    FileName = accoreConsolePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            // Read both streams concurrently to avoid deadlocks on full buffers.
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeout ?? DefaultTimeout))
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                throw new TimeoutException($"accoreconsole.exe did not finish within {(timeout ?? DefaultTimeout).TotalSeconds:0} seconds.");
            }

            string output = outputTask.GetAwaiter().GetResult();
            string error = errorTask.GetAwaiter().GetResult();

            Debug.WriteLine($"Output:\n{output}\nError:\n{error}");

            return string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
        }

        public void DeleteTempFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}

/*
    /i <input file>: Specifies the input drawing file.
    /s <script file>: Specifies the script file to run.
    /l <language>: Specifies the language to use, e.g. /l en-US
    /p <profile>: Specifies the profile to use.
    /isolate <UserIdOrRegKey> <UserDataFolder>: Runs the console in an isolated environment.
    /product <product name>: Specifies the product to use.
    /setenv <variable>=<value>: Sets an environment variable.

    For example:
    accoreconsole.exe /i "path_to_your_drawing.dwg" /s "path_to_your_script.scr" /p "path_to_your_profile.arg"
*/
