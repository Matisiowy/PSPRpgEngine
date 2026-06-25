namespace PSPRpgEditor.App;

internal static class EditorTheme
{
    public static readonly Color Window = Color.FromArgb(30, 32, 37);
    public static readonly Color Panel = Color.FromArgb(39, 42, 48);
    public static readonly Color Header = Color.FromArgb(47, 51, 58);
    public static readonly Color Text = Color.FromArgb(225, 229, 235);

    public static Control CreatePanel(string title, Control content)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Panel,
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = Header,
            ForeColor = Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        }, 0, 0);
        panel.Controls.Add(content, 0, 1);
        return panel;
    }
}
