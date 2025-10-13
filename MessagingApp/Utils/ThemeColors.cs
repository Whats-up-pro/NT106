namespace MessagingApp.Utils
{
    public static class ThemeColors
    {
        // Primary Blue Colors
        public static readonly Color PrimaryDarkBlue = Color.FromArgb(30, 58, 138);      // #1E3A8A
        public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);          // #2563EB
        public static readonly Color PrimaryLightBlue = Color.FromArgb(59, 130, 246);    // #3B82F6
        public static readonly Color SecondaryBlue = Color.FromArgb(96, 165, 250);       // #60A5FA
        public static readonly Color TertiaryBlue = Color.FromArgb(147, 197, 253);       // #93C5FD

        // Dark/Black Colors
        public static readonly Color Black = Color.FromArgb(0, 0, 0);                    // #000000
        public static readonly Color DarkGray = Color.FromArgb(31, 41, 55);              // #1F2937
        public static readonly Color MediumGray = Color.FromArgb(55, 65, 81);            // #374151
        public static readonly Color LightGray = Color.FromArgb(107, 114, 128);          // #6B7280

        // Accent Colors
        public static readonly Color White = Color.White;
        public static readonly Color SuccessGreen = Color.FromArgb(34, 197, 94);         // #22C55E
        public static readonly Color ErrorRed = Color.FromArgb(239, 68, 68);             // #EF4444
        public static readonly Color WarningYellow = Color.FromArgb(234, 179, 8);        // #EAB308

        // Background Colors
        public static readonly Color BackgroundDark = Color.FromArgb(17, 24, 39);        // #111827
        public static readonly Color BackgroundMedium = Color.FromArgb(31, 41, 55);      // #1F2937
        public static readonly Color BackgroundLight = Color.FromArgb(55, 65, 81);       // #374151

        /// <summary>
        /// Apply standard theme to a form
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            form.BackColor = BackgroundDark;
            form.ForeColor = White;
        }

        /// <summary>
        /// Style a primary button
        /// </summary>
        public static void StylePrimaryButton(Button button)
        {
            button.BackColor = PrimaryBlue;
            button.ForeColor = White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        /// <summary>
        /// Style a secondary button
        /// </summary>
        public static void StyleSecondaryButton(Button button)
        {
            button.BackColor = DarkGray;
            button.ForeColor = White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = PrimaryBlue;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 10F);
        }

        /// <summary>
        /// Style a text box
        /// </summary>
        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = BackgroundMedium;
            textBox.ForeColor = White;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F);
        }

        /// <summary>
        /// Style a label
        /// </summary>
        public static void StyleLabel(Label label, bool isTitle = false)
        {
            label.ForeColor = White;
            if (isTitle)
            {
                label.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            }
            else
            {
                label.Font = new Font("Segoe UI", 10F);
            }
        }

        /// <summary>
        /// Style a panel
        /// </summary>
        public static void StylePanel(Panel panel, bool isDark = true)
        {
            panel.BackColor = isDark ? BackgroundDark : BackgroundMedium;
        }
    }
}
