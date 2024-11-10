namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

public partial class MainForm : Form
{
    private readonly IAnnotationService _annotationService;
    private readonly IAnnotationFactory _annotationFactory;
    private Bitmap? _currentImage;
    private PointF _startPoint;
    private bool _isDrawing = false;
    private Color _currentColor = Color.Red;
    private AnnotationType _currentType = AnnotationType.Rectangle;
    private IAnnotation _selectedAnnotation;

    public MainForm()
    {
        InitializeComponent();
        _annotationService = new AnnotationService();
        _annotationFactory = new AnnotationFactory();
        pictureBox.MouseClick += (s, e) =>
        {
            var clickPoint = new PointF(e.X, e.Y);
            var annotation = _annotationService.GetAnnotationAt(clickPoint);
            _annotationService.SelectAnnotation(annotation);
            pictureBox.Refresh();
        };
        _annotationService.AnnotationsChanged += (s, e) => pictureBox.Refresh();
        SetupUI();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Delete key to remove selected annotation
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
        // Create and configure PictureBox
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.AutoSize
        };

        // Create toolbar
        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top  // Add this line to dock the toolbar at the top
        };

        var openButton = new ToolStripButton("Open Image");
        openButton.Click += OpenImage;

        var saveButton = new ToolStripButton("Save");
        saveButton.Click += SaveImage;

        var colorButton = new ToolStripButton("Color");
        colorButton.Click += ChangeColor;

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
        openButton,
        saveButton,
        new ToolStripSeparator(),
        colorButton
        });

        // Layout
        Controls.Add(pictureBox);
        Controls.Add(toolStrip);
        // Remove the SendToFront() call and use proper docking instead

        // Wire up picture box events
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
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
            using var pen = new Pen(_currentColor, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_currentImage == null) return;

        if (e.Button == MouseButtons.Left)
        {
            var clickedAnnotation = _annotationService.GetAnnotationAt(e.Location);
            if (clickedAnnotation != null)
            {
                _annotationService.SelectAnnotation(clickedAnnotation);
            }
            else
            {
                _isDrawing = true;
                _startPoint = e.Location;
                _annotationService.SelectAnnotation(null);
            }
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
            _annotationService.MoveSelectedAnnotation(e.Location);
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDrawing)
        {
            _isDrawing = false;
            var rect = GetRectangle(_startPoint, e.Location);
            if (rect.Width > 5 && rect.Height > 5)
            {
                var annotation = _annotationFactory.CreateAnnotation(
                    _currentType,
                    rect,
                    _currentColor
                );
                _annotationService.AddAnnotation(annotation);
            }
        }
    }

    private static Rectangle GetRectangle(Point startPoint, Point endPoint)
    {
        return new Rectangle(
            Math.Min(startPoint.X, endPoint.X),
            Math.Min(startPoint.Y, endPoint.Y),
            Math.Abs(endPoint.X - startPoint.X),
            Math.Abs(endPoint.Y - startPoint.Y)
        );
    }

    private void pictureBox_MouseClick(object sender, MouseEventArgs e)
    {
        var clickPoint = e.Location;
        _selectedAnnotation = _annotationService.GetAnnotationAt(clickPoint);

        // Optional: Add visual feedback that something was selected
        pictureBox.Refresh();
    }

    private PictureBox pictureBox;

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}