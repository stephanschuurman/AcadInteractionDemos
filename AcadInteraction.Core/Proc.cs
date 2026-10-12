using System.Diagnostics;

namespace AcadInteraction.Core
{
    public class Proc
    {
        /// <summary>
        /// Returns the running acad.exe processes as PID -> process name.
        /// </summary>
        public static Dictionary<int, string> GetProcess(bool debug = false)
        {
            var processDict = new Dictionary<int, string>();

            foreach (var process in Process.GetProcessesByName("acad"))
            {
                using (process)
                    processDict.Add(process.Id, process.ProcessName);
            }

            if (debug)
                foreach (var proc in processDict)
                    Debug.WriteLine($"PID: {proc.Key}, Name: {proc.Value}");

            return processDict;
        }
    }
}
