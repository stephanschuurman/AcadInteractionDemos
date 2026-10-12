using Microsoft.Win32;

namespace AcadInteraction.Core
{
    /// <summary>
    /// An installed Autodesk application as registered in the Windows uninstall registry.
    /// </summary>
    public sealed record InstalledProduct(string Name, string? Version, string? Publisher, string? InstallLocation, string? InstallDate);

    /// <summary>
    /// An installed AutoCAD release (or vertical such as AutoCAD Mechanical) from HKLM\SOFTWARE\Autodesk\AutoCAD.
    /// </summary>
    public sealed record AcadRelease(string Release, string ProductKey, string? ProductName, string? AcadLocation, bool IsCurrent)
    {
        public string? AcadExePath => AcadLocation == null ? null : Path.Combine(AcadLocation, "acad.exe");
        public string? AccoreConsolePath => AcadLocation == null ? null : Path.Combine(AcadLocation, "accoreconsole.exe");
    }

    /// <summary>
    /// Reads the Windows registry to find installed Autodesk / AutoCAD applications.
    /// </summary>
    public static class AutodeskProducts
    {
        private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string AcadPath = @"SOFTWARE\Autodesk\AutoCAD";

        /// <summary>
        /// Returns all installed applications published by Autodesk (64-bit, 32-bit and per-user installs).
        /// </summary>
        public static IReadOnlyList<InstalledProduct> GetInstalledProducts()
        {
            var result = new Dictionary<string, InstalledProduct>(StringComparer.OrdinalIgnoreCase);

            foreach (var (hive, view) in new[]
            {
                (RegistryHive.LocalMachine, RegistryView.Registry64),
                (RegistryHive.LocalMachine, RegistryView.Registry32),
                (RegistryHive.CurrentUser, RegistryView.Default)
            })
            {
                TryRead(() =>
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstall = baseKey.OpenSubKey(UninstallPath);
                    if (uninstall == null)
                        return;

                    foreach (var subKeyName in uninstall.GetSubKeyNames())
                    {
                        using var key = uninstall.OpenSubKey(subKeyName);
                        if (key?.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name))
                            continue;

                        var publisher = key.GetValue("Publisher") as string;
                        if (!IsAutodesk(name, publisher))
                            continue;

                        var version = key.GetValue("DisplayVersion") as string;
                        result.TryAdd($"{name}|{version}", new InstalledProduct(
                            name.Trim(),
                            version,
                            publisher,
                            key.GetValue("InstallLocation") as string,
                            key.GetValue("InstallDate") as string));
                    }
                });
            }

            return result.Values.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Returns all installed AutoCAD releases and verticals, e.g. R25.0\ACAD-8101:409 (AutoCAD 2025).
        /// </summary>
        public static IReadOnlyList<AcadRelease> GetAcadReleases()
        {
            var result = new List<AcadRelease>();

            TryRead(() =>
            {
                using var root = Registry.LocalMachine.OpenSubKey(AcadPath);
                if (root == null)
                    return;

                var curRelease = root.GetValue("CurVer") as string;

                foreach (var release in root.GetSubKeyNames())
                {
                    using var releaseKey = root.OpenSubKey(release);
                    if (releaseKey == null)
                        continue;

                    var curProduct = releaseKey.GetValue("CurVer") as string;

                    foreach (var productKeyName in releaseKey.GetSubKeyNames())
                    {
                        using var productKey = releaseKey.OpenSubKey(productKeyName);
                        var location = productKey?.GetValue("AcadLocation") as string;
                        if (location == null)
                            continue;

                        result.Add(new AcadRelease(
                            release,
                            productKeyName,
                            productKey!.GetValue("ProductName") as string,
                            location,
                            string.Equals(release, curRelease, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(productKeyName, curProduct, StringComparison.OrdinalIgnoreCase)));
                    }
                }
            });

            return result.OrderByDescending(r => r.Release, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// True when the publisher is Autodesk, or the name looks like an Autodesk product when no publisher is set.
        /// </summary>
        public static bool IsAutodesk(string name, string? publisher) =>
            publisher != null
                ? publisher.Contains("Autodesk", StringComparison.OrdinalIgnoreCase)
                : name.Contains("Autodesk", StringComparison.OrdinalIgnoreCase) || name.StartsWith("AutoCAD", StringComparison.OrdinalIgnoreCase);

        private static void TryRead(Action read)
        {
            try
            {
                read();
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // Key not accessible; skip
            }
        }
    }
}
