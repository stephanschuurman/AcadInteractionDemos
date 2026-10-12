using AcadInteraction.Core;

namespace AcadInteraction.WinForms
{
    /// <summary>
    /// Simple chat window for the AutoCAD AI assistant (built in code, no designer file).
    /// </summary>
    public class FormAssistant : Form
    {
        private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        private readonly TextBox _input = new() { Dock = DockStyle.Fill };
        private readonly Button _send = new() { Text = "Send", Dock = DockStyle.Right, Width = 90 };
        private AcadAssistant? _assistant;

        public FormAssistant()
        {
            Text = "AutoCAD AI assistant";
            Width = 600;
            Height = 450;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            bottom.Controls.Add(_input);
            bottom.Controls.Add(_send);

            Controls.Add(_log);
            Controls.Add(bottom);

            AcceptButton = _send;
            _send.Click += async (_, _) => await SendAsync();
        }

        private async Task SendAsync()
        {
            var prompt = _input.Text.Trim();
            if (prompt.Length == 0)
                return;

            _input.Clear();
            _send.Enabled = false;
            Append($"You: {prompt}");

            try
            {
                _assistant ??= new AcadAssistant();
                // Awaiting on the UI (STA) thread keeps the COM calls on this thread.
                var answer = await _assistant.AskAsync(prompt);
                Append($"AI: {answer}");
            }
            catch (Exception ex)
            {
                Append($"Error: {ex.Message}");
            }
            finally
            {
                _send.Enabled = true;
                _input.Focus();
            }
        }

        private void Append(string line) =>
            _log.AppendText(line + Environment.NewLine + Environment.NewLine);
    }
}
