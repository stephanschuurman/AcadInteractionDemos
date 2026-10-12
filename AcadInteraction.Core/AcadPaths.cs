using Microsoft.Win32;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Locates AutoCAD executables. Order: environment variable, registry (current AutoCAD version), default path.
    /// </summary>
    public static class AcadPaths
    {
        public const string AccoreConsoleEnvironmentVariable = "ACAD_ACCORECONSOLE";
        public const string DefaultAccoreConsolePath = @"C:\Program Files\Autodesk\AutoCAD 2025\accoreconsole.exe";
        public const string AcLauncherPath = @"C:\Program Files\Common Files\Autodesk Shared\AcShellEx\AcLauncher.exe";

        public static string FindAccoreConsole()
        {
            var fromEnv = Environment.GetEnvironmentVariable(AccoreConsoleEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            var location = FindAcadLocation();
            if (location != null)
            {
                var path = Path.Combine(location, "accoreconsole.exe");
                if (File.Exists(path))
                    return path;
            }

            return DefaultAccoreConsolePath;
        }

        /// <summary>
        /// Reads AcadLocation for the current AutoCAD version from HKLM\SOFTWARE\Autodesk\AutoCAD.
        /// </summary>
        public static string? FindAcadLocation()
        {
            try
            {
                using var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Autodesk\AutoCAD");
                if (root?.GetValue("CurVer") is not string curVer)
                    return null;

                using var versionKey = root.OpenSubKey(curVer);
                if (versionKey?.GetValue("CurVer") is not string curProduct)
                    return null;

                using var productKey = versionKey.OpenSubKey(curProduct);
                return productKey?.GetValue("AcadLocation") as string;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return null;
            }
        }
    }
}
