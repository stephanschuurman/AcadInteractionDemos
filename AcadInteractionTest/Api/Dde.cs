using Microsoft.Win32;
using NDde.Client;
using System.Diagnostics;

namespace AcadInteractionTest.Api
{
    internal class Dde
    {
        public void Test(string[] list)
        {
            var service = GetDDS(".dwg");

            if (service == null || service.GetType() != typeof(string))
                return;

            using (DdeClient client = new DdeClient((string)service, "System")) // AutoCAD.R25.DDE // AutoCAD.Application
            {
                try
                {
                    // Verbind met de DDE-server
                    client.Connect();

                    foreach (string line in list)
                    {
                        Debug.WriteLine($"Exec: {line}");
                        client.Execute(line, 1000);
                    }



                    // + Environment.NewLine

                    ////[open("%1")]
                    //var path = "T:\\INNO\\2-Projects General\\770002 - Project Zero\\8-Engineering\\WP5 Electrical & Instrumentation\\1. Documents\\HG-87000\\HG-87000-001-E-S-001\\HG-87000-001-E-S-001 Rev A1.dwg";
                    //client.Execute($"[open(\"{path}\")]");

                    //// "C:\Program Files\Common Files\Autodesk Shared\AcShellEx\AcLauncher.exe" /O "%1"
                    //// "C:\Program Files\Common Files\Autodesk Shared\AcShellEx\AcLauncher.exe" /O "T:\INNO\2-Projects General\770002 - Project Zero\8-Engineering\WP5 Electrical & Instrumentation\1. Documents\HG-87000\HG-87000-001-E-S-001\HG-87000-001-E-S-001 Rev A1.dwg"
                    ////// Stuur een commando naar AutoCAD
                    ////string command = "(command \"_.LINE\" \"0,0\" \"10000,10000\" \"\")";
                    ////byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                    ////client.Poke("Execute", commandBytes.ToString(), commandBytes.Length);


                    //



                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fout bij het verzenden van het commando: " + ex.Message);
                }
                finally
                {
                    // Verbreek de verbinding
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
