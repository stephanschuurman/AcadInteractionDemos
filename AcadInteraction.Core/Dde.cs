using System.Diagnostics;
using Microsoft.Win32;
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
            var service = GetDdeService(".dwg") // e.g. AutoCAD.R25.DDE
                ?? throw new InvalidOperationException("No AutoCAD DDE service registered for .dwg files.");

            using var client = new DdeClient(service, "System");
            client.Connect();
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

        public static string? GetDdeService(string fileExtension = ".dwg")
        {
            string openWithProgidsPath = $@"Software\Classes\{fileExtension}\OpenWithProgids";

            using var progidsKey = Registry.CurrentUser.OpenSubKey(openWithProgidsPath);
            if (progidsKey == null)
            {
                Debug.WriteLine($"Registry path not found: {openWithProgidsPath}");
                return null;
            }

            foreach (string progId in progidsKey.GetValueNames())
            {
                using var ddeExecKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\ddeexec\application");
                if (ddeExecKey?.GetValue(null) is string value && value.Length > 0)
                {
                    Debug.WriteLine($"DDEExec Application for {fileExtension}: {value}");
                    return value;
                }
            }

            Debug.WriteLine($"No DDEExec Application found for {fileExtension}");
            return null;
        }
    }
}
