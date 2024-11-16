namespace Slapon.UI.Forms
{
    using Slapon.Core.Interfaces;
    using Slapon.Core.Models;
    using Slapon.Core.Services;
    using System.Drawing.Imaging;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    public class SelectionOverlayForm : Form
    {
        private Point _startPoint;
        private Rectangle _selectionRect;
        private bool _isSelecting;
        private readonly Bitmap _screenshot;
        private Rectangle _virtualScreenBounds;

        public SelectionOverlayForm(Bitmap screenshot)
        {
            _screenshot = screenshot;
            _virtualScreenBounds = GetVirtualScreenBounds();
            InitializeOverlay();
        }

        private Rectangle GetVirtualScreenBounds()
        {
            // Get the bounds that encompass all screens
            var bounds = new Rectangle();
            foreach (Screen screen in Screen.AllScreens)
            {
                bounds = Rectangle.Union(bounds, screen.Bounds);
            }
            return bounds;
        }

        private void InitializeOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;

            // Set the form to cover all screens
            Location = new Point(_virtualScreenBounds.X, _virtualScreenBounds.Y);
            Size = new Size(_virtualScreenBounds.Width, _virtualScreenBounds.Height);

            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.5;
            Cursor = Cursors.Cross;
            DoubleBuffered = true;

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _startPoint = e.Location;
                    _isSelecting = true;
                    _selectionRect = Rectangle.Empty;
                }
            };

            MouseMove += (s, e) =>
            {
                if (_isSelecting)
                {
                    _selectionRect = GetRectangle(_startPoint, e.Location);
                    Invalidate();
                }
            };

            MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isSelecting = false;
                    if (_selectionRect.Width > 10 && _selectionRect.Height > 10)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                }
            };

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_selectionRect.IsEmpty)
            {
                using var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
                var region = new Region(ClientRectangle);
                region.Exclude(_selectionRect);
                e.Graphics.FillRegion(brush, region);

                // Convert screen coordinates to image coordinates
                var imageRect = new Rectangle(
                    _selectionRect.X - _virtualScreenBounds.X,
                    _selectionRect.Y - _virtualScreenBounds.Y,
                    _selectionRect.Width,
                    _selectionRect.Height
                );

                // Draw the actual screenshot in the selection area
                e.Graphics.DrawImage(_screenshot, _selectionRect, imageRect, GraphicsUnit.Pixel);

                // Draw border around selection
                using var pen = new Pen(Color.White, 2);
                e.Graphics.DrawRectangle(pen, _selectionRect);
            }
        }

        public Rectangle SelectionBounds
        {
            get
            {
                // Convert screen coordinates to image coordinates
                return new Rectangle(
                    _selectionRect.X - _virtualScreenBounds.X,
                    _selectionRect.Y - _virtualScreenBounds.Y,
                    _selectionRect.Width,
                    _selectionRect.Height
                );
            }
        }

        private static Rectangle GetRectangle(Point start, Point end)
        {
            return new Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y)
            );
        }
    }
}