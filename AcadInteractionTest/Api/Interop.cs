using System;
using System.Diagnostics;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;

namespace AcadInteractionTest.Api
{
    internal class Interop
    {
        static void InteropTest()
        {
            // Create a new instance of AutoCAD
            AcadApplication acadApp = new AcadApplication();
            acadApp.Visible = true;

            // Get the active document
            AcadDocument acadDoc = acadApp.ActiveDocument;

            //// Open a DWG file
            //string dwgPath = @"C:\path\to\your\file.dwg";
            //AcadDocument acadDoc = acadApp.Documents.Open(dwgPath);
            //// Get the block references
            //AcadBlocks blocks = acadDoc.Blocks;
            //foreach (AcadBlock block in blocks)
            //{
            //    Console.WriteLine("Block Name: " + block.Name);
            //}

            //// Close the document
            //acadDoc.Close(false);

            // Define the start and end points of the line
            double[] startPoint = new double[] { 0, 0, 0 };
            double[] endPoint = new double[] { 10, 10, 0 };

            // Add the line to the model space
            AcadLine line = acadDoc.ModelSpace.AddLine(startPoint, endPoint);

            // Zoom to the extents of the drawing
            acadDoc.Application.ZoomExtents();

            Debug.WriteLine("Line created successfully!");
        }
    }
}


//// Connect to the first AutoCAD process found
//object acadApp = null;
//foreach (var process in processes)
//{
//    try
//    {
//        acadApp = Marshal.GetActiveObject("AutoCAD.Application." + process.Id);
//        break;
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Failed to connect to AutoCAD process {process.Id}: {ex.Message}");
//    }
//}

//if (acadApp == null)
//{
//    Console.WriteLine("Could not connect to any AutoCAD process.");
//    return;
//}

//// Check if no dialog is active
//AcadState acadState = acadApp.GetAcadState();
//if (acadState.IsQuiescent)
//{
//    // Open a drawing
//    string drawingPath = @"C:\path\to\your\drawing.dwg";
//    try
//    {
//        acadApp.Documents.Open(drawingPath);
//        Console.WriteLine($"Opened drawing: {drawingPath}");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Failed to open drawing: {ex.Message}");
//    }
//}
//else
//{
//    Console.WriteLine("AutoCAD is busy or a dialog is active.");
//}




//using System;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using Autodesk.AutoCAD.Interop;
//using Autodesk.AutoCAD.ApplicationServices;

//namespace AcadInteractionTest.Api
//{
//    internal class Proc
//    {
//        public static void GetProcess()
//        {
//            // Get all running acad.exe processes
//            var processes = Process.GetProcessesByName("acad");

//            if (processes.Length == 0)
//            {
//                Debug.WriteLine("No AutoCAD processes found.");
//                return;
//            }

//            // Connect to the first AutoCAD process found
//            AcadApplication acadApp = null;
//            foreach (var process in processes)
//            {
//                try
//                {
//                    acadApp = (AcadApplication)Marshal.GetActiveObject("AutoCAD.Application." + process.Id);
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Failed to connect to AutoCAD process {process.Id}: {ex.Message}");
//                }
//            }

//            if (acadApp == null)
//            {
//                Console.WriteLine("Could not connect to any AutoCAD process.");
//                return;
//            }

//            // Check if no dialog is active
//            AcadState acadState = acadApp.GetAcadState();
//            if (acadState.IsQuiescent)
//            {
//                // Open a drawing
//                string drawingPath = @"C:\path\to\your\drawing.dwg";
//                try
//                {
//                    acadApp.Documents.Open(drawingPath);
//                    Console.WriteLine($"Opened drawing: {drawingPath}");
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Failed to open drawing: {ex.Message}");
//                }
//            }
//            else
//            {
//                Console.WriteLine("AutoCAD is busy or a dialog is active.");
//            }
//        }
//    }
//}
