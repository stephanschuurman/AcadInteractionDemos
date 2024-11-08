using Microsoft.Win32;
using NDde.Client;
using System.Diagnostics;

namespace AcadInteractionTest.Api
{
    internal class Dde
    {
        public void Test(string[] list)
        {
            var service = GetDDS(".dwg"); // AutoCAD.R25.DDE

            if (service == null || service.GetType() != typeof(string))
                return;

            using (DdeClient client = new DdeClient((string)service, "System")) 
            {
                try
                {
                    // Connect to DDE-server
                    client.Connect();

                    foreach (string line in list)
                    {
                        Debug.WriteLine($"Exec: {line}");
                        client.Execute(line, 1000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error when executing: " + ex.Message);
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        static object? GetDDS(string fileExtension = ".dwg")
        {
            // Vervang dit door de gewenste bestandsextensie
            string openWithProgidsPath = $@"Software\Classes\{fileExtension}\OpenWithProgids";

            try
            {
                using (var progidsKey = Registry.CurrentUser.OpenSubKey(openWithProgidsPath))
                {
                    if (progidsKey != null)
                    {
                        foreach (string subKeyName in progidsKey.GetValueNames())
                        {
                            string ddeExecPath = $@"Software\Classes\{subKeyName}\shell\open\ddeexec\application";

                            try
                            {
                                using (RegistryKey ddeExecKey = Registry.CurrentUser.OpenSubKey(ddeExecPath))
                                {
                                    if (ddeExecKey != null)
                                    {
                                        object value = ddeExecKey.GetValue(null);
                                        if (value != null)
                                        {
                                            Debug.WriteLine($"DDEExec Application for {fileExtension}: {value}");
                                            return value;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error accessing DDEExec key: {ex.Message}");
                            }
                        }
                        Debug.WriteLine($"No DDEExec Application found for {fileExtension}");
                    }
                    else
                    {
                        Debug.WriteLine($"Registry path not found: {openWithProgidsPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing registry: {ex.Message}");
            }
            return null;
        }
    }
}
