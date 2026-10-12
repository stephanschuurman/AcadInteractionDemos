using AcadInteraction.Core;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AcadInteraction.WinForms
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();

            /// Add item to TODO list
            listBox1.Items.Add("_.LINE 0,0 100,100 " + Environment.NewLine);
            listBox1.Items.Add("_.PL 300,300 1000,1000 100,300" + Environment.NewLine);

            /// Add text to textBoxInput
            textBoxInput.Text = "_.C .CIRCLE 50,50 40" + Environment.NewLine;

            /// MCP config button (created in code to keep the designer untouched)
            var buttonMcpConfig = new Button
            {
                Text = "MCP config",
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            buttonMcpConfig.Location = new Point(ClientSize.Width - 260, ClientSize.Height - 40);
            buttonMcpConfig.Click += (_, _) => new FormMcpConfig().ShowDialog(this);
            Controls.Add(buttonMcpConfig);
            buttonMcpConfig.BringToFront();

            var buttonItemHyperlinks = new Button
            {
                Text = "Item hyperlinks",
                Location = new Point(674, 213),
                Size = new Size(157, 33)
            };
            buttonItemHyperlinks.Click += buttonItemHyperlinks_Click;
            Controls.Add(buttonItemHyperlinks);

            var buttonAutodeskApps = new Button
            {
                Text = "Autodesk apps",
                Location = new Point(674, 253),
                Size = new Size(157, 33)
            };
            buttonAutodeskApps.Click += buttonAutodeskApps_Click;
            Controls.Add(buttonAutodeskApps);

            var buttonScriptPro = new Button
            {
                Text = "ScriptPro",
                Location = new Point(674, 293),
                Size = new Size(157, 33)
            };
            buttonScriptPro.Click += (_, _) => new FormScriptPro().Show(this);
            Controls.Add(buttonScriptPro);

            CreateLogConsole();
            Log("Ready.");

            // Receive tool calls from running MCP servers
            _ = LogPipe.ListenAsync(Log, _logPipeCts.Token);
            FormClosed += (_, _) => _logPipeCts.Cancel();
        }

        private readonly CancellationTokenSource _logPipeCts = new();

        private TextBox textBoxLog = null!;

        /// Read-only log console at the bottom (created in code to keep the designer untouched)
        private void CreateLogConsole()
        {
            const int logHeight = 150;

            // Grow the form without moving or stretching the existing controls
            var anchors = Controls.Cast<Control>().ToDictionary(c => c, c => c.Anchor);
            foreach (var c in anchors.Keys)
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            int top = ClientSize.Height;
            MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height + logHeight);
            ClientSize = new Size(ClientSize.Width, top + logHeight);

            foreach (var (c, anchor) in anchors)
                c.Anchor = anchor;

            textBoxLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Location = new Point(11, top),
                Size = new Size(ClientSize.Width - 22, logHeight - 10),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                TabStop = false
            };
            Controls.Add(textBoxLog);
        }

        private void Log(string message)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(() => Log(message));
                return;
            }
            textBoxLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void LogError(Exception ex)
        {
            Log("ERROR: " + ex.Message);
            MessageBox.Show(ex.Message);
        }

        private string[] recipe()
        {
            if (listBox1.Items == null || listBox1.Items.Count == 0)
                return Array.Empty<string>();
            else
                return listBox1.Items.OfType<string>().ToArray();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add(textBoxInput.Text);
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                listBox1.Items.Remove(listBox1.SelectedItem);

                if (listBox1.Items.Count > 0)
                    listBox1.SelectedIndex = 0;
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void buttonExecDde_Click(object sender, EventArgs e)
        {
            listBoxActions.Items.Add("Executing DDE...");
            Log("Executing DDE...");
            try
            {
                var commands = recipe();
                var dde = new Dde();
                dde.Execute(commands);
                listBoxActions.Items.Add("DDE commands sent.");
                Log($"{commands.Length} DDE command(s) sent.");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private static OpenFileDialog CreateDwgDialog() => new()
        {
            Filter = "DWG files (*.dwg)|*.dwg",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            using var openFileDialog = CreateDwgDialog();

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                Log("Selected file: " + filePath);
                listBox1.Items.Add($"[open(\"{filePath}\")]" + Environment.NewLine);
            }
        }

        private void buttonLaunch_Click(object sender, EventArgs e)
        {
            using var openFileDialog = CreateDwgDialog();

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Log("Opening with AutoCAD Drawing Launcher: " + openFileDialog.FileName);
                    AcShellEx.OpenWithAcLauncher(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }
        }

        private void buttonProc_Click(object sender, EventArgs e)
        {
            var procList = Proc.GetProcess();
            Log($"{procList.Count} acad.exe process(es) found.");
            MessageBox.Show(procList.Count == 0
                ? "No acad.exe processes found."
                : "Found the following PID's:" + Environment.NewLine + string.Join(Environment.NewLine, procList.Select(p => $"{p.Key}: {p.Value}")));
        }

        private void buttonInterop_Click(object sender, EventArgs e)
        {
            //Interop.InteropTest();
            try
            {
                Log("Interop: opening window...");
                Interop.OpenWindow();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private async void buttonAC_Click(object sender, EventArgs e)
        {
            string drawingFilePath = Path.Combine(Path.GetTempPath(), @"test_drawing.dwg");

            // Define the content of the script file
            string scriptContent = "_NEW .\n";
            scriptContent += "._LINE 0,0 100,100 \n";
            scriptContent += "._CIRCLE 50,50 25\n";
            scriptContent += "._RECTANGLE 10,10 60,40\n";
            scriptContent += "._TEXT 20,20 2.5 0 \"Hello, AutoCAD!\"\n";
            scriptContent += "._PLINE 10,10 20,20 30,10 C\n";
            scriptContent += $"_SAVEAS 2018 \"{drawingFilePath}\"\n";
            scriptContent += "._CLOSE\n";

            //https://help.autodesk.com/view/ACD/2024/ENU/?guid=GUID-BE44AE86-7638-48C9-BE5B-C1DF8E4C8808

            buttonAC.Enabled = false;
            listBoxActions.Items.Add("Running accoreconsole...");
            Log("Running accoreconsole...");
            try
            {
                // accoreconsole.exe is detected automatically
                var runner = new AccoreConsoleRunner();
                string scriptFilePath = runner.CreateTempScriptFile(scriptContent);
                try
                {
                    await Task.Run(() => runner.RunScript(scriptFilePath));
                    listBoxActions.Items.Add($"Saved: {drawingFilePath}");
                    Log($"Saved: {drawingFilePath}");
                }
                finally
                {
                    runner.DeleteTempFile(scriptFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                buttonAC.Enabled = true;
            }
        }

        private void buttonItemHyperlinks_Click(object? sender, EventArgs e)
        {
            try
            {
                string blockName = Interaction.InputBox("Block name:", "Item code hyperlinks", "ITEM_BLOCK");
                if (string.IsNullOrWhiteSpace(blockName))
                    return;

                string attName = Interaction.InputBox("Attribute tag:", "Item code hyperlinks", "ITEM_CODE");
                if (string.IsNullOrWhiteSpace(attName))
                    return;

                string baseUrl = Interaction.InputBox("Base URL (item code is appended):", "Item code hyperlinks", "https://example.com/items/");
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return;

                Log($"Updating item hyperlinks (block {blockName}, tag {attName})...");
                ItemCodeHyperlinks.UpdateItemCode(blockName, attName, baseUrl);
                Log("Item hyperlinks updated.");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void buttonAutodeskApps_Click(object? sender, EventArgs e)
        {
            try
            {
                var releases = AutodeskProducts.GetAcadReleases();
                Log($"{releases.Count} AutoCAD release(s):");
                foreach (var r in releases)
                    Log($"  {r.Release}\\{r.ProductKey} | {r.ProductName} | {r.AcadLocation}{(r.IsCurrent ? " | current" : "")}");

                var products = AutodeskProducts.GetInstalledProducts();
                Log($"{products.Count} Autodesk application(s):");
                foreach (var p in products)
                    Log($"  {p.Name} | {p.Version} | {p.InstallLocation}");
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
        }
    }
}

// https://github.com/gileCAD/AutoCAD-Csharp-Project-Template