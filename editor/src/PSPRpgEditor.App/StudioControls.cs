using System.Globalization;
using PSPRpgEditor.Core;

namespace PSPRpgEditor.App;

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(56, 61, 70);
    public override Color MenuItemBorder => Color.FromArgb(77, 84, 96);
    public override Color MenuBorder => Color.FromArgb(62, 67, 76);
    public override Color ToolStripDropDownBackground => Color.FromArgb(37, 40, 46);
    public override Color ImageMarginGradientBegin => ToolStripDropDownBackground;
    public override Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
    public override Color ImageMarginGradientEnd => ToolStripDropDownBackground;
    public override Color ButtonSelectedHighlight => Color.FromArgb(65, 71, 82);
    public override Color ButtonSelectedGradientBegin => ButtonSelectedHighlight;
    public override Color ButtonSelectedGradientEnd => ButtonSelectedHighlight;
    public override Color ButtonPressedGradientBegin => Color.FromArgb(73, 81, 95);
    public override Color ButtonPressedGradientEnd => ButtonPressedGradientBegin;
    public override Color SeparatorDark => Color.FromArgb(65, 70, 79);
    public override Color SeparatorLight => Color.FromArgb(65, 70, 79);
}

internal sealed class StudioTabControl : TabControl
{
    public StudioTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(130, 27);
        SizeMode = TabSizeMode.Fixed;
        Padding = new Point(12, 4);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var selected = e.Index == SelectedIndex;
        var bounds = GetTabRect(e.Index);
        using var background = new SolidBrush(selected
            ? Color.FromArgb(48, 52, 60)
            : Color.FromArgb(34, 37, 43));
        e.Graphics.FillRectangle(background, bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(75, 160, 255));
            e.Graphics.FillRectangle(accent, bounds.Left, bounds.Bottom - 2, bounds.Width, 2);
        }

        TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font,
            bounds, selected ? Color.White : Color.FromArgb(170, 176, 186),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class InspectorPanel : UserControl
{
    private readonly TableLayoutPanel content = new();
    private object? selectedObject;
    private bool refreshing;

    public event Action? ObjectChanged;

    public object? SelectedObject
    {
        get => selectedObject;
        set
        {
            selectedObject = value;
            RefreshView();
        }
    }

    public InspectorPanel()
    {
        AutoScroll = true;
        BackColor = Color.FromArgb(31, 34, 40);
        content.Dock = DockStyle.Top;
        content.AutoSize = true;
        content.ColumnCount = 1;
        Controls.Add(content);
    }

    public void RefreshView()
    {
        refreshing = true;
        content.Controls.Clear();
        content.RowStyles.Clear();

        switch (selectedObject)
        {
            case GameProject project:
                AddHeader("PROJECT", project.Name, "Project settings");
                AddCard("Settings",
                    TextField("Name", project.Name, value => project.Name = value),
                    TextField("Project ID", project.ProjectId.ToString(), null),
                    TextField("Startup Scene", project.StartupScene.ToString(), null));
                break;
            case SceneDocument scene:
                AddHeader("SCENE", scene.Name, $"{scene.Entities.Count} entities");
                AddCard("Scene",
                    TextField("Name", scene.Name, value => scene.Name = value),
                    NumberField("Width", scene.Width, value => scene.Width = (int)value),
                    NumberField("Height", scene.Height, value => scene.Height = (int)value));
                break;
            case EntityDocument entity:
                AddHeader("OBJECT", entity.Name, entity.Id.ToString("N")[..8]);
                AddCard("Object",
                    TextField("Name", entity.Name, value => entity.Name = value),
                    TextField("ID", entity.Id.ToString(), null));
                AddCard("Transform 2D",
                    NumberField("Position X", entity.Transform.X,
                        value => entity.Transform.X = value),
                    NumberField("Position Y", entity.Transform.Y,
                        value => entity.Transform.Y = value),
                    NumberField("Width", entity.Transform.Width,
                        value => entity.Transform.Width = value),
                    NumberField("Height", entity.Transform.Height,
                        value => entity.Transform.Height = value));
                AddCard("Sprite Renderer",
                    TextField("Color", entity.SpriteRenderer.Color,
                        value => entity.SpriteRenderer.Color = value),
                    BoolField("Visible", entity.SpriteRenderer.Visible,
                        value => entity.SpriteRenderer.Visible = value));
                AddComponentButton();
                break;
            default:
                AddEmptyState();
                break;
        }
        refreshing = false;
    }

    private void AddHeader(string kind, string name, string subtitle)
    {
        var panel = new Panel
        {
            Height = 72,
            Dock = DockStyle.Top,
            Padding = new Padding(12, 9, 12, 7),
            BackColor = Color.FromArgb(38, 41, 48),
            Margin = new Padding(0, 0, 0, 6)
        };
        panel.Controls.Add(new Label
        {
            Text = subtitle,
            Dock = DockStyle.Bottom,
            Height = 20,
            ForeColor = Color.FromArgb(137, 144, 157)
        });
        panel.Controls.Add(new Label
        {
            Text = name,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 11, FontStyle.Bold)
        });
        panel.Controls.Add(new Label
        {
            Text = kind,
            Dock = DockStyle.Top,
            Height = 16,
            ForeColor = Color.FromArgb(94, 170, 255),
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 7, FontStyle.Bold)
        });
        content.Controls.Add(panel);
    }

    private void AddCard(string title, params Control[] rows)
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.FromArgb(42, 45, 52),
            Padding = new Padding(8, 5, 8, 8),
            Margin = new Padding(0, 0, 0, 6)
        };
        body.Controls.Add(new Label
        {
            Text = "▾  " + title,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(221, 225, 231),
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        foreach (var row in rows)
            body.Controls.Add(row);
        content.Controls.Add(body);
    }

    private Control TextField(string label, string value, Action<string>? setter)
    {
        var textBox = new TextBox
        {
            Text = value,
            Dock = DockStyle.Fill,
            ReadOnly = setter is null,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = setter is null
                ? Color.FromArgb(35, 38, 44)
                : Color.FromArgb(29, 32, 38),
            ForeColor = setter is null
                ? Color.FromArgb(130, 136, 147)
                : Color.White
        };
        if (setter is not null)
            textBox.Validated += (_, _) => Change(() => setter(textBox.Text));
        return FieldRow(label, textBox);
    }

    private Control NumberField(string label, float value, Action<float> setter)
    {
        var input = new NumericUpDown
        {
            DecimalPlaces = value % 1 == 0 ? 0 : 2,
            Minimum = -100000,
            Maximum = 100000,
            Value = (decimal)value,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(29, 32, 38),
            ForeColor = Color.White
        };
        input.ValueChanged += (_, _) => Change(() => setter((float)input.Value));
        return FieldRow(label, input);
    }

    private Control BoolField(string label, bool value, Action<bool> setter)
    {
        var checkBox = new CheckBox
        {
            Checked = value,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        checkBox.CheckedChanged += (_, _) => Change(() => setter(checkBox.Checked));
        return FieldRow(label, checkBox);
    }

    private static Control FieldRow(string label, Control input)
    {
        var row = new TableLayoutPanel
        {
            Height = 29,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Margin = new Padding(0, 2, 0, 1)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        row.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(166, 172, 182),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        row.Controls.Add(input, 1, 0);
        return row;
    }

    private void AddComponentButton()
    {
        var button = new Button
        {
            Text = "+ Add Component",
            Dock = DockStyle.Top,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 53, 62),
            ForeColor = Color.FromArgb(194, 201, 212),
            Margin = new Padding(10, 4, 10, 8)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(70, 77, 89);
        button.Click += (_, _) => MessageBox.Show(
            "More components arrive with the tilemap and gameplay milestones.",
            "Component library");
        content.Controls.Add(button);
    }

    private void AddEmptyState()
    {
        content.Controls.Add(new Label
        {
            Text = "Select an object to inspect it.",
            Dock = DockStyle.Top,
            Height = 80,
            ForeColor = Color.FromArgb(135, 142, 153),
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    private void Change(Action action)
    {
        if (refreshing)
            return;
        action();
        ObjectChanged?.Invoke();
    }
}

internal sealed class AssetBrowserPanel : UserControl
{
    private readonly ListView assets = new();
    private readonly TextBox search = new();
    private string? projectDirectory;

    public AssetBrowserPanel()
    {
        BackColor = Color.FromArgb(28, 31, 36);
        var sidebar = new TreeView
        {
            Dock = DockStyle.Left,
            Width = 150,
            BackColor = Color.FromArgb(32, 35, 41),
            ForeColor = Color.FromArgb(190, 196, 206),
            BorderStyle = BorderStyle.None,
            FullRowSelect = true
        };
        sidebar.Nodes.Add("Scenes");
        sidebar.Nodes.Add("Assets");
        sidebar.Nodes.Add("Animations");
        sidebar.Nodes.Add("Events");
        sidebar.Nodes.Add("RPG");

        search.Dock = DockStyle.Top;
        search.Height = 25;
        search.PlaceholderText = "Search files...";
        search.BackColor = Color.FromArgb(36, 39, 46);
        search.ForeColor = Color.White;
        search.BorderStyle = BorderStyle.FixedSingle;
        search.TextChanged += (_, _) => RefreshFiles();

        assets.Dock = DockStyle.Fill;
        assets.View = View.Tile;
        assets.TileSize = new Size(130, 48);
        assets.BackColor = Color.FromArgb(28, 31, 36);
        assets.ForeColor = Color.FromArgb(205, 210, 218);
        assets.BorderStyle = BorderStyle.None;

        Controls.Add(assets);
        Controls.Add(search);
        Controls.Add(sidebar);
    }

    public void SetProject(string? manifestPath)
    {
        projectDirectory = manifestPath is null
            ? null
            : Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        RefreshFiles();
    }

    private void RefreshFiles()
    {
        assets.Items.Clear();
        if (projectDirectory is null || !Directory.Exists(projectDirectory))
            return;

        var filter = search.Text.Trim();
        foreach (var path in Directory.EnumerateFiles(
                     projectDirectory, "*", SearchOption.AllDirectories)
                     .Where(path => !path.Contains(
                         $"{Path.DirectorySeparatorChar}Build{Path.DirectorySeparatorChar}"))
                     .Where(path => string.IsNullOrEmpty(filter) ||
                         Path.GetFileName(path).Contains(
                             filter, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(Path.GetFileName)
                     .Take(200))
        {
            var relative = Path.GetRelativePath(projectDirectory, path);
            assets.Items.Add(new ListViewItem(Path.GetFileName(path))
            {
                ToolTipText = relative,
                Tag = path
            });
        }
    }
}

internal sealed class EventGraphPreview : Control
{
    public EventGraphPreview()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(23, 26, 31);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var grid = new Pen(Color.FromArgb(25, 255, 255, 255));
        for (var x = 0; x < Width; x += 20)
            e.Graphics.DrawLine(grid, x, 0, x, Height);
        for (var y = 0; y < Height; y += 20)
            e.Graphics.DrawLine(grid, 0, y, Width, y);

        DrawNode(e.Graphics, new Rectangle(100, 90, 150, 86),
            "On Scene Start", Color.FromArgb(70, 167, 104),
            ["Event", "Next"]);
        DrawNode(e.Graphics, new Rectangle(340, 90, 150, 86),
            "Show Dialog", Color.FromArgb(207, 126, 66),
            ["Guide", "Hello PSP!"]);
        using var link = new Pen(Color.FromArgb(210, 217, 226), 3);
        e.Graphics.DrawBezier(link, 250, 133, 290, 133, 300, 133, 340, 133);
    }

    private static void DrawNode(
        Graphics graphics, Rectangle rectangle, string title, Color color,
        IReadOnlyList<string> rows)
    {
        using var body = new SolidBrush(Color.FromArgb(49, 53, 62));
        using var header = new SolidBrush(color);
        graphics.FillRectangle(body, rectangle);
        graphics.FillRectangle(header, rectangle.X, rectangle.Y, rectangle.Width, 25);
        TextRenderer.DrawText(graphics, title, SystemFonts.DefaultFont,
            new Rectangle(rectangle.X + 7, rectangle.Y, rectangle.Width - 14, 25),
            Color.White, TextFormatFlags.VerticalCenter);
        for (var index = 0; index < rows.Count; index++)
            TextRenderer.DrawText(graphics, rows[index], SystemFonts.DefaultFont,
                new Point(rectangle.X + 8, rectangle.Y + 34 + index * 20),
                Color.FromArgb(195, 201, 211));
    }
}
