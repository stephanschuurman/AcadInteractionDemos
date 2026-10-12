using System.Diagnostics;
using System.Text.Json;
using AcadInteraction.Core;

namespace AcadInteraction.WinForms
{
    /// <summary>
    /// Batch runs an AutoCAD script (.scr) on a list of drawings with accoreconsole.exe,
    /// inspired by Autodesk ScriptPro (built in code, no designer file).
    /// </summary>
    public class FormScriptPro : Form
    {
        private sealed record ProjectFile(string? Script, int TimeoutSeconds, List<ProjectDrawing> Drawings);
        private sealed record ProjectDrawing(string Path, bool Checked);

        private const string Title = "ScriptPro";
        private const string ProjectFilter = "ScriptPro project (*.scpx)|*.scpx|All files (*.*)|*.*";

        private readonly ListView listDrawings;
        private readonly TextBox textScript;
        private readonly TextBox textLog;
        private readonly NumericUpDown numericTimeout;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly ToolStripProgressBar progressBar;
        private readonly List<ToolStripItem> runDisabledItems = new();
        private readonly ToolStripButton buttonStop;

        private string? projectPath;
        private bool modified;
        private CancellationTokenSource? runCts;

        public FormScriptPro()
        {
            Text = Title;
            Size = new Size(1000, 650);
            MinimumSize = new Size(700, 450);
            StartPosition = FormStartPosition.CenterParent;

            // ---- Toolbar (ribbon groups as separated sections) ----
            var toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(16, 16) };
            AddButton(toolStrip, "New", (_, _) => NewList());
            AddButton(toolStrip, "Wizard", (_, _) => RunWizard());
            AddButton(toolStrip, "Load", (_, _) => LoadList());
            AddButton(toolStrip, "Save", (_, _) => SaveList(false));
            AddButton(toolStrip, "Save As", (_, _) => SaveList(true));
            AddButton(toolStrip, "Load SCP Project", (_, _) => LoadScp());
            toolStrip.Items.Add(new ToolStripSeparator());
            AddButton(toolStrip, "Add", (_, _) => AddFiles());
            AddButton(toolStrip, "Add From Folder", (_, _) => AddFolder());
            AddButton(toolStrip, "Remove", (_, _) => RemoveSelected());
            AddButton(toolStrip, "Check/Uncheck", (_, _) => ToggleChecked());
            toolStrip.Items.Add(new ToolStripSeparator());
            AddButton(toolStrip, "Run Checked", async (_, _) => await RunAsync(i => i.Checked));
            AddButton(toolStrip, "Run Selected", async (_, _) => await RunAsync(i => i.Selected));
            AddButton(toolStrip, "Run Failed", async (_, _) => await RunAsync(i => i.SubItems[1].Text == "Failed"));
            toolStrip.Items.Add(new ToolStripSeparator());
            buttonStop = new ToolStripButton("Stop") { Enabled = false };
            buttonStop.Click += (_, _) => runCts?.Cancel();
            toolStrip.Items.Add(buttonStop);

            // ---- Script settings ----
            var panelSettings = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 5, Padding = new Padding(6) };
            panelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panelSettings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            textScript = new TextBox { Dock = DockStyle.Fill };
            textScript.TextChanged += (_, _) => SetModified();
            var buttonBrowseScript = new Button { Text = "...", AutoSize = true };
            buttonBrowseScript.Click += (_, _) => BrowseScript();
            numericTimeout = new NumericUpDown { Minimum = 10, Maximum = 3600, Value = 300, Width = 70 };
            numericTimeout.ValueChanged += (_, _) => SetModified();

            panelSettings.Controls.Add(new Label { Text = "Script file:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            panelSettings.Controls.Add(textScript, 1, 0);
            panelSettings.Controls.Add(buttonBrowseScript, 2, 0);
            panelSettings.Controls.Add(new Label { Text = "Timeout (s):", AutoSize = true, Anchor = AnchorStyles.Left }, 3, 0);
            panelSettings.Controls.Add(numericTimeout, 4, 0);

            // ---- Drawing list ----
            listDrawings = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                AllowDrop = true
            };
            listDrawings.Columns.Add("Drawing", 300);
            listDrawings.Columns.Add("Status", 80);
            listDrawings.Columns.Add("Folder", 300);
            listDrawings.Columns.Add("Result", 250);
            listDrawings.ItemChecked += (_, _) => SetModified();
            listDrawings.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) RemoveSelected(); };
            listDrawings.DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
            listDrawings.DragDrop += (_, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
                    AddDrawings(paths.SelectMany(p => Directory.Exists(p)
                        ? Directory.EnumerateFiles(p, "*.dwg", SearchOption.AllDirectories)
                        : new[] { p }));
            };

            // ---- Log ----
            textLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
                Font = new Font(FontFamily.GenericMonospace, 9f)
            };

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            split.Panel1.Controls.Add(listDrawings);
            split.Panel2.Controls.Add(textLog);

            // ---- Status bar ----
            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            progressBar = new ToolStripProgressBar { Visible = false };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);

            Controls.Add(split);
            Controls.Add(panelSettings);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);

            Shown += (_, _) => split.SplitterDistance = (int)(split.Height * 0.65);
            FormClosing += FormScriptPro_FormClosing;

            modified = false;
            UpdateTitle();
            UpdateStatus();
        }

        private void AddButton(ToolStrip toolStrip, string text, EventHandler onClick)
        {
            var button = new ToolStripButton(text);
            button.Click += onClick;
            toolStrip.Items.Add(button);
            runDisabledItems.Add(button);
        }

        // ---- List ----

        private void NewList()
        {
            if (!ConfirmDiscard())
                return;
            listDrawings.Items.Clear();
            textScript.Clear();
            numericTimeout.Value = 300;
            projectPath = null;
            modified = false;
            UpdateTitle();
            UpdateStatus();
        }

        private void LoadList()
        {
            if (!ConfirmDiscard())
                return;

            using var dialog = new OpenFileDialog { Filter = ProjectFilter };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var project = JsonSerializer.Deserialize<ProjectFile>(File.ReadAllText(dialog.FileName))
                    ?? throw new InvalidDataException("Empty project file.");

                listDrawings.Items.Clear();
                textScript.Text = project.Script ?? "";
                numericTimeout.Value = Math.Clamp(project.TimeoutSeconds, (int)numericTimeout.Minimum, (int)numericTimeout.Maximum);
                foreach (var drawing in project.Drawings)
                    AddDrawing(drawing.Path, drawing.Checked);

                projectPath = dialog.FileName;
                modified = false;
                Log($"Loaded {dialog.FileName} ({project.Drawings.Count} drawing(s)).");
                UpdateTitle();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private void RunWizard()
        {
            if (!ConfirmDiscard())
                return;

            MessageBox.Show(this, "Step 1 : Select the script file", Title);
            using var scriptDialog = new OpenFileDialog { Filter = "AutoCAD script (*.scr)|*.scr|All files (*.*)|*.*", Title = "Step 1 : Select the script file" };
            if (scriptDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var choice = MessageBox.Show(this,
                "Step 2 : Add drawing files\n\nYes = select files\nNo = add all drawings from a folder",
                Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel)
                return;

            listDrawings.Items.Clear();
            projectPath = null;
            textScript.Text = scriptDialog.FileName;

            if (choice == DialogResult.Yes)
                AddFiles();
            else
                AddFolder();

            SetModified();
            UpdateStatus();
            Log("Wizard finished. Use Run Checked to start.");
        }

        private void LoadScp()
        {
            if (!ConfirmDiscard())
                return;

            using var dialog = new OpenFileDialog { Filter = "ScriptPro project files (*.scp)|*.scp|All files (*.*)|*.*", Title = "Load ScriptPro project files" };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var scp = ScpProject.Parse(File.ReadLines(dialog.FileName));
                listDrawings.Items.Clear();
                textScript.Text = scp.ScriptFile ?? "";
                if (scp.TimeoutSeconds is int t)
                    numericTimeout.Value = Math.Clamp(t, (int)numericTimeout.Minimum, (int)numericTimeout.Maximum);
                foreach (var drawing in scp.Drawings)
                    AddDrawing(drawing, true);

                // Imported projects are saved in the new .scpx format
                projectPath = null;
                modified = true;
                Log($"Imported {dialog.FileName}: {scp.Drawings.Count} drawing(s), script {(scp.ScriptFile ?? "(not found)")}.");
                UpdateTitle();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        private bool SaveList(bool saveAs)
        {
            string? path = projectPath;
            if (saveAs || path == null)
            {
                using var dialog = new SaveFileDialog { Filter = ProjectFilter, FileName = Path.GetFileName(projectPath ?? "Project.scpx") };
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;
                path = dialog.FileName;
            }

            try
            {
                var project = new ProjectFile(
                    textScript.Text,
                    (int)numericTimeout.Value,
                    listDrawings.Items.Cast<ListViewItem>().Select(i => new ProjectDrawing((string)i.Tag!, i.Checked)).ToList());
                File.WriteAllText(path, JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true }));

                projectPath = path;
                modified = false;
                Log($"Saved {path}.");
                UpdateTitle();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private bool ConfirmDiscard()
        {
            if (!modified)
                return true;

            return MessageBox.Show(this, "Save changes to the drawing list?", Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch
            {
                DialogResult.Yes => SaveList(false),
                DialogResult.No => true,
                _ => false
            };
        }

        private void FormScriptPro_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (runCts != null)
            {
                MessageBox.Show(this, "Stop the running batch first.", Title);
                e.Cancel = true;
                return;
            }
            e.Cancel = !ConfirmDiscard();
        }

        // ---- Drawing files ----

        private void AddFiles()
        {
            using var dialog = new OpenFileDialog { Filter = "DWG files (*.dwg)|*.dwg", Multiselect = true };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                AddDrawings(dialog.FileNames);
        }

        private void AddFolder()
        {
            using var dialog = new FolderBrowserDialog { Description = "Add all drawings from folder" };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var includeSubfolders = MessageBox.Show(this, "Include subfolders?", Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            AddDrawings(Directory.EnumerateFiles(dialog.SelectedPath, "*.dwg",
                includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
        }

        private void AddDrawings(IEnumerable<string> paths)
        {
            int added = 0;
            listDrawings.BeginUpdate();
            foreach (var path in paths.Where(p => p.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase)))
            {
                if (AddDrawing(path, true))
                    added++;
            }
            listDrawings.EndUpdate();

            if (added > 0)
            {
                SetModified();
                Log($"{added} drawing(s) added.");
            }
            UpdateStatus();
        }

        private bool AddDrawing(string path, bool isChecked)
        {
            if (listDrawings.Items.Cast<ListViewItem>().Any(i => string.Equals((string)i.Tag!, path, StringComparison.OrdinalIgnoreCase)))
                return false;

            var item = new ListViewItem(Path.GetFileName(path)) { Tag = path, Checked = isChecked };
            item.SubItems.Add(File.Exists(path) ? "" : "Missing");
            item.SubItems.Add(Path.GetDirectoryName(path) ?? "");
            item.SubItems.Add("");
            listDrawings.Items.Add(item);
            return true;
        }

        private void RemoveSelected()
        {
            if (runCts != null || listDrawings.SelectedItems.Count == 0)
                return;
            foreach (ListViewItem item in listDrawings.SelectedItems)
                listDrawings.Items.Remove(item);
            SetModified();
            UpdateStatus();
        }

        private void ToggleChecked()
        {
            var items = listDrawings.SelectedItems.Count > 0
                ? listDrawings.SelectedItems.Cast<ListViewItem>()
                : listDrawings.Items.Cast<ListViewItem>();
            foreach (var item in items.ToList())
                item.Checked = !item.Checked;
        }

        private void BrowseScript()
        {
            using var dialog = new OpenFileDialog { Filter = "AutoCAD script (*.scr)|*.scr|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                textScript.Text = dialog.FileName;
        }

        // ---- Run ----

        private async Task RunAsync(Func<ListViewItem, bool> filter)
        {
            if (runCts != null)
                return;

            var items = listDrawings.Items.Cast<ListViewItem>().Where(filter).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show(this, "No drawings to process.", Title);
                return;
            }
            if (!File.Exists(textScript.Text))
            {
                MessageBox.Show(this, "Select an existing script file (.scr) first.", Title);
                return;
            }

            string script = textScript.Text;
            string scriptContent = File.ReadAllText(script);
            bool useKeywords = ScriptKeywords.ContainsKeywords(scriptContent);
            var timeout = TimeSpan.FromSeconds((double)numericTimeout.Value);
            runCts = new CancellationTokenSource();
            SetRunning(true, items.Count);
            Log($"Running {Path.GetFileName(script)} on {items.Count} drawing(s){(useKeywords ? " (with <acet:...> keywords)" : "")}...");

            int ok = 0, failed = 0;
            var total = Stopwatch.StartNew();
            try
            {
                foreach (var item in items)
                {
                    if (runCts.IsCancellationRequested)
                    {
                        Log("Stopped by user.");
                        break;
                    }

                    string drawing = (string)item.Tag!;
                    SetItemStatus(item, "Running", "");
                    item.EnsureVisible();
                    statusLabel.Text = $"Processing {item.Text}...";

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        if (!File.Exists(drawing))
                            throw new FileNotFoundException("Drawing not found.", drawing);

                        var runner = new AccoreConsoleRunner(drawingFilePath: drawing);
                        if (useKeywords)
                        {
                            string tempScript = runner.CreateTempScriptFile(ScriptKeywords.Expand(scriptContent, drawing));
                            try
                            {
                                await Task.Run(() => runner.RunScript(tempScript, timeout));
                            }
                            finally
                            {
                                runner.DeleteTempFile(tempScript);
                            }
                        }
                        else
                        {
                            await Task.Run(() => runner.RunScript(script, timeout));
                        }
                        SetItemStatus(item, "Done", $"{sw.Elapsed.TotalSeconds:0.0} s");
                        Log($"OK     {drawing} ({sw.Elapsed.TotalSeconds:0.0} s)");
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        SetItemStatus(item, "Failed", ex.Message);
                        Log($"FAILED {drawing}: {ex.Message}");
                        failed++;
                    }
                    progressBar.PerformStep();
                }
            }
            finally
            {
                Log($"Finished: {ok} done, {failed} failed in {total.Elapsed:mm\\:ss}.");
                runCts.Dispose();
                runCts = null;
                SetRunning(false, 0);
                UpdateStatus();
            }
        }

        private static void SetItemStatus(ListViewItem item, string status, string result)
        {
            item.SubItems[1].Text = status;
            item.SubItems[3].Text = result;
            item.ForeColor = status switch
            {
                "Done" => Color.DarkGreen,
                "Failed" => Color.Firebrick,
                "Running" => Color.RoyalBlue,
                _ => SystemColors.WindowText
            };
        }

        private void SetRunning(bool running, int count)
        {
            foreach (var item in runDisabledItems)
                item.Enabled = !running;
            buttonStop.Enabled = running;
            textScript.Enabled = !running;
            numericTimeout.Enabled = !running;
            progressBar.Visible = running;
            progressBar.Maximum = Math.Max(count, 1);
            progressBar.Value = 0;
            progressBar.Step = 1;
        }

        // ---- Helpers ----

        private void SetModified()
        {
            if (modified)
                return;
            modified = true;
            UpdateTitle();
        }

        private void UpdateTitle() =>
            Text = $"{Title}  -  {(projectPath == null ? "Untitled" : Path.GetFileNameWithoutExtension(projectPath))}{(modified ? " *" : "")}";

        private void UpdateStatus()
        {
            if (runCts == null)
                statusLabel.Text = $"{listDrawings.Items.Count} drawing(s), {listDrawings.CheckedItems.Count} checked";
        }

        private void Log(string message) =>
            textLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");

        private void LogError(Exception ex)
        {
            Log("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
