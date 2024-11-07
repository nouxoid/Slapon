namespace CapSnip
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources being disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing)
            {
                if (pictureBox != null)
                    pictureBox.Dispose();
                if (toolStrip != null)
                    toolStrip.Dispose();
                if (saveButton != null)
                    saveButton.Dispose();
                if (copyButton != null)
                    copyButton.Dispose();
                if (annotateButton != null)
                    annotateButton.Dispose();
                if (newCaptureButton != null)
                    newCaptureButton.Dispose();
                if (exitButton != null)
                    exitButton.Dispose();
                if (centeringPanel != null)
                    centeringPanel.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated variables

        private System.Windows.Forms.PictureBox pictureBox;
        // Removed duplicate definition of centeringPanel
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton saveButton;
        private System.Windows.Forms.ToolStripButton copyButton;
        private System.Windows.Forms.ToolStripButton annotateButton;
        private System.Windows.Forms.ToolStripButton newCaptureButton;
        private System.Windows.Forms.ToolStripButton exitButton;

        #endregion
    }
}