namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.UI.Forms;
using Slapon.UI.Properties;

public partial class MainForm : Form
{

    private enum AnnotationTool
    {
        None,
        Rectangle,
        Highlight,
        Line,
        Text
    }

    private AnnotationTool _currentTool = AnnotationTool.None;
    private Point? _drawStart;
    private IAnnotation? _currentAnnotation;
    private readonly Color _defaultHighlightColor = Color.Yellow;

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
    public MainForm()
    {
        InitializeComponent();
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
        // PictureBox setup (keep your existing PictureBox configuration)
        pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Dock = DockStyle.None
        };

        // Enable double buffering for PictureBox
        typeof(PictureBox).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, pictureBox, new object[] { true });

        // Panel setup (keep your existing Panel configuration)
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

        // In SetupUI method, update the toolStrip initialization:
        var toolStrip = new ToolStrip
        {
            Renderer = new CustomToolStripRenderer(),
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(240, 240, 240), // Light gray background
            ForeColor = Color.FromArgb(50, 50, 50), // Darker text color
            Padding = new Padding(2), // Reduced padding
            Height = 32, // Reduced height
            Dock = DockStyle.Top
        };

        // Capture Group
        var screenshotButton = CreateModernButton("New Capture", Resources.newcapture, StartScreenCapture);

        // Annotation Group
        btnRectangleTool = CreateModernButton("", Resources.rectangle, (s, e) => SetActiveTool(AnnotationTool.Rectangle));
        btnHighlightTool = CreateModernButton("", Resources.highlighter, (s, e) => SetActiveTool(AnnotationTool.Highlight));
        var lineButton = CreateModernButton("", Resources.line, (s, e) => SetActiveTool(AnnotationTool.Line));
        var textButton = CreateModernButton("", Resources.text, (s, e) => SetActiveTool(AnnotationTool.Text));

        // Utility Group
        var colorButton = CreateModernButton("Color", null, ChangeColor);
        var clearAllButton = CreateModernButton("", Resources.clearall, (s, e) => ClearAllAnnotations());

        var expandingSeparator = new ToolStripSeparator
        {
            AutoSize = true,
            Margin = new Padding(0),
            Alignment = ToolStripItemAlignment.Right // This is key for right alignment
        };



        // Action Group (right-aligned)
        var copyButton = CreateModernButton("Copy", null, (s, e) => CopyScreenshotWithAnnotationsToClipboard());
        var saveButton = CreateModernButton("Save", null, SaveImage);

        // Configure right-aligned buttons
        copyButton.Alignment = ToolStripItemAlignment.Right;
        saveButton.Alignment = ToolStripItemAlignment.Right;

        // Add all items to toolbar with separators
        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripSeparator(),
            screenshotButton,
            new ToolStripSeparator(),
            btnRectangleTool,
            btnHighlightTool,
            lineButton,
            textButton,
            new ToolStripSeparator(),
            colorButton,
            clearAllButton,
            expandingSeparator,
            copyButton,
            saveButton
        });



        Controls.Add(panel);
        Controls.Add(toolStrip);

        // PictureBox event handlers
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
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
            Margin = new Padding(1), // Reduced margin
            Padding = new Padding(3), // Reduced padding
            ForeColor = Color.FromArgb(50, 50, 50) // Darker text color
        };

        if (icon != null)
        {
            var size = new Size(20, 20); // Slightly smaller icons
            var resizedImage = new Bitmap(icon, size);
            button.Image = resizedImage;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageScaling = ToolStripItemImageScaling.None;
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
        UpdateToolButtons();
    }

    private void UpdateToolButtons()
    {
        btnRectangleTool.BackColor = (_currentTool == AnnotationTool.Rectangle) ? Color.LightBlue : SystemColors.Control;
        btnHighlightTool.BackColor = (_currentTool == AnnotationTool.Highlight) ? Color.LightBlue : SystemColors.Control;
        // Repeat for other tools as needed
    }

    private void BtnRectangleTool_Click(object sender, EventArgs e)
    {
        SetActiveTool(AnnotationTool.Rectangle);
    }

    private void BtnHighlightTool_Click(object sender, EventArgs e)
    {
        SetActiveTool(AnnotationTool.Highlight);
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
        if (_currentImage == null) return;

        var bitmap = new Bitmap(_currentImage.Width, _currentImage.Height);
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
        using var dialog = new ColorDialog
        {
            Color = _currentColor
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentColor = dialog.Color;
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
        if (e.Button == MouseButtons.Left)
        {
            _drawStart = e.Location;

            // Clear selection when starting new annotation
            _annotationService.SelectAnnotation(null);
            pictureBox.Invalidate();
        }
    }


    private void PictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _drawStart.HasValue)
        {
            var rectangle = GetRectangle(_drawStart.Value, e.Location);

            // Remove the previous preview annotation if it exists
            if (_currentAnnotation != null)
            {
                _annotationService.RemoveAnnotation(_currentAnnotation);
            }

            // Create and add the new preview annotation with default opacity
            _currentAnnotation = _currentTool switch
            {
                AnnotationTool.Rectangle => new RectangleAnnotation(rectangle, _currentColor, 1.0f),
                AnnotationTool.Highlight => new HighlightAnnotation(rectangle, _currentColor, 0.4f),
                _ => null
            };

            if (_currentAnnotation != null)
            {
                _annotationService.AddAnnotation(_currentAnnotation);
            }

            // Invalidate the PictureBox to trigger a repaint
            pictureBox.Invalidate();
        }
    }

    private void PictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _drawStart.HasValue)
        {
            var rectangle = GetRectangle(_drawStart.Value, e.Location);

            // Only create annotation if the rectangle has some size
            if (rectangle.Width > 1 && rectangle.Height > 1)
            {
                IAnnotation? annotation = _currentTool switch
                {
                    AnnotationTool.Rectangle => new RectangleAnnotation(rectangle, _currentColor, 1.0f),
                    AnnotationTool.Highlight => new HighlightAnnotation(rectangle, _currentColor, 0.4f),
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
                }
                // Copy the screenshot with annotations to the clipboard
                CopyScreenshotWithAnnotationsToClipboard();
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


    


    private void MainForm_Load(object sender, EventArgs e)
    {
        UpdateToolButtons();
    }
}