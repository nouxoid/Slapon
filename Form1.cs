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
        // Control declarations
        private Panel annotationPanel;
        private Button deleteButton;
        private Button colorButton;
        private TrackBar opacityTrackBar;
        private Label opacityLabel;
        private RadioButton rectangleButton;
        private RadioButton highlighterButton;
        private Panel thicknessPanel;
        private TrackBar thicknessTrackBar;
        private Label thicknessLabel;

        // Tool settings
        private Color currentColor = Color.Red; // Default color
        private float currentOpacity = 1.0f;   // Default opacity (100%)
        private float currentThickness = 2.0f;  // Default line thickness
        private AnnotationType currentAnnotationType = AnnotationType.Rectangle; // Default tool


        private bool isDragging;
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

        private const int HANDLE_SIZE = 8;
        private bool isResizing = false;
        private ResizeHandle currentHandle = ResizeHandle.None;

        private System.Windows.Forms.ToolStrip toolStrip1;


        private System.Windows.Forms.Label dateTimeLabel;

        private Color currentColor = Color.Red; // Default color
        private ToolStripDropDownButton colorPickerButton;
        private const int COLOR_BUTTON_SIZE = 20;

        // Add these at the class level with your other private fields
        private System.Windows.Forms.TrackBar opacityTrackBar;
        private System.Windows.Forms.Label opacityLabel;
        private int defaultOpacity = 50;

        private System.Windows.Forms.TrackBar thicknessTrackBar;
        private Label thicknessLabel;
        private float currentThickness = 2f;  // Default thickness




        public enum AnnotationType
        {
            Rectangle,
            Highlighter
        }


        public class Annotation
        {
            // Properties
            public Rectangle Bounds { get; private set; }  // Using 'Bounds' to avoid confusion with System.Drawing.Rectangle
            public Color Color { get; private set; }
            public AnnotationType Type { get; private set; }
            public float Opacity { get; private set; }
            public float LineThickness { get; private set; }

            // Constructor
            public Annotation(Rectangle bounds, Color color, AnnotationType type, float opacity, float lineThickness = 2f)
            {
                Bounds = bounds;
                Color = color;
                Type = type;
                Opacity = opacity;
                LineThickness = lineThickness;
            }

            // Add method to update LineThickness
            public void UpdateLineThickness(float thickness)
            {
                LineThickness = thickness;
            }

            // Update Clone method to include LineThickness
            public Annotation Clone()
            {
                return new Annotation(Bounds, Color, Type, Opacity, LineThickness);
            }


            // Methods for modifying properties
            public void MoveTo(Point newLocation)
            {
                Bounds = new Rectangle(newLocation, Bounds.Size);
            }

            public void Resize(Rectangle newBounds)
            {
                Bounds = newBounds;
            }

            public void UpdateProperties(Color color, float opacity)
            {
                Color = color;
                Opacity = opacity;
            }

            // Hit testing
            public bool Contains(Point point)
            {
                return Bounds.Contains(point);
            }

            // Resize handle detection
            public ResizeHandle GetResizeHandle(Point point)
            {
                const int handleSize = 6;
                var handleRect = new Rectangle(point.X - handleSize / 2, point.Y - handleSize / 2, handleSize, handleSize);

                // Top-left
                if (handleRect.IntersectsWith(new Rectangle(Bounds.Left, Bounds.Top, handleSize, handleSize)))
                    return ResizeHandle.TopLeft;

                // Top-right
                if (handleRect.IntersectsWith(new Rectangle(Bounds.Right - handleSize, Bounds.Top, handleSize, handleSize)))
                    return ResizeHandle.TopRight;

                // Bottom-left
                if (handleRect.IntersectsWith(new Rectangle(Bounds.Left, Bounds.Bottom - handleSize, handleSize, handleSize)))
                    return ResizeHandle.BottomLeft;

                // Bottom-right
                if (handleRect.IntersectsWith(new Rectangle(Bounds.Right - handleSize, Bounds.Bottom - handleSize, handleSize, handleSize)))
                    return ResizeHandle.BottomRight;

                return ResizeHandle.None;
            }

            // Deep clone support
            public Annotation Clone()
            {
                return new Annotation(Bounds, Color, Type, Opacity);
            }
        }


        // Command for handling annotation moves
        public class MoveAnnotationCommand : ICommand
        {
            private readonly Annotation annotation;
            private readonly Point originalLocation;
            private readonly Point newLocation;

            public MoveAnnotationCommand(Annotation annotation, Point originalLocation, Point newLocation)
            {
                this.annotation = annotation;
                this.originalLocation = originalLocation;
                this.newLocation = newLocation;
            }

            public void Execute()
            {
                annotation.MoveTo(newLocation);
            }

            public void Undo()
            {
                annotation.MoveTo(originalLocation);
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
            InitializeThicknessControls();
            InitializeAnnotationControls();
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

        // Add this method to initialize the controls
        private void InitializeAnnotationControls()
        {
            // Create main annotation panel
            annotationPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                Padding = new Padding(10)
            };

            // Create color button
            colorButton = new Button
            {
                Text = "Color",
                BackColor = currentColor,
                FlatStyle = FlatStyle.Flat,
                Width = 80,
                Height = 30
            };
            colorButton.Click += ColorButton_Click;

            // Create delete button
            deleteButton = new Button
            {
                Text = "Delete",
                Width = 80,
                Height = 30,
                Enabled = false
            };
            deleteButton.Click += DeleteButton_Click;

            // Create opacity controls
            opacityLabel = new Label
            {
                Text = "Opacity:",
                AutoSize = true
            };

            opacityTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = (int)(currentOpacity * 100),
                Width = 150
            };
            opacityTrackBar.ValueChanged += OpacityTrackBar_ValueChanged;

            // Create annotation type controls
            rectangleButton = new RadioButton
            {
                Text = "Rectangle",
                Checked = true
            };
            rectangleButton.CheckedChanged += AnnotationType_CheckedChanged;

            highlighterButton = new RadioButton
            {
                Text = "Highlighter"
            };
            highlighterButton.CheckedChanged += AnnotationType_CheckedChanged;

            // Create thickness controls
            thicknessPanel = new Panel
            {
                Height = 50
            };

            thicknessLabel = new Label
            {
                Text = "Thickness:",
                AutoSize = true
            };

            thicknessTrackBar = new TrackBar
            {
                Minimum = 1,
                Maximum = 10,
                Value = (int)currentThickness,
                Width = 150
            };
            thicknessTrackBar.ValueChanged += ThicknessTrackBar_ValueChanged;

            // Layout controls
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            flowLayout.Controls.AddRange(new Control[]
            {
            colorButton,
            deleteButton,
            opacityLabel,
            opacityTrackBar,
            rectangleButton,
            highlighterButton,
            thicknessPanel
            });

            thicknessPanel.Controls.AddRange(new Control[]
            {
            thicknessLabel,
            thicknessTrackBar
            });

            annotationPanel.Controls.Add(flowLayout);
            this.Controls.Add(annotationPanel);
        }

        // Add these event handlers
        private void ColorButton_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = currentColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = colorDialog.Color;
                    colorButton.BackColor = currentColor;
                    if (selectedAnnotation != null)
                    {
                        var originalState = selectedAnnotation.Clone();
                        selectedAnnotation.UpdateProperties(currentColor, selectedAnnotation.Opacity);
                        var command = new AnnotationPropertyChangeCommand(
                            selectedAnnotation,
                            originalState,
                            selectedAnnotation.Clone()
                        );
                        undoRedoManager.ExecuteCommand(command);
                        pictureBox.Invalidate();
                    }
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (selectedAnnotation != null)
            {
                annotations.Remove(selectedAnnotation);
                selectedAnnotation = null;
                pictureBox.Invalidate();
                UpdateControlsForSelectedAnnotation();
            }
        }

        private void OpacityTrackBar_ValueChanged(object sender, EventArgs e)
        {
            float opacity = opacityTrackBar.Value / 100f;
            if (selectedAnnotation != null)
            {
                var originalState = selectedAnnotation.Clone();
                selectedAnnotation.UpdateProperties(selectedAnnotation.Color, opacity);
                var command = new AnnotationPropertyChangeCommand(
                    selectedAnnotation,
                    originalState,
                    selectedAnnotation.Clone()
                );
                undoRedoManager.ExecuteCommand(command);
                pictureBox.Invalidate();
            }
            else
            {
                currentOpacity = opacity;
            }
            opacityLabel.Text = $"Opacity: {opacityTrackBar.Value}%";
        }

        private void ThicknessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (selectedAnnotation != null && selectedAnnotation.Type == AnnotationType.Rectangle)
            {
                var originalState = selectedAnnotation.Clone();
                selectedAnnotation.UpdateLineThickness(thicknessTrackBar.Value);
                var command = new AnnotationPropertyChangeCommand(
                    selectedAnnotation,
                    originalState,
                    selectedAnnotation.Clone()
                );
                undoRedoManager.ExecuteCommand(command);
                pictureBox.Invalidate();
            }
            else
            {
                currentThickness = thicknessTrackBar.Value;
            }
            thicknessLabel.Text = $"Thickness: {thicknessTrackBar.Value}";
        }

        private void AnnotationType_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                currentAnnotationType = rb == rectangleButton ?
                    AnnotationType.Rectangle : AnnotationType.Highlighter;
                thicknessPanel.Visible = currentAnnotationType == AnnotationType.Rectangle;
            }
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

        private void DrawSelectionHandles(Graphics g, Rectangle rect)
        {
            var handles = new[]
            {
        // Corners
        new { Pos = ResizeHandle.TopLeft, Rect = new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.TopRight, Rect = new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.BottomLeft, Rect = new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.BottomRight, Rect = new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        // Sides
        new { Pos = ResizeHandle.Top, Rect = new Rectangle(rect.Left + rect.Width/2 - HANDLE_SIZE/2, rect.Top - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.Bottom, Rect = new Rectangle(rect.Left + rect.Width/2 - HANDLE_SIZE/2, rect.Bottom - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.Left, Rect = new Rectangle(rect.Left - HANDLE_SIZE/2, rect.Top + rect.Height/2 - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) },
        new { Pos = ResizeHandle.Right, Rect = new Rectangle(rect.Right - HANDLE_SIZE/2, rect.Top + rect.Height/2 - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE) }
    };

            using (var handleBrush = new SolidBrush(Color.White))
            using (var handlePen = new Pen(Color.FromArgb(0, 120, 215), 1))
            {
                foreach (var handle in handles)
                {
                    g.FillRectangle(handleBrush, handle.Rect);
                    g.DrawRectangle(handlePen, handle.Rect);
                }
            }
        }

        private void InitializeThicknessControls()
        {
            // Create toolStrip if it doesn't exist
            if (toolStrip1 == null)
            {
                toolStrip1 = new System.Windows.Forms.ToolStrip();
                this.Controls.Add(toolStrip1);
            }

            // Create the label
            thicknessLabel = new System.Windows.Forms.Label
            {
                Text = "Thickness:",
                AutoSize = true,
                Visible = false
            };

            // Create the trackbar with fully qualified name
            thicknessTrackBar = new System.Windows.Forms.TrackBar
            {   // Changed this line - removed extra {
                Minimum = 1,
                Maximum = 10,
                Value = 2,
                Width = 100,
                Visible = false,
                TickFrequency = 1,
                TickStyle = System.Windows.Forms.TickStyle.Both
            };  // Object initializer ends here

            thicknessTrackBar.ValueChanged += ThicknessTrackBar_ValueChanged;

            var labelHost = new System.Windows.Forms.ToolStripControlHost(thicknessLabel);
            var trackBarHost = new System.Windows.Forms.ToolStripControlHost(thicknessTrackBar);

            toolStrip1.Items.Add(labelHost);
            toolStrip1.Items.Add(trackBarHost);
        }

        private void ThicknessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (selectedAnnotation != null && selectedAnnotation.Type == AnnotationType.Rectangle)
            {
                float oldThickness = selectedAnnotation.LineThickness;
                float newThickness = thicknessTrackBar.Value;

                var command = new AnnotationPropertyChangeCommand(
                    selectedAnnotation,
                    "LineThickness",
                    oldThickness,
                    newThickness
                );

                undoRedoManager.ExecuteCommand(command);
                pictureBox.Invalidate();
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

        private void UpdateSelectionRectangle(Point currentPoint)
        {
            if (isDrawingAnnotation)
            {
                int x = Math.Min(startPoint.X, currentPoint.X);
                int y = Math.Min(startPoint.Y, currentPoint.Y);
                int width = Math.Abs(currentPoint.X - startPoint.X);
                int height = Math.Abs(currentPoint.Y - startPoint.Y);
                selectionRect = new Rectangle(x, y, width, height);
            }
        }

        private void UpdateCursor(Point point)
        {
            if (selectedAnnotation != null)
            {
                ResizeHandle handle = selectedAnnotation.GetResizeHandle(point);
                switch (handle)
                {
                    case ResizeHandle.TopLeft:
                    case ResizeHandle.BottomRight:
                        this.Cursor = Cursors.SizeNWSE;
                        break;
                    case ResizeHandle.TopRight:
                    case ResizeHandle.BottomLeft:
                        this.Cursor = Cursors.SizeNESW;
                        break;
                    case ResizeHandle.Top:
                    case ResizeHandle.Bottom:
                        this.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeHandle.Left:
                    case ResizeHandle.Right:
                        this.Cursor = Cursors.SizeWE;
                        break;
                    default:
                        this.Cursor = selectedAnnotation.Contains(point) ? Cursors.SizeAll : Cursors.Default;
                        break;
                }
            }
            else
            {
                this.Cursor = isDrawingAnnotation ? Cursors.Cross : Cursors.Default;
            }
        }

        private void ResizeSelectedAnnotation(Point currentPoint)
        {
            if (selectedAnnotation != null && isResizing)
            {
                Rectangle oldBounds = selectedAnnotation.Bounds;
                Rectangle newBounds = oldBounds;

                switch (currentHandle)
                {
                    case ResizeHandle.TopLeft:
                        newBounds = new Rectangle(
                            currentPoint.X,
                            currentPoint.Y,
                            oldBounds.Right - currentPoint.X,
                            oldBounds.Bottom - currentPoint.Y
                        );
                        break;
                    case ResizeHandle.TopRight:
                        newBounds = new Rectangle(
                            oldBounds.X,
                            currentPoint.Y,
                            currentPoint.X - oldBounds.X,
                            oldBounds.Bottom - currentPoint.Y
                        );
                        break;
                    case ResizeHandle.BottomLeft:
                        newBounds = new Rectangle(
                            currentPoint.X,
                            oldBounds.Y,
                            oldBounds.Right - currentPoint.X,
                            currentPoint.Y - oldBounds.Y
                        );
                        break;
                    case ResizeHandle.BottomRight:
                        newBounds = new Rectangle(
                            oldBounds.X,
                            oldBounds.Y,
                            currentPoint.X - oldBounds.X,
                            currentPoint.Y - oldBounds.Y
                        );
                        break;
                    case ResizeHandle.Top:
                        newBounds = new Rectangle(
                            oldBounds.X,
                            currentPoint.Y,
                            oldBounds.Width,
                            oldBounds.Bottom - currentPoint.Y
                        );
                        break;
                    case ResizeHandle.Bottom:
                        newBounds = new Rectangle(
                            oldBounds.X,
                            oldBounds.Y,
                            oldBounds.Width,
                            currentPoint.Y - oldBounds.Y
                        );
                        break;
                    case ResizeHandle.Left:
                        newBounds = new Rectangle(
                            currentPoint.X,
                            oldBounds.Y,
                            oldBounds.Right - currentPoint.X,
                            oldBounds.Height
                        );
                        break;
                    case ResizeHandle.Right:
                        newBounds = new Rectangle(
                            oldBounds.X,
                            oldBounds.Y,
                            currentPoint.X - oldBounds.X,
                            oldBounds.Height
                        );
                        break;
                }

                // Ensure minimum size
                if (newBounds.Width >= 5 && newBounds.Height >= 5)
                {
                    // Store original state
                    Annotation originalState = selectedAnnotation.Clone();

                    // Apply the resize
                    selectedAnnotation.Resize(newBounds);

                    // Create command with full annotation state
                    var command = new AnnotationPropertyChangeCommand(
                        selectedAnnotation,
                        originalState,
                        selectedAnnotation.Clone()
                    );

                    undoRedoManager.ExecuteCommand(command);
                    pictureBox.Invalidate();
                    CopyImageToClipboard();
                }
            }
        }

        private void MoveSelectedAnnotation(Point currentPoint)
        {
            if (selectedAnnotation != null && this.isDragging)
            {
                int dx = currentPoint.X - startPoint.X;
                int dy = currentPoint.Y - startPoint.Y;

                Rectangle oldBounds = selectedAnnotation.Bounds;
                Point newLocation = new Point(oldBounds.X + dx, oldBounds.Y + dy);

                // Store original state before move
                Annotation originalState = selectedAnnotation.Clone();

                // Move the annotation
                selectedAnnotation.MoveTo(newLocation);

                // Create command with full annotation state
                var command = new AnnotationPropertyChangeCommand(
                    selectedAnnotation,
                    originalState,
                    selectedAnnotation.Clone()
                );

                undoRedoManager.ExecuteCommand(command);
                startPoint = currentPoint;
                pictureBox.Invalidate();
                CopyImageToClipboard();
            }
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

        private void DrawSelectionIndicator(Graphics g, Annotation annotation)
        {
            using (Pen selectionPen = new Pen(Color.FromArgb(128, 0, 120, 215), 1))
            {
                selectionPen.DashStyle = DashStyle.Dash;
                Rectangle selectionBounds = annotation.Bounds;
                selectionBounds.Inflate(2, 2); // Make selection slightly larger
                g.DrawRectangle(selectionPen, selectionBounds);

                // Draw resize handles
                DrawResizeHandles(g, selectionBounds);
            }
        }

        private void HandleSelection(Point mousePoint)
        {
            // Find the most recently created annotation that contains the click point
            selectedAnnotation = annotations
                .Where(a => a.Bounds.Contains(mousePoint))  // Changed Rectangle to Bounds
                .LastOrDefault();

            if (selectedAnnotation != null)
            {
                // Enable all annotation controls
                annotationPanel.Enabled = true;
                deleteButton.Enabled = true;

                // Update color button
                colorButton.BackColor = selectedAnnotation.Color;

                // Show and update opacity controls
                opacityTrackBar.Visible = true;
                opacityLabel.Visible = true;
                opacityTrackBar.Value = (int)(selectedAnnotation.Opacity * 100);

                // Update thickness controls for rectangle annotations
                thicknessTrackBar.Visible = (selectedAnnotation.Type == AnnotationType.Rectangle);
                thicknessLabel.Visible = (selectedAnnotation.Type == AnnotationType.Rectangle);
                if (selectedAnnotation.Type == AnnotationType.Rectangle)
                {
                    thicknessTrackBar.Value = (int)selectedAnnotation.LineThickness;
                }
            }
            else
            {
                // Reset controls when no annotation is selected
                annotationPanel.Enabled = true;
                deleteButton.Enabled = false;

                // Keep controls visible but show current tool settings
                opacityTrackBar.Value = (int)(currentOpacity * 100);
                thicknessTrackBar.Value = (int)currentThickness;

                // Update visibility based on current tool type
                thicknessTrackBar.Visible = (currentAnnotationType == AnnotationType.Rectangle);
                thicknessLabel.Visible = (currentAnnotationType == AnnotationType.Rectangle);
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
                startPoint = e.Location;

                // Check if clicking on an existing annotation
                selectedAnnotation = annotations
                    .LastOrDefault(a => a.Contains(e.Location));

                if (selectedAnnotation != null)
                {
                    currentHandle = selectedAnnotation.GetResizeHandle(e.Location);

                    if (currentHandle != ResizeHandle.None)
                    {
                        isResizing = true;
                    }
                    else
                    {
                        isDragging = true;
                        originalPosition = e.Location;
                        originalAnnotation = selectedAnnotation.Clone();
                    }

                    // Show appropriate controls based on annotation type
                    UpdateControlsForSelectedAnnotation();
                }
                else
                {
                    // Start drawing new annotation
                    isDrawingAnnotation = true;
                    selectionRect = new Rectangle(startPoint, Size.Empty);
                    selectedAnnotation = null;
                }

                pictureBox.Invalidate();
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawingAnnotation)
                {
                    // Update selection rectangle
                    int width = e.X - startPoint.X;
                    int height = e.Y - startPoint.Y;
                    selectionRect = new Rectangle(
                        width > 0 ? startPoint.X : e.X,
                        height > 0 ? startPoint.Y : e.Y,
                        Math.Abs(width),
                        Math.Abs(height)
                    );
                }
                else if (isDragging && selectedAnnotation != null)
                {
                    // Calculate the offset from the original position
                    int deltaX = e.X - originalPosition.X;
                    int deltaY = e.Y - originalPosition.Y;

                    // Move the annotation by the offset
                    Point newLocation = new Point(
                        originalAnnotation.Bounds.X + deltaX,
                        originalAnnotation.Bounds.Y + deltaY
                    );
                    selectedAnnotation.MoveTo(newLocation);
                }
                else if (isResizing && selectedAnnotation != null)
                {
                    Rectangle newBounds = selectedAnnotation.Bounds;
                    Point mousePoint = e.Location;

                    // Adjust the bounds based on which handle is being dragged
                    switch (currentHandle)
                    {
                        case ResizeHandle.TopLeft:
                            newBounds = new Rectangle(
                                mousePoint.X,
                                mousePoint.Y,
                                selectedAnnotation.Bounds.Right - mousePoint.X,
                                selectedAnnotation.Bounds.Bottom - mousePoint.Y
                            );
                            break;
                        case ResizeHandle.TopRight:
                            newBounds = new Rectangle(
                                selectedAnnotation.Bounds.Left,
                                mousePoint.Y,
                                mousePoint.X - selectedAnnotation.Bounds.Left,
                                selectedAnnotation.Bounds.Bottom - mousePoint.Y
                            );
                            break;
                        case ResizeHandle.BottomLeft:
                            newBounds = new Rectangle(
                                mousePoint.X,
                                selectedAnnotation.Bounds.Top,
                                selectedAnnotation.Bounds.Right - mousePoint.X,
                                mousePoint.Y - selectedAnnotation.Bounds.Top
                            );
                            break;
                        case ResizeHandle.BottomRight:
                            newBounds = new Rectangle(
                                selectedAnnotation.Bounds.Left,
                                selectedAnnotation.Bounds.Top,
                                mousePoint.X - selectedAnnotation.Bounds.Left,
                                mousePoint.Y - selectedAnnotation.Bounds.Top
                            );
                            break;
                    }

                    // Ensure width and height are positive
                    if (newBounds.Width > 0 && newBounds.Height > 0)
                    {
                        selectedAnnotation.Resize(newBounds);
                    }
                }

                pictureBox.Invalidate();
            }
        }

        // Update your PictureBox_MouseUp method to include opacity
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isDrawingAnnotation && selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    // Create new annotation
                    var newAnnotation = new Annotation(
                        selectionRect,
                        currentColor,
                        currentAnnotationType,
                        currentOpacityy,
                        thicknessTrackBar.Value  // Include current thickness
                    );
                    annotations.Add(newAnnotation);
                    selectedAnnotation = newAnnotation;
                }
                else if (isDragging && selectedAnnotation != null)
                {
                    // Create and execute move command for undo/redo
                    var command = new MoveAnnotationCommand(
                        selectedAnnotation,
                        new Point(originalAnnotation.Bounds.X, originalAnnotation.Bounds.Y),
                        new Point(selectedAnnotation.Bounds.X, selectedAnnotation.Bounds.Y)
                    );
                    undoRedoManager.ExecuteCommand(command);
                }

                // Reset states
                isDrawingAnnotation = false;
                isDragging = false;
                isResizing = false;
                currentHandle = ResizeHandle.None;

                pictureBox.Invalidate();
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
                    DrawSoftHighlight(e.Graphics, annotation.Bounds, annotation.Color, annotation.Opacity);
                }
                else
                {
                    using (Pen pen = new Pen(annotation.Color, annotation.LineThickness))
                    {
                        e.Graphics.DrawRectangle(pen, annotation.Bounds);
                    }
                }

                // Draw selection indicator if this annotation is selected
                if (annotation == selectedAnnotation)
                {
                    DrawSelectionIndicator(e.Graphics, annotation);
                }
            }

            // Draw the current selection rectangle if we're drawing a new annotation
            if (isDrawingAnnotation && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                using (Pen pen = new Pen(currentColor, currentThickness))
                {
                    if (currentAnnotationType == AnnotationType.Highlighter)
                    {
                        DrawSoftHighlight(e.Graphics, selectionRect, currentColor, currentOpacity);
                    }
                    else
                    {
                        e.Graphics.DrawRectangle(pen, selectionRect);
                    }
                }
            }

            // Draw resize handles for selected annotation
            if (selectedAnnotation != null)
            {
                DrawResizeHandles(e.Graphics, selectedAnnotation.Bounds);
            }
        }

        private void UpdateControlsForSelectedAnnotation()
        {
            if (selectedAnnotation != null)
            {
                // Enable all annotation controls
                annotationPanel.Visible = true;
                annotationPanel.Enabled = true;
                deleteButton.Enabled = true;

                // Update color button to show current annotation color
                colorButton.BackColor = selectedAnnotation.Color;

                // Update opacity control
                opacityTrackBar.Value = (int)(selectedAnnotation.Opacity * 100);
                opacityLabel.Text = $"Opacity: {opacityTrackBar.Value}%";

                // Update annotation type selection
                rectangleButton.Checked = (selectedAnnotation.Type == AnnotationType.Rectangle);
                highlighterButton.Checked = (selectedAnnotation.Type == AnnotationType.Highlighter);

                // Show/hide specific controls based on annotation type
                if (selectedAnnotation.Type == AnnotationType.Rectangle)
                {
                    thicknessPanel.Visible = true;
                    thicknessTrackBar.Value = (int)selectedAnnotation.LineThickness;
                    thicknessLabel.Text = $"Thickness: {thicknessTrackBar.Value}";
                }
                else if (selectedAnnotation.Type == AnnotationType.Highlighter)
                {
                    // Highlighter-specific controls
                    thicknessPanel.Visible = false;
                }

                // Update the current tool settings to match the selected annotation
                currentColor = selectedAnnotation.Color;
                currentOpacity = selectedAnnotation.Opacity;
                currentAnnotationType = selectedAnnotation.Type;
            }
            else
            {
                // No annotation selected - reset controls to defaults
                annotationPanel.Visible = true;
                annotationPanel.Enabled = true;
                deleteButton.Enabled = false;

                // Reset type selection to current tool settings
                rectangleButton.Checked = (currentAnnotationType == AnnotationType.Rectangle);
                highlighterButton.Checked = (currentAnnotationType == AnnotationType.Highlighter);

                // Update controls to show current tool settings
                colorButton.BackColor = currentColor;
                opacityTrackBar.Value = (int)(currentOpacity * 100);
                opacityLabel.Text = $"Opacity: {opacityTrackBar.Value}%";

                // Show/hide specific controls based on current tool type
                if (currentAnnotationType == AnnotationType.Rectangle)
                {
                    thicknessPanel.Visible = true;
                    thicknessTrackBar.Value = (int)(currentOpacity * 100);
                    thicknessLabel.Text = $"Thickness: {thicknessTrackBar.Value}%";
                }
                else if (currentAnnotationType == AnnotationType.Highlighter)
                {
                    thicknessPanel.Visible = false;
                }
            }

            // Force control updates
            annotationPanel.Refresh();
        }

        public class AnnotationPropertyChangeCommand : ICommand
        {
            private readonly Annotation annotation;
            private readonly Annotation oldState;
            private readonly Annotation newState;

            public AnnotationPropertyChangeCommand(Annotation annotation, Annotation oldState, Annotation newState)
            {
                this.annotation = annotation;
                this.oldState = oldState;
                this.newState = newState;
            }

            public void Execute()
            {
                // Apply all properties from newState
                ApplyState(newState);
            }

            public void Undo()
            {
                // Restore all properties from oldState
                ApplyState(oldState);
            }

            private void ApplyState(Annotation state)
            {
                annotation.Resize(state.Bounds);
                annotation.UpdateProperties(state.Color, state.Opacity);
                annotation.UpdateLineThickness(state.LineThickness);
                // Add any other properties that need to be updated
            }
        }

        private void SetupThicknessControl()
        {
            thicknessLabel = new Label
            {
                Text = "Thickness:",
                AutoSize = true,
                Visible = false
            };

            thicknessTrackBar = new System.Windows.Forms.TrackBar
            {
                Minimum = 1,
                Maximum = 10,
                Value = 2,
                Width = 100,
                Visible = false,
                TickFrequency = 1,
                TickStyle = TickStyle.Both
            };

            thicknessTrackBar.ValueChanged += (s, e) =>
            {
                if (selectedAnnotation != null && selectedAnnotation.Type == AnnotationType.Rectangle)
                {
                    float oldThickness = selectedAnnotation.LineThickness;
                    float newThickness = thicknessTrackBar.Value;

                    var command = new AnnotationPropertyChangeCommand(
                        selectedAnnotation,
                        "LineThickness",
                        oldThickness,
                        newThickness
                    );

                    undoRedoManager.ExecuteCommand(command);
                    pictureBox.Invalidate();
                    CopyImageToClipboard();
                }
            };

            // Add to toolstrip similarly to opacity controls
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
                            g.FillRectangle(brush, annotation.Bounds);
                        }
                    }
                    else // Rectangle
                    {
                        using (Pen pen = new Pen(annotation.Color, annotation.LineThickness))
                        {
                            g.DrawRectangle(pen, annotation.Bounds);
                        }
                    }

                    // Draw resize handles if this is the selected annotation
                    if (annotation == selectedAnnotation)
                    {
                        DrawResizeHandles(g, annotation.Bounds);
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
                            DrawSoftHighlight(g, annotation.Bounds, annotation.Color, annotation.Opacity);
                        }
                        else
                        {
                            // Regular rectangle annotation
                            using (Pen pen = new Pen(annotation.Color, annotation.LineThickness))
                            {
                                g.DrawRectangle(pen, annotation.Bounds);
                            }
                        }
                    }
                }

                Clipboard.SetImage(imageCopy);
            }
        }



        private Tool currentTool = Tool.Rectangle;


        // Helper method for drawing resize handles
        // Make sure this matches your current DrawResizeHandles implementation
        private void DrawResizeHandles(Graphics g, Rectangle bounds)
        {
            const int handleSize = 6;
            var handleColor = Color.White;
            var handleBorderColor = Color.Black;

            void DrawHandle(int x, int y)
            {
                var handleRect = new Rectangle(x - handleSize / 2, y - handleSize / 2, handleSize, handleSize);
                using (var brush = new SolidBrush(handleColor))
                using (var pen = new Pen(handleBorderColor, 1))
                {
                    g.FillRectangle(brush, handleRect);
                    g.DrawRectangle(pen, handleRect);
                }
            }

            // Draw handles at all corners
            DrawHandle(bounds.Left, bounds.Top);           // Top-left
            DrawHandle(bounds.Right, bounds.Top);          // Top-right
            DrawHandle(bounds.Left, bounds.Bottom);        // Bottom-left
            DrawHandle(bounds.Right, bounds.Bottom);       // Bottom-right
        }

        // Update your DrawSoftHighlight method to use the trackbar value
        private void DrawSoftHighlight(Graphics g, Rectangle bounds, Color color, float opacity)
        {
            using (var brush = new SolidBrush(Color.FromArgb(
                (int)(25 * opacity),
                color)))
            {
                g.FillRectangle(brush, bounds);
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


    // Make sure you have this enum defined
    public enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right
    }
    public class CaptureForm : Form
    {
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isDragging;


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