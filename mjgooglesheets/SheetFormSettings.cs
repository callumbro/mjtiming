using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace RaceBeam
{
    internal sealed class SheetFormSettings
    {
        /// <summary>Resolved <c>..\config</c> next to the install (same as other MJ Timing apps).</summary>
        internal static string InstallConfigDirectory =>
            Path.GetFullPath(
                Path.Combine(
                    Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) ?? "",
                    "..",
                    "config"));

        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string SpreadsheetId { get; set; } = "";
        public string ApplicationName { get; set; } = "MJ Timing Live";
        public bool SyncEnabled { get; set; } = true;

        private static string FilePath =>
            Path.Combine(InstallConfigDirectory, "sheet_form_settings.json");

        internal static SheetFormSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var s = JsonSerializer.Deserialize<SheetFormSettings>(json);
                    return s ?? new SheetFormSettings();
                }
            }
            catch
            {
                // ignore corrupt file
            }

            return new SheetFormSettings();
        }

        internal void Save()
        {
            string dir = Path.GetDirectoryName(FilePath) ?? "";
            if (dir != "")
            {
                Directory.CreateDirectory(dir);
            }

            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, opts));
        }
    }
}
