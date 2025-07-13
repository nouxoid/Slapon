using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using System.Drawing.Drawing2D;

public class LineAnnotation : BaseAnnotation
{
    private readonly Point _originalStart;
    private readonly Point _originalEnd;
    private Point _start;
    private Point _end;
    public float LineThickness { get; set; } = 2f;

    public LineAnnotation(Point start, Point end, Color color)
        : base(GetBounds(start, end), color, 1.0f)
    {
        _originalStart = start;
        _originalEnd = end;
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

        // Draw selection indicators if selected (using base class method for consistency)
        DrawLineSelectionIndicators(g, _start, _end);
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

    public override void Move(int deltaX, int deltaY)
    {
        _start = new Point(_start.X + deltaX, _start.Y + deltaY);
        _end = new Point(_end.X + deltaX, _end.Y + deltaY);
        Bounds = GetBounds(_start, _end);
    }

    public override bool HitTest(Point point)
    {
        using var path = new GraphicsPath();
        path.AddLine(_start, _end);
        using var pen = new Pen(Color.Black, LineThickness * 5); // Wider hit area for easier selection
        return path.IsOutlineVisible(point, pen);
    }

    public override void Resize(float scaleX, float scaleY)
    {
        _start = new Point((int)(_originalStart.X * scaleX), (int)(_originalStart.Y * scaleY));
        _end = new Point((int)(_originalEnd.X * scaleX), (int)(_originalEnd.Y * scaleY));
        Bounds = GetBounds(_start, _end);
        LineThickness *= Math.Min(scaleX, scaleY);
    }
}