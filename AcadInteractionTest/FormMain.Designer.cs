namespace AcadInteractionTest
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBoxInput = new TextBox();
            groupBox1 = new GroupBox();
            buttonClear = new Button();
            buttonExecDde = new Button();
            listBoxActions = new ListBox();
            buttonOpen = new Button();
            buttonDelete = new Button();
            buttonAdd = new Button();
            listBox1 = new ListBox();
            groupBox2 = new GroupBox();
            buttonLaunch = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // textBoxInput
            // 
            textBoxInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxInput.Location = new Point(6, 434);
            textBoxInput.Multiline = true;
            textBoxInput.Name = "textBoxInput";
            textBoxInput.Size = new Size(518, 80);
            textBoxInput.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(buttonClear);
            groupBox1.Controls.Add(buttonExecDde);
            groupBox1.Controls.Add(listBoxActions);
            groupBox1.Controls.Add(buttonOpen);
            groupBox1.Controls.Add(buttonDelete);
            groupBox1.Controls.Add(buttonAdd);
            groupBox1.Controls.Add(listBox1);
            groupBox1.Controls.Add(textBoxInput);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(650, 520);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "DDE";
            // 
            // buttonClear
            // 
            buttonClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClear.Location = new Point(451, 385);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(73, 39);
            buttonClear.TabIndex = 5;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // buttonExecDde
            // 
            buttonExecDde.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonExecDde.Location = new Point(530, 474);
            buttonExecDde.Name = "buttonExecDde";
            buttonExecDde.Size = new Size(112, 39);
            buttonExecDde.TabIndex = 0;
            buttonExecDde.Text = "Run DDE";
            buttonExecDde.UseVisualStyleBackColor = true;
            buttonExecDde.Click += buttonExecDde_Click;
            // 
            // listBoxActions
            // 
            listBoxActions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            listBoxActions.FormattingEnabled = true;
            listBoxActions.ItemHeight = 25;
            listBoxActions.Location = new Point(234, 30);
            listBoxActions.Name = "listBoxActions";
            listBoxActions.Size = new Size(408, 329);
            listBoxActions.TabIndex = 4;
            // 
            // buttonOpen
            // 
            buttonOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOpen.Location = new Point(530, 385);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(112, 39);
            buttonOpen.TabIndex = 4;
            buttonOpen.Text = "&Open";
            buttonOpen.UseVisualStyleBackColor = true;
            buttonOpen.Click += buttonOpen_Click;
            // 
            // buttonDelete
            // 
            buttonDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonDelete.Location = new Point(372, 385);
            buttonDelete.Name = "buttonDelete";
            buttonDelete.Size = new Size(73, 39);
            buttonDelete.TabIndex = 3;
            buttonDelete.Text = "&Del";
            buttonDelete.UseVisualStyleBackColor = true;
            buttonDelete.Click += buttonDelete_Click;
            // 
            // buttonAdd
            // 
            buttonAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAdd.Location = new Point(530, 430);
            buttonAdd.Name = "buttonAdd";
            buttonAdd.Size = new Size(112, 39);
            buttonAdd.TabIndex = 2;
            buttonAdd.Text = "&Add";
            buttonAdd.UseVisualStyleBackColor = true;
            buttonAdd.Click += buttonAdd_Click;
            // 
            // listBox1
            // 
            listBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 25;
            listBox1.Location = new Point(6, 30);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(222, 329);
            listBox1.TabIndex = 1;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            groupBox2.Controls.Add(buttonLaunch);
            groupBox2.Location = new Point(668, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(298, 235);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "AutoCAD Drawing Launcher";
            // 
            // buttonLaunch
            // 
            buttonLaunch.Location = new Point(6, 30);
            buttonLaunch.Name = "buttonLaunch";
            buttonLaunch.Size = new Size(157, 34);
            buttonLaunch.TabIndex = 5;
            buttonLaunch.Text = "Open File";
            buttonLaunch.UseVisualStyleBackColor = true;
            buttonLaunch.Click += buttonLaunch_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(978, 544);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            MinimumSize = new Size(1000, 600);
            Name = "FormMain";
            Text = "AutoCAD Interaction Test";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TextBox textBoxInput;
        private GroupBox groupBox1;
        private ListBox listBox1;
        private GroupBox groupBox2;
        private Button buttonAdd;
        private Button buttonExecDde;
        private Button buttonDelete;
        private ListBox listBoxActions;
        private Button buttonOpen;
        private Button buttonLaunch;
        private Button buttonClear;
    }
}
