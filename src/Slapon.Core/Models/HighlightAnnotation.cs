using System.Drawing;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class HighlightAnnotation : BaseAnnotation
{
    public HighlightAnnotation(RectangleF bounds, Color color, float opacity = 0.4f)
        : base(bounds, color, opacity)
    {
    }

    public override void Draw(Graphics g)
    {
        using (var highlightBrush = new SolidBrush(GetTransparentColor()))
        {
            g.FillRectangle(highlightBrush, Bounds);
        }
    }

    public override bool Contains(PointF point)
    {
        return Bounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new HighlightAnnotation(Bounds, Color, Opacity)
        {
            IsSelected = this.IsSelected
        };
    }
}