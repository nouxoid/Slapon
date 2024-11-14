using System.Drawing;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class HighlightAnnotation : IAnnotation
{
    public RectangleF Bounds { get; set; }
    public Color Color { get; set; }
    public bool IsSelected { get; set; }
    private const float HIGHLIGHT_OPACITY = 0.4f;

    public HighlightAnnotation(RectangleF bounds, Color color)
    {
        Bounds = bounds;
        Color = color;
    }

    public void Draw(Graphics g)
    {
        using (var highlightBrush = new SolidBrush(Color.FromArgb((int)(255 * HIGHLIGHT_OPACITY), Color)))
        {
            g.FillRectangle(highlightBrush, Bounds);
        }

        if (IsSelected)
        {
            using (var pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawRectangle(pen, Rectangle.Round(Bounds));
            }
        }
    }

    public bool Contains(PointF point)
    {
        return Bounds.Contains(point);
    }

    public void MoveTo(PointF location)
    {
        Bounds = new RectangleF(location, Bounds.Size);
    }
}