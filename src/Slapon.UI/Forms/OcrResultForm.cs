using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slapon.UI.Forms
{
    public partial class OcrResultForm : Form
    {
        public OcrResultForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Match your application's styling
            BackColor = Color.White;
            Size = new Size(400, 300);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            textBoxResult = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Margin = new Padding(10)
            };

            buttonCopy = new Button
            {
                Text = "Copy to Clipboard",
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat
            };

            buttonCopy.Click += ButtonCopy_Click;

            Controls.Add(textBoxResult);
            Controls.Add(buttonCopy);
        }

        private void ButtonCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxResult.Text))
            {
                Clipboard.SetText(textBoxResult.Text);
                MessageBox.Show("Text copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void SetText(string text)
        {
            textBoxResult.Text = text;
        }

        private TextBox textBoxResult;
        private Button buttonCopy;
    }
}
