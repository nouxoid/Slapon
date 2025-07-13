using System.Drawing;

namespace Slapon.Core.Models;

public class TextStyle
{
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public Color BorderColor { get; set; } = Color.Black;
    public Color ShadowColor { get; set; } = Color.Gray;
    
    public bool HasBackground { get; set; } = false;
    public bool HasBorder { get; set; } = false;
    public bool HasShadow { get; set; } = false;
    
    public int BackgroundPadding { get; set; } = 4;
    public bool BackgroundRoundedCorners { get; set; } = true;
    public int CornerRadius { get; set; } = 6;
    
    public float BorderWidth { get; set; } = 1.0f;
    
    public Point ShadowOffset { get; set; } = new Point(2, 2);
    
    public TextAlign Alignment { get; set; } = TextAlign.Left;
}

public enum TextAlign
{
    Left,
    Center,
    Right
}