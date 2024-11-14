using System.Drawing;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Models;
using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net;

public class RectangleAnnotation : BaseAnnotation
{
    public float BorderThickness { get; set; } = 2f;

    public RectangleAnnotation(RectangleF bounds, Color color, float opacity)
        : base(bounds, color, opacity)
    {
    }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(GetTransparentColor(), BorderThickness);
      /*  if (IsSelected)
        {
            pen.DashStyle = DashStyle.Dash;
        }*/
        g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(BorderThickness, BorderThickness);
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    public override IAnnotation Clone()
    {
        return new RectangleAnnotation(Bounds, Color, Opacity)
        {
            BorderThickness = BorderThickness,
            IsSelected = IsSelected
        };
    }
}