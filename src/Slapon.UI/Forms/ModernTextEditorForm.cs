using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Slapon.Core.Models;

namespace Slapon.UI.Forms
{
    public partial class ModernTextEditorForm : Form
    {
        private TextBox textInput;
        private ComboBox fontFamilyCombo;
        private NumericUpDown fontSizeNumeric;
        private CheckBox boldCheckBox;
        private CheckBox italicCheckBox;
        private CheckBox underlineCheckBox;
        private CheckBox backgroundCheckBox;
        private CheckBox borderCheckBox;
        private CheckBox shadowCheckBox;
        private Button backgroundColorButton;
        private Button borderColorButton;
        private Button textColorButton;
        private Panel previewPanel;
        private Button okButton;
        private Button cancelButton;

        // Modern UI theme support
        private readonly bool _isDarkTheme;
        private readonly Color _surfaceColor;
        private readonly Color _backgroundRoot;
        private readonly Color _textPrimary;
        private readonly Color _textSecondary;
        private readonly Color _borderColor;
        private readonly Color _accentColor;
        private readonly Color _hoverColor;

        public string TextContent { get; private set; } = "";
        public Font SelectedFont { get; private set; }
        public Color SelectedTextColor { get; private set; } = Color.Black;
        public TextStyle SelectedStyle { get; private set; } = new TextStyle();

        public ModernTextEditorForm(string initialText = "", Font? initialFont = null, Color? initialColor = null, TextStyle? initialStyle = null)
        {
            // Detect system theme
            _isDarkTheme = IsSystemDarkTheme();
            
            // Modern Windows 11 color palette
            if (_isDarkTheme)
            {
                _backgroundRoot = Color.FromArgb(32, 32, 32);
                _surfaceColor = Color.FromArgb(45, 45, 45);
                _textPrimary = Color.FromArgb(255, 255, 255);
                _textSecondary = Color.FromArgb(200, 200, 200);
                _borderColor = Color.FromArgb(66, 66, 66);
                _accentColor = Color.FromArgb(96, 207, 255);
                _hoverColor = Color.FromArgb(55, 55, 55);
            }
            else
            {
                _backgroundRoot = Color.FromArgb(243, 243, 243);
                _surfaceColor = Color.White;
                _textPrimary = Color.FromArgb(50, 50, 50);
                _textSecondary = Color.FromArgb(96, 96, 96);
                _borderColor = Color.FromArgb(229, 229, 229);
                _accentColor = Color.FromArgb(0, 120, 215);
                _hoverColor = Color.FromArgb(246, 246, 246);
            }
            
            InitializeComponent();
            SetupModernStyling();
            
            // Initialize with provided values - use fallback fonts if Variable Text not available
            TextContent = initialText ?? "";
            SelectedFont = initialFont ?? CreateDefaultFont();
            SelectedTextColor = initialColor ?? _textPrimary;
            SelectedStyle = initialStyle ?? new TextStyle();
            
            LoadInitialValues();
            UpdatePreview();
        }

        private Font CreateDefaultFont()
        {
            // Try modern fonts first, fallback to Segoe UI
            var preferredFonts = new[] { "Segoe UI Variable Text", "Segoe UI", "Arial" };
            
            foreach (var fontName in preferredFonts)
            {
                try
                {
                    return new Font(fontName, 12, FontStyle.Regular);
                }
                catch
                {
                    // Continue to next font if this one fails
                }
            }
            
            // Final fallback
            return new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular);
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light theme if detection fails
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Text Annotation Editor";
            this.Size = new Size(560, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;

            // Create main container with proper layout
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Set up row styles
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Text Content
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Typography
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Appearance
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Preview (flexible)
            mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // Column style
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Create sections
            var textSection = CreateSection("Text Content", CreateTextInputPanel());
            var typographySection = CreateSection("Typography", CreateTypographyPanel());
            var appearanceSection = CreateSection("Appearance", CreateAppearancePanel());
            var previewSection = CreateSection("Preview", CreatePreviewPanel());
            var buttonPanel = CreateButtonPanel();

            // Add to main container
            mainContainer.Controls.Add(textSection, 0, 0);
            mainContainer.Controls.Add(typographySection, 0, 1);
            mainContainer.Controls.Add(appearanceSection, 0, 2);
            mainContainer.Controls.Add(previewSection, 0, 3);
            mainContainer.Controls.Add(buttonPanel, 0, 4);

            this.Controls.Add(mainContainer);
        }

        private Panel CreateSection(string title, Panel content)
        {
            var section = new Panel
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20),
                Dock = DockStyle.Fill
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = _textPrimary,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8)
            };

            section.Controls.Add(content);
            section.Controls.Add(titleLabel);

            return section;
        }

        private Panel CreateTextInputPanel()
        {
            var panel = new Panel
            {
                Height = 90,
                Dock = DockStyle.Fill
            };

            textInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _surfaceColor,
                ForeColor = _textPrimary,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Text = "" // Ensure it's empty by default
            };
            textInput.TextChanged += (s, e) => UpdatePreview();

            panel.Controls.Add(textInput);
            return panel;
        }

        private Panel CreateTypographyPanel()
        {
            var panel = new Panel
            {
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            // Font controls
            var fontPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Top,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Font family
            fontFamilyCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                BackColor = _surfaceColor,
                ForeColor = _textPrimary,
                Width = 160
            };
            fontFamilyCombo.SelectedIndexChanged += (s, e) => UpdatePreview();

            // Font size
            fontSizeNumeric = new NumericUpDown
            {
                Minimum = 6,
                Maximum = 128,
                Value = 12,
                Font = new Font("Segoe UI", 9F),
                BackColor = _surfaceColor,
                ForeColor = _textPrimary,
                Width = 60,
                TextAlign = HorizontalAlignment.Center
            };
            fontSizeNumeric.ValueChanged += (s, e) => UpdatePreview();

            fontPanel.Controls.Add(fontFamilyCombo);
            fontPanel.Controls.Add(new Label { Text = "Size:", AutoSize = true, Margin = new Padding(10, 3, 5, 0) });
            fontPanel.Controls.Add(fontSizeNumeric);

            // Style toggles
            var stylePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Top,
                WrapContents = false
            };

            boldCheckBox = CreateModernToggle("Bold", "B");
            italicCheckBox = CreateModernToggle("Italic", "I");
            underlineCheckBox = CreateModernToggle("Underline", "U");

            stylePanel.Controls.Add(boldCheckBox);
            stylePanel.Controls.Add(italicCheckBox);
            stylePanel.Controls.Add(underlineCheckBox);

            panel.Controls.Add(stylePanel);
            panel.Controls.Add(fontPanel);

            return panel;
        }

        private Panel CreateAppearancePanel()
        {
            var panel = new Panel
            {
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            // Colors
            var colorPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Top,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 10)
            };

            textColorButton = CreateModernColorButton("Text Color", _textPrimary);
            textColorButton.Name = "textColorButton";
            textColorButton.Click += (s, e) => ChooseColor(textColorButton, color => SelectedTextColor = color);

            backgroundColorButton = CreateModernColorButton("Background", SelectedStyle.BackgroundColor);
            backgroundColorButton.Name = "backgroundColorButton";
            backgroundColorButton.Click += (s, e) => ChooseColor(backgroundColorButton, color => { SelectedStyle.BackgroundColor = color; UpdatePreview(); });

            borderColorButton = CreateModernColorButton("Border", SelectedStyle.BorderColor);
            borderColorButton.Name = "borderColorButton";
            borderColorButton.Click += (s, e) => ChooseColor(borderColorButton, color => { SelectedStyle.BorderColor = color; UpdatePreview(); });

            colorPanel.Controls.Add(textColorButton);
            colorPanel.Controls.Add(backgroundColorButton);
            colorPanel.Controls.Add(borderColorButton);

            // Effects
            var effectsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Dock = DockStyle.Top,
                WrapContents = false
            };

            backgroundCheckBox = CreateModernToggle("Background", "??");
            borderCheckBox = CreateModernToggle("Border", "?");
            shadowCheckBox = CreateModernToggle("Shadow", "??");

            effectsPanel.Controls.Add(backgroundCheckBox);
            effectsPanel.Controls.Add(borderCheckBox);
            effectsPanel.Controls.Add(shadowCheckBox);

            panel.Controls.Add(effectsPanel);
            panel.Controls.Add(colorPanel);

            return panel;
        }

        private Panel CreatePreviewPanel()
        {
            var panel = new Panel
            {
                Height = 120,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _isDarkTheme ? Color.FromArgb(40, 40, 40) : Color.FromArgb(250, 250, 250)
            };

            previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                BackColor = Color.Transparent
            };
            previewPanel.Paint += PreviewPanel_Paint;

            panel.Controls.Add(previewPanel);
            return panel;
        }

        private Button CreateModernColorButton(string text, Color color)
        {
            var button = new Button
            {
                Text = $"   {text}", // Extra space for color indicator
                Size = new Size(110, 32), // Slightly wider
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                BackColor = _surfaceColor,
                ForeColor = _textPrimary,
                Margin = new Padding(0, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = _borderColor;
            button.FlatAppearance.MouseOverBackColor = _hoverColor;

            // Store the initial color for this button
            var currentColor = color;

            // Add color indicator that updates with current color
            button.Paint += (s, e) =>
            {
                // Get the current color based on button type
                Color displayColor = button.Name switch
                {
                    "textColorButton" => SelectedTextColor,
                    "backgroundColorButton" => SelectedStyle.BackgroundColor,
                    "borderColorButton" => SelectedStyle.BorderColor,
                    _ => currentColor
                };

                var colorRect = new Rectangle(8, 8, 16, 16);
                using var brush = new SolidBrush(displayColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(brush, colorRect);
                
                using var borderPen = new Pen(_borderColor, 1);
                e.Graphics.DrawRectangle(borderPen, colorRect);
            };

            return button;
        }

        private CheckBox CreateModernToggle(string text, string icon)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                AutoSize = false,
                Size = new Size(80, 28),
                Font = new Font("Segoe UI", 9F),
                ForeColor = _textSecondary,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = _surfaceColor,
                Margin = new Padding(0, 0, 8, 0)
            };

            checkBox.FlatAppearance.BorderSize = 1;
            checkBox.FlatAppearance.BorderColor = _borderColor;
            checkBox.FlatAppearance.CheckedBackColor = _accentColor;
            checkBox.FlatAppearance.MouseOverBackColor = _hoverColor;

            checkBox.CheckedChanged += (s, e) =>
            {
                checkBox.BackColor = checkBox.Checked ? _accentColor : _surfaceColor;
                checkBox.ForeColor = checkBox.Checked ? Color.White : _textSecondary;
                UpdatePreview();
            };

            return checkBox;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Fill
            };

            var flowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                WrapContents = false
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                BackColor = _surfaceColor,
                ForeColor = _textSecondary,
                Margin = new Padding(8, 0, 0, 0)
            };
            cancelButton.FlatAppearance.BorderSize = 1;
            cancelButton.FlatAppearance.BorderColor = _borderColor;
            cancelButton.FlatAppearance.MouseOverBackColor = _hoverColor;

            okButton = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = _accentColor,
                ForeColor = Color.White
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Max(0, _accentColor.R - 20), Math.Max(0, _accentColor.G - 20), Math.Max(0, _accentColor.B - 20));
            okButton.Click += OkButton_Click;

            flowPanel.Controls.Add(okButton);
            flowPanel.Controls.Add(cancelButton);
            panel.Controls.Add(flowPanel);

            return panel;
        }

        private void SetupModernStyling()
        {
            this.BackColor = _backgroundRoot;
            this.Font = new Font("Segoe UI", 9F);
            
            // Enable double buffering and modern visual styles
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        private void LoadInitialValues()
        {
            // Load font families with Windows 11 preferred fonts first
            fontFamilyCombo.Items.Clear();
            var preferredFonts = new[] { "Segoe UI Variable Display", "Segoe UI Variable Text", "Segoe UI", "Calibri", "Arial" };
            
            foreach (var preferred in preferredFonts)
            {
                if (FontFamily.Families.Any(f => f.Name == preferred))
                {
                    fontFamilyCombo.Items.Add(preferred);
                }
            }
            
            fontFamilyCombo.Items.Add("---"); // Separator
            
            foreach (FontFamily family in FontFamily.Families)
            {
                if (!preferredFonts.Contains(family.Name))
                {
                    fontFamilyCombo.Items.Add(family.Name);
                }
            }

            // Set initial values
            textInput.Text = TextContent;
            fontFamilyCombo.SelectedItem = SelectedFont.FontFamily.Name;
            fontSizeNumeric.Value = (decimal)SelectedFont.Size;
            boldCheckBox.Checked = SelectedFont.Bold;
            italicCheckBox.Checked = SelectedFont.Italic;
            underlineCheckBox.Checked = SelectedFont.Underline;

            backgroundCheckBox.Checked = SelectedStyle.HasBackground;
            borderCheckBox.Checked = SelectedStyle.HasBorder;
            shadowCheckBox.Checked = SelectedStyle.HasShadow;

            UpdateColorButtonAppearance();
        }

        private void UpdateColorButtonAppearance()
        {
            textColorButton.Invalidate();
            backgroundColorButton.Invalidate();
            borderColorButton.Invalidate();
        }

        private void UpdatePreview()
        {
            // Update font
            var fontStyle = FontStyle.Regular;
            if (boldCheckBox.Checked) fontStyle |= FontStyle.Bold;
            if (italicCheckBox.Checked) fontStyle |= FontStyle.Italic;
            if (underlineCheckBox.Checked) fontStyle |= FontStyle.Underline;

            var fontFamily = fontFamilyCombo.SelectedItem?.ToString();
            if (fontFamily == "---") fontFamily = "Segoe UI";
            
            try
            {
                SelectedFont = new Font(fontFamily ?? "Segoe UI", (float)fontSizeNumeric.Value, fontStyle);
            }
            catch
            {
                // Fallback to safe font if the selected one fails
                SelectedFont = new Font(FontFamily.GenericSansSerif, (float)fontSizeNumeric.Value, fontStyle);
            }

            // Update text content
            TextContent = textInput.Text;

            // Update style options
            SelectedStyle.HasBackground = backgroundCheckBox.Checked;
            SelectedStyle.HasBorder = borderCheckBox.Checked;
            SelectedStyle.HasShadow = shadowCheckBox.Checked;

            // Refresh preview
            previewPanel.Invalidate();
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(TextContent))
            {
                // Show placeholder text
                var placeholderText = "Preview text will appear here...";
                var placeholderFont = new Font("Segoe UI", 10F, FontStyle.Italic);
                var placeholderRect = new Rectangle(16, 16, previewPanel.Width - 32, previewPanel.Height - 32);
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                
                TextRenderer.DrawText(e.Graphics, placeholderText, placeholderFont, placeholderRect, _textSecondary);
                return;
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var textRect = new Rectangle(16, 16, previewPanel.Width - 32, previewPanel.Height - 32);

            // Draw background if enabled
            if (SelectedStyle.HasBackground)
            {
                var bgRect = textRect;
                bgRect.Inflate(SelectedStyle.BackgroundPadding, SelectedStyle.BackgroundPadding);
                
                using var bgBrush = new SolidBrush(SelectedStyle.BackgroundColor);
                if (SelectedStyle.BackgroundRoundedCorners)
                {
                    using var path = CreateRoundedRectanglePath(bgRect, SelectedStyle.CornerRadius);
                    g.FillPath(bgBrush, path);
                }
                else
                {
                    g.FillRectangle(bgBrush, bgRect);
                }

                if (SelectedStyle.HasBorder)
                {
                    using var borderPen = new Pen(SelectedStyle.BorderColor, SelectedStyle.BorderWidth);
                    if (SelectedStyle.BackgroundRoundedCorners)
                    {
                        using var path = CreateRoundedRectanglePath(bgRect, SelectedStyle.CornerRadius);
                        g.DrawPath(borderPen, path);
                    }
                    else
                    {
                        g.DrawRectangle(borderPen, bgRect);
                    }
                }
            }

            // Draw shadow if enabled
            if (SelectedStyle.HasShadow)
            {
                var shadowRect = textRect;
                shadowRect.Offset(SelectedStyle.ShadowOffset);
                TextRenderer.DrawText(g, TextContent, SelectedFont, shadowRect, SelectedStyle.ShadowColor);
            }

            // Draw main text
            TextRenderer.DrawText(g, TextContent, SelectedFont, textRect, SelectedTextColor);
        }

        private void ChooseColor(Button button, Action<Color> onColorSelected)
        {
            using var dialog = new ColorDialog
            {
                Color = button.Name == "textColorButton" ? SelectedTextColor : 
                        button.Name == "backgroundColorButton" ? SelectedStyle.BackgroundColor : 
                        SelectedStyle.BorderColor,
                FullOpen = true,
                ShowHelp = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                onColorSelected(dialog.Color);
                UpdateColorButtonAppearance();
                UpdatePreview();
            }
        }

        private Color GetContrastColor(Color color)
        {
            var brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
            return brightness > 128 ? Color.Black : Color.White;
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        private Panel CreateSpacer(int width)
        {
            return new Panel { Width = width, Height = 1 };
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextContent))
            {
                MessageBox.Show("Please enter some text to create an annotation.", "Text Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                textInput.Focus();
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Enhanced keyboard navigation
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Enter))
            {
                OkButton_Click(okButton, EventArgs.Empty);
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}