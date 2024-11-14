﻿namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.UI.Forms;

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
        // Create and configure PictureBox
        pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
            Dock = DockStyle.None // Remove Dock setting to allow scrolling
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16),
        };

        // Add resize handler to center the image
        panel.Resize += Panel_Resize;
        panel.Controls.Add(pictureBox);

        // Create modern toolbar
        var toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            RenderMode = ToolStripRenderMode.System,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(248, 249, 250),
            GripStyle = ToolStripGripStyle.Hidden,
            Height = 40
        };

        // Screenshot button
        var screenshotButton = CreateToolStripButton("Screenshot");
        screenshotButton.Click += StartScreenCapture;

        // Rectangle annotation button
        var rectangleButton = CreateToolStripButton("Rectangle");

        // Highlighter button
        var highlighterButton = CreateToolStripButton("Highlight");
        highlighterButton.Click += (s, e) => _currentTool = AnnotationTool.Highlight;

        // Line button
        var lineButton = CreateToolStripButton("Line");

        // Text button
        var textButton = CreateToolStripButton("Text");

        // Color button
        var colorButton = CreateToolStripButton("Color");
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

        // Handle PictureBox painting
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
    }

    private void Panel_Resize(object? sender, EventArgs e)
    {
        CenterPictureBox();
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



    private async void StartScreenCapture(object? sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
        await Task.Delay(200);

        var captureService = new ScreenCaptureService();
        var screenshot = captureService.CaptureScreen();

        using var overlay = new SelectionOverlayForm(screenshot);
        if (overlay.ShowDialog() == DialogResult.OK)
        {
            var region = overlay.SelectionBounds;
            var capturedImage = captureService.CaptureRegion(region);

            _currentImage?.Dispose();
            _currentImage = capturedImage;

            // Clear existing annotations
            _annotationService.ClearAnnotations();

            // Update PictureBox on the UI thread
            if (pictureBox.InvokeRequired)
            {
                pictureBox.Invoke(() =>
                {
                    pictureBox.Image = _currentImage;
                    SetWindowAndImageSize(capturedImage);
                });
            }
            else
            {
                pictureBox.Image = _currentImage;
                SetWindowAndImageSize(capturedImage);
            }
        }

        this.WindowState = FormWindowState.Normal;
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

        if (_isDrawing)
        {
            var rect = GetRectangle(Point.Round(_startPoint), pictureBox.PointToClient(Cursor.Position));
            using var pen = new Pen(_currentColor, 2f) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, rect);
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


    private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
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

            pictureBox.Invalidate();
        }
    }

    private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
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

    }
}