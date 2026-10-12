using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace AcadInteraction.WinForms
{
    /// <summary>
    /// Generates a .mcp.json for Visual Studio that registers AcadInteraction.McpServer.
    /// </summary>
    public class FormMcpConfig : Form
    {
        private readonly TextBox textBoxExe = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly TextBox textBoxJson = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9f),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        public FormMcpConfig()
        {
            Text = "Generate .mcp.json for Visual Studio";
            ClientSize = new Size(760, 530);
            StartPosition = FormStartPosition.CenterParent;

            var label = new Label { Text = "AcadInteraction.McpServer.exe:", AutoSize = true, Location = new Point(10, 15) };
            textBoxExe.SetBounds(170, 12, 480, 30);
            textBoxExe.TextChanged += (_, _) => UpdateJson();

            var buttonBrowse = new Button { Text = "Browse...", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            buttonBrowse.SetBounds(660, 10, 90, 34);
            buttonBrowse.Click += (_, _) => Browse();

            textBoxJson.SetBounds(10, 55, 740, 360);

            var buttonCopy = new Button { Text = "Copy", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            buttonCopy.SetBounds(240, 430, 80, 38);
            buttonCopy.Click += (_, _) => Clipboard.SetText(textBoxJson.Text);

            var labelVs = new Label { Text = "Visual Studio:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new Point(330, 437) };

            var buttonSaveSolution = new Button { Text = "Save to solution", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            buttonSaveSolution.SetBounds(470, 430, 140, 38);
            buttonSaveSolution.Click += (_, _) => Save(FindSolutionDir(), ".mcp.json");

            var buttonSaveUser = new Button { Text = "Save to user", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            buttonSaveUser.SetBounds(620, 430, 130, 38);
            buttonSaveUser.Click += (_, _) => Save(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcp.json");

            var labelVsCode = new Label { Text = "VS Code:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new Point(330, 485) };

            var buttonSaveVsCodeWorkspace = new Button { Text = "Save to .vscode", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            buttonSaveVsCodeWorkspace.SetBounds(470, 478, 140, 38);
            buttonSaveVsCodeWorkspace.Click += (_, _) =>
            {
                string? solutionDir = FindSolutionDir();
                Save(solutionDir == null ? null : Path.Combine(solutionDir, ".vscode"), "mcp.json");
            };

            var buttonSaveVsCodeUser = new Button { Text = "Copilot home", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            buttonSaveVsCodeUser.SetBounds(620, 478, 130, 38);
            buttonSaveVsCodeUser.Click += (_, _) => SaveToCopilotHome();

            Controls.AddRange([label, textBoxExe, buttonBrowse, textBoxJson, buttonCopy,
                labelVs, buttonSaveSolution, buttonSaveUser,
                labelVsCode, buttonSaveVsCodeWorkspace, buttonSaveVsCodeUser]);

            textBoxExe.Text = FindServerExe() ?? string.Empty;
            UpdateJson();
        }

        private void UpdateJson()
        {
            var config = new { servers = new { acad = new { type = "stdio", command = textBoxExe.Text } } };
            textBoxJson.Text = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        }

        private void Browse()
        {
            using var dialog = new OpenFileDialog { Filter = "AcadInteraction.McpServer.exe|AcadInteraction.McpServer.exe|Executables (*.exe)|*.exe" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                textBoxExe.Text = dialog.FileName;
        }

        private bool ConfirmExePath() =>
            File.Exists(textBoxExe.Text) ||
            MessageBox.Show(this, "AcadInteraction.McpServer.exe not found at this path. Save anyway?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        private void Save(string? initialDir, string fileName)
        {
            if (!ConfirmExePath())
                return;

            if (!string.IsNullOrEmpty(initialDir))
                Directory.CreateDirectory(initialDir);

            using var dialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "MCP config (*.json)|*.json",
                InitialDirectory = initialDir ?? string.Empty,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllText(dialog.FileName, textBoxJson.Text);
                MessageBox.Show(this, $"Saved: {dialog.FileName}\n\nRestart Visual Studio / VS Code or reopen the solution/folder to load the server.", Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Adds or updates the 'acad' entry in ~/.copilot/mcp-config.json, keeping other servers intact.
        /// </summary>
        private void SaveToCopilotHome()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "mcp-config.json");

            if (!ConfirmExePath())
                return;

            try
            {
                JsonObject root = File.Exists(path)
                    ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject()
                    : new JsonObject();

                if (root["mcpServers"] is not JsonObject servers)
                {
                    servers = new JsonObject();
                    root["mcpServers"] = servers;
                }

                servers["acad"] = new JsonObject
                {
                    ["type"] = "local",
                    ["command"] = textBoxExe.Text,
                    ["args"] = new JsonArray(),
                    ["tools"] = new JsonArray("*")
                };

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show(this, $"Saved: {path}\n\nRestart VS Code to load the server.", Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? FindSolutionDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.GetFiles("*.sln*").Length == 0)
                dir = dir.Parent;
            return dir?.FullName;
        }

        private static string? FindServerExe()
        {
            string? solutionDir = FindSolutionDir();
            string binDir = solutionDir == null ? string.Empty : Path.Combine(solutionDir, "AcadInteraction.McpServer", "bin");
            if (!Directory.Exists(binDir))
                return null;

            return Directory.EnumerateFiles(binDir, "AcadInteraction.McpServer.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
    }
}
