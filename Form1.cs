using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private Bitmap screenshot;
        private Bitmap originalScreenshot;
        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();
        private Rectangle selectionArea;
        private Point startPoint;
        private bool isSelectingScreenshotArea;
        private bool isDrawingMode;
        private bool isLineMode;
        private bool isHighlightMode;
        private PictureBox pictureBox;
        private Panel buttonPanel;
        private Panel picturePanel;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.WindowState = FormWindowState.Maximized;
            isSelectingScreenshotArea = false;
            isDrawingMode = false;

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Initialize panel to hold the PictureBox
            picturePanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            this.Controls.Add(picturePanel);

            pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(0, 0)
            };
            pictureBox.MouseDown += pictureBox_MouseDown;
            pictureBox.MouseMove += pictureBox_MouseMove;
            pictureBox.MouseUp += pictureBox_MouseUp;
            pictureBox.Paint += pictureBox_Paint;
            picturePanel.Controls.Add(pictureBox);

            // Initialize panel to hold buttons
            buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.LightGray
            };
            this.Controls.Add(buttonPanel);

            CreateButton("Take Screenshot", new Point(10, 10), btnTakeScreenshot_Click);
            CreateButton("Draw Rectangle", new Point(10, 50), btnDrawRectangle_Click);
            CreateButton("Save Screenshot", new Point(120, 10), btnSaveScreenshot_Click);
            CreateButton("Clear Drawing", new Point(120, 50), btnClearDrawing_Click);
            CreateButton("Copy to Clipboard", new Point(230, 10), btnCopyToClipboard_Click);
            CreateButton("Undo", new Point(230, 50), btnUndo_Click);
            CreateButton("Redo", new Point(340, 50), btnRedo_Click);
            CreateButton("Draw Line", new Point(340, 10), btnDrawLine_Click);
            CreateButton("Highlight Text", new Point(450, 10), btnHighlightText_Click);
            CreateButton("Exit", new Point(10, 90), btnExit_Click);
        }

        private Button CreateButton(string text, Point location, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(100, 30),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            button.Click += clickHandler;
            buttonPanel.Controls.Add(button);
            return button;
        }

        private void SaveToUndoStack()
        {
            if (screenshot != null)
            {
                undoStack.Push((Bitmap)screenshot.Clone());
                redoStack.Clear(); // Clear redo stack on new action
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push((Bitmap)screenshot.Clone());
                screenshot = undoStack.Pop();
                pictureBox.Image = screenshot;
                pictureBox.Invalidate();
            }
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push((Bitmap)screenshot.Clone());
                screenshot = redoStack.Pop();
                pictureBox.Image = screenshot;
                pictureBox.Invalidate();
            }
        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            if (screenshot != null)
            {
                Clipboard.SetImage(screenshot);
                MessageBox.Show("Screenshot copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No screenshot available to copy.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDrawLine_Click(object sender, EventArgs e)
        {
            if (screenshot == null)
            {
                MessageBox.Show("Please take a screenshot first.", "No Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isLineMode = !isLineMode;
            Cursor = isLineMode ? Cursors.Cross : Cursors.Default;
            ((Button)sender).BackColor = isLineMode ? Color.LightBlue : Color.White;
        }

        private void btnHighlightText_Click(object sender, EventArgs e)
        {
            if (screenshot == null)
            {
                MessageBox.Show("Please take a screenshot first.", "No Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isHighlightMode = !isHighlightMode;
            Cursor = isHighlightMode ? Cursors.Cross : Cursors.Default;
            ((Button)sender).BackColor = isHighlightMode ? Color.LightGreen : Color.White;
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (isDrawingMode || isLineMode || isHighlightMode)
            {
                SaveToUndoStack();
                startPoint = e.Location;
                selectionArea = new Rectangle(startPoint, new Size(0, 0));
                pictureBox.Invalidate();
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if ((isDrawingMode || isLineMode || isHighlightMode) && e.Button == MouseButtons.Left)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);

                selectionArea = new Rectangle(x, y, width, height);
                pictureBox.Invalidate();
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (screenshot != null)
            {
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    if (isDrawingMode)
                    {
                        using (Pen pen = new Pen(Color.Red, 2))
                        {
                            g.DrawRectangle(pen, selectionArea);
                        }
                    }
                    else if (isLineMode)
                    {
                        using (Pen pen = new Pen(Color.Blue, 2))
                        {
                            g.DrawLine(pen, startPoint, e.Location);
                        }
                    }
                    else if (isHighlightMode)
                    {
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(128, Color.Yellow)))
                        {
                            g.FillRectangle(brush, selectionArea);
                        }
                    }
                }

                pictureBox.Image = screenshot;
                selectionArea = new Rectangle(0, 0, 0, 0); // Reset selection area
                pictureBox.Invalidate();
            }
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (selectionArea.Width > 0 && selectionArea.Height > 0)
            {
                using (Pen pen = new Pen(isDrawingMode ? Color.Red : isLineMode ? Color.Blue : Color.Yellow, 2))
                {
                    if (isDrawingMode || isHighlightMode)
                    {
                        pen.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, selectionArea);
                    }
                    else if (isLineMode)
                    {
                        e.Graphics.DrawLine(pen, startPoint, new Point(selectionArea.Right, selectionArea.Bottom));
                    }
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            screenshot?.Dispose();
        }
        private void btnTakeScreenshot_Click(object sender, EventArgs e)
        {
            this.Hide();
            System.Threading.Thread.Sleep(500); // Short delay to hide form

            // Take a full screenshot first
            screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
            }

            // Show selection form
            using (var selectForm = new ScreenSelectForm())
            {
                if (selectForm.ShowDialog() == DialogResult.OK)
                {
                    // Get the selection rectangle from the form
                    Rectangle selection = selectForm.SelectionRectangle;

                    if (selection.Width > 0 && selection.Height > 0)
                    {
                        // Crop the screenshot to the selected area
                        Bitmap croppedScreenshot = new Bitmap(selection.Width, selection.Height);
                        using (Graphics g = Graphics.FromImage(croppedScreenshot))
                        {
                            g.DrawImage(screenshot,
                                new Rectangle(0, 0, croppedScreenshot.Width, croppedScreenshot.Height),
                                selection,
                                GraphicsUnit.Pixel);
                        }

                        // Dispose of the previous screenshot and update it with the cropped version
                        screenshot.Dispose();
                        screenshot = croppedScreenshot;

                        // Update PictureBox with the new cropped image
                        pictureBox.Image = screenshot;
                        pictureBox.Size = screenshot.Size;
                    }
                }
            }

            // Re-show the main form
            this.Show();
            this.WindowState = FormWindowState.Maximized;
        }


        private void btnDrawRectangle_Click(object sender, EventArgs e)
        {
            if (screenshot == null)
            {
                MessageBox.Show("Please take a screenshot first.", "No Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isDrawingMode = !isDrawingMode;
            Cursor = isDrawingMode ? Cursors.Cross : Cursors.Default;
            ((Button)sender).BackColor = isDrawingMode ? Color.LightBlue : Color.White;
        }

        private void btnSaveScreenshot_Click(object sender, EventArgs e)
        {
            if (screenshot != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                    saveFileDialog.Title = "Save Screenshot";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        screenshot.Save(saveFileDialog.FileName, ImageFormat.Png);
                        MessageBox.Show("Screenshot saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("No screenshot available to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearDrawing_Click(object sender, EventArgs e)
        {
            if (originalScreenshot != null)
            {
                screenshot = (Bitmap)originalScreenshot.Clone();
                pictureBox.Image = screenshot;
                pictureBox.Invalidate();
            }
        }
    }
}
