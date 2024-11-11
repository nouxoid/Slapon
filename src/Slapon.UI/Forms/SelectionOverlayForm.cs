namespace Slapon.UI.Forms;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

public class SelectionOverlayForm : Form
{
    private Point _startPoint;
    private Rectangle _selectionRect;
    private bool _isSelecting;
    private readonly Bitmap _screenshot;

    public SelectionOverlayForm(Bitmap screenshot)
    {
        _screenshot = screenshot;
        InitializeOverlay();
    }

    private void InitializeOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
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
            
            // Draw the actual screenshot in the selection area
            e.Graphics.DrawImage(_screenshot, _selectionRect, _selectionRect, GraphicsUnit.Pixel);
            
            // Draw border around selection
            using var pen = new Pen(Color.White, 2);
            e.Graphics.DrawRectangle(pen, _selectionRect);
        }
    }

    public Rectangle SelectionBounds => _selectionRect;

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