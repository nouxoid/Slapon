namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.UI.Forms;
using Slapon.UI.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Forms;

// Modern UI helper classes
public class ModernColorScheme
{
    public Color Background { get; set; }
    public Color Surface { get; set; }
    public Color Primary { get; set; }
    public Color Secondary { get; set; }
    public Color Accent { get; set; }
    public Color Text { get; set; }
    public Color Border { get; set; }
    public Color Hover { get; set; }
}

public class ModernStatusStripRenderer : ToolStripProfessionalRenderer
{
    private readonly ModernColorScheme _colorScheme;

    public ModernStatusStripRenderer(ModernColorScheme colorScheme)
    {
        _colorScheme = colorScheme;
    }

    protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
    {
        // Don't render the sizing grip
    }
}

public class ModernToolStripRenderer : ToolStripProfessionalRenderer
{
    private readonly ModernColorScheme _colorScheme;

    public ModernToolStripRenderer(ModernColorScheme colorScheme)
    {
        _colorScheme = colorScheme;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using (var brush = new SolidBrush(_colorScheme.Surface))
        {
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }
    }
}

public partial class MainForm : Form
{

    private enum AnnotationTool
    {
        Rectangle,
        Highlight,
        Line,
        Text,
        Select
    }

    private TextBox? _textBox;
    private Point? _lineStart;
    private AnnotationTool _currentTool = AnnotationTool.Select;
    private Point? _drawStart;
    private IAnnotation? _currentAnnotation;
    private readonly Color _defaultHighlightColor = Color.Yellow;

    private Point? _dragStart;
    private Point _lastMousePosition;
    private IAnnotation? _draggedAnnotation;

    // Fixed: Store original image dimensions that never change
    private float _originalImageWidth;
    private float _originalImageHeight;
    private bool _annotationsInitialized = false;

    private PointF? _dragStartPosition;

    private readonly IAnnotationService _annotationService;
    private readonly IAnnotationFactory _annotationFactory;
    private Panel scrollablePanel;
    private Bitmap? _currentImage;
    private PointF _startPoint;
    private bool _isDrawing = false;
    private Color _currentColor = Color.Red;
    private AnnotationType _currentType = AnnotationType.Rectangle;
    private bool _isDragging = false;
    private PictureBox pictureBox;
    private readonly ScreenCaptureService _screenCaptureService;

    private ToolStripButton ocrButton;
    private ToolStripButton btnRectangleTool;
    private ToolStripButton btnHighlightTool;
    private ToolStripButton lineButton;
    private ToolStripButton textButton;
    private ToolStripButton rotateButton;
    private ToolStripButton selectButton;
    private ToolStrip toolStrip;
    private Panel panel;

    private IOcrService _ocrService;
    private ToolStripButton btnOcr;

    // Modern UI enhancements
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
    private ToolStripStatusLabel imageInfoLabel;
    private ToolStripStatusLabel toolLabel;
    private Panel floatingToolbar;
    private Timer animationTimer;
    private int animationStep = 0;
    private bool isDarkTheme = false;

    // Color schemes
    private readonly ModernColorScheme lightTheme = new ModernColorScheme
    {
        Background = Color.FromArgb(248, 249, 250),
        Surface = Color.White,
        Primary = Color.FromArgb(0, 120, 215),
        Secondary = Color.FromArgb(118, 118, 118),
        Accent = Color.FromArgb(0, 103, 192),
        Text = Color.FromArgb(50, 50, 50),
        Border = Color.FromArgb(225, 225, 225),
        Hover = Color.FromArgb(243, 244, 246)
    };

    private readonly ModernColorScheme darkTheme = new ModernColorScheme
    {
        Background = Color.FromArgb(32, 32, 32),
        Surface = Color.FromArgb(45, 45, 45),
        Primary = Color.FromArgb(100, 181, 246),
        Secondary = Color.FromArgb(158, 158, 158),
        Accent = Color.FromArgb(66, 165, 245),
        Text = Color.FromArgb(240, 240, 240),
        Border = Color.FromArgb(66, 66, 66),
        Hover = Color.FromArgb(55, 55, 55)
    };

    private ModernColorScheme CurrentTheme => isDarkTheme ? darkTheme : lightTheme;

    public MainForm()
    {
        InitializeComponent();
        ApplyModernStyling();
        SetupModernUI();
        
        _annotationService = new AnnotationService();
        _annotationFactory = new AnnotationFactory();
        _screenCaptureService = new ScreenCaptureService();
        _ocrService = new TesseractOcrService(Path.Combine(Application.StartupPath, "tessdata"));
        
        _annotationService.AnnotationsChanged += (s, e) =>
        {
            pictureBox.Invalidate();
            UpdateUndoRedoState();
            UpdateStatusBar();
        };
        
        SetupUI();
        SetupAnimations();

        // Automatically start screen capture on startup
        StartScreenCapture(null, null);
    }

    private void ApplyModernStyling()
    {
        // Apply modern form styling
        this.BackColor = CurrentTheme.Background;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        
        // Enable modern visual effects
        SetStyle(ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.UserPaint | 
                 ControlStyles.DoubleBuffer | 
                 ControlStyles.ResizeRedraw, true);
    }

    private void SetupModernUI()
    {
        // Initialize animation timer
        animationTimer = new Timer { Interval = 16 }; // ~60 FPS
        animationTimer.Tick += AnimationTimer_Tick;

        // Create modern status bar
        CreateModernStatusBar();
    }

    private void CreateModernStatusBar()
    {
        statusStrip = new StatusStrip
        {
            BackColor = CurrentTheme.Surface,
            ForeColor = CurrentTheme.Text,
            Font = new Font("Segoe UI", 9F),
            Renderer = new ModernStatusStripRenderer(CurrentTheme)
        };

        statusLabel = new ToolStripStatusLabel
        {
            Text = "Ready",
            Font = new Font("Segoe UI", 9F),
            ForeColor = CurrentTheme.Text
        };

        toolLabel = new ToolStripStatusLabel
        {
            Text = "Tool: Select",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = CurrentTheme.Primary
        };

        imageInfoLabel = new ToolStripStatusLabel
        {
            Text = "No image loaded",
            Font = new Font("Segoe UI", 9F),
            ForeColor = CurrentTheme.Secondary,
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight
        };

        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, toolLabel, imageInfoLabel });
        Controls.Add(statusStrip);
    }

    private void SetupAnimations()
    {
        // Add smooth hover animations to toolbar buttons
        foreach (Control control in this.Controls)
        {
            if (control is ToolStrip toolstrip)
            {
                foreach (ToolStripItem item in toolstrip.Items)
                {
                    if (item is ToolStripButton button)
                    {
                        button.MouseEnter += (s, e) => AnimateButtonHover(button, true);
                        button.MouseLeave += (s, e) => AnimateButtonHover(button, false);
                    }
                }
            }
        }
    }

    private void AnimateButtonHover(ToolStripButton button, bool isHovering)
    {
        // Subtle animation for button hover state
        if (isHovering)
        {
            button.BackColor = CurrentTheme.Hover;
        }
        else
        {
            button.BackColor = CurrentTheme.Surface;
        }
    }

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        animationStep++;
        // Add any continuous animations here
        if (animationStep > 100) animationStep = 0;
    }

    private void UpdateStatusBar()
    {
        if (statusLabel == null || toolLabel == null || imageInfoLabel == null) return;

        // Update tool information
        toolLabel.Text = $"Tool: {_currentTool}";
        
        // Update image information
        if (_currentImage != null)
        {
            var annotationCount = _annotationService.Annotations.Count();
            imageInfoLabel.Text = $"{_currentImage.Width}×{_currentImage.Height} | {annotationCount} annotations";
        }
        else
        {
            imageInfoLabel.Text = "No image loaded";
        }

        // Update status message
        statusLabel.Text = _currentTool switch
        {
            AnnotationTool.Rectangle => "Click and drag to create a rectangle",
            AnnotationTool.Highlight => "Click and drag to highlight an area",
            AnnotationTool.Line => "Click and drag to draw a line",
            AnnotationTool.Text => "Click to add text",
            AnnotationTool.Select => "Click to select annotations",
            _ => "Ready"
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Delete)
        {
            var selected = _annotationService.SelectedAnnotation;
            if (selected != null)
            {
                _annotationService.RemoveAnnotation(selected);
                pictureBox.Refresh();
                return true;
            }
        }
        if (keyData == (Keys.Control | Keys.Z) && _annotationService.CanUndo)
        {
            _annotationService.Undo();
            pictureBox.Invalidate();
            return true;
        }
        if (keyData == (Keys.Control | Keys.Y) && _annotationService.CanRedo)
        {
            _annotationService.Redo();
            pictureBox.Invalidate();
            return true;
        }
        // Quick tool switching with keyboard shortcuts
        if (keyData == Keys.R) { SetActiveTool(AnnotationTool.Rectangle); return true; }
        if (keyData == Keys.H) { SetActiveTool(AnnotationTool.Highlight); return true; }
        if (keyData == Keys.L) { SetActiveTool(AnnotationTool.Line); return true; }
        if (keyData == Keys.T) { SetActiveTool(AnnotationTool.Text); return true; }
        if (keyData == Keys.S) { SetActiveTool(AnnotationTool.Select); return true; }
        
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SetupUI()
    {
        InitializePictureBox();
        InitializePanel();
        InitializeToolStrip();
        SetupEventHandlers();
    }

    private void InitializePictureBox()
    {
        pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Dock = DockStyle.None
        };

        EnableDoubleBuffering(pictureBox);
    }

    private void InitializePanel()
    {
        panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16),
            BackColor = CurrentTheme.Background
        };

        EnableDoubleBuffering(panel);
        panel.Controls.Add(pictureBox);
    }

    private void EnableDoubleBuffering(Control control)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, control, new object[] { true });
    }

    private void InitializeToolStrip()
    {
        toolStrip = CreateModernToolStrip();
        var toolStripItems = new List<ToolStripItem>();

        // Create and add drawing tools
        toolStripItems.AddRange(CreateDrawingTools());

        // Add separator
        toolStripItems.Add(CreateModernSeparator());

        // Create and add color tools
        toolStripItems.AddRange(CreateColorTools());

        // Create and add utility tools
        toolStripItems.AddRange(CreateUtilityTools());

        toolStrip.Items.AddRange(toolStripItems.ToArray());
    }

    private ToolStrip CreateModernToolStrip() => new()
    {
        Renderer = new ModernToolStripRenderer(CurrentTheme),
        GripStyle = ToolStripGripStyle.Hidden,
        BackColor = CurrentTheme.Surface,
        ForeColor = CurrentTheme.Text,
        Padding = new Padding(12, 8, 12, 8),
        Height = 56,
        Dock = DockStyle.Top,
        Font = new Font("Segoe UI", 9F)
    };

    private ToolStripSeparator CreateModernSeparator() => new()
    {
        Margin = new Padding(8, 0, 8, 0),
        ForeColor = CurrentTheme.Border
    };

    private IEnumerable<ToolStripItem> CreateDrawingTools()
    {
        yield return CreateModernButton("New Capture", Resources.newcapture, StartScreenCapture, "Ctrl+N");
        
        // Add Undo/Redo buttons with proper references
        undoButton = CreateModernButton("", Resources.undo, (s, e) =>
        {
            if (_annotationService.CanUndo)
            {
                _annotationService.Undo();
                pictureBox.Invalidate();
                UpdateUndoRedoState();
            }
        }, "Ctrl+Z");

        redoButton = CreateModernButton("", Resources.redo, (s, e) =>
        {
            if (_annotationService.CanRedo)
            {
                _annotationService.Redo();
                pictureBox.Invalidate();
                UpdateUndoRedoState();
            }
        }, "Ctrl+Y");

        yield return undoButton;
        yield return redoButton;

        // Add a separator between undo/redo and drawing tools
        yield return CreateModernSeparator();

        // Initialize and store tool buttons as class fields
        btnRectangleTool = CreateModernButton("", Resources.rectangle, (s, e) => SetActiveTool(AnnotationTool.Rectangle), "R");
        btnHighlightTool = CreateModernButton("", Resources.highlighter, (s, e) => SetActiveTool(AnnotationTool.Highlight), "H");
        lineButton = CreateModernButton("", Resources.line, (s, e) => SetActiveTool(AnnotationTool.Line), "L");
        textButton = CreateModernButton("", Resources.text, (s, e) => SetActiveTool(AnnotationTool.Text), "T");
        selectButton = CreateModernButton("", Resources.select, (s, e) => SetActiveTool(AnnotationTool.Select), "S");

        yield return btnRectangleTool;
        yield return btnHighlightTool;
        yield return lineButton;
        yield return textButton;
        yield return selectButton;
    }

    // Add these as class fields
    private ToolStripButton? undoButton;
    private ToolStripButton? redoButton;

    // Add this method to update undo/redo button states
    private void UpdateUndoRedoState()
    {
        if (undoButton != null) 
        {
            undoButton.Enabled = _annotationService.CanUndo;
            undoButton.ForeColor = _annotationService.CanUndo ? CurrentTheme.Text : CurrentTheme.Secondary;
        }
        if (redoButton != null) 
        {
            redoButton.Enabled = _annotationService.CanRedo;
            redoButton.ForeColor = _annotationService.CanRedo ? CurrentTheme.Text : CurrentTheme.Secondary;
        }
    }

    private IEnumerable<ToolStripItem> CreateColorTools()
    {
        var colors = new[]
        {
            Color.FromArgb(220, 53, 69),   // Modern Red
            Color.FromArgb(25, 135, 84),   // Modern Green
            Color.FromArgb(13, 110, 253),  // Modern Blue
            Color.FromArgb(255, 193, 7),   // Modern Yellow
            Color.FromArgb(214, 51, 132)   // Modern Pink
        };

        foreach (var color in colors)
        {
            var colorButton = CreateModernColorButton(color);
            colorButton.Tag = "color";
            yield return colorButton;
        }

        yield return CreateColorPickerButton();
    }

    private ToolStripButton CreateColorPickerButton()
    {
        var button = new ToolStripButton
        {
            Image = Resources.colorIcon,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            AutoSize = false,
            Size = new Size(32, 32),
            Margin = new Padding(4),
            ToolTipText = "Custom Color Picker"
        };
        button.Click += ChangeColor;
        return button;
    }

    private IEnumerable<ToolStripItem> CreateUtilityTools()
    {
        yield return CreateModernSeparator();

        rotateButton = CreateModernButton("", Resources.rotate, RotateImage, "");
        rotateButton.ToolTipText = "Rotate Image 90°";
        yield return rotateButton;

        yield return CreateModernButton("", Resources.clearall, (s, e) => ClearAllAnnotations(), "");
        
        ocrButton = CreateModernButton("", Resources.ocr, async (s, e) => await PerformOcr(), "");
        ocrButton.ToolTipText = "Extract Text (OCR)";
        yield return ocrButton;
        
        yield return CreateModernButton("Copy", null, (s, e) => CopyScreenshotWithAnnotationsToClipboard(), "Ctrl+C");
        yield return CreateModernButton("Save", null, SaveImage, "Ctrl+S");
    }

    private void SetupEventHandlers()
    {
        panel.Resize += Panel_Resize;
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;

        Controls.Add(panel);
        Controls.Add(toolStrip);
    }

    private ToolStripButton CreateModernColorButton(Color color)
    {
        var button = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.None,
            AutoSize = false,
            Size = new Size(28, 28),
            Margin = new Padding(2),
            BackColor = Color.Transparent,
            Tag = "color",
            ToolTipText = $"Color: {color.Name}"
        };

        button.Paint += (s, e) =>
        {
            if (s is ToolStripButton btn)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(2, 2, btn.Width - 4, btn.Height - 4);
                var radius = 6;

                // Draw rounded rectangle with color
                using (var brush = new SolidBrush(color))
                using (var path = CreateRoundedRectangle(rect, radius))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Draw selection indicator if this is the current color
                if (_currentColor == color)
                {
                    using var pen = new Pen(CurrentTheme.Primary, 2);
                    using var selectionPath = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), radius + 2);
                    e.Graphics.DrawPath(pen, selectionPath);
                }
            }
        };

        button.Click += (s, e) =>
        {
            _currentColor = color;
            UpdateColorButtonStates();
            UpdateStatusBar();
        };

        return button;
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

    private void UpdateColorButtonStates()
    {
        // Refresh all color buttons to update their appearance
        foreach (ToolStripItem item in toolStrip.Items)
        {
            if (item is ToolStripButton btn && btn.Tag?.ToString() == "color")
            {
                btn.Invalidate(); // This will trigger the Paint event
            }
        }
    }

    // Enhanced CreateModernButton method with keyboard shortcuts
    private ToolStripButton CreateModernButton(string text, Image? icon, EventHandler clickHandler, string shortcut = "")
    {
        var button = new ToolStripButton
        {
            Text = text,
            DisplayStyle = icon != null && text != "" ? ToolStripItemDisplayStyle.ImageAndText :
                          icon != null ? ToolStripItemDisplayStyle.Image :
                          ToolStripItemDisplayStyle.Text,
            AutoSize = true,
            Margin = new Padding(4),
            Padding = new Padding(10, 8, 10, 8),
            ForeColor = CurrentTheme.Text,
            Font = new Font("Segoe UI", 9F),
            BackColor = CurrentTheme.Surface
        };

        if (icon != null)
        {
            var size = new Size(20, 20);
            var resizedImage = new Bitmap(icon, size);
            button.Image = resizedImage;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageScaling = ToolStripItemImageScaling.None;
            button.ImageTransparentColor = Color.Transparent;
        }

        // Enhanced tooltip with keyboard shortcut
        var tooltipText = text;
        if (!string.IsNullOrEmpty(shortcut))
        {
            tooltipText += $" ({shortcut})";
        }
        button.ToolTipText = tooltipText;

        button.Click += clickHandler;
        return button;
    }

    private void SetActiveTool(AnnotationTool tool)
    {
        _currentTool = tool;
        UpdateCursor();
        UpdateToolbarState();
        UpdateStatusBar();
    }

    private void UpdateToolbarState()
    {
        // Update button states with modern styling
        var buttons = new[] { selectButton, btnRectangleTool, btnHighlightTool, lineButton, textButton };
        var tools = new[] { AnnotationTool.Select, AnnotationTool.Rectangle, AnnotationTool.Highlight, AnnotationTool.Line, AnnotationTool.Text };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                bool isSelected = _currentTool == tools[i];
                buttons[i].Checked = isSelected;
                buttons[i].BackColor = isSelected ? CurrentTheme.Primary : CurrentTheme.Surface;
                buttons[i].ForeColor = isSelected ? Color.White : CurrentTheme.Text;
            }
        }

        // Update tooltips with enhanced information
        if (selectButton != null) selectButton.ToolTipText = $"Select Tool{(_currentTool == AnnotationTool.Select ? " (Active)" : "")} (S)";
        if (btnRectangleTool != null) btnRectangleTool.ToolTipText = $"Rectangle Tool{(_currentTool == AnnotationTool.Rectangle ? " (Active)" : "")} (R)";
        if (btnHighlightTool != null) btnHighlightTool.ToolTipText = $"Highlight Tool{(_currentTool == AnnotationTool.Highlight ? " (Active)" : "")} (H)";
        if (lineButton != null) lineButton.ToolTipText = $"Line Tool{(_currentTool == AnnotationTool.Line ? " (Active)" : "")} (L)";
        if (textButton != null) textButton.ToolTipText = $"Text Tool{(_currentTool == AnnotationTool.Text ? " (Active)" : "")} (T)";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    private async Task PerformOcr()
    {
        if (_currentImage == null)
        {
            ShowModernMessage("Please capture or open an image first.", "No Image", MessageBoxIcon.Warning);
            return;
        }

        try
        {
            statusLabel.Text = "Preparing OCR...";
            var originalCursor = Cursor;
            Cursor = Cursors.Cross;

            Point startPoint = Point.Empty;
            Rectangle selectionRect = Rectangle.Empty;
            bool isSelecting = false;

            var tempImage = new Bitmap(_currentImage);
            pictureBox.Image = tempImage;

            var tcs = new TaskCompletionSource<bool>();

            MouseEventHandler mouseDownHandler = null;
            MouseEventHandler mouseMoveHandler = null;
            MouseEventHandler mouseUpHandler = null;

            mouseDownHandler = (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isSelecting = true;
                    startPoint = e.Location;
                }
            };

            mouseMoveHandler = (s, e) =>
            {
                if (isSelecting)
                {
                    using (var g = Graphics.FromImage(tempImage))
                    {
                        g.DrawImage(_currentImage, 0, 0);

                        selectionRect = new Rectangle(
                            Math.Min(startPoint.X, e.X),
                            Math.Min(startPoint.Y, e.Y),
                            Math.Abs(e.X - startPoint.X),
                            Math.Abs(e.Y - startPoint.Y)
                        );

                        using (Pen pen = new Pen(CurrentTheme.Primary, 2))
                        {
                            pen.DashStyle = DashStyle.Dash;
                            g.DrawRectangle(pen, selectionRect);
                        }
                    }
                    pictureBox.Refresh();
                }
            };

            mouseUpHandler = async (s, e) =>
            {
                if (e.Button == MouseButtons.Left && isSelecting)
                {
                    isSelecting = false;

                    pictureBox.MouseDown -= mouseDownHandler;
                    pictureBox.MouseMove -= mouseMoveHandler;
                    pictureBox.MouseUp -= mouseUpHandler;

                    pictureBox.Image = _currentImage;
                    Cursor = originalCursor;

                    if (selectionRect.Width > 10 && selectionRect.Height > 10)
                    {
                        try
                        {
                            statusLabel.Text = "Processing OCR...";
                            using var selectedRegion = new Bitmap(selectionRect.Width, selectionRect.Height);
                            using (var g = Graphics.FromImage(selectedRegion))
                            {
                                g.DrawImage(_currentImage,
                                    new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                                    selectionRect,
                                    GraphicsUnit.Pixel);
                            }

                            var ocrService = new TesseractOcrService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"));
                            string result = await ocrService.ExtractTextAsync(selectedRegion);

                            if (string.IsNullOrWhiteSpace(result))
                            {
                                ShowModernMessage("No text was detected in the selected area.", "OCR Result", MessageBoxIcon.Information);
                            }
                            else
                            {
                                var resultForm = new ModernOcrResultForm();
                                resultForm.SetText(result);
                                resultForm.ShowDialog();
                            }
                            statusLabel.Text = "OCR completed";
                        }
                        catch (Exception ex)
                        {
                            ShowModernMessage($"OCR failed: {ex.Message}", "Error", MessageBoxIcon.Error);
                            statusLabel.Text = "OCR failed";
                        }
                    }
                    tcs.SetResult(true);
                }
            };

            pictureBox.MouseDown += mouseDownHandler;
            pictureBox.MouseMove += mouseMoveHandler;
            pictureBox.MouseUp += mouseUpHandler;

            await tcs.Task;
        }
        catch (Exception ex)
        {
            ShowModernMessage($"OCR failed: {ex.Message}", "Error", MessageBoxIcon.Error);
            statusLabel.Text = "Ready";
        }
    }

    private void ShowModernMessage(string message, string title, MessageBoxIcon icon)
    {
        // Use standard MessageBox for now, but with consistent styling
        MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
    }

    private void ClearAllAnnotations()
    {
        _annotationService.ClearAnnotations();
        if (pictureBox.Image != null)
        {
            pictureBox.Invalidate();
        }
        statusLabel.Text = "All annotations cleared";
        UpdateStatusBar();
    }

    private void Panel_Resize(object? sender, EventArgs e)
    {
        CenterPictureBox();
        ResizeAnnotations();
        // Fixed: Removed calls that cause blurriness and annotation jumping
        // Only center the picture box, don't resize or redraw the image
    }

    private void CopyScreenshotToClipboard()
    {
        if (_currentImage != null)
        {
            Clipboard.SetImage(_currentImage);
        }
    }

    private async void OcrButton_Click(object sender, EventArgs e)
    {
        if (_currentImage == null)
        {
            ShowModernMessage("Please open an image first.", "No Image", MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var ocrService = new TesseractOcrService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"));
            string result = await ocrService.ExtractTextAsync(new Bitmap(_currentImage));

            var resultForm = new ModernOcrResultForm();
            resultForm.SetText(result);
            resultForm.ShowDialog(this);
        }
        catch (Exception ex)
        {
            ShowModernMessage($"OCR failed: {ex.Message}", "Error", MessageBoxIcon.Error);
        }
    }

    private void CenterPictureBox()
    {
        if (pictureBox.Image == null) return;

        int x = Math.Max(0, (panel.ClientSize.Width - pictureBox.Width) / 2);
        int y = Math.Max(0, (panel.ClientSize.Height - pictureBox.Height) / 2);

        pictureBox.Location = new Point(x, y);

        if (pictureBox.Width > panel.ClientSize.Width)
        {
            panel.HorizontalScroll.Value = Math.Max(0, (pictureBox.Width - panel.ClientSize.Width) / 2);
        }
        if (pictureBox.Height > panel.ClientSize.Height)
        {
            panel.VerticalScroll.Value = Math.Max(0, (pictureBox.Height - panel.ClientSize.Height) / 2);
        }

        panel.AutoScrollPosition = new Point(
            panel.HorizontalScroll.Value,
            panel.VerticalScroll.Value
        );
    }

    private ToolStripButton CreateToolStripButton(string text)
    {
        return new ToolStripButton
        {
            Text = text,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Font = new Font("Segoe UI", 8, FontStyle.Regular),
            Padding = new Padding(8, 0, 8, 0),
            AutoSize = true,
            ForeColor = CurrentTheme.Text,
            BackColor = Color.Transparent,
        };
    }

    private void PrintScreenInfo()
    {
        foreach (Screen screen in Screen.AllScreens)
        {
            System.Diagnostics.Debug.WriteLine($"\nScreen: {screen.DeviceName}");
            System.Diagnostics.Debug.WriteLine($"Primary: {screen.Primary}");
            System.Diagnostics.Debug.WriteLine($"Bounds: X={screen.Bounds.X}, Y={screen.Bounds.Y}, Width={screen.Bounds.Width}, Height={screen.Bounds.Height}");
            System.Diagnostics.Debug.WriteLine($"Working Area: X={screen.WorkingArea.X}, Y={screen.WorkingArea.Y}, Width={screen.WorkingArea.Width}, Height={screen.WorkingArea.Height}");
        }
    }

    private async Task StartOcrCapture()
    {
        var screenshot = CaptureScreen();
        using var selectionForm = new SelectionOverlayForm(screenshot);

        if (selectionForm.ShowDialog() == DialogResult.OK)
        {
            var bounds = selectionForm.SelectionBounds;
            using var selectedArea = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(selectedArea))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                string extractedText = await _ocrService.ExtractTextAsync(selectedArea);

                var resultForm = new ModernOcrResultForm();
                resultForm.SetText(extractedText);
                resultForm.Show(this);
            }
            catch (Exception ex)
            {
                ShowModernMessage($"OCR Error: {ex.Message}", "Error", MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    private async void StartScreenCapture(object? sender, EventArgs e)
    {
        PrintScreenInfo();
        this.WindowState = FormWindowState.Minimized;
        await Task.Delay(200);

        try
        {
            statusLabel.Text = "Capturing screen...";
            var captureService = new ScreenCaptureService();
            var screenshot = captureService.CaptureScreen();

            using var overlay = new SelectionOverlayForm(screenshot);
            if (overlay.ShowDialog() == DialogResult.OK)
            {
                var region = overlay.SelectionBounds;
                System.Diagnostics.Debug.WriteLine($"Selected region: {region}");

                var capturedImage = captureService.CaptureRegion(region);

                _currentImage?.Dispose();
                _currentImage = capturedImage;

                _annotationService.ClearAnnotations();

                pictureBox.Invoke(() =>
                {
                    pictureBox.Image = _currentImage;
                    SetWindowAndImageSize(capturedImage);
                });

                CopyScreenshotWithAnnotationsToClipboard();
                statusLabel.Text = "Screen captured successfully";
                UpdateStatusBar();
            }
            else
            {
                statusLabel.Text = "Screen capture cancelled";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot error: {ex.Message}");
            ShowModernMessage($"Error capturing screenshot: {ex.Message}", "Error", MessageBoxIcon.Error);
            statusLabel.Text = "Screen capture failed";
        }
        finally
        {
            this.WindowState = FormWindowState.Normal;
        }
    }

    private void CopyScreenshotWithAnnotationsToClipboard()
    {
        if (_currentImage == null) return;

        try
        {
            using var bitmap = new Bitmap(_currentImage.Width, _currentImage.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(_currentImage, Point.Empty);
                foreach (var annotation in _annotationService.Annotations)
                {
                    annotation.Draw(g);
                }
            }
            Clipboard.SetImage(bitmap);
            statusLabel.Text = "Copied to clipboard";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CopyScreenshotWithAnnotationsToClipboard: {ex.Message}");
            statusLabel.Text = "Failed to copy to clipboard";
        }
    }

    private void SetWindowAndImageSize(Image image)
    {
        if (image == null) return;

        SuspendLayout();
        panel.SuspendLayout();

        try
        {
            // Fixed: Store original dimensions and mark annotations as initialized
            _originalImageWidth = image.Width;
            _originalImageHeight = image.Height;
            _annotationsInitialized = true;

            int toolStripItemsWidth = toolStrip.Items.Cast<ToolStripItem>().Sum(item => item.Width);
            int minWidth = Math.Max(800, toolStripItemsWidth + 40);
            int minHeight = Math.Max(600, toolStrip.Height + 100);

            // Fixed: Get screen working area to avoid taskbar
            var screenBounds = Screen.FromControl(this).WorkingArea;
            int maxWidth = (int)(screenBounds.Width * 0.9f);  // Leave some margin
            int maxHeight = (int)(screenBounds.Height * 0.9f);

            // Calculate target size while maintaining aspect ratio
            float imageAspect = (float)image.Width / image.Height;
            int targetWidth = Math.Min(maxWidth, image.Width);
            int targetHeight = Math.Min(maxHeight, image.Height);

            // Adjust to maintain aspect ratio
            if (targetWidth / imageAspect > targetHeight)
            {
                targetWidth = (int)(targetHeight * imageAspect);
            }
            else
            {
                targetHeight = (int)(targetWidth / imageAspect);
            }

            MinimumSize = new Size(minWidth, minHeight);
            ClientSize = new Size(
                Math.Max(minWidth, targetWidth + SystemInformation.VerticalScrollBarWidth + 32),
                Math.Max(minHeight, targetHeight + toolStrip.Height + SystemInformation.HorizontalScrollBarHeight + statusStrip.Height + 32)
            );

            // Fixed: Always set to AutoSize to maintain 1:1 pixel ratio and prevent blur
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.Size = image.Size;
            pictureBox.Image = image;
            
            CenterToScreen();
            CenterPictureBox();
        }
        finally
        {
            panel.ResumeLayout(true);
            ResumeLayout(true);
        }
    }

    private void ResizePictureBox()
    {
        // Fixed: Simplified to maintain crisp 1:1 pixel ratio
        if (pictureBox == null || _currentImage == null)
        {
            return;
        }

        // Always keep the image at 1:1 pixel ratio to maintain crisp quality
        pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
        pictureBox.Size = _currentImage.Size;
        
        // Only assign the image if it's not already assigned to prevent unnecessary redraws
        if (pictureBox.Image != _currentImage)
        {
            pictureBox.Image = _currentImage;
        }
    }

    private void OpenImage(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentImage?.Dispose();
            _currentImage = new Bitmap(dialog.FileName);
            pictureBox.Image = _currentImage;
            _annotationService.ClearAnnotations();
            UpdateStatusBar();
        }
    }

    private Bitmap CaptureScreen()
    {
        var bounds = Screen.PrimaryScreen.Bounds;
        var screenshot = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(screenshot))
        {
            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
        }
        return screenshot;
    }

    private void RotateImage(object? sender, EventArgs e)
    {
        if (_currentImage == null) return;

        SuspendLayout();
        panel.SuspendLayout();

        try
        {
            var rotated = new Bitmap(_currentImage.Height, _currentImage.Width);

            using (Graphics g = Graphics.FromImage(rotated))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.TranslateTransform((float)rotated.Width / 2, (float)rotated.Height / 2);
                g.RotateTransform(90);
                g.TranslateTransform(-(float)_currentImage.Width / 2, -(float)_currentImage.Height / 2);
                g.DrawImage(_currentImage, Point.Empty);
            }

            _currentImage.Dispose();
            _currentImage = rotated;

            // Fixed: Update original dimensions after rotation
            _originalImageWidth = _currentImage.Width;
            _originalImageHeight = _currentImage.Height;

            pictureBox.Image = _currentImage;
            pictureBox.Size = _currentImage.Size;

            panel.AutoScrollPosition = Point.Empty;
            CenterPictureBox();
            CopyScreenshotWithAnnotationsToClipboard();
            statusLabel.Text = "Image rotated";
        }
        finally
        {
            panel.ResumeLayout(true);
            ResumeLayout(true);
        }
    }

    private void SaveImage(object? sender, EventArgs e)
    {
        if (_currentImage == null) return;

        using var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            DefaultExt = "png"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var bitmap = new Bitmap(_currentImage.Width, _currentImage.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(_currentImage, Point.Empty);
                foreach (var annotation in _annotationService.Annotations)
                {
                    annotation.Draw(g);
                }
            }
            bitmap.Save(dialog.FileName, ImageFormat.Png);
            statusLabel.Text = "Image saved successfully";
        }
    }

    private void ChangeColor(object? sender, EventArgs e)
    {
        using var dialog = new ModernColorPickerForm(_currentColor);
        dialog.StartPosition = FormStartPosition.CenterParent;

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentColor = dialog.SelectedColor;
            UpdateColorButtonStates();
            UpdateStatusBar();
        }
    }

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_currentImage == null) return;

        foreach (var annotation in _annotationService.Annotations)
        {
            annotation.Draw(e.Graphics);
        }

        if (_isDrawing && _currentAnnotation != null)
        {
            _currentAnnotation.Draw(e.Graphics);
        }
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_currentImage == null) return;

        if (e.Button == MouseButtons.Left)
        {
            _drawStart = e.Location;
            _lineStart = e.Location;

            if (_currentTool == AnnotationTool.Select)
            {
                var clickedAnnotation = _annotationService.Annotations
                    .FirstOrDefault(a => a.HitTest(e.Location));

                if (clickedAnnotation != null)
                {
                    _dragStart = e.Location;
                    _lastMousePosition = e.Location;
                    _draggedAnnotation = clickedAnnotation;
                    _dragStartPosition = null;
                    _annotationService.SelectAnnotation(clickedAnnotation);
                }
                else
                {
                    _annotationService.SelectAnnotation(null);
                }
            }
            else if (_currentTool == AnnotationTool.Text)
            {
                if (_textBox == null)
                {
                    CreateTextBox(e.Location);
                }
            }
            else
            {
                _annotationService.SelectAnnotation(null);
            }

            pictureBox.Invalidate();
        }
    }

    private void PictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentTool == AnnotationTool.Select && e.Button == MouseButtons.Left && _draggedAnnotation != null)
        {
            int deltaX = e.Location.X - _lastMousePosition.X;
            int deltaY = e.Location.Y - _lastMousePosition.Y;

            if (!_dragStartPosition.HasValue)
            {
                _dragStartPosition = _draggedAnnotation.Bounds.Location;
            }

            _draggedAnnotation.Move(deltaX, deltaY);
            _lastMousePosition = e.Location;
            pictureBox.Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left && _drawStart.HasValue)
        {
            if (_currentAnnotation != null)
            {
                _annotationService.RemovePreviewAnnotation(_currentAnnotation);
            }

            _currentAnnotation = _currentTool switch
            {
                AnnotationTool.Rectangle => new RectangleAnnotation(GetRectangle(_drawStart.Value, e.Location), _currentColor, 1.0f),
                AnnotationTool.Highlight => new HighlightAnnotation(GetRectangle(_drawStart.Value, e.Location), _currentColor, 0.4f),
                AnnotationTool.Line => new LineAnnotation(_lineStart!.Value, e.Location, _currentColor),
                _ => null
            };

            if (_currentAnnotation != null)
            {
                _annotationService.AddPreviewAnnotation(_currentAnnotation);
            }

            pictureBox.Invalidate();
        }
    }

    private void PictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (_currentTool == AnnotationTool.Select && _draggedAnnotation != null)
        {
            if (_dragStartPosition.HasValue)
            {
                _annotationService.MoveAnnotation(_draggedAnnotation, _dragStartPosition.Value, _draggedAnnotation.Bounds.Location);
            }

            _dragStart = null;
            _draggedAnnotation = null;
            pictureBox.Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left && _drawStart.HasValue)
        {
            if (_currentTool != AnnotationTool.Text)
            {
                var rectangle = GetRectangle(_drawStart.Value, e.Location);

                if (rectangle.Width > 1 || rectangle.Height > 1)
                {
                    IAnnotation? annotation = _currentTool switch
                    {
                        AnnotationTool.Rectangle => new RectangleAnnotation(rectangle, _currentColor, 1.0f),
                        AnnotationTool.Highlight => new HighlightAnnotation(rectangle, _currentColor, 0.4f),
                        AnnotationTool.Line => new LineAnnotation(_lineStart!.Value, e.Location, _currentColor),
                        _ => null
                    };

                    if (annotation != null)
                    {
                        if (_currentAnnotation != null)
                        {
                            _annotationService.RemovePreviewAnnotation(_currentAnnotation);
                        }

                        _annotationService.AddAnnotation(annotation);
                        _annotationService.SelectAnnotation(annotation);
                        CopyScreenshotWithAnnotationsToClipboard();
                        UpdateStatusBar();
                    }
                }
            }

            _drawStart = null;
            _currentAnnotation = null;
            pictureBox.Invalidate();
        }
    }

    private static Rectangle GetRectangle(Point start, Point end)
    {
        return new Rectangle(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y)
        );
    }

    private void CreateTextBox(Point location)
    {
        if (_currentImage == null) return;

        _textBox?.Dispose();

        _textBox = new TextBox
        {
            Location = location,
            BackColor = CurrentTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12),
            ForeColor = _currentColor,
            Width = 200,
            Height = 24,
            Multiline = true,
            MaxLength = 500
        };

        var tooltip = new ToolTip();
        tooltip.SetToolTip(_textBox, "Enter: Confirm | Esc: Cancel | Click away: Confirm if text entered");

        _textBox.KeyDown += TextBox_KeyDown;
        _textBox.LostFocus += TextBox_LostFocus;
        _textBox.TextChanged += TextBox_TextChanged;

        pictureBox.Controls.Add(_textBox);
        _textBox.Focus();
    }

    private void TextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_textBox == null) return;

        var textSize = TextRenderer.MeasureText(_textBox.Text + "\n", _textBox.Font);
        int newHeight = Math.Max(24, Math.Min(100, textSize.Height + 10));
        int newWidth = Math.Max(200, Math.Min(400, textSize.Width + 20));

        if (_textBox.Height != newHeight || _textBox.Width != newWidth)
        {
            _textBox.Height = newHeight;
            _textBox.Width = newWidth;
        }
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            ConfirmTextAnnotation();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            if (!string.IsNullOrWhiteSpace(_textBox?.Text))
            {
                if (MessageBox.Show("Discard the text for now?", "Confirm Cancel",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CancelTextAnnotation();
                }
                else
                {
                    _textBox?.Focus();
                }
            }
            else
            {
                CancelTextAnnotation();
            }
        }
    }

    private void TextBox_LostFocus(object? sender, EventArgs e)
    {
        if (_textBox == null) return;

        if (string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CancelTextAnnotation();
        }
        else
        {
            ConfirmTextAnnotation();
        }
    }

    private void ConfirmTextAnnotation()
    {
        if (_textBox == null) return;

        try
        {
            var textBox = _textBox;
            _textBox = null;

            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                var annotation = new TextAnnotation(textBox.Location, _currentColor, textBox.Text);
                _annotationService.AddAnnotation(annotation);
                _annotationService.SelectAnnotation(annotation);
            }

            pictureBox.Controls.Remove(textBox);
            textBox.Dispose();

            pictureBox.Invalidate();

            if (_currentImage != null)
            {
                CopyScreenshotWithAnnotationsToClipboard();
            }

            SetActiveTool(AnnotationTool.Select);
            UpdateStatusBar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ConfirmTextAnnotation: {ex.Message}");
        }
    }

    private void CancelTextAnnotation()
    {
        if (_textBox == null) return;

        var textBox = _textBox;
        _textBox = null;

        pictureBox.Controls.Remove(textBox);
        textBox.Dispose();
        pictureBox.Invalidate();

        SetActiveTool(AnnotationTool.Select);
    }

    private void UpdateCursor()
    {
        if (_currentTool == AnnotationTool.Text)
        {
            pictureBox.Cursor = Cursors.IBeam;
        }
        else
        {
            pictureBox.Cursor = Cursors.Default;
        }
    }

    private void FinishTextAnnotation()
    {
        if (_textBox == null) return;

        try
        {
            var textBox = _textBox;
            _textBox = null;

            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                var annotation = new TextAnnotation(textBox.Location, _currentColor, textBox.Text);
                _annotationService.AddAnnotation(annotation);
                _annotationService.SelectAnnotation(annotation);
            }

            pictureBox.Controls.Remove(textBox);
            textBox.Dispose();

            pictureBox.Invalidate();

            if (_currentImage != null)
            {
                CopyScreenshotWithAnnotationsToClipboard();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in FinishTextAnnotation: {ex.Message}");
        }
    }

    private void CancelTextInput()
    {
        if (_textBox != null)
        {
            pictureBox.Controls.Remove(_textBox);
            _textBox.Dispose();
            _textBox = null;
            pictureBox.Invalidate();
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        UpdateToolbarState();
        UpdateStatusBar();
    }

    private void ResizeAnnotations()
    {
        // Fixed: Only resize annotations when we actually have a valid original size
        // and this is not the first initialization
        if (!_annotationsInitialized || _originalImageWidth <= 0 || _originalImageHeight <= 0)
            return;

        float widthScale = (float)pictureBox.Width / _originalImageWidth;
        float heightScale = (float)pictureBox.Height / _originalImageHeight;

        // Only resize if the scale has actually changed significantly
        if (Math.Abs(widthScale - 1.0f) > 0.01f || Math.Abs(heightScale - 1.0f) > 0.01f)
        {
            foreach (var annotation in _annotationService.Annotations)
            {
                annotation.Resize(widthScale, heightScale);
            }
            pictureBox.Invalidate();
        }
    }

    // Modern Color Picker Form
    public class ModernColorPickerForm : Form
    {
        private Color selectedColor;
        private readonly int wheelSize = 220;

        public Color SelectedColor => selectedColor;

        public ModernColorPickerForm(Color initialColor)
        {
            selectedColor = initialColor;
            InitializeModernColorPicker();
        }

        private void InitializeModernColorPicker()
        {
            this.Text = "Color Picker";
            this.Size = new Size(350, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9F);

            var colorWheel = new Panel
            {
                Size = new Size(wheelSize, wheelSize),
                Location = new Point(65, 60)
            };

            colorWheel.Paint += ColorWheel_Paint;
            colorWheel.MouseDown += ColorWheel_MouseDown;
            colorWheel.MouseMove += ColorWheel_MouseMove;

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(140, 370),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(230, 370),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(225, 225, 225),
                ForeColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };

            this.Controls.AddRange(new Control[] { colorWheel, okButton, cancelButton });
        }

        private void ColorWheel_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_wheelBitmap == null || _wheelBitmap.Size != new Size(wheelSize, wheelSize))
            {
                _wheelBitmap?.Dispose();
                _wheelBitmap = new Bitmap(wheelSize, wheelSize);
                using (var g = Graphics.FromImage(_wheelBitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    for (int x = 0; x < wheelSize; x++)
                    {
                        for (int y = 0; y < wheelSize; y++)
                        {
                            double dx = (x - wheelSize / 2.0) / (wheelSize / 2.0);
                            double dy = (y - wheelSize / 2.0) / (wheelSize / 2.0);
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance <= 1)
                            {
                                double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                                if (angle < 0) angle += 360;

                                var color = ColorFromHSV(angle, distance, 1.0);
                                _wheelBitmap.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }

            e.Graphics.DrawImage(_wheelBitmap, 0, 0);

            using (var pen = new Pen(Color.White, 3))
            {
                e.Graphics.DrawEllipse(pen, wheelSize / 2 - 18, wheelSize / 2 - 18, 36, 36);
            }
            using (var brush = new SolidBrush(selectedColor))
            {
                e.Graphics.FillEllipse(brush, wheelSize / 2 - 15, wheelSize / 2 - 15, 30, 30);
            }
        }

        private Bitmap? _wheelBitmap;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wheelBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ColorWheel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is Control control)
            {
                SelectColorFromPoint(e.Location, control);
            }
        }

        private void ColorWheel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && sender is Control control)
            {
                SelectColorFromPoint(e.Location, control);
            }
        }

        private void SelectColorFromPoint(Point location, Control sourceControl)
        {
            var center = new Point(wheelSize / 2, wheelSize / 2);
            var dx = location.X - center.X;
            var dy = location.Y - center.Y;

            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            if (angle < 0) angle += 360;

            var distance = Math.Sqrt(dx * dx + dy * dy);
            var saturation = Math.Min(distance / (wheelSize / 2), 1.0);

            selectedColor = ColorFromHSV(angle, saturation, 1.0);
            sourceControl.Invalidate();
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }

    // Modern OCR Result Form
    public class ModernOcrResultForm : Form
    {
        private TextBox textBoxResult;
        private Button buttonCopy;

        public ModernOcrResultForm()
        {
            InitializeModernUI();
        }

        private void InitializeModernUI()
        {
            BackColor = Color.FromArgb(248, 249, 250);
            Size = new Size(500, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "OCR Result";
            Font = new Font("Segoe UI", 9F);

            textBoxResult = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Margin = new Padding(16),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            buttonCopy = new Button
            {
                Text = "Copy to Clipboard",
                Dock = DockStyle.Bottom,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            buttonCopy.Click += ButtonCopy_Click;

            Controls.Add(textBoxResult);
            Controls.Add(buttonCopy);
        }

        private void ButtonCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxResult.Text))
            {
                Clipboard.SetText(textBoxResult.Text);
                MessageBox.Show("Text copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void SetText(string text)
        {
            textBoxResult.Text = text;
        }
    }
}