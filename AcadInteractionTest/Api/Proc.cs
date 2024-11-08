using Autodesk.AutoCAD.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AcadInteractionTest.Api
{
    internal class Proc
    {
        public static Dictionary<int, string> GetProcess(bool debug = false)
        {
            // Get all running acad.exe processes
            var processes = Process.GetProcessesByName("acad");
            var processDict = new Dictionary<int, string>();

            foreach (var process in processes)
            {
                processDict.Add(process.Id, process.ProcessName);
            }

            // Print the dictionary when debug is true
            if (debug)
                foreach (var proc in processDict)
                {
                    Debug.WriteLine($"Name: {proc.Key}, PID: {proc.Value}");
                }

            return processDict;
        }
    }
}