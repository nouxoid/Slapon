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

    private ToolStripButton btnRectangleTool;
    private ToolStripButton btnHighlightTool;
    private ToolStripButton lineButton;
    private ToolStripButton textButton;
    // Add this with your other button declarations
    private ToolStripButton selectButton;
    private ToolStrip toolStrip;


    public MainForm()
    {
        InitializeComponent();
        this.BackColor = Color.White;
        _annotationService = new AnnotationService();
        _annotationFactory = new AnnotationFactory();
        _screenCaptureService = new ScreenCaptureService();
        _annotationService.AnnotationsChanged += (s, e) =>
        {
            pictureBox.Invalidate();
            
        };
        SetupUI();

        // Automatically start screen capture on startup
        StartScreenCapture(null, null);
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
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private void SetupUI()
    {
        // PictureBox setup
        pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Dock = DockStyle.None
        };

        // Enable double buffering for PictureBox
        typeof(PictureBox).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, pictureBox, new object[] { true });

        // Panel setup
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16),
        };

        // Enable double buffering for Panel
        typeof(Panel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, panel, new object[] { true });

        panel.Resize += Panel_Resize;
        panel.Controls.Add(pictureBox);

        // First create all the tool buttons
        btnRectangleTool = CreateModernButton("", Resources.rectangle, (s, e) => SetActiveTool(AnnotationTool.Rectangle));
        btnHighlightTool = CreateModernButton("", Resources.highlighter, (s, e) => SetActiveTool(AnnotationTool.Highlight));
        lineButton = CreateModernButton("", Resources.line, (s, e) => SetActiveTool(AnnotationTool.Line));
        textButton = CreateModernButton("", Resources.text, (s, e) => SetActiveTool(AnnotationTool.Text));
        selectButton = CreateModernButton("", Resources.select, (s, e) => SetActiveTool(AnnotationTool.Select));

        var colorPickerButton = new ToolStripButton
        {
            Image = Resources.colorIcon,
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Text = string.Empty, // Ensure text is empty
            TextImageRelation = TextImageRelation.ImageBeforeText // Image before text
        };

        colorPickerButton.Click += ChangeColor;

        

        // Toolbar setup
        toolStrip = new ToolStrip
        {
            Renderer = new CustomToolStripRenderer(),
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.White,
            ForeColor = Color.White,
            Padding = new Padding(8, 2, 8, 2),
            Height = 48,
            Dock = DockStyle.Top
        };

        // Create all buttons
        var leftGroup = new List<ToolStripItem>
    {
        CreateModernButton("New Capture", Resources.newcapture, StartScreenCapture)
    };

        var centerGroup = new List<ToolStripItem>
    {
        btnRectangleTool,
        btnHighlightTool,
        lineButton,
        textButton,
        selectButton,
        new ToolStripSeparator(),
        colorPickerButton
    };

        // Create color buttons
        var commonColors = new[]
        {
        Color.FromArgb(255, 51, 51),   // Red
        Color.FromArgb(51, 255, 51),   // Green
        Color.FromArgb(51, 51, 255),   // Blue
        Color.FromArgb(255, 255, 51),  // Yellow
        Color.FromArgb(255, 51, 255),  // Pink
    };

        foreach (var color in commonColors)
        {
            var colorButton = CreateColorButton(color);
            colorButton.Tag = "color";
            centerGroup.Add(colorButton);
        }

        // Add color picker and clear button to center group
        centerGroup.Add(new ToolStripSeparator());
        centerGroup.Add(CreateModernButton("", Resources.clearall, (s, e) => ClearAllAnnotations()));

        // Create right-aligned buttons
        var rightGroup = new List<ToolStripItem>
    {
        CreateModernButton("Copy", null, (s, e) => CopyScreenshotWithAnnotationsToClipboard()),
        CreateModernButton("Save", null, SaveImage)
    };

        // Create springs
        var leftSpring = new ToolStripSeparator
        {
            AutoSize = true,
            Margin = new Padding(0),
            Alignment = ToolStripItemAlignment.Left
        };

        var rightSpring = new ToolStripSeparator
        {
            AutoSize = true,
            Margin = new Padding(0),
            Alignment = ToolStripItemAlignment.Right
        };

        // Set alignments
        foreach (var item in centerGroup)
        {
            item.Alignment = ToolStripItemAlignment.Left;
        }

        foreach (var item in rightGroup)
        {
            item.Alignment = ToolStripItemAlignment.Right;
        }

        // Add all items to toolbar in correct order
        toolStrip.Items.AddRange(leftGroup.ToArray());
        toolStrip.Items.Add(leftSpring);
        toolStrip.Items.AddRange(centerGroup.ToArray());
        toolStrip.Items.Add(rightSpring);
        toolStrip.Items.AddRange(rightGroup.ToArray());

        Controls.Add(panel);
        Controls.Add(toolStrip);

        // PictureBox event handlers
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
    }

    private class ColorPalette
    {
        public static readonly Color[] CommonColors = new[]
        {
        Color.FromArgb(255, 51, 51),   // Red
        Color.FromArgb(51, 255, 51),   // Green
        Color.FromArgb(51, 51, 255),   // Blue
        Color.FromArgb(255, 255, 51),  // Yellow
        Color.FromArgb(255, 51, 255),  // Pink
    };
    }

    private ToolStripButton CreateColorButton(Color color)
    {
        var button = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.None,
            AutoSize = false,
            Size = new Size(24, 24),
            Margin = new Padding(2),
            BackColor = Color.Transparent,
            Tag = "color"
        };

        button.Paint += (s, e) =>
        {
            if (s is ToolStripButton btn)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var circleRect = new Rectangle(4, 4, btn.Width - 8, btn.Height - 8);

                // Draw the color circle
                using (var brush = new SolidBrush(color))
                {
                    e.Graphics.FillEllipse(brush, circleRect);
                }

                // Draw selection indicator if this is the current color
                if (_currentColor == color)
                {
                    using var pen = new Pen(Color.White, 2);
                    e.Graphics.DrawEllipse(pen, circleRect);

                    // Draw outer ring
                    using var outerPen = new Pen(Color.FromArgb(100, 100, 100), 1);
                    e.Graphics.DrawEllipse(outerPen, circleRect);
                }
            }
        };

        button.Click += (s, e) =>
        {
            _currentColor = color;
            UpdateColorButtonStates();
        };

        return button;
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

    // Update CreateModernButton method
    private ToolStripButton CreateModernButton(string text, Image? icon, EventHandler clickHandler)
    {
        var button = new ToolStripButton
        {
            Text = text,
            DisplayStyle = icon != null && text != "" ? ToolStripItemDisplayStyle.ImageAndText :
                          icon != null ? ToolStripItemDisplayStyle.Image :
                          ToolStripItemDisplayStyle.Text,
            AutoSize = true,
            Margin = new Padding(2), // Slightly increased margin
            Padding = new Padding(8, 6, 8, 6), // Increased padding for wider buttons
            ForeColor = Color.FromArgb(50, 50, 50), // Darker text color
            Width = 40 // Minimum width for the button
        };

        if (icon != null)
        {
            var size = new Size(24, 24); // Slightly larger icons
            var resizedImage = new Bitmap(icon, size);
            button.Image = resizedImage;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageScaling = ToolStripItemImageScaling.None;
            button.ImageTransparentColor = Color.Transparent;
        }
        else
        {
            button.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        }

        button.Click += clickHandler;
        return button;
    }

    private void ClearAllAnnotations()
    {
        _annotationService.ClearAnnotations();
        if (pictureBox.Image != null)
        {
            pictureBox.Invalidate();
        }
    }

    // Custom renderer for modern look
    private class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        public CustomToolStripRenderer() : base(new CustomColorTable())
        {
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Don't render borders
        }
    }

    private class CustomColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Color.FromArgb(240, 240, 240);
        public override Color ToolStripGradientMiddle => Color.FromArgb(240, 240, 240);
        public override Color ToolStripGradientEnd => Color.FromArgb(240, 240, 240);
        public override Color ButtonSelectedBorder => Color.FromArgb(200, 200, 200);
        public override Color ButtonSelectedHighlight => Color.FromArgb(220, 220, 220);
        public override Color ButtonSelectedHighlightBorder => Color.FromArgb(200, 200, 200);
        public override Color ButtonPressedBorder => Color.FromArgb(180, 180, 180);
        public override Color ButtonPressedHighlight => Color.FromArgb(200, 200, 200);
        public override Color ButtonPressedHighlightBorder => Color.FromArgb(180, 180, 180);
        public override Color GripLight => Color.FromArgb(240, 240, 240);
        public override Color GripDark => Color.FromArgb(240, 240, 240);
        public override Color OverflowButtonGradientBegin => Color.FromArgb(240, 240, 240);
        public override Color OverflowButtonGradientEnd => Color.FromArgb(240, 240, 240);
        public override Color OverflowButtonGradientMiddle => Color.FromArgb(240, 240, 240);
    }

    private void SetActiveTool(AnnotationTool tool)
    {
        _currentTool = tool;
        UpdateCursor();

        // Update UI to reflect current tool (you'll need to implement this)
        UpdateToolbarState();
    }

    private void UpdateToolbarState()
    {
        // Update button states
        selectButton.Checked = (_currentTool == AnnotationTool.Select);
        btnRectangleTool.Checked = (_currentTool == AnnotationTool.Rectangle);
        btnHighlightTool.Checked = (_currentTool == AnnotationTool.Highlight);
        lineButton.Checked = (_currentTool == AnnotationTool.Line);
        textButton.Checked = (_currentTool == AnnotationTool.Text);

        // Update button backgrounds
        selectButton.BackColor = (_currentTool == AnnotationTool.Select) ? Color.LightBlue : SystemColors.Control;
        btnRectangleTool.BackColor = (_currentTool == AnnotationTool.Rectangle) ? Color.LightBlue : SystemColors.Control;
        btnHighlightTool.BackColor = (_currentTool == AnnotationTool.Highlight) ? Color.LightBlue : SystemColors.Control;
        lineButton.BackColor = (_currentTool == AnnotationTool.Line) ? Color.LightBlue : SystemColors.Control;
        textButton.BackColor = (_currentTool == AnnotationTool.Text) ? Color.LightBlue : SystemColors.Control;

        // Set the background color of the annotation buttons to white
        btnRectangleTool.BackColor = Color.White;
        btnHighlightTool.BackColor = Color.White;
        lineButton.BackColor = Color.White;
        textButton.BackColor = Color.White;
        selectButton.BackColor = Color.White;

        // Update tooltips
        selectButton.ToolTipText = _currentTool == AnnotationTool.Select ? "Select Tool (Selected)" : "Select Tool";
        btnRectangleTool.ToolTipText = _currentTool == AnnotationTool.Rectangle ? "Rectangle Tool (Selected)" : "Rectangle Tool";
        btnHighlightTool.ToolTipText = _currentTool == AnnotationTool.Highlight ? "Highlight Tool (Selected)" : "Highlight Tool";
        lineButton.ToolTipText = _currentTool == AnnotationTool.Line ? "Line Tool (Selected)" : "Line Tool";
        textButton.ToolTipText = _currentTool == AnnotationTool.Text ? "Text Tool (Selected)" : "Text Tool";
    }

    

   

    private void Panel_Resize(object? sender, EventArgs e)
    {
        CenterPictureBox();
    }

    private void CopyScreenshotToClipboard()
    {
        if (_currentImage != null)
        {
            Clipboard.SetImage(_currentImage);
        }
    }

    private void CenterPictureBox()
    {
        if (pictureBox.Image == null || pictureBox.Parent == null) return;

        var panel = (Panel)pictureBox.Parent;

        // Calculate center position considering both panel size and image size
        int x = Math.Max(0, (panel.ClientSize.Width - pictureBox.Width) / 2);
        int y = Math.Max(0, (panel.ClientSize.Height - pictureBox.Height) / 2);

        // If the image is smaller than the panel, center it
        // If the image is larger, start from padding
        if (pictureBox.Width < panel.ClientSize.Width)
        {
            x = (panel.ClientSize.Width - pictureBox.Width) / 2;
        }
        else
        {
            x = panel.Padding.Left;
        }

        if (pictureBox.Height < panel.ClientSize.Height)
        {
            y = (panel.ClientSize.Height - pictureBox.Height) / 2;
        }
        else
        {
            y = panel.Padding.Top;
        }

        pictureBox.Location = new Point(x, y);
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
            ForeColor = Color.FromArgb(33, 37, 41), // Dark gray text
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


    private async void StartScreenCapture(object? sender, EventArgs e)
    {
        PrintScreenInfo(); // Keeping your debug info printing
        this.WindowState = FormWindowState.Minimized;
        await Task.Delay(200);

        try
        {
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

                // Clear existing annotations
                _annotationService.ClearAnnotations();

                // Update PictureBox on the UI thread
                pictureBox.Invoke(() =>
                {
                    pictureBox.Image = _currentImage;
                    SetWindowAndImageSize(capturedImage);
                });

                CopyScreenshotWithAnnotationsToClipboard();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot error: {ex.Message}");
            MessageBox.Show($"Error capturing screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.WindowState = FormWindowState.Normal;
        }
    }

    private void CopyScreenshotWithAnnotationsToClipboard()
    {
        if (_currentImage == null) return;  // Early return if no image

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CopyScreenshotWithAnnotationsToClipboard: {ex.Message}");
            // Optionally show a message to the user
            // MessageBox.Show("Failed to copy to clipboard", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetWindowAndImageSize(Bitmap capturedImage)
    {
        // Set window size to be 80% of screen size or image size, whichever is smaller
        var screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
        var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
        var maxWidth = (int)(screenWidth * 0.8);
        var maxHeight = (int)(screenHeight * 0.8);

        var width = Math.Min(maxWidth, capturedImage.Width + 50);
        var height = Math.Min(maxHeight, capturedImage.Height + 50);

        this.ClientSize = new Size(width, height);
        this.CenterToScreen();

        // Allow layout to update
        Application.DoEvents();

        // Center the picture box after everything is set
        CenterPictureBox();
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
        }
    }

    private void ChangeColor(object? sender, EventArgs e)
    {
        using var dialog = new ColorPickerForm(_currentColor);
        dialog.StartPosition = FormStartPosition.CenterParent;

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentColor = dialog.SelectedColor;
            UpdateColorButtonStates();
        }
    }

    // Add this new form for the modern color picker
    public class ColorPickerForm : Form
    {
        private Color selectedColor;
        private readonly int wheelSize = 200;
        private readonly List<Color> recentColors = new List<Color>();

        public Color SelectedColor => selectedColor;

        public ColorPickerForm(Color initialColor)
        {
            selectedColor = initialColor;
            InitializeColorPicker();
        }

        private void InitializeColorPicker()
        {
            this.Text = "Color Picker";
            this.Size = new Size(300, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var colorWheel = new Panel
            {
                Size = new Size(wheelSize, wheelSize),
                Location = new Point(50, 50)
            };

            colorWheel.Paint += ColorWheel_Paint;
            colorWheel.MouseDown += ColorWheel_MouseDown;
            colorWheel.MouseMove += ColorWheel_MouseMove;

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(120, 320)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(200, 320)
            };

            this.Controls.AddRange(new Control[] { colorWheel, okButton, cancelButton });
        }

        private void ColorWheel_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Create bitmap for the wheel if not exists or if size changed
            if (_wheelBitmap == null || _wheelBitmap.Size != new Size(wheelSize, wheelSize))
            {
                _wheelBitmap?.Dispose();
                _wheelBitmap = new Bitmap(wheelSize, wheelSize);
                using (var g = Graphics.FromImage(_wheelBitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Draw the color wheel using a more efficient method
                    for (int x = 0; x < wheelSize; x++)
                    {
                        for (int y = 0; y < wheelSize; y++)
                        {
                            // Convert to polar coordinates
                            double dx = (x - wheelSize / 2.0) / (wheelSize / 2.0);
                            double dy = (y - wheelSize / 2.0) / (wheelSize / 2.0);
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance <= 1) // Only draw within the circle
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

            // Draw the cached wheel
            e.Graphics.DrawImage(_wheelBitmap, 0, 0);

            // Draw selected color indicator
            using (var pen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawEllipse(pen, wheelSize / 2 - 15, wheelSize / 2 - 15, 30, 30);
            }
            using (var brush = new SolidBrush(selectedColor))
            {
                e.Graphics.FillEllipse(brush, wheelSize / 2 - 14, wheelSize / 2 - 14, 28, 28);
            }
        }

        // Add this field to the ColorPickerForm class
        private Bitmap? _wheelBitmap;

        // Don't forget to dispose of the bitmap when the form closes
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wheelBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Event handlers using the method
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

        // Method definition
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
                // Try to select an annotation under the cursor
                var clickedAnnotation = _annotationService.Annotations
                    .FirstOrDefault(a => a.HitTest(e.Location));

                if (clickedAnnotation != null)
                {
                    _dragStart = e.Location;
                    _lastMousePosition = e.Location;
                    _draggedAnnotation = clickedAnnotation;
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
            // Calculate the offset from last position
            int deltaX = e.Location.X - _lastMousePosition.X;
            int deltaY = e.Location.Y - _lastMousePosition.Y;

            // Move the annotation
            _draggedAnnotation.Move(deltaX, deltaY);

            // Update last mouse position
            _lastMousePosition = e.Location;
            pictureBox.Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left && _drawStart.HasValue)
        {
            // Remove the previous preview annotation if it exists
            if (_currentAnnotation != null)
            {
                _annotationService.RemoveAnnotation(_currentAnnotation);
            }

            // Create and add the new preview annotation
            _currentAnnotation = _currentTool switch
            {
                AnnotationTool.Rectangle => new RectangleAnnotation(GetRectangle(_drawStart.Value, e.Location), _currentColor, 1.0f),
                AnnotationTool.Highlight => new HighlightAnnotation(GetRectangle(_drawStart.Value, e.Location), _currentColor, 0.4f),
                AnnotationTool.Line => new LineAnnotation(_lineStart!.Value, e.Location, _currentColor),
                _ => null
            };

            if (_currentAnnotation != null)
            {
                _annotationService.AddAnnotation(_currentAnnotation);
            }

            pictureBox.Invalidate();
        }
    }

    // In PictureBox_MouseUp method, after creating any annotation:
    private void PictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (_currentTool == AnnotationTool.Select && _draggedAnnotation != null)
        {
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

                // Only create annotation if there's some size/distance
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
                        // Remove the preview annotation
                        if (_currentAnnotation != null)
                        {
                            _annotationService.RemoveAnnotation(_currentAnnotation);
                        }

                        // Add the final annotation
                        _annotationService.AddAnnotation(annotation);
                        _annotationService.SelectAnnotation(annotation);

                        // Auto-copy to clipboard after creating annotation
                        CopyScreenshotWithAnnotationsToClipboard();
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
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Arial", 12),
            ForeColor = _currentColor,
            Width = 200,
            Height = 24,
            Multiline = true,
            MaxLength = 500
        };

        // Add tooltip to help users
        var tooltip = new ToolTip();
        tooltip.SetToolTip(_textBox, "Enter: Confirm | Esc: Cancel | Click away: Confirm if text entered");

        _textBox.KeyDown += TextBox_KeyDown;
        _textBox.LostFocus += TextBox_LostFocus;
        _textBox.TextChanged += TextBox_TextChanged;

        pictureBox.Controls.Add(_textBox);
        _textBox.Focus();
    }

    // Add this method to handle text box resizing
    private void TextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_textBox == null) return;

        // Calculate required height based on text
        var textSize = TextRenderer.MeasureText(_textBox.Text + "\n", _textBox.Font);
        int newHeight = Math.Max(24, Math.Min(100, textSize.Height + 10)); // Min 24, Max 100

        // Adjust width based on content, with minimum and maximum values
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
                // Show confirmation dialog only if there's text
                if (MessageBox.Show("Discard the text for now?", "Confirm Cancel",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CancelTextAnnotation();
                }
                else
                {
                    _textBox?.Focus(); // Return focus if user decides not to cancel
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

        // If text is empty, just cancel
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
            var textBox = _textBox;  // Store reference
            _textBox = null;  // Clear reference immediately

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

            // Reset to default tool after confirming text
            SetActiveTool(AnnotationTool.Select);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ConfirmTextAnnotation: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

        // Reset to default tool after canceling text
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
        if (_textBox == null) return;  // Early return if textbox is already disposed

        try
        {
            var textBox = _textBox;  // Store reference
            _textBox = null;  // Clear reference immediately

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
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
    }
}