using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WinFormsApp1
{
    public partial class ScreenSelectForm : Form
    {
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting;
        public Rectangle SelectionRectangle => selectionRect;

        public ScreenSelectForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            this.MouseDown += ScreenSelectForm_MouseDown;
            this.MouseMove += ScreenSelectForm_MouseMove;
            this.MouseUp += ScreenSelectForm_MouseUp;
            this.Paint += ScreenSelectForm_Paint;
        }

        private void ScreenSelectForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectionRect = new Rectangle(startPoint, new Size(0, 0));
            }
        }

        private void ScreenSelectForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);

                selectionRect = new Rectangle(x, y, width, height);
                this.Invalidate();
            }
        }

        private void ScreenSelectForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                isSelecting = false;
                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void ScreenSelectForm_Paint(object sender, PaintEventArgs e)
        {
            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, selectionRect);

                    string size = $"{selectionRect.Width} x {selectionRect.Height}";
                    using (Font font = new Font("Arial", 10))
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    using (SolidBrush bgBrush = new SolidBrush(Color.Black))
                    {
                        SizeF textSize = e.Graphics.MeasureString(size, font);
                        Point textLocation = new Point(
                            selectionRect.X + (selectionRect.Width - (int)textSize.Width) / 2,
                            selectionRect.Y + selectionRect.Height + 5
                        );

                        e.Graphics.FillRectangle(bgBrush,
                            textLocation.X - 2, textLocation.Y - 2,
                            textSize.Width + 4, textSize.Height + 4);

                        e.Graphics.DrawString(size, font, brush, textLocation);
                    }
                }
            }
        }
    }
}
