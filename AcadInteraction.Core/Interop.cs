using System.Diagnostics;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;

namespace AcadInteraction.Core
{
    /// <summary>
    /// COM interop helpers to attach to (or start) AutoCAD.
    /// </summary>
    public class Interop
    {
        #region "DLL Imports"

        [DllImport("oleaut32.dll")]
        private static extern int GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern int CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid clsid);

        #endregion

        public static Guid GetCLSIDFromProgID(string progID)
        {
            int result = CLSIDFromProgIDEx(progID, out Guid clsid);

            if (result != 0) // S_OK is 0
                throw new COMException("Failed to get CLSID from ProgID", result);

            return clsid;
        }

        /// <summary>
        /// Returns the CLSID for a ProgID (e.g. "AutoCAD.Application"), or <see cref="Guid.Empty"/>.
        /// </summary>
        private static Guid GetCLSID(string progID)
        {
            try
            {
                Guid clsid = GetCLSIDFromProgID(progID);
                Debug.WriteLine($"CLSID for {progID} is {clsid}");
                return clsid;
            }
            catch (COMException ex)
            {
                Debug.WriteLine($"Error: {ex.Message} (HRESULT: {ex.ErrorCode})");
                return Guid.Empty;
            }
        }

        private static object? GetActiveObject(string progID)
        {
            var clsid = GetCLSID(progID);
            if (clsid == Guid.Empty)
                return null;

            int hr = GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            return hr == 0 ? obj : null;
        }

        /// <summary>
        /// Starts a new AutoCAD instance and draws a line.
        /// </summary>
        public static void InteropTest()
        {
            AcadApplication acadApp = new AcadApplication { Visible = true };
            AcadDocument acadDoc = acadApp.ActiveDocument;

            AcadLine line = acadDoc.ModelSpace.AddLine(new double[] { 0, 0, 0 }, new double[] { 10, 10, 0 });
            acadDoc.Application.ZoomExtents();

            ComRetry.Release(line);
            Debug.WriteLine("Line created successfully!");
        }

        /// <summary>
        /// Returns the running AutoCAD application as a late-bound (dynamic) COM object, or null.
        /// </summary>
        public static dynamic? GetAcadApp() => GetActiveObject("AutoCAD.Application");

        /// <summary>
        /// Makes the running AutoCAD window visible and zooms to extents.
        /// </summary>
        public static void OpenWindow()
        {
            dynamic? acadApp = GetAcadApp();
            if (acadApp == null)
            {
                Debug.WriteLine("AutoCAD is not running.");
                return;
            }

            try
            {
                acadApp.Visible = true;
                if (acadApp.Documents.Count > 0)
                    acadApp.ZoomExtents();
                else
                    Debug.WriteLine("No open drawing; skipping ZoomExtents.");
                Debug.WriteLine($"AutoCAD version: {acadApp.Version}");
            }
            finally
            {
                ComRetry.Release((object)acadApp);
            }
        }

        /// <summary>
        /// Returns the running AutoCAD application (early-bound), or null.
        /// </summary>
        /// <param name="version">Major version of AutoCAD, e.g. 25 for 2025. Use 0 for any version.</param>
        public static AcadApplication? GetActiveAcadApplication(int version = 25)
        {
            var progID = version != 0 ? $"AutoCAD.Application.{version}" : "AutoCAD.Application";
            return GetActiveObject(progID) as AcadApplication;
        }
    }
}
