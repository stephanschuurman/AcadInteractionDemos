using System.Diagnostics;

namespace AcadInteraction.Core
{
    public class AcShellEx
    {
        /// <summary>
        /// Open DWG using AutoCAD Drawing Launcher (2016+), which selects the correct AutoCAD version.
        /// </summary>
        /// <param name="path">The path to the DWG.</param>
        public static void OpenWithAcLauncher(string path)
        {
            var executable = AcadPaths.AcLauncherPath;

            if (!File.Exists(executable))
                throw new FileNotFoundException($"AutoCAD Drawing Launcher not found at: '{executable}'.", executable);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Drawing not found: '{path}'.", path);

            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".dwg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Wrong extension, should be .dwg and not: '{extension}'.", nameof(path));

            using var process = new Process
            {
                StartInfo =
                {
                    FileName = executable,
                    Arguments = $"/O \"{path}\"",
                    ErrorDialog = false,
                    CreateNoWindow = false,
                    UseShellExecute = true
                }
            };
            process.Start();
        }
    }
}
