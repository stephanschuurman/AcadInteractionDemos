using AcadInteractionTest.Api;
using System.Diagnostics;
using System.IO;

namespace AcadInteractionTest
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
            try
            {
                var dde = new Api.Dde();
                dde.Test(recipe());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DWG files (*.dwg)|*.dwg";
            openFileDialog.InitialDirectory = @"C:\Program Files\Autodesk\AutoCAD 2025\Sample\Database Connectivity";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                Debug.WriteLine("Selected file: " + filePath);
                listBox1.Items.Add($"[open(\"{filePath}\")]" + Environment.NewLine);
            }
        }

        private void buttonLaunch_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DWG files (*.dwg)|*.dwg";
            openFileDialog.InitialDirectory = @"C:\Program Files\Autodesk\AutoCAD 2025\Sample\Database Connectivity";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    AcShellEx.OpenWithAcLauncher(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

    }
}
