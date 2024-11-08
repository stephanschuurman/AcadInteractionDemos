using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcadInteractionTest.Api
{
    internal class AcShellEx
    {
        /// <summary>
        /// Open DWG using AutoCAD Drawing Launcher (2016+)
        /// </summary>
        /// <param name="path">The path to the DWG.</param>
        public static void OpenWithAcLauncher(string path)
        {
            var executable = @"C:\Program Files\Common Files\Autodesk Shared\AcShellEx\AcLauncher.exe";
            var extension = Path.GetExtension(path).ToLower();
            var process = new Process();

            if (!File.Exists(path))
                throw new FileNotFoundException($"AutoCAD Drawing Launcher not found at: '{executable}'.");
            else if (extension == null || extension != ".dwg")
                throw new Exception($"Wrong extension, should be .dwg and not: '{extension}'.");
            else
            {
                process.StartInfo.FileName = executable;
                // Open Read only (/r).
                process.StartInfo.Arguments = $"/O \"{path}\"";
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
        }
    }
}
