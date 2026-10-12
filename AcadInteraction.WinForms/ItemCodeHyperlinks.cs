using System.Diagnostics;
using AcadInteraction.Core;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;

namespace AcadInteraction.WinForms
{
    /// <summary>
    /// Replaces item code attributes with hyperlink fields.
    /// </summary>
    internal static class ItemCodeHyperlinks
    {
        private static bool IsItemCode(string value, IReadOnlySet<string>? itemCodes) =>
            !string.IsNullOrWhiteSpace(value)
            && !value.StartsWith("%<", StringComparison.Ordinal)
            && (itemCodes == null || itemCodes.Contains(value));

        private static string CreateField(string baseUrl, string itemCode) =>
            @"%<\AcVar \href """ + $"{baseUrl}{itemCode}## #0" + @""">%";

        /// <summary>
        /// Updates all MText attributes <paramref name="attName"/> of block <paramref name="blockName"/> in model space.
        /// </summary>
        /// <param name="baseUrl">URL prefix; the item code is appended to it.</param>
        /// <param name="itemCodes">Item codes to convert; all item codes when null.</param>
        public static void UpdateItemCode(string blockName, string attName, string baseUrl, IReadOnlySet<string>? itemCodes = null)
        {
            AcadApplication? acadApp = Interop.GetActiveAcadApplication();
            if (acadApp == null)
                return;

            AcadDocument acadDoc = acadApp.ActiveDocument;

            foreach (AcadEntity entity in acadDoc.Database.ModelSpace)
            {
                if (entity is not AcadBlockReference blockRef
                    || !blockRef.EffectiveName.Equals(blockName, StringComparison.OrdinalIgnoreCase)
                    || !blockRef.HasAttributes)
                    continue;

                foreach (AcadAttributeReference attRef in (object[])blockRef.GetAttributes())
                {
                    if (!attRef.TagString.Equals(attName, StringComparison.OrdinalIgnoreCase) || !attRef.MTextAttribute)
                        continue;

                    string itemCode = attRef.TextString.Replace("\\P", "").Replace("\r", "").Replace("\n", "").Trim();
                    Debug.WriteLine($"Found: {blockName}, {attName} = {attRef.TextString}");

                    if (IsItemCode(itemCode, itemCodes))
                    {
                        attRef.TextString = CreateField(baseUrl, itemCode);
                        attRef.Update();
                        Debug.WriteLine($"Updating: {attRef.TextString}");
                    }
                }
            }
        }

        /// <summary>
        /// Lets the user pick a block reference and prints its attributes, converting known item codes.
        /// </summary>
        /// <param name="baseUrl">URL prefix; the item code is appended to it.</param>
        /// <param name="itemCodes">Item codes to convert; all item codes when null.</param>
        public static void ReadAttribute(string baseUrl, IReadOnlySet<string>? itemCodes = null)
        {
            AcadApplication? acadApp = Interop.GetActiveAcadApplication();
            if (acadApp == null)
                return;

            AcadDocument acadDoc = acadApp.ActiveDocument;

            const string promptMessage = "Select a block reference: ";
            acadDoc.Utility.GetEntity(out object selectedObject, out _, promptMessage);

            if (selectedObject is not AcadBlockReference blockRef)
            {
                acadDoc.Utility.Prompt("\nSelected entity is not a block reference.");
                return;
            }

            if (!blockRef.HasAttributes)
            {
                acadDoc.Utility.Prompt("\nThe selected block has no attributes.");
                return;
            }

            foreach (AcadAttributeReference attRef in (object[])blockRef.GetAttributes())
            {
                string value = attRef.TextString;
                if (attRef.MTextAttribute && IsItemCode(value, itemCodes))
                    attRef.TextString = CreateField(baseUrl, value);

                acadDoc.Utility.Prompt($"\nAttribute Tag: {attRef.TagString}, Value: {value}");
            }
        }
    }
}
