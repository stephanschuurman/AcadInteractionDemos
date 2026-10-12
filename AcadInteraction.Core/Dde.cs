using System.Diagnostics;
using Microsoft.Win32;
using NDde;
using NDde.Client;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Sends commands via DDE to the AutoCAD instance registered as the handler for .dwg files.
    /// </summary>
    public class Dde
    {
        /// <summary>
        /// Executes the commands. Throws when no DDE service is registered or a command fails.
        /// </summary>
        public void Execute(IEnumerable<string> commands)
        {
            var services = GetDdeServices(".dwg"); // e.g. AutoCAD.R26.DDE, AutoCAD.R25.DDE
            if (services.Count == 0)
                throw new InvalidOperationException("No AutoCAD DDE service registered for .dwg files.");

            using var client = Connect(services);
            try
            {
                foreach (string line in commands)
                {
                    Debug.WriteLine($"Exec: {line}");
                    client.Execute(line, 1000);
                }
            }
            finally
            {
                if (client.IsConnected)
                    client.Disconnect();
            }
        }

        /// <summary>
        /// Connects to the first DDE service that accepts a connection. Throws with all tried services when none does.
        /// </summary>
        private static DdeClient Connect(IReadOnlyList<string> services)
        {
            var errors = new List<string>();
            foreach (var service in services)
            {
                var client = new DdeClient(service, "System");
                try
                {
                    client.Connect();
                    Debug.WriteLine($"Connected to DDE service {service}");
                    return client;
                }
                catch (DdeException ex)
                {
                    client.Dispose();
                    errors.Add($"{service}: {ex.Message.Split('\n')[0].Trim()}");
                }
            }
            throw new InvalidOperationException(
                "Could not connect to AutoCAD via DDE. Is AutoCAD running?" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        public static string? GetDdeService(string fileExtension = ".dwg") => GetDdeServices(fileExtension).FirstOrDefault();

        /// <summary>
        /// Returns all registered AutoCAD DDE services for the extension (HKCU and HKLM),
        /// with the release of a running acad.exe first (e.g. AutoCAD 2027 = R26).
        /// </summary>
        public static IReadOnlyList<string> GetDdeServices(string fileExtension = ".dwg")
        {
            var services = new List<string>();
            foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                using var progidsKey = root.OpenSubKey($@"Software\Classes\{fileExtension}\OpenWithProgids");
                if (progidsKey == null)
                    continue;

                foreach (string progId in progidsKey.GetValueNames())
                {
                    using var ddeExecKey = root.OpenSubKey($@"Software\Classes\{progId}\shell\open\ddeexec\application");
                    if (ddeExecKey?.GetValue(null) is string value && value.Length > 0
                        && !services.Contains(value, StringComparer.OrdinalIgnoreCase))
                        services.Add(value);
                }
            }

            var running = GetRunningReleases();
            Debug.WriteLine($"DDE services: {string.Join(", ", services)}; running releases: {string.Join(", ", running)}");
            return OrderServices(services, running);
        }

        /// <summary>
        /// Services of running releases first, then newest release first.
        /// </summary>
        public static IReadOnlyList<string> OrderServices(IEnumerable<string> services, IReadOnlyCollection<string> runningReleases) =>
            services
                .OrderBy(s => runningReleases.Any(r => s.Contains($".{r}.", StringComparison.OrdinalIgnoreCase)) ? 0 : 1)
                .ThenByDescending(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// Major releases of running acad.exe processes, e.g. "R26" from file version "R26.0.118.0.0".
        /// </summary>
        private static List<string> GetRunningReleases()
        {
            var releases = new List<string>();
            foreach (var process in Process.GetProcessesByName("acad"))
            {
                using (process)
                {
                    try
                    {
                        var version = process.MainModule?.FileVersionInfo.FileVersion;
                        if (version?.Split('.')[0] is string major && major.StartsWith('R'))
                            releases.Add(major);
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
                }
            }
            return releases;
        }
    }
}
