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
            _virtualScreenBounds = SystemInformation.VirtualScreen;
            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;

            // Set the form to cover all screens using SystemInformation.VirtualScreen
            this.Bounds = SystemInformation.VirtualScreen;

            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.5;
            Cursor = Cursors.Cross;
            DoubleBuffered = true;

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _startPoint = this.PointToScreen(e.Location);
                    _isSelecting = true;
                    _selectionRect = Rectangle.Empty;
                    System.Diagnostics.Debug.WriteLine($"Start position (screen coordinates): {_startPoint}");
                }
            };

            MouseMove += (s, e) =>
            {
                if (_isSelecting)
                {
                    // Get current position in screen coordinates
                    Point currentPos = this.PointToScreen(e.Location);

                    _selectionRect = new Rectangle(
                        Math.Min(_startPoint.X, currentPos.X),
                        Math.Min(_startPoint.Y, currentPos.Y),
                        Math.Abs(currentPos.X - _startPoint.X),
                        Math.Abs(currentPos.Y - _startPoint.Y)
                    );

                    System.Diagnostics.Debug.WriteLine($"Current selection: {_selectionRect}");
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

                // Convert screen coordinates to client coordinates for drawing
                Rectangle clientRect = this.RectangleToClient(_selectionRect);
                region.Exclude(clientRect);
                e.Graphics.FillRegion(brush, region);

                // Convert screen coordinates to image coordinates
                var imageRect = new Rectangle(
                    _selectionRect.X - _virtualScreenBounds.X,
                    _selectionRect.Y - _virtualScreenBounds.Y,
                    _selectionRect.Width,
                    _selectionRect.Height
                );

                // Draw the actual screenshot in the selection area
                e.Graphics.DrawImage(_screenshot, clientRect, imageRect, GraphicsUnit.Pixel);

                // Draw border around selection
                using var pen = new Pen(Color.White, 2);
                e.Graphics.DrawRectangle(pen, clientRect);
            }
        }

        public Rectangle SelectionBounds
        {
            get
            {
                // Return the actual screen coordinates
                return _selectionRect;
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