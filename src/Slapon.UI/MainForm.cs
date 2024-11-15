﻿namespace Slapon.UI;

using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.UI.Forms;
using System.Text.Json;
using System;


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
        if (keyData == (Keys.Control | Keys.S))
        {
            SaveFile();
            return true;
        }
        else if (keyData == (Keys.Control | Keys.O))
        {
            LoadFile();
            return true;
        }
        else if (keyData == Keys.Delete)
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

        // Enable double buffering for PictureBox using reflection
        typeof(PictureBox).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, pictureBox, new object[] { true });

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16),
        };

        // Enable double buffering for Panel using reflection
        typeof(Panel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
            null, panel, new object[] { true });

        // Add Save and Open buttons
        var saveButton = CreateToolStripButton("Save");
        saveButton.Click += (s, e) => SaveFile();

        var openButton = CreateToolStripButton("Open");
        openButton.Click += (s, e) => LoadFile();

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

        var copyButton = CreateToolStripButton("Copy to Clipboard");
        copyButton.Click += (s, e) => CopyScreenshotToClipboard(); // Trigger the copy method

        toolStrip.Items.Add(copyButton);

        // Screenshot button
        var screenshotButton = CreateToolStripButton("Screenshot");
        screenshotButton.Click += StartScreenCapture;

        // Rectangle annotation button
        btnRectangleTool = CreateToolStripButton("Rectangle");
        btnRectangleTool.Click += (s, e) => SetActiveTool(AnnotationTool.Rectangle);

        // Highlighter button
        btnHighlightTool = CreateToolStripButton("Highlight");
        btnHighlightTool.Click += (s, e) => SetActiveTool(AnnotationTool.Highlight);

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
            openButton,
            saveButton,
            new ToolStripSeparator(),
            btnRectangleTool,
            btnHighlightTool,
            lineButton,
            textButton,
            new ToolStripSeparator(),
            colorButton,
            new ToolStripSeparator(),
            copyButton
        });

        Controls.Add(panel);
        Controls.Add(toolStrip);

        // Handle PictureBox painting
        pictureBox.Paint += PictureBox_Paint;
        pictureBox.MouseDown += PictureBox_MouseDown;
        pictureBox.MouseMove += PictureBox_MouseMove;
        pictureBox.MouseUp += PictureBox_MouseUp;
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
            // Copy the screenshot with annotations to the clipboard
            CopyScreenshotWithAnnotationsToClipboard();
        }

        this.WindowState = FormWindowState.Normal;
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

    private void SaveAsImage()
    {
        if (_currentImage == null) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg",
            Title = "Save Screenshot with Annotations"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            using var bitmap = new Bitmap(_currentImage.Width, _currentImage.Height);
            using var g = Graphics.FromImage(bitmap);

            // Draw the screenshot
            g.DrawImage(_currentImage, 0, 0);

            // Draw all annotations
            foreach (var annotation in _annotationService.Annotations)
            {
                annotation.Draw(g);
            }

            bitmap.Save(saveDialog.FileName,
                saveDialog.FilterIndex == 1 ? ImageFormat.Png : ImageFormat.Jpeg);
        }
    }

    private void SaveAsEditable()
    {
        if (_currentImage == null) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "Slapon File|*.slap",
            Title = "Save Editable Screenshot"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            byte[] screenshotBytes;
            // Create the screenshot bytes using a MemoryStream
            using (var ms = new MemoryStream())
            {
                _currentImage.Save(ms, ImageFormat.Png);
                screenshotBytes = ms.ToArray();
            }

            var saveData = new SaveData
            {
                Annotations = _annotationService.Annotations.Select(a => new SerializableAnnotation
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = a.GetType().Name.Replace("Annotation", ""),
                    Bounds = a.Bounds,
                    Color = a.Color,
                    Opacity = a.GetType().GetProperty("Opacity")?.GetValue(a) as float? ?? 1.0f
                }).ToList(),
                Screenshot = screenshotBytes
            };

            var json = JsonSerializer.Serialize(saveData);
            File.WriteAllText(saveDialog.FileName, json);
        }
    }

    private byte[] ImageToByteArray(Image image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private void LoadFile()
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "Slapon Files|*.slap|Image Files|*.png;*.jpg",
            Title = "Open File"
        };

        if (openDialog.ShowDialog() == DialogResult.OK)
        {
            if (openDialog.FileName.EndsWith(".slap"))
            {
                var json = File.ReadAllText(openDialog.FileName);
                var saveData = JsonSerializer.Deserialize<SaveData>(json);

                using var ms = new MemoryStream(saveData.Screenshot);
                _currentImage = (Bitmap?)Image.FromStream(ms);
                pictureBox.Image = _currentImage;

                // Clear existing annotations
                _annotationService.ClearAnnotations();

                // Restore annotations
                foreach (var annotationData in saveData.Annotations)
                {
                    IAnnotation? annotation = annotationData.Type switch
                    {
                        "Rectangle" => new RectangleAnnotation(annotationData.Bounds, annotationData.Color, annotationData.Opacity),
                        "Highlight" => new HighlightAnnotation(annotationData.Bounds, annotationData.Color, annotationData.Opacity),
                        _ => null
                    };

                    if (annotation != null)
                    {
                        _annotationService.AddAnnotation(annotation);
                    }
                }
            }
            else
            {
                // Load as regular image file
                _currentImage = (Bitmap?)Image.FromFile(openDialog.FileName);
                pictureBox.Image = _currentImage;
                _annotationService.ClearAnnotations();
            }

            CenterPictureBox();
            pictureBox.Invalidate();
        }
    }

    public class SaveData
    {
        public byte[] Screenshot { get; set; }
        public List<SerializableAnnotation> Annotations { get; set; }
    }

    public class AnnotationData
    {
        public string Type { get; set; }
        public RectangleF Bounds { get; set; }
        public Color Color { get; set; }
        public float Opacity { get; set; }
    }

    private void SaveFile()
    {
        if (_currentImage == null) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "Slapon File|*.slap|PNG Image|*.png",
            DefaultExt = "slap",
            Title = "Save File"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            if (saveDialog.FileName.EndsWith(".slap"))
            {
                byte[] screenshotBytes;
                // Create the screenshot bytes first
                using (var ms = new MemoryStream())
                {
                    _currentImage.Save(ms, ImageFormat.Png);
                    screenshotBytes = ms.ToArray();
                }

                // Then create the save data
                var saveData = new SaveData
                {
                    Annotations = _annotationService.Annotations.Select(a => new SerializableAnnotation
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = a.GetType().Name.Replace("Annotation", ""),
                        Bounds = a.Bounds,
                        Color = a.Color,
                        Opacity = a.GetType().GetProperty("Opacity")?.GetValue(a) as float? ?? 1.0f
                    }).ToList(),
                    Screenshot = screenshotBytes
                };

                // Serialize and save
                var json = JsonSerializer.Serialize(saveData);
                File.WriteAllText(saveDialog.FileName, json);
            }
            else
            {
                // Save as PNG
                SaveAsImage(saveDialog.FileName);
            }
        }
    }

    private void SaveAsImage(string fileName)
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
        bitmap.Save(fileName, ImageFormat.Png);
    }

    private void LoadFromSlap(string fileName)
    {
        try
        {
            var json = File.ReadAllText(fileName);
            var saveData = JsonSerializer.Deserialize<SaveData>(json);

            if (saveData == null)
            {
                MessageBox.Show("Failed to load file: Invalid format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load the image
            using var ms = new MemoryStream(saveData.Screenshot);
            _currentImage?.Dispose();
            _currentImage = new Bitmap(ms);
            pictureBox.Image = _currentImage;

            // Clear existing annotations
            _annotationService.ClearAnnotations();

            // Restore annotations
            foreach (var serializedAnnotation in saveData.Annotations)
            {
                IAnnotation? annotation = serializedAnnotation.Type switch
                {
                    "Rectangle" => new RectangleAnnotation(serializedAnnotation.Bounds, serializedAnnotation.Color, serializedAnnotation.Opacity),
                    "Highlight" => new HighlightAnnotation(serializedAnnotation.Bounds, serializedAnnotation.Color, serializedAnnotation.Opacity),
                    // Add other annotation types here as needed
                    _ => null
                };

                if (annotation != null)
                {
                    _annotationService.AddAnnotation(annotation);
                }
            }

            SetWindowAndImageSize((Bitmap)_currentImage);
            pictureBox.Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        UpdateToolButtons();
    }
}