using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing.Imaging;
using static CapSnip.MainForm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CapSnip
{
    public partial class MainForm : Form
    {
        private bool isDragging = false;
        private Point startPoint;
        private Rectangle selectionRect;
        private Image capturedImage;
        private bool isAnnotating = false;
        private Point annotationStart;
        //private List<Rectangle> annotations = new List<Rectangle>();
        // Change the annotations list to store Annotation objects instead of Rectangles
        private List<Annotation> annotations = new List<Annotation>();
        private bool isDrawingAnnotation = false;
        private Panel centeringPanel;
        private Annotation selectedAnnotation = null;
        private ToolStripButton highlighterButton;
        private AnnotationType currentAnnotationType = AnnotationType.Rectangle;

        private const int WINDOW_PADDING = 100; // Padding around the image
        private const int MIN_WINDOW_WIDTH = 800;
        private const int MIN_WINDOW_HEIGHT = 600;

        private System.Windows.Forms.Label dateTimeLabel;

        private Color currentColor = Color.Red; // Default color
        private ToolStripDropDownButton colorPickerButton;
        private const int COLOR_BUTTON_SIZE = 20;

        // Add these at the class level with your other private fields
        private System.Windows.Forms.TrackBar opacityTrackBar;
        private System.Windows.Forms.Label opacityLabel;
        private int defaultOpacity = 50;

        public enum AnnotationType
        {
            Rectangle,
            Highlighter
        }
        public class Annotation
        {
            public Rectangle Rectangle { get; }
            public Color Color { get; }
            public AnnotationType Type { get; }
            public float Opacity { get; set; }

            public Annotation(Rectangle rectangle, Color color, AnnotationType type, float opacity)
            {
                Rectangle = rectangle;
                Color = color;
                Type = type;
                Opacity = opacity;
            }
        }


        private enum Tool
        {
            Select,
            Rectangle,
            Highlighter
        }

        public MainForm()
        {
            InitializeComponent();
            SetupOpacityControls();
            SetupUI();
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
            this.KeyPreview = true; // Ensure the form captures key events
            this.KeyDown += MainForm_KeyDown; // Add KeyDown event handler
            UpdateUndoRedoButtons();
            this.BackColor = Color.White;
        }
        public bool CanUndo => undoRedoManager.CanUndo;
        public bool CanRedo => undoRedoManager.CanRedo;

        private ToolStripButton undoButton;
        private ToolStripButton clearAllButton;
        private ToolStripButton redoButton;
        private UndoRedoManager undoRedoManager = new UndoRedoManager();



        private void InitializeComponent()
        {
            dateTimeLabel = new Label();
            pictureBox = new PictureBox();
            toolStrip = new ToolStrip();
            newCaptureButton = new ToolStripButton();
            saveButton = new ToolStripButton();
            copyButton = new ToolStripButton();
            annotateButton = new ToolStripButton();
            exitButton = new ToolStripButton();
            undoButton = new ToolStripButton();
            redoButton = new ToolStripButton();
            colorPickerButton = new ToolStripDropDownButton();
            centeringPanel = new Panel();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            toolStrip.SuspendLayout();
            centeringPanel.SuspendLayout();
            SuspendLayout();
            // 
            // DateTime Label
            this.dateTimeLabel = new Label();
            this.dateTimeLabel.AutoSize = true; // Allow label to size to content
            this.dateTimeLabel.BackColor = Color.FromArgb(128, 128, 128, 128); // Gray transparent background
            this.dateTimeLabel.ForeColor = Color.White;
            this.dateTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.dateTimeLabel.Padding = new Padding(0, 10, 0, 10);
            this.dateTimeLabel.Location = new Point((this.ClientSize.Width - this.dateTimeLabel.Width) / 2, this.ClientSize.Height - 45); // Center at bottom
            this.dateTimeLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; // Anchor to bottom to maintain position

            // Add DateTime Label to the form
            this.Controls.Add(this.dateTimeLabel);

            this.Resize += MainForm_Resize;
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.White; // Change to white
            pictureBox.Location = new Point(0, 0);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(100, 50);
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox.TabIndex = 0;
            pictureBox.TabStop = false;
            pictureBox.Paint += PictureBox_Paint;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;

            // Initialize colorPickerButton
            //colorPickerButton.Name = "colorPickerButton";
            //colorPickerButton.Size = new Size(80, 22);
            //colorPickerButton.Text = "Color Picker";
            clearAllButton = new ToolStripButton();

            // Add this in your form initialization
            highlighterButton = new ToolStripButton();
            highlighterButton.ForeColor = Color.Black;
            highlighterButton.Name = "highlighterButton";
            highlighterButton.Size = new Size(70, 22);
            highlighterButton.Text = "Highlight";
            highlighterButton.Click += Highlighter_Click;
            // 
            // clearAllButton
            //
            clearAllButton.ForeColor = Color.White;
            clearAllButton.Name = "clearAllButton";
            clearAllButton.Size = new Size(60, 22);
            clearAllButton.Text = "Clear All";
            clearAllButton.Click += ClearAllButton_Click;

            // 
            // toolStrip
            // 
            toolStrip.BackColor = Color.White;
            toolStrip.ForeColor = Color.Black;  // Dark text for contrast
            toolStrip.Renderer = new CustomToolStripRenderer();

            toolStrip.Items.AddRange(new ToolStripItem[] { newCaptureButton, saveButton, copyButton, annotateButton, undoButton, redoButton, clearAllButton, exitButton });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(784, 25);
            toolStrip.TabIndex = 2;

            // Clear existing items
            toolStrip.Items.Clear();

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton button)
                {
                    button.ForeColor = Color.Black;  // Ensure text is visible on white background
                }
            }

            // Create padding item for left side
            var leftPadding = new ToolStripLabel();
            leftPadding.Text = "   ";

            // Create padding item for right side
            var rightPadding = new ToolStripLabel();
            rightPadding.Text = "   ";

            // Create dividers
            var leftDivider = new ToolStripSeparator();
            var rightDivider = new ToolStripSeparator();

            // Create a spring to push items to the right
            var spring = new ToolStripSeparator();
            spring.Alignment = ToolStripItemAlignment.Right;

            // Add items in the desired order with padding and dividers
            toolStrip.Items.AddRange(new ToolStripItem[] {
                // Left side
                leftPadding,
                newCaptureButton,
    
                // Left divider
                leftDivider,
    
                // Middle section
                annotateButton,
                highlighterButton,
                undoButton,
                redoButton,
                clearAllButton,
    
                // Right divider
                rightDivider,
    
                // Right side (using spring to push to the right)
                spring,
                saveButton,
                copyButton,
                rightPadding
            });

            // Configure specific items for right alignment
            saveButton.Alignment = ToolStripItemAlignment.Right;
            copyButton.Alignment = ToolStripItemAlignment.Right;

            // Optional: Add some margin to the buttons
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton)
                {
                    item.Margin = new Padding(3, 0, 3, 0);
                }
            }

            // Optional: Configure separators to be more visible
            leftDivider.Margin = new Padding(5, 0, 5, 0);
            rightDivider.Margin = new Padding(5, 0, 5, 0);
            // 
            // newCaptureButton
            // 

            newCaptureButton.ForeColor = Color.Black; // Change text color to black
            newCaptureButton.Name = "newCaptureButton";
            newCaptureButton.Size = new Size(80, 22);
            newCaptureButton.Text = "New Capture";
            newCaptureButton.Click += NewCapture_Click;

            // 
            // saveButton
            // 
            saveButton.ForeColor = Color.Black; // Change text color to black
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(35, 22);
            saveButton.Text = "Save";
            saveButton.Click += Save_Click;

            // 
            // copyButton
            // 
            copyButton.ForeColor = Color.Black; // Change text color to black
            copyButton.Name = "copyButton";
            copyButton.Size = new Size(39, 22);
            copyButton.Text = "Copy";
            copyButton.Click += Copy_Click;

            // 
            // annotateButton
            // 
            annotateButton.ForeColor = Color.Black; // Change text color to black
            annotateButton.Name = "annotateButton";
            annotateButton.Size = new Size(60, 22);
            annotateButton.Text = "Rectangle";
            annotateButton.Click += Annotate_Click;

            // 
            // undoButton
            // 
            undoButton.ForeColor = Color.Black; // Change text color to black
            undoButton.Name = "undoButton";
            undoButton.Size = new Size(40, 22);
            undoButton.Text = "Undo";
            undoButton.Click += UndoButton_Click;

            // 
            // redoButton
            // 
            redoButton.ForeColor = Color.Black; // Change text color to black
            redoButton.Name = "redoButton";
            redoButton.Size = new Size(40, 22);
            redoButton.Text = "Redo";
            redoButton.Click += RedoButton_Click;

            // 
            // clearAllButton
            // 
            clearAllButton.ForeColor = Color.Black; // Change text color to black
            clearAllButton.Name = "clearAllButton";
            clearAllButton.Size = new Size(60, 22);
            clearAllButton.Text = "Clear All";
            clearAllButton.Click += ClearAllButton_Click;

            // 
            // exitButton
            // 
            exitButton.ForeColor = Color.Black; // Change text color to black
            exitButton.Name = "exitButton";
            exitButton.Size = new Size(30, 22);
            exitButton.Text = "Exit";
            exitButton.Click += Exit_Click;
            // 
            // centeringPanel
            // 
            centeringPanel.AutoScroll = true;
            centeringPanel.BackColor = Color.White; // Change to white
            centeringPanel.Controls.Add(pictureBox);
            centeringPanel.Dock = DockStyle.Fill;
            centeringPanel.Location = new Point(0, 25);
            centeringPanel.Name = "centeringPanel";
            centeringPanel.Size = new Size(784, 536);
            centeringPanel.TabIndex = 1;


            // 
            // MainForm
            // 
            BackColor = Color.FromArgb(45, 45, 48);
            ClientSize = new Size(784, 561);
            Controls.Add(dateTimeLabel);
            Controls.Add(centeringPanel);
            Controls.Add(toolStrip);
            MinimumSize = new Size(800, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CapSnip";
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            centeringPanel.ResumeLayout(false);
            centeringPanel.PerformLayout();
            AddToolStripSpacing();
            ResumeLayout(false);
            PerformLayout();
        }


        private void SetupUI()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Normal;
            // Set minimum size to prevent window from becoming too small
            this.MinimumSize = new Size(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);

            // Modern font for toolbar
            Font modernFont = new Font("Segoe UI", 9F, FontStyle.Regular);
            toolStrip.Font = modernFont;

            // Style the toolbar
            toolStrip.RenderMode = ToolStripRenderMode.Professional;
            toolStrip.Renderer = new CustomToolStripRenderer();
            toolStrip.Padding = new Padding(5, 0, 5, 0);
            toolStrip.ImageScalingSize = new Size(20, 20);  // Larger icons

            // Add this line to initialize the color picker
            AddColorPicker();

            // Style individual buttons
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton button)
                {
                    button.AutoSize = false;
                    button.Size = new Size(80, 30);  // Uniform button size
                    button.Margin = new Padding(3, 0, 3, 0);
                    button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                    button.TextImageRelation = TextImageRelation.ImageBeforeText;
                }
            }
        }

        private void AddToolStripSpacing()
        {
            // Add flexible spacing between button groups
            var leftGroup = new ToolStripButton[] { newCaptureButton };
            var middleGroup = new ToolStripButton[] { annotateButton,highlighterButton ,undoButton, redoButton, clearAllButton };
            var rightGroup = new ToolStripButton[] { saveButton, copyButton };

            // Add spacing between groups
            toolStrip.Items.Clear();
            toolStrip.Items.AddRange(leftGroup);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.AddRange(middleGroup);
            //Image = Image.FromFile("move.png"); // Add appropriate icon image
            toolStrip.Items.Add(new ToolStripSeparator());

            var selectButton = new ToolStripButton
            {
                Text = "Select",
                 
            };
            selectButton.Click += (s, e) =>
            {
                currentTool = Tool.Select;
                // Update button states
            };
            toolStrip.Items.Add(selectButton);


            // Push remaining items to right
            var spring = new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right };
            toolStrip.Items.Add(spring);
            foreach (var button in rightGroup)
            {
                button.Alignment = ToolStripItemAlignment.Right;
                toolStrip.Items.Add(button);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            StartCapture();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterPictureBox();
            // Reposition the DateTime label
            dateTimeLabel.Location = new Point((this.ClientSize.Width - dateTimeLabel.Width) / 2, this.ClientSize.Height - 45);
        }

        private void SetupOpacityControls()
        {
            // Debug check to ensure toolStrip exists
            if (toolStrip == null)
            {
                MessageBox.Show("ToolStrip is null!");
                return;
            }

            opacityLabel = new System.Windows.Forms.Label
            {
                Text = "Opacity:",
                AutoSize = true,
                Visible = false
            };

            opacityTrackBar = new System.Windows.Forms.TrackBar
            {
                Minimum = 10,
                Maximum = 100,
                Value = defaultOpacity,
                Width = 100,
                Visible = false,
                TickFrequency = 10,
                TickStyle = System.Windows.Forms.TickStyle.Both
            };

            // Try adding to toolStrip with more explicit control
            try
            {
                ToolStripControlHost labelHost = new ToolStripControlHost(opacityLabel);
                ToolStripControlHost trackBarHost = new ToolStripControlHost(opacityTrackBar);

                toolStrip.Items.Add(labelHost);
                toolStrip.Items.Add(trackBarHost);

                // Force a refresh of the toolStrip
                toolStrip.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding opacity controls: " + ex.Message);
            }

            opacityTrackBar.ValueChanged += (s, e) =>
            {
                float newOpacity = opacityTrackBar.Value / 100f;

                if (isDrawingAnnotation && currentAnnotationType == AnnotationType.Highlighter)
                {
                    // Update preview when drawing
                    pictureBox.Invalidate();
                }
                else if (selectedAnnotation != null)
                {
                    // Update selected annotation's opacity
                    selectedAnnotation.Opacity = newOpacity;
                    pictureBox.Invalidate();
                }
            };
        }

        private void CenterPictureBox()
        {
            if (pictureBox.Image != null)
            {
                // Calculate the center position within the panel
                int x = Math.Max(0, (centeringPanel.ClientSize.Width - pictureBox.Width) / 2);
                int y = Math.Max(0, (centeringPanel.ClientSize.Height - pictureBox.Height) / 2);

                // If the image is smaller than the panel, center it
                // If it's larger, start from top-left with small margin
                x = pictureBox.Width > centeringPanel.ClientSize.Width ? 10 : x;
                y = pictureBox.Height > centeringPanel.ClientSize.Height ? 10 : y;

                pictureBox.Location = new Point(x, y);
            }
        }

        private void HighlighterButton_Click(object sender, EventArgs e)
        {
            currentAnnotationType = currentAnnotationType == AnnotationType.Rectangle ?
                AnnotationType.Highlighter : AnnotationType.Rectangle;

            highlighterButton.Checked = currentAnnotationType == AnnotationType.Highlighter;
            annotateButton.Checked = currentAnnotationType == AnnotationType.Rectangle;
        }

        private void UpdatePictureBox(Image newImage)
        {
            if (newImage == null) return;

            pictureBox.Image = newImage;
            pictureBox.Size = newImage.Size;
            CenterPictureBox();
        }

        

        private void AdjustWindowSizeToImage()
        {
            if (capturedImage == null) return;

            // Get the working area of the screen (excludes taskbar)
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;

            // Calculate desired window size (image size + padding + toolbar height)
            int desiredWidth = capturedImage.Width + (WINDOW_PADDING * 2);
            int desiredHeight = capturedImage.Height + (WINDOW_PADDING * 2) + toolStrip.Height;

            // Ensure window size doesn't exceed screen size
            int windowWidth = Math.Min(workingArea.Width, Math.Max(desiredWidth, MIN_WINDOW_WIDTH));
            int windowHeight = Math.Min(workingArea.Height, Math.Max(desiredHeight, MIN_WINDOW_HEIGHT));

            // Calculate window position to center it
            int windowX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
            int windowY = workingArea.Top + (workingArea.Height - windowHeight) / 2;

            // Set new window bounds
            this.SetBounds(windowX, windowY, windowWidth, windowHeight);
        }

        private void StartCapture()
        {
            this.Hide();
            using (var captureForm = new CaptureForm())
            {
                if (captureForm.ShowDialog() == DialogResult.OK)
                {
                    capturedImage = captureForm.CapturedImage;
                    UpdatePictureBox(capturedImage);
                    Clipboard.SetImage(capturedImage);

                    // Reset tool state
                    annotations.Clear();
                    undoRedoManager = new UndoRedoManager();
                    UpdateUndoRedoButtons();
                    currentTool = Tool.Rectangle;  // Reset to default tool
                    currentAnnotationType = AnnotationType.Rectangle;
                    annotateButton.Checked = false;
                    highlighterButton.Checked = false;
                    selectedAnnotation = null;
                    opacityTrackBar.Visible = false;
                    opacityLabel.Visible = false;

                    // Adjust window size before showing
                    AdjustWindowSizeToImage();
                    this.Show();
                    this.Activate();

                    dateTimeLabel.Text = $"Captured on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    pictureBox.Invalidate();
                }
                else
                {
                    this.Show();
                }
            }
        }

        private void AddColorPicker()
        {
            colorPickerButton = new ToolStripDropDownButton();
            colorPickerButton.Text = "Color";
            colorPickerButton.ToolTipText = "Choose annotation color";

            // Create the dropdown menu
            ToolStripDropDown dropDown = new ToolStripDropDown();

            // Define your color palette
            Color[] colors = new Color[]
            {
            Color.Red, Color.Blue, Color.Green, Color.Yellow,
            Color.Orange, Color.Purple, Color.Black, Color.White
            };

            foreach (Color color in colors)
            {
                var colorItem = new ToolStripMenuItem()
                {
                    BackColor = color,
                    // Use white text for dark colors, black for light colors
                    ForeColor = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) > 186
                        ? Color.Black
                        : Color.White,
                    Text = GetColorName(color),
                    Tag = color
                };

                colorItem.Click += ColorItem_Click;
                dropDown.Items.Add(colorItem);
            }

            // Add custom color option
            var customColorItem = new ToolStripMenuItem("Custom Color...");
            customColorItem.Click += CustomColorItem_Click;
            dropDown.Items.Add(new ToolStripSeparator());
            dropDown.Items.Add(customColorItem);

            colorPickerButton.DropDown = dropDown;

            // Add to toolbar (add this where you setup your toolbar items)
            toolStrip.Items.Add(colorPickerButton);

            // Update the button's appearance to show current color
            UpdateColorButtonAppearance();
        }

        private void ColorItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is Color color)
            {
                currentColor = color;
                UpdateColorButtonAppearance();
            }
        }

        private void CustomColorItem_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = currentColor;
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = colorDialog.Color;
                    UpdateColorButtonAppearance();
                }
            }
        }

        private void UpdateColorButtonAppearance()
        {
            // Create a small bitmap showing the current color
            using (Bitmap colorBitmap = new Bitmap(COLOR_BUTTON_SIZE, COLOR_BUTTON_SIZE))
            using (Graphics g = Graphics.FromImage(colorBitmap))
            {
                using (SolidBrush brush = new SolidBrush(currentColor))
                {
                    g.FillRectangle(brush, 0, 0, COLOR_BUTTON_SIZE, COLOR_BUTTON_SIZE);
                }
                using (Pen pen = new Pen(Color.Gray))
                {
                    g.DrawRectangle(pen, 0, 0, COLOR_BUTTON_SIZE - 1, COLOR_BUTTON_SIZE - 1);
                }
                colorPickerButton.Image = new Bitmap(colorBitmap);
            }
        }

        private string GetColorName(Color color)
        {
            if (color.IsNamedColor)
                return color.Name;
            return $"RGB({color.R},{color.G},{color.B})";
        }
    

    private void NewCapture_Click(object sender, EventArgs e)
        {
            StartCapture();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (capturedImage == null) return;

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveDialog.Title = "Save Screenshot";
                saveDialog.FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    ImageFormat format = ImageFormat.Png;
                    string ext = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                    switch (ext)
                    {
                        case ".jpg":
                            format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;
                    }

                    capturedImage.Save(saveDialog.FileName, format);
                }
            }
        }

        private void HandleSelection(Point mousePoint)
        {
            // Find the most recently created annotation that contains the click point
            selectedAnnotation = annotations
                .Where(a => a.Rectangle.Contains(mousePoint))  // Remove the Type filter to allow all annotations
                .LastOrDefault();

            if (selectedAnnotation != null)
            {
                // Show and update opacity controls only for highlighter annotations
                opacityTrackBar.Visible = selectedAnnotation.Type == AnnotationType.Highlighter;
                opacityLabel.Visible = selectedAnnotation.Type == AnnotationType.Highlighter;
                if (selectedAnnotation.Type == AnnotationType.Highlighter)
                {
                    opacityTrackBar.Value = (int)(selectedAnnotation.Opacity * 100);
                }
            }
            else
            {
                // Hide opacity controls when no annotation is selected
                opacityTrackBar.Visible = false;
                opacityLabel.Visible = false;
            }

            pictureBox.Invalidate(); // Redraw to show selection state
        }
        private void Copy_Click(object sender, EventArgs e)
        {
            if (capturedImage != null)
            {
                Clipboard.SetImage(capturedImage);
            }
        }

        private void Annotate_Click(object sender, EventArgs e)
        {
            currentTool = Tool.Rectangle;
            currentAnnotationType = AnnotationType.Rectangle;
            annotateButton.Checked = true;
            highlighterButton.Checked = false;
            this.Cursor = Cursors.Cross;

            // Hide opacity controls for rectangle tool
            opacityTrackBar.Visible = false;
            opacityLabel.Visible = false;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z) // Ctrl+Z for Undo
            {
                UndoButton_Click(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.Y) // Ctrl+Y for Redo
            {
                RedoButton_Click(sender, e);
            }
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (currentTool == Tool.Select)
                {
                    HandleSelection(e.Location);
                }
                else
                {
                    isDrawingAnnotation = true;
                    startPoint = e.Location;
                    annotationStart = e.Location;  // Add this line
                    selectionRect = new Rectangle(startPoint, Size.Empty);
                    selectedAnnotation = null; // Clear selection when starting new annotation
                }
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawingAnnotation)
            {
                // Calculate rectangle dimensions based on start point and current position
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(e.X - startPoint.X);
                int height = Math.Abs(e.Y - startPoint.Y);

                selectionRect = new Rectangle(x, y, width, height);
                pictureBox.Invalidate();
            }
        }


        // Update your PictureBox_MouseUp method to include opacity
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawingAnnotation)
            {
                isDrawingAnnotation = false;
                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    var annotation = new Annotation(
                        selectionRect,
                        currentColor,
                        currentAnnotationType,
                        currentAnnotationType == AnnotationType.Highlighter ? opacityTrackBar.Value / 100f : 1f
                    );

                    var addAnnotationCommand = new AddAnnotationCommand(
                        annotations,
                        annotation
                    );

                    undoRedoManager.ExecuteCommand(addAnnotationCommand);
                    RedrawAnnotations();
                    pictureBox.Invalidate();
                    UpdateUndoRedoButtons();
                    CopyImageToClipboard();
                }
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (capturedImage != null)
            {
                e.Graphics.DrawImage(capturedImage, Point.Empty);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Draw existing annotations
            foreach (var annotation in annotations)
            {
                if (annotation.Type == AnnotationType.Highlighter)
                {
                    DrawSoftHighlight(e.Graphics, annotation.Rectangle, annotation.Color, annotation.Opacity);
                }
                else
                {
                    using (Pen pen = new Pen(annotation.Color, 2))
                    {
                        e.Graphics.DrawRectangle(pen, annotation.Rectangle);
                    }
                }

                // Draw selection indicator if this annotation is selected
                if (annotation == selectedAnnotation)
                {
                    using (Pen selectionPen = new Pen(Color.FromArgb(128, 0, 120, 215), 1))
                    {
                        selectionPen.DashStyle = DashStyle.Dash;
                        Rectangle selectionBounds = annotation.Rectangle;
                        selectionBounds.Inflate(2, 2); // Make selection slightly larger
                        e.Graphics.DrawRectangle(selectionPen, selectionBounds);
                    }
                }
            }

            // Draw the current selection rectangle
            if (isDrawingAnnotation && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                if (currentAnnotationType == AnnotationType.Highlighter)
                {
                    float currentOpacity = opacityTrackBar.Value / 100f;
                    DrawSoftHighlight(e.Graphics, selectionRect, currentColor, currentOpacity);
                }
                else
                {
                    using (Pen pen = new Pen(currentColor, 2))
                    {
                        e.Graphics.DrawRectangle(pen, selectionRect);
                    }
                }
            }
        }

        // Update your Highlighter_Click method to show/hide opacity controls
        private void Highlighter_Click(object sender, EventArgs e)
        {
            currentTool = Tool.Highlighter;
            currentAnnotationType = AnnotationType.Highlighter;
            highlighterButton.Checked = true;
            annotateButton.Checked = false;
            this.Cursor = Cursors.Cross;

            // Show opacity controls for highlighter tool
            opacityTrackBar.Visible = true;
            opacityLabel.Visible = true;
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            undoRedoManager.Undo();
            RedrawAnnotations();
            pictureBox.Invalidate(); // Refresh the PictureBox to reflect changes
            UpdateUndoRedoButtons(); // Update the state of the buttons

            // Copy the updated image to the clipboard
            CopyImageToClipboard();
        }

        private void RedoButton_Click(object sender, EventArgs e)
        {
            undoRedoManager.Redo();
            RedrawAnnotations();
            pictureBox.Invalidate(); // Refresh the PictureBox to reflect changes
            UpdateUndoRedoButtons(); // Update the state of the buttons

            // Copy the updated image to the clipboard
            CopyImageToClipboard();
        }
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = undoRedoManager.CanUndo;
            redoButton.Enabled = undoRedoManager.CanRedo;
            clearAllButton.Enabled = annotations.Count > 0;
        }
        private void RedrawAnnotations()
        {
            if (capturedImage == null) return;

            Image imageCopy = new Bitmap(capturedImage);

            using (Graphics g = Graphics.FromImage(imageCopy))
            {
                foreach (var annotation in annotations)
                {
                    if (annotation.Type == AnnotationType.Highlighter)
                    {
                        // Create semi-transparent brush for highlighter effect
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(
                            (int)(25 * annotation.Opacity),
                            annotation.Color)))
                        {
                            g.FillRectangle(brush, annotation.Rectangle);
                        }
                    }
                    else // Rectangle
                    {
                        using (Pen pen = new Pen(annotation.Color, 2))
                        {
                            g.DrawRectangle(pen, annotation.Rectangle);
                        }
                    }
                }
            }

            pictureBox.Image = imageCopy;
        }
        private void CopyImageToClipboard()
        {
            if (capturedImage == null) return;

            using (Bitmap imageCopy = new Bitmap(capturedImage.Width, capturedImage.Height))
            {
                using (Graphics g = Graphics.FromImage(imageCopy))
                {
                    // Enable high quality rendering
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    // Draw the original image
                    g.DrawImage(capturedImage, Point.Empty);

                    // Draw annotations
                    foreach (var annotation in annotations)
                    {
                        if (annotation.Type == AnnotationType.Highlighter)
                        {
                            DrawSoftHighlight(g, annotation.Rectangle, annotation.Color, annotation.Opacity);
                        }
                        else
                        {
                            // Regular rectangle annotation
                            using (Pen pen = new Pen(annotation.Color, 2))
                            {
                                g.DrawRectangle(pen, annotation.Rectangle);
                            }
                        }
                    }
                }

                Clipboard.SetImage(imageCopy);
            }
        }

     

        private Tool currentTool = Tool.Rectangle;

        // Update your DrawSoftHighlight method to use the trackbar value
        private void DrawSoftHighlight(Graphics g, Rectangle rect, Color color, float opacity)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRectangle(rect);
                using (PathGradientBrush pgBrush = new PathGradientBrush(path))
                {
                    // Use the specific opacity value for this highlight
                    int centerAlpha = (int)(160 * opacity);
                    int surroundAlpha = (int)(100 * opacity);

                    pgBrush.CenterColor = Color.FromArgb(centerAlpha, color);
                    Color[] surroundColors = new Color[] { Color.FromArgb(surroundAlpha, color) };
                    pgBrush.SurroundColors = surroundColors;
                    pgBrush.FocusScales = new PointF(0.95f, 0.85f);
                    g.FillPath(pgBrush, path);
                }
            }
        }
        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (annotations.Count > 0)
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to clear all annotations?",
                    "Confirm Clear All",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    annotations.Clear();
                    RedrawAnnotations();
                    undoRedoManager = new UndoRedoManager();
                    pictureBox.Invalidate();
                    UpdateUndoRedoButtons();
                    CopyImageToClipboard();
                }
            }
        }
    }

    public class CaptureForm : Form
    {
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isDragging = false;
        public Image CapturedImage { get; private set; }

        public CaptureForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.TopMost = true;

            this.MouseDown += CaptureForm_MouseDown;
            this.MouseMove += CaptureForm_MouseMove;
            this.MouseUp += CaptureForm_MouseUp;
            this.Paint += CaptureForm_Paint;
            this.KeyDown += CaptureForm_KeyDown;
        }

        private void CaptureForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void CaptureForm_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            startPoint = e.Location;
            selectionRect = new Rectangle();
        }

        private void CaptureForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(e.X - startPoint.X);
                int height = Math.Abs(e.Y - startPoint.Y);

                selectionRect = new Rectangle(x, y, width, height);
                this.Invalidate();
            }
        }

        private void CaptureForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    // Hide the form before capturing to avoid capturing the overlay
                    this.Hide();
                    System.Threading.Thread.Sleep(100); // Give Windows time to redraw
                    CaptureScreen();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void CaptureForm_Paint(object sender, PaintEventArgs e)
        {
            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    e.Graphics.DrawRectangle(pen, selectionRect);

                    // Draw size indicator
                    string sizeText = $"{selectionRect.Width} x {selectionRect.Height}";
                    using (Font font = new Font("Arial", 10))
                    using (SolidBrush brush = new SolidBrush(Color.White))
                    using (var bgBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
                    {
                        var textSize = e.Graphics.MeasureString(sizeText, font);
                        var textRect = new RectangleF(
                            selectionRect.X,
                            selectionRect.Y - textSize.Height - 5,
                            textSize.Width + 10,
                            textSize.Height + 5
                        );

                        e.Graphics.FillRectangle(bgBrush, textRect);
                        e.Graphics.DrawString(sizeText, font, brush,
                            selectionRect.X + 5,
                            selectionRect.Y - textSize.Height - 3);
                    }
                }
            }
        }

        private void CaptureScreen()
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(selectionRect.Width, selectionRect.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(selectionRect.Location, Point.Empty, selectionRect.Size);
                    }
                    CapturedImage = new Bitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screenshot: {ex.Message}", "Capture Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        public CustomToolStripRenderer() : base(new CustomColorTable())
        {
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripButton button)
            {
                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

                // Draw modern button background with rounded corners
                if (button.Selected || button.Checked)
                {
                    using (GraphicsPath path = CreateRoundedRectangle(bounds, 4))
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(30, 144, 255)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                else if (button.Pressed)
                {
                    using (GraphicsPath path = CreateRoundedRectangle(bounds, 4))
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }



    public class CustomColorTable : ProfessionalColorTable
    {
        // Modern light theme colors
        private Color primaryColor = Color.White;           // White background
        private Color hoverColor = Color.FromArgb(242, 242, 242);    // Light gray for hover
        private Color accentColor = Color.FromArgb(0, 120, 215);     // Blue accent for selections

        public override Color ToolStripGradientBegin => primaryColor;
        public override Color ToolStripGradientMiddle => primaryColor;
        public override Color ToolStripGradientEnd => primaryColor;
        public override Color ButtonSelectedHighlight => hoverColor;
        public override Color ButtonSelectedHighlightBorder => accentColor;
        public override Color ButtonSelectedBorder => accentColor;
        public override Color ButtonCheckedHighlight => hoverColor;
        public override Color ButtonCheckedHighlightBorder => accentColor;
        public override Color ButtonPressedBorder => accentColor;
        public override Color MenuItemSelected => hoverColor;
        public override Color MenuItemBorder => Color.FromArgb(229, 229, 229);
        public override Color MenuBorder => Color.FromArgb(229, 229, 229);
    }
    public class AddAnnotationCommand : ICommand
    {
        private List<Annotation> _annotations;
        private Annotation _annotation;

        public AddAnnotationCommand(List<Annotation> annotations, Annotation annotation)
        {
            _annotations = annotations;
            _annotation = annotation;
        }

        public void Execute()
        {
            _annotations.Add(_annotation);
        }

        public void Undo()
        {
            _annotations.Remove(_annotation);
        }
    }



    // Modify your Annotation class to handle variable opacity
    
    public class UndoRedoManager
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (CanUndo)
            {
                ICommand command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                ICommand command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
            }
        }
    }
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    
}