using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Reads version information from executables and DLLs, and details of acad.exe installations and processes.
    /// </summary>
    public static class FileDetails
    {
        /// <summary>
        /// File and assembly version details for an .exe or .dll.
        /// </summary>
        public static string GetVersionInfo(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: '{path}'.", path);

            var info = FileVersionInfo.GetVersionInfo(path);
            var file = new FileInfo(path);
            var sb = new StringBuilder();

            sb.AppendLine($"Path: {file.FullName}");
            sb.AppendLine($"Size: {file.Length:N0} bytes");
            sb.AppendLine($"Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            Append(sb, "File version", info.FileVersion);
            Append(sb, "Product version", info.ProductVersion);
            Append(sb, "Product name", info.ProductName);
            Append(sb, "Description", info.FileDescription);
            Append(sb, "Company", info.CompanyName);
            Append(sb, "Copyright", info.LegalCopyright);
            Append(sb, "Original filename", info.OriginalFilename);
            Append(sb, "Language", info.Language);
            if (info.IsDebug) sb.AppendLine("Debug build: yes");

            sb.AppendLine($"Managed assembly: {GetAssemblyDescription(path) ?? "no (native)"}");

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Returns "Name, Version=..., target framework" for a .NET assembly, or null for native files.
        /// </summary>
        public static string? GetAssemblyDescription(string path)
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                return $"{name.Name}, Version={name.Version}{(name.ProcessorArchitecture != ProcessorArchitecture.None ? $", {name.ProcessorArchitecture}" : "")}";
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Details for acad.exe: running processes (with path, version, start time, memory, main window title)
        /// or, when none run, the installed releases from the registry.
        /// </summary>
        public static string GetAcadExeDetails(int? pid = null)
        {
            var sb = new StringBuilder();
            var processes = Process.GetProcessesByName("acad")
                .Where(p => pid == null || p.Id == pid)
                .ToList();

            if (pid != null && processes.Count == 0)
                throw new ArgumentException($"No acad.exe process with PID {pid}.");

            foreach (var process in processes)
            {
                using (process)
                {
                    sb.AppendLine($"== acad.exe PID {process.Id} ==");
                    try
                    {
                        sb.AppendLine($"Started: {process.StartTime:yyyy-MM-dd HH:mm:ss}");
                        sb.AppendLine($"Memory (working set): {process.WorkingSet64 / (1024 * 1024):N0} MB");
                        Append(sb, "Main window", process.MainWindowTitle);
                        Append(sb, "Responding", process.Responding ? "yes" : "no");
                        var path = process.MainModule?.FileName;
                        if (path != null)
                            sb.AppendLine(GetVersionInfo(path));
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                    {
                        sb.AppendLine($"Details not available: {ex.Message}");
                    }
                    sb.AppendLine();
                }
            }

            if (pid == null)
            {
                var releases = AutodeskProducts.GetAcadReleases();
                if (releases.Count == 0 && processes.Count == 0)
                    return "No running acad.exe and no AutoCAD installation found in the registry.";

                foreach (var release in releases)
                {
                    sb.AppendLine($"== Installed {release.ProductName ?? release.ProductKey} ({release.Release}\\{release.ProductKey}){(release.IsCurrent ? " [current]" : "")} ==");
                    if (release.AcadExePath != null && File.Exists(release.AcadExePath))
                        sb.AppendLine(GetVersionInfo(release.AcadExePath));
                    else
                        sb.AppendLine($"acad.exe not found in {release.AcadLocation}");
                    sb.AppendLine();
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Resolves a DLL name (e.g. "accoremgd.dll") to a path: as-is when it exists, otherwise inside
        /// the AutoCAD install folder (current release or the given acad.exe process).
        /// </summary>
        public static string ResolveAcadFile(string fileOrPath, int? pid = null)
        {
            if (File.Exists(fileOrPath))
                return Path.GetFullPath(fileOrPath);

            var folders = new List<string>();
            foreach (var process in Process.GetProcessesByName("acad").Where(p => pid == null || p.Id == pid))
            {
                using (process)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(process.MainModule?.FileName);
                        if (dir != null) folders.Add(dir);
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
                }
            }
            if (AcadPaths.FindAcadLocation() is string location)
                folders.Add(location);
            folders.AddRange(AutodeskProducts.GetAcadReleases().Select(r => r.AcadLocation!).Where(l => l != null));

            foreach (var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(folder, fileOrPath);
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException($"'{fileOrPath}' not found as path or in the AutoCAD install folder(s).", fileOrPath);
        }

        private static void Append(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value.Trim()}");
        }
    }
}
