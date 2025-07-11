using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class TextAnnotation : BaseAnnotation
{
    private readonly string _text;
    private readonly Font _font;
    private readonly TextStyle _style;
    private readonly Size _originalTextSize;
    private Size _textSize;

    public string Text => _text;
    public Font Font => _font;
    public TextStyle Style => _style;

    public TextAnnotation(Point location, Color color, string text, Font? font = null, TextStyle? style = null)
        : base(GetInitialBounds(location, text, font ?? GetDefaultFont()), color, 1.0f)
    {
        _text = text;
        _font = font ?? GetDefaultFont();
        _style = style ?? new TextStyle();
        _originalTextSize = TextRenderer.MeasureText(text, _font);
        _textSize = _originalTextSize;
        UpdateBounds(new PointF(location.X, location.Y));
    }

    private static Font GetDefaultFont()
    {
        return new Font("Segoe UI", 12, FontStyle.Regular);
    }

    private static RectangleF GetInitialBounds(Point location, string text, Font font)
    {
        var size = TextRenderer.MeasureText(text, font);
        return new RectangleF(location, size);
    }

    private void UpdateBounds(PointF location)
    {
        Bounds = new RectangleF(location, _textSize);
    }

    public override void Draw(Graphics g)
    {
        if (string.IsNullOrEmpty(_text)) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var textRect = Rectangle.Round(Bounds);
        
        // Calculate scaled font size
        float scaleX = (float)_textSize.Width / _originalTextSize.Width;
        float scaleY = (float)_textSize.Height / _originalTextSize.Height;
        float scale = Math.Min(scaleX, scaleY);
        float scaledFontSize = Math.Max(6, _font.Size * scale);
        
        using var scaledFont = new Font(_font.FontFamily, scaledFontSize, _font.Style);

        // Draw background if enabled
        if (_style.HasBackground)
        {
            var bgRect = textRect;
            bgRect.Inflate(_style.BackgroundPadding, _style.BackgroundPadding);
            
            if (_style.BackgroundRoundedCorners)
            {
                using var path = CreateRoundedRectangle(bgRect, _style.CornerRadius);
                using var bgBrush = new SolidBrush(_style.BackgroundColor);
                g.FillPath(bgBrush, path);
                
                if (_style.HasBorder)
                {
                    using var borderPen = new Pen(_style.BorderColor, _style.BorderWidth);
                    g.DrawPath(borderPen, path);
                }
            }
            else
            {
                using var bgBrush = new SolidBrush(_style.BackgroundColor);
                g.FillRectangle(bgBrush, bgRect);
                
                if (_style.HasBorder)
                {
                    using var borderPen = new Pen(_style.BorderColor, _style.BorderWidth);
                    g.DrawRectangle(borderPen, bgRect);
                }
            }
        }

        // Draw shadow if enabled
        if (_style.HasShadow)
        {
            var shadowRect = textRect;
            shadowRect.Offset(_style.ShadowOffset);
            using var shadowBrush = new SolidBrush(_style.ShadowColor);
            TextRenderer.DrawText(g, _text, scaledFont, shadowRect, _style.ShadowColor, 
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoClipping);
        }

        // Draw main text
        var textColor = GetTransparentColor();
        TextRenderer.DrawText(g, _text, scaledFont, textRect, textColor, 
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoClipping);

        // Draw selection indicator
        if (IsSelected)
        {
            using var pen = new Pen(Color.FromArgb(100, 181, 246), 2f) { DashStyle = DashStyle.Dash };
            var selectionRect = textRect;
            selectionRect.Inflate(4, 4);
            g.DrawRectangle(pen, selectionRect);

            // Draw resize handles
            DrawResizeHandles(g, selectionRect);
        }
    }

    private void DrawResizeHandles(Graphics g, Rectangle bounds)
    {
        const int handleSize = 6;
        var handleColor = Color.FromArgb(100, 181, 246);
        
        using var brush = new SolidBrush(handleColor);
        using var pen = new Pen(Color.White, 1);

        var handles = new[]
        {
            new Point(bounds.Left - handleSize/2, bounds.Top - handleSize/2),
            new Point(bounds.Right - handleSize/2, bounds.Top - handleSize/2),
            new Point(bounds.Right - handleSize/2, bounds.Bottom - handleSize/2),
            new Point(bounds.Left - handleSize/2, bounds.Bottom - handleSize/2)
        };

        foreach (var handle in handles)
        {
            var handleRect = new Rectangle(handle, new Size(handleSize, handleSize));
            g.FillEllipse(brush, handleRect);
            g.DrawEllipse(pen, handleRect);
        }
    }

    private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(6, 6); // Slightly larger hit area for easier selection
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
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text, _font, _style)
        {
            IsSelected = IsSelected
        };
    }

    public override void Resize(float scaleX, float scaleY)
    {
        var location = new PointF(Bounds.X * scaleX, Bounds.Y * scaleY);
        _textSize = new Size((int)(_originalTextSize.Width * scaleX), (int)(_originalTextSize.Height * scaleY));
        UpdateBounds(location);
    }

    // Create a new text annotation with updated text while preserving formatting
    public TextAnnotation UpdateText(string newText)
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, newText, _font, _style)
        {
            IsSelected = IsSelected
        };
    }

    // Create a new text annotation with updated style
    public TextAnnotation UpdateStyle(TextStyle newStyle)
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text, _font, newStyle)
        {
            IsSelected = IsSelected
        };
    }

    // Create a new text annotation with updated font
    public TextAnnotation UpdateFont(Font newFont)
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text, newFont, _style)
        {
            IsSelected = IsSelected
        };
    }
}

public class TextStyle
{
    public bool HasBackground { get; set; } = false;
    public Color BackgroundColor { get; set; } = Color.FromArgb(240, 255, 255, 255);
    public bool BackgroundRoundedCorners { get; set; } = true;
    public int CornerRadius { get; set; } = 4;
    public int BackgroundPadding { get; set; } = 4;
    
    public bool HasBorder { get; set; } = false;
    public Color BorderColor { get; set; } = Color.FromArgb(200, 200, 200);
    public float BorderWidth { get; set; } = 1f;
    
    public bool HasShadow { get; set; } = false;
    public Color ShadowColor { get; set; } = Color.FromArgb(100, 0, 0, 0);
    public Point ShadowOffset { get; set; } = new Point(1, 1);
    
    public TextAlign Alignment { get; set; } = TextAlign.Left;
}

public enum TextAlign
{
    Left,
    Center,
    Right
}