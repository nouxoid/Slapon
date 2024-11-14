namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.UI.Forms;

public partial class MainForm : Form
{
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


    public MainForm()
    {
        InitializeComponent();
        _annotationService = new AnnotationService();
        _annotationFactory = new AnnotationFactory();
        _screenCaptureService = new ScreenCaptureService();
        _annotationService.AnnotationsChanged += (s, e) => pictureBox.Refresh();
        SetupUI();
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
        // Create and configure PictureBox with padding
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(16)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16),
        };
        panel.Controls.Add(pictureBox);

        // Create modern toolbar
        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            RenderMode = ToolStripRenderMode.Professional,
            Padding = new Padding(8),
            BackColor = Color.White,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(24, 24)
        };

        // Screenshot button
        var screenshotButton = CreateToolStripButton("Screenshot", "📷");
        screenshotButton.Click += StartScreenCapture;

        // Rectangle annotation button
        var rectangleButton = CreateToolStripButton("Rectangle", "⬜");

        // Highlighter button
        var highlighterButton = CreateToolStripButton("Highlight", "🖊️");

        // Line button
        var lineButton = CreateToolStripButton("Line", "📏");

        // Text button
        var textButton = CreateToolStripButton("Text", "T");

        // Color button
        var colorButton = CreateToolStripButton("Color", "🎨");
        colorButton.Click += ChangeColor;

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
        screenshotButton,
        new ToolStripSeparator(),
        rectangleButton,
        highlighterButton,
        lineButton,
        textButton,
        new ToolStripSeparator(),
        colorButton
        });

        Controls.Add(panel);
        Controls.Add(toolStrip);
    }

    private ToolStripButton CreateToolStripButton(string text, string symbol)
    {
        return new ToolStripButton
        {
            Text = text,
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = CreateIcon(symbol),
            TextImageRelation = TextImageRelation.ImageAboveText,
            Font = new Font("Segoe UI", 12),
            Padding = new Padding(8),
            AutoSize = true,
            ForeColor = Color.Black,
            BackColor = Color.White,
            //FlatStyle = FlatStyle.Flat
        };
    }

    private Image CreateIcon(string symbol)
    {
        var bitmap = new Bitmap(24, 24);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using (var font = new Font("Calibri", 12))
            {
                g.DrawString(symbol, font, Brushes.Black, new PointF(-4, -4));
            }
        }
        return bitmap;
    }

    private async void StartScreenCapture(object? sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
        await Task.Delay(200); // Give time for window to minimize

        var captureService = new ScreenCaptureService();
        var screenshot = captureService.CaptureScreen();

        using var overlay = new SelectionOverlayForm(screenshot);
        if (overlay.ShowDialog() == DialogResult.OK)
        {
            var region = overlay.SelectionBounds;
            var capturedImage = captureService.CaptureRegion(region);

            _currentImage?.Dispose();
            _currentImage = capturedImage;
            pictureBox.Image = _currentImage;

            // Resize form to fit image with padding
            var padding = 32; // 16px on each side
            var width = Math.Min(Screen.PrimaryScreen.WorkingArea.Width - padding, capturedImage.Width + padding);
            var height = Math.Min(Screen.PrimaryScreen.WorkingArea.Height - padding, capturedImage.Height + padding);

            this.ClientSize = new Size(width, height);
            this.CenterToScreen();
        }

        this.WindowState = FormWindowState.Normal;
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

        if (_isDrawing)
        {
            var rect = GetRectangle(_startPoint, pictureBox.PointToClient(Cursor.Position));
            using var pen = new Pen(_currentColor, 2f) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var clickPoint = new PointF(e.X, e.Y);
            var clickedAnnotation = _annotationService.GetAnnotationAt(clickPoint);

            if (clickedAnnotation != null)
            {
                _annotationService.SelectAnnotation(clickedAnnotation);
                _isDragging = true;
            }
            else
            {
                _isDrawing = true;
                _startPoint = clickPoint;
                _annotationService.SelectAnnotation(null);
            }
            pictureBox.Refresh();
        }
    }

    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDrawing)
        {
            pictureBox.Refresh();
        }
        else if (e.Button == MouseButtons.Left && _annotationService.SelectedAnnotation != null)
        {
            _annotationService.MoveSelectedAnnotation(new PointF(e.X, e.Y));
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_isDrawing)
            {
                var endPoint = new PointF(e.X, e.Y);
                var bounds = GetRectangle(_startPoint, endPoint);
                var annotation = new RectangleAnnotation(bounds, _currentColor, 1.0f);
                _annotationService.AddAnnotation(annotation);
                _isDrawing = false;
            }
            _isDragging = false;
            pictureBox.Refresh();
        }
    }


    private static Rectangle GetRectangle(PointF startPoint, PointF endPoint)
    {
        return new Rectangle(
            (int)Math.Min(startPoint.X, endPoint.X),
            (int)Math.Min(startPoint.Y, endPoint.Y),
            (int)Math.Abs(endPoint.X - startPoint.X),
            (int)Math.Abs(endPoint.Y - startPoint.Y)
        );
    }

    

    

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}