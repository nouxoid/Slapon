using System.Drawing;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class HighlightAnnotation : IAnnotation
{
    public Guid Id { get; set; }
    public RectangleF Bounds { get; set; }
    public Color Color { get; set; }
    public bool IsSelected { get; set; }
    public float Opacity { get; set; } // Add Opacity property

    public HighlightAnnotation(RectangleF bounds, Color color, float opacity = 0.4f)
    {
        Id = Guid.NewGuid();
        Bounds = bounds;
        Color = color;
        Opacity = opacity;
    }

    public void Draw(Graphics g)
    {
        using (var highlightBrush = new SolidBrush(Color.FromArgb((int)(255 * Opacity), Color)))
        {
            g.FillRectangle(highlightBrush, Bounds);
        }
        /*
        if (IsSelected)
        {
            using (var pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawRectangle(pen, Rectangle.Round(Bounds));
            }
        }*/
    }

    public bool Contains(PointF point)
    {
        return Bounds.Contains(point);
    }

    public bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    public void MoveTo(PointF location)
    {
        Bounds = new RectangleF(location, Bounds.Size);
    }

    public IAnnotation Clone()
    {
        return new HighlightAnnotation(Bounds, Color, Opacity)
        {
            Id = this.Id,
            IsSelected = this.IsSelected
        };
    }
}