using System.Drawing;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Models;
using Slapon.Core.Interfaces;

public class LineAnnotation : BaseAnnotation
{
    private readonly Point _start;
    private readonly Point _end;
    public float LineThickness { get; set; } = 2f;

    public LineAnnotation(Point start, Point end, Color color)
        : base(GetBounds(start, end), color, 1.0f)
    {
        _start = start;
        _end = end;
    }

    private static RectangleF GetBounds(Point start, Point end)
    {
        return new RectangleF(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y)
        );
    }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(GetTransparentColor(), LineThickness);
        g.DrawLine(pen, _start, _end);
    }

    public override bool Contains(PointF point)
    {
        // Create a slightly wider hit area for the line
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(LineThickness, LineThickness);
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    public override IAnnotation Clone()
    {
        return new LineAnnotation(_start, _end, Color)
        {
            LineThickness = LineThickness,
            IsSelected = IsSelected
        };
    }
}