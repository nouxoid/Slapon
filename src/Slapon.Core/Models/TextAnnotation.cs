using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class TextAnnotation : BaseAnnotation
{
    private readonly string _text;
    private readonly Font _originalFont;
    private readonly Size _originalTextSize;
    private Size _textSize;

    public TextAnnotation(Point location, Color color, string text)
        : base(GetInitialBounds(location, text), color, 1.0f)
    {
        _text = text;
        _originalFont = new Font("Arial", 12, FontStyle.Regular);
        _originalTextSize = TextRenderer.MeasureText(text, _originalFont);
        _textSize = _originalTextSize;
        UpdateBounds(new PointF(location.X, location.Y));
    }

    private static RectangleF GetInitialBounds(Point location, string text)
    {
        using var tempFont = new Font("Arial", 12, FontStyle.Regular);
        var size = TextRenderer.MeasureText(text, tempFont);
        return new RectangleF(location, size);
    }

    private void UpdateBounds(PointF location)
    {
        Bounds = new RectangleF(location, _textSize);
    }

    public override void Draw(Graphics g)
    {
        if (!string.IsNullOrEmpty(_text))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var brush = new SolidBrush(GetTransparentColor());
            float scaledFontSize = Math.Max(1, _originalFont.Size * ((_textSize.Height / (float)_originalTextSize.Height) + (_textSize.Width / (float)_originalTextSize.Width)) / 2);
            using var scaledFont = new Font(_originalFont.FontFamily, scaledFontSize, _originalFont.Style);
            TextRenderer.DrawText(g, _text, scaledFont, Point.Round(Bounds.Location), Color);
        }

        if (IsSelected)
        {
            using var pen = new Pen(Color.Blue, 1f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, Rectangle.Round(Bounds));
        }
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(4, 4); // Slightly larger hit area for easier selection
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text)
        {
            IsSelected = IsSelected
        };
    }

    public override void Resize(float scaleX, float scaleY)
    {
        var location = new PointF(Bounds.X * scaleX, Bounds.Y * scaleY);
        _textSize = new Size((int)(_originalTextSize.Width * scaleX), (int)(_originalTextSize.Height * scaleY));
        UpdateBounds(new PointF(location.X / scaleX, location.Y / scaleY));
    }
}