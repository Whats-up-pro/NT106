using System;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for managing application theme (Light/Dark mode)
    /// Uses Sky Blue as primary color
    /// </summary>
    public class ThemeService
    {
        private static ThemeService? _instance;
        private static readonly object _lock = new object();

        public enum ThemeMode
        {
            Light,
            Dark
        }

        private ThemeMode _currentTheme = ThemeMode.Light;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ThemeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ThemeService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Current active theme
        /// </summary>
        public ThemeMode CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnThemeChanged?.Invoke(_currentTheme);
                }
            }
        }

        /// <summary>
        /// Event fired when theme changes
        /// </summary>
        public event Action<ThemeMode>? OnThemeChanged;

        // ========== LIGHT THEME COLORS ==========
        public static class Light
        {
            public static readonly Color Primary = ColorTranslator.FromHtml("#0ea5e9");      // Sky 500
            public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#0284c7"); // Sky 600
            public static readonly Color PrimaryLight = ColorTranslator.FromHtml("#38bdf8"); // Sky 400
            public static readonly Color Accent = ColorTranslator.FromHtml("#06b6d4");       // Cyan 500

            public static readonly Color Background = ColorTranslator.FromHtml("#f8fafc");   // Slate 50
            public static readonly Color Surface = Color.White;
            public static readonly Color SurfaceAlt = ColorTranslator.FromHtml("#f1f5f9");  // Slate 100

            public static readonly Color TextPrimary = ColorTranslator.FromHtml("#0f172a");  // Slate 950
            public static readonly Color TextSecondary = ColorTranslator.FromHtml("#475569");// Slate 600
            public static readonly Color TextMuted = ColorTranslator.FromHtml("#94a3b8");    // Slate 400

            public static readonly Color Border = ColorTranslator.FromHtml("#e2e8f0");       // Slate 200
            public static readonly Color BorderFocus = ColorTranslator.FromHtml("#0ea5e9");  // Sky 500

            public static readonly Color Success = ColorTranslator.FromHtml("#10b981");      // Emerald 500
            public static readonly Color Warning = ColorTranslator.FromHtml("#f59e0b");      // Amber 500
            public static readonly Color Error = ColorTranslator.FromHtml("#ef4444");        // Red 500
        }

        // ========== DARK THEME COLORS ==========
        public static class Dark
        {
            public static readonly Color Primary = ColorTranslator.FromHtml("#38bdf8");      // Sky 400
            public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#0ea5e9"); // Sky 500
            public static readonly Color PrimaryLight = ColorTranslator.FromHtml("#7dd3fc"); // Sky 300
            public static readonly Color Accent = ColorTranslator.FromHtml("#22d3ee");       // Cyan 400

            public static readonly Color Background = ColorTranslator.FromHtml("#0f172a");   // Slate 950
            public static readonly Color Surface = ColorTranslator.FromHtml("#1e293b");      // Slate 800
            public static readonly Color SurfaceAlt = ColorTranslator.FromHtml("#334155");   // Slate 700

            public static readonly Color TextPrimary = ColorTranslator.FromHtml("#f1f5f9");  // Slate 100
            public static readonly Color TextSecondary = ColorTranslator.FromHtml("#cbd5e1");// Slate 300
            public static readonly Color TextMuted = ColorTranslator.FromHtml("#64748b");    // Slate 500

            public static readonly Color Border = ColorTranslator.FromHtml("#334155");       // Slate 700
            public static readonly Color BorderFocus = ColorTranslator.FromHtml("#38bdf8");  // Sky 400

            public static readonly Color Success = ColorTranslator.FromHtml("#34d399");      // Emerald 400
            public static readonly Color Warning = ColorTranslator.FromHtml("#fbbf24");      // Amber 400
            public static readonly Color Error = ColorTranslator.FromHtml("#f87171");        // Red 400
        }

        // ========== DYNAMIC PROPERTIES (Based on current theme) ==========

        public Color Primary => CurrentTheme == ThemeMode.Light ? Light.Primary : Dark.Primary;
        public Color PrimaryHover => CurrentTheme == ThemeMode.Light ? Light.PrimaryHover : Dark.PrimaryHover;
        public Color PrimaryLight => CurrentTheme == ThemeMode.Light ? Light.PrimaryLight : Dark.PrimaryLight;
        public Color Accent => CurrentTheme == ThemeMode.Light ? Light.Accent : Dark.Accent;

        public Color Background => CurrentTheme == ThemeMode.Light ? Light.Background : Dark.Background;
        public Color Surface => CurrentTheme == ThemeMode.Light ? Light.Surface : Dark.Surface;
        public Color SurfaceAlt => CurrentTheme == ThemeMode.Light ? Light.SurfaceAlt : Dark.SurfaceAlt;

        public Color TextPrimary => CurrentTheme == ThemeMode.Light ? Light.TextPrimary : Dark.TextPrimary;
        public Color TextSecondary => CurrentTheme == ThemeMode.Light ? Light.TextSecondary : Dark.TextSecondary;
        public Color TextMuted => CurrentTheme == ThemeMode.Light ? Light.TextMuted : Dark.TextMuted;

        public Color Border => CurrentTheme == ThemeMode.Light ? Light.Border : Dark.Border;
        public Color BorderFocus => CurrentTheme == ThemeMode.Light ? Light.BorderFocus : Dark.BorderFocus;

        public Color Success => CurrentTheme == ThemeMode.Light ? Light.Success : Dark.Success;
        public Color Warning => CurrentTheme == ThemeMode.Light ? Light.Warning : Dark.Warning;
        public Color Error => CurrentTheme == ThemeMode.Light ? Light.Error : Dark.Error;

        // ========== METHODS ==========

        /// <summary>
        /// Toggle between Light and Dark themes
        /// </summary>
        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        }

        /// <summary>
        /// Apply theme to form
        /// </summary>
        public void ApplyTheme(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;

            foreach (Control control in GetAllControls(form))
            {
                ApplyThemeToControl(control);
            }
        }

        /// <summary>
        /// Get all controls recursively
        /// </summary>
        private System.Collections.Generic.IEnumerable<Control> GetAllControls(Control container)
        {
            var controls = new System.Collections.Generic.List<Control>();

            foreach (Control control in container.Controls)
            {
                controls.Add(control);
                if (control.HasChildren)
                {
                    controls.AddRange(GetAllControls(control));
                }
            }

            return controls;
        }

        /// <summary>
        /// Apply theme to individual control
        /// </summary>
        private void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case Button btn:
                    StyleButton(btn);
                    break;
                case TextBox txt:
                    StyleTextBox(txt);
                    break;
                case Label lbl:
                    StyleLabel(lbl);
                    break;
                case Panel pnl:
                    StylePanel(pnl);
                    break;
                case ComboBox cmb:
                    StyleComboBox(cmb);
                    break;
                case ListView lv:
                    StyleListView(lv);
                    break;
            }
        }

        /// <summary>
        /// Style button (primary by default)
        /// </summary>
        public void StyleButton(Button button, bool isPrimary = true)
        {
            if (isPrimary)
            {
                button.BackColor = Primary;
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = PrimaryHover;
                button.Cursor = Cursors.Hand;
                button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            }
            else
            {
                button.BackColor = Surface;
                button.ForeColor = TextPrimary;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = SurfaceAlt;
                button.Cursor = Cursors.Hand;
                button.Font = new Font("Segoe UI", 10F);
            }
        }

        /// <summary>
        /// Style textbox
        /// </summary>
        public void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = Surface;
            textBox.ForeColor = TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F);
        }

        /// <summary>
        /// Style label
        /// </summary>
        public void StyleLabel(Label label, bool isHeader = false)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = isHeader ? TextPrimary : TextSecondary;
            label.Font = isHeader ?
                new Font("Segoe UI", 14F, FontStyle.Bold) :
                new Font("Segoe UI", 10F);
        }

        /// <summary>
        /// Style panel
        /// </summary>
        public void StylePanel(Panel panel, bool isSurface = true)
        {
            panel.BackColor = isSurface ? Surface : Background;
            panel.ForeColor = TextPrimary;
        }

        /// <summary>
        /// Style combobox
        /// </summary>
        public void StyleComboBox(ComboBox comboBox)
        {
            comboBox.BackColor = Surface;
            comboBox.ForeColor = TextPrimary;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.Font = new Font("Segoe UI", 10F);
        }

        /// <summary>
        /// Style ListView
        /// </summary>
        public void StyleListView(ListView listView)
        {
            listView.BackColor = Surface;
            listView.ForeColor = TextPrimary;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.Font = new Font("Segoe UI", 10F);
        }
    }
}
