using System.Globalization;
using PSPRpgEditor.Core;

namespace PSPRpgEditor.App;

public sealed class MainForm : Form
{
    private static readonly Color WindowColor = Color.FromArgb(30, 32, 37);
    private static readonly Color PanelColor = Color.FromArgb(39, 42, 48);
    private static readonly Color HeaderColor = Color.FromArgb(47, 51, 58);
    private static readonly Color AccentColor = Color.FromArgb(66, 150, 250);
    private static readonly Color TextColor = Color.FromArgb(225, 229, 235);

    private readonly TreeView hierarchy = new();
    private readonly PropertyGrid inspector = new();
    private readonly SceneView sceneView = new();
    private readonly ToolStripStatusLabel status = new("Ready");
    private readonly Label sceneTitle = new();

    private GameProject project = GameProject.CreateDefault();
    private string? projectPath;
    private bool dirty;

    public MainForm(string? startupProject = null)
    {
        Text = "PSP RPG Editor";
        Width = 1360;
        Height = 820;
        MinimumSize = new Size(1000, 650);
        BackColor = WindowColor;
        ForeColor = TextColor;

        var menu = BuildMenu();
        var toolbar = BuildToolbar();
        var statusStrip = BuildStatusStrip();
        var workspace = BuildWorkspace();

        Controls.Add(workspace);
        Controls.Add(statusStrip);
        Controls.Add(toolbar);
        Controls.Add(menu);
        MainMenuStrip = menu;

        hierarchy.AfterSelect += (_, eventArgs) => SelectObject(eventArgs.Node?.Tag);
        inspector.PropertyValueChanged += (_, _) =>
        {
            RefreshHierarchy(inspector.SelectedObject);
            sceneView.Invalidate();
            sceneTitle.Text = sceneView.Scene?.Name ?? "Scene";
            MarkChanged();
        };
        sceneView.SelectionChanged += entity =>
        {
            inspector.SelectedObject = entity;
            SelectHierarchyObject(entity);
        };
        sceneView.SceneChanged += () =>
        {
            inspector.Refresh();
            MarkChanged();
        };

        FormClosing += (_, eventArgs) =>
        {
            if (!ConfirmDiscardChanges())
            {
                eventArgs.Cancel = true;
            }
        };

        if (!string.IsNullOrWhiteSpace(startupProject))
        {
            try
            {
                ShowProject(ProjectSerializer.Load(startupProject), startupProject);
                status.Text = $"Opened {Path.GetFileName(startupProject)}";
            }
            catch (Exception exception)
            {
                ShowProject(project, null);
                MessageBox.Show(exception.Message, "Cannot open project",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            ShowProject(project, null);
        }
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            BackColor = HeaderColor,
            ForeColor = TextColor,
            RenderMode = ToolStripRenderMode.System
        };

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("&New Project", null, (_, _) => NewProject());
        file.DropDownItems.Add("&Open...", null, (_, _) => OpenProject());
        file.DropDownItems.Add("&Save", null, (_, _) => SaveProject(false));
        file.DropDownItems.Add("Save &As...", null, (_, _) => SaveProject(true));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("E&xit", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add("&Add Entity", null, (_, _) => AddEntity());
        var deleteItem = new ToolStripMenuItem(
            "&Delete Selected", null, (_, _) => DeleteSelected())
        {
            ShortcutKeys = Keys.Delete
        };
        edit.DropDownItems.Add(deleteItem);

        var build = new ToolStripMenuItem("&Build");
        build.DropDownItems.Add("Build for PSP", null, (_, _) => ShowBuildInfo());

        menu.Items.AddRange([file, edit, build]);
        return menu;
    }

    private ToolStrip BuildToolbar()
    {
        var toolbar = new ToolStrip
        {
            BackColor = HeaderColor,
            ForeColor = TextColor,
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(6, 3, 6, 3)
        };
        toolbar.Items.Add(MakeButton("New", (_, _) => NewProject()));
        toolbar.Items.Add(MakeButton("Open", (_, _) => OpenProject()));
        toolbar.Items.Add(MakeButton("Save", (_, _) => SaveProject(false)));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeButton("+ Entity", (_, _) => AddEntity()));
        toolbar.Items.Add(MakeButton("Delete", (_, _) => DeleteSelected()));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripLabel("Snap: 16 px"));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeButton("Build PSP", (_, _) => ShowBuildInfo()));
        return toolbar;
    }

    private static ToolStripButton MakeButton(string text, EventHandler click)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        button.Click += click;
        return button;
    }

    private StatusStrip BuildStatusStrip()
    {
        var strip = new StatusStrip { BackColor = HeaderColor, ForeColor = TextColor };
        strip.Items.Add(status);
        strip.Items.Add(new ToolStripStatusLabel { Spring = true });
        strip.Items.Add(new ToolStripStatusLabel("PSP 480 × 272  |  Grid 16"));
        return strip;
    }

    private Control BuildWorkspace()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = WindowColor,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        hierarchy.Dock = DockStyle.Fill;
        hierarchy.BackColor = PanelColor;
        hierarchy.ForeColor = TextColor;
        hierarchy.BorderStyle = BorderStyle.None;
        hierarchy.HideSelection = false;
        hierarchy.FullRowSelect = true;

        inspector.Dock = DockStyle.Fill;
        inspector.BackColor = PanelColor;
        inspector.ViewBackColor = PanelColor;
        inspector.ViewForeColor = TextColor;
        inspector.ViewBorderColor = HeaderColor;
        inspector.HelpBackColor = PanelColor;
        inspector.HelpForeColor = TextColor;
        inspector.CommandsBackColor = PanelColor;
        inspector.CommandsForeColor = TextColor;
        inspector.PropertySort = PropertySort.Categorized;
        inspector.ToolbarVisible = false;

        sceneView.Dock = DockStyle.Fill;
        sceneTitle.Text = "Scene";
        sceneTitle.Dock = DockStyle.Fill;
        sceneTitle.ForeColor = TextColor;
        sceneTitle.TextAlign = ContentAlignment.MiddleLeft;
        sceneTitle.Padding = new Padding(8, 0, 0, 0);

        layout.Controls.Add(CreatePanel("HIERARCHY", hierarchy), 0, 0);
        layout.Controls.Add(CreateScenePanel(), 1, 0);
        layout.Controls.Add(CreatePanel("INSPECTOR", inspector), 2, 0);
        return layout;
    }

    private Control CreateScenePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(6, 0, 6, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = HeaderColor };
        header.Controls.Add(sceneTitle);
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(sceneView, 0, 1);
        return panel;
    }

    private static Control CreatePanel(string title, Control content)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = HeaderColor,
            ForeColor = TextColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
        }, 0, 0);
        panel.Controls.Add(content, 0, 1);
        return panel;
    }

    private void NewProject()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }
        ShowProject(GameProject.CreateDefault(), null);
        status.Text = "Created a new project";
    }

    private void OpenProject()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        using var dialog = new OpenFileDialog { Filter = ProjectSerializer.FileFilter };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ShowProject(ProjectSerializer.Load(dialog.FileName), dialog.FileName);
            status.Text = $"Opened {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Cannot open project",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveProject(bool choosePath)
    {
        if (choosePath || projectPath is null)
        {
            using var dialog = new SaveFileDialog
            {
                FileName = $"{MakeSafeFileName(project.Name)}{ProjectSerializer.FileExtension}",
                DefaultExt = ProjectSerializer.FileExtension.TrimStart('.'),
                AddExtension = true,
                Filter = ProjectSerializer.FileFilter
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            projectPath = dialog.FileName;
        }

        try
        {
            ProjectSerializer.Save(project, projectPath);
            dirty = false;
            UpdateTitle();
            status.Text = $"Saved {Path.GetFileName(projectPath)}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Cannot save project",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddEntity()
    {
        var scene = sceneView.Scene;
        if (scene is null)
        {
            return;
        }

        var number = scene.Entities.Count + 1;
        var entity = new EntityDocument
        {
            Id = $"entity_{number}",
            Name = $"Entity {number}",
            X = 224,
            Y = 120,
            Width = 32,
            Height = 32,
            Color = "#FFB347FF"
        };
        scene.Entities.Add(entity);
        sceneView.SelectedEntity = entity;
        RefreshHierarchy(entity);
        inspector.SelectedObject = entity;
        sceneView.Invalidate();
        MarkChanged();
        status.Text = $"Added {entity.Name}";
    }

    private void DeleteSelected()
    {
        if (inspector.SelectedObject is not EntityDocument entity ||
            sceneView.Scene is not { } scene)
        {
            return;
        }

        scene.Entities.Remove(entity);
        sceneView.SelectedEntity = null;
        inspector.SelectedObject = scene;
        RefreshHierarchy(scene);
        sceneView.Invalidate();
        MarkChanged();
        status.Text = $"Deleted {entity.Name}";
    }

    private void SelectObject(object? selectedObject)
    {
        if (selectedObject is null)
        {
            return;
        }

        inspector.SelectedObject = selectedObject;
        if (selectedObject is SceneDocument scene)
        {
            sceneView.Scene = scene;
            sceneView.SelectedEntity = null;
            sceneTitle.Text = scene.Name;
        }
        else if (selectedObject is EntityDocument entity)
        {
            sceneView.SelectedEntity = entity;
            sceneView.Invalidate();
        }
    }

    private void ShowProject(GameProject value, string? path)
    {
        project = value;
        projectPath = path;
        dirty = false;

        var startup = project.Scenes.FirstOrDefault(
            scene => scene.Id == project.StartupScene) ?? project.Scenes.FirstOrDefault();
        sceneView.Scene = startup;
        sceneView.SelectedEntity = null;
        sceneTitle.Text = startup?.Name ?? "Scene";
        inspector.SelectedObject = project;
        RefreshHierarchy(project);
        UpdateTitle();
    }

    private void RefreshHierarchy(object? selection = null)
    {
        hierarchy.BeginUpdate();
        hierarchy.Nodes.Clear();

        var projectNode = new TreeNode(project.Name) { Tag = project };
        foreach (var scene in project.Scenes)
        {
            var sceneNode = new TreeNode($"▦  {scene.Name}") { Tag = scene };
            foreach (var entity in scene.Entities)
            {
                sceneNode.Nodes.Add(new TreeNode($"◆  {entity.Name}") { Tag = entity });
            }
            projectNode.Nodes.Add(sceneNode);
        }

        hierarchy.Nodes.Add(projectNode);
        projectNode.ExpandAll();
        hierarchy.EndUpdate();
        SelectHierarchyObject(selection);
    }

    private void SelectHierarchyObject(object? value)
    {
        if (value is null)
        {
            return;
        }

        foreach (TreeNode root in hierarchy.Nodes)
        {
            var match = FindNode(root, value);
            if (match is not null)
            {
                hierarchy.SelectedNode = match;
                return;
            }
        }
    }

    private static TreeNode? FindNode(TreeNode node, object value)
    {
        if (ReferenceEquals(node.Tag, value))
        {
            return node;
        }

        foreach (TreeNode child in node.Nodes)
        {
            var match = FindNode(child, value);
            if (match is not null)
            {
                return match;
            }
        }
        return null;
    }

    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            "This project has unsaved changes. Save them now?",
            "PSP RPG Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if (result == DialogResult.Cancel)
        {
            return false;
        }
        if (result == DialogResult.Yes)
        {
            SaveProject(false);
            return !dirty;
        }
        return true;
    }

    private void MarkChanged()
    {
        dirty = true;
        UpdateTitle();
        status.Text = "Project has unsaved changes";
    }

    private void UpdateTitle()
    {
        Text = $"PSP RPG Editor — {project.Name}{(dirty ? " *" : "")}";
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        return string.IsNullOrWhiteSpace(name) ? "game" : name;
    }

    private static void ShowBuildInfo()
    {
        MessageBox.Show(
            "The editor project format is ready.\n\n" +
            "Next milestone: compile this scene into game.pak and attach it to EBOOT.PBP.",
            "Build for PSP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

internal sealed class SceneView : Control
{
    private const int GridSize = 16;
    private const int MarginSize = 40;
    private EntityDocument? draggingEntity;
    private PointF dragOffset;
    private float scale = 1;
    private PointF origin;

    public event Action<EntityDocument?>? SelectionChanged;
    public event Action? SceneChanged;

    public SceneDocument? Scene { get; set; }
    public EntityDocument? SelectedEntity { get; set; }

    public SceneView()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(22, 24, 28);
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.Selectable, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Scene is null)
        {
            return;
        }

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        CalculateViewport();
        var worldWidth = Scene.Width * scale;
        var worldHeight = Scene.Height * scale;

        using var worldBrush = new SolidBrush(Color.FromArgb(66, 91, 48));
        e.Graphics.FillRectangle(worldBrush, origin.X, origin.Y, worldWidth, worldHeight);
        DrawGrid(e.Graphics);

        foreach (var entity in Scene.Entities)
        {
            var rectangle = EntityRectangle(entity);
            using var brush = new SolidBrush(ParseColor(entity.Color));
            e.Graphics.FillRectangle(brush, rectangle);

            using var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            e.Graphics.FillRectangle(shadow, rectangle.X, rectangle.Bottom - 4, rectangle.Width, 4);

            if (ReferenceEquals(entity, SelectedEntity))
            {
                using var selectionPen = new Pen(Color.FromArgb(255, 230, 90), 2);
                e.Graphics.DrawRectangle(selectionPen, rectangle.X - 2, rectangle.Y - 2,
                    rectangle.Width + 4, rectangle.Height + 4);
                DrawHandles(e.Graphics, rectangle);
            }
        }

        using var border = new Pen(Color.FromArgb(150, 255, 255, 255));
        e.Graphics.DrawRectangle(border, origin.X, origin.Y, worldWidth, worldHeight);
        DrawPspFrame(e.Graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (Scene is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        for (var index = Scene.Entities.Count - 1; index >= 0; index--)
        {
            var entity = Scene.Entities[index];
            if (!EntityRectangle(entity).Contains(e.Location))
            {
                continue;
            }

            SelectedEntity = entity;
            draggingEntity = entity;
            var world = ScreenToWorld(e.Location);
            dragOffset = new PointF(world.X - entity.X, world.Y - entity.Y);
            Cursor = Cursors.SizeAll;
            SelectionChanged?.Invoke(entity);
            Invalidate();
            return;
        }

        SelectedEntity = null;
        SelectionChanged?.Invoke(null);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (draggingEntity is null || Scene is null)
        {
            return;
        }

        var world = ScreenToWorld(e.Location);
        var x = Snap(world.X - dragOffset.X);
        var y = Snap(world.Y - dragOffset.Y);
        draggingEntity.X = Math.Clamp(x, 0, Math.Max(0, Scene.Width - draggingEntity.Width));
        draggingEntity.Y = Math.Clamp(y, 0, Math.Max(0, Scene.Height - draggingEntity.Height));
        SceneChanged?.Invoke();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        draggingEntity = null;
        Cursor = Cursors.Cross;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    private void CalculateViewport()
    {
        if (Scene is null)
        {
            return;
        }
        scale = Math.Min(
            Math.Max(1, ClientSize.Width - MarginSize * 2f) / Scene.Width,
            Math.Max(1, ClientSize.Height - MarginSize * 2f) / Scene.Height);
        scale = Math.Clamp(scale, 0.1f, 3f);
        origin = new PointF(
            (ClientSize.Width - Scene.Width * scale) / 2f,
            (ClientSize.Height - Scene.Height * scale) / 2f);
    }

    private void DrawGrid(Graphics graphics)
    {
        if (Scene is null || GridSize * scale < 5)
        {
            return;
        }
        using var gridPen = new Pen(Color.FromArgb(28, 255, 255, 255));
        for (var x = 0; x <= Scene.Width; x += GridSize)
        {
            var sx = origin.X + x * scale;
            graphics.DrawLine(gridPen, sx, origin.Y, sx, origin.Y + Scene.Height * scale);
        }
        for (var y = 0; y <= Scene.Height; y += GridSize)
        {
            var sy = origin.Y + y * scale;
            graphics.DrawLine(gridPen, origin.X, sy, origin.X + Scene.Width * scale, sy);
        }
    }

    private void DrawPspFrame(Graphics graphics)
    {
        if (Scene is null || Scene.Width < 480 || Scene.Height < 272)
        {
            return;
        }
        using var pspPen = new Pen(Color.FromArgb(110, 90, 180, 255), 2)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
        };
        graphics.DrawRectangle(pspPen, origin.X, origin.Y, 480 * scale, 272 * scale);
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8);
        using var brush = new SolidBrush(Color.FromArgb(190, 150, 210, 255));
        graphics.DrawString("PSP viewport 480×272", font, brush, origin.X + 5, origin.Y + 5);
    }

    private static void DrawHandles(Graphics graphics, RectangleF rectangle)
    {
        const float size = 6;
        using var brush = new SolidBrush(Color.White);
        foreach (var point in new[]
        {
            new PointF(rectangle.Left, rectangle.Top),
            new PointF(rectangle.Right, rectangle.Top),
            new PointF(rectangle.Left, rectangle.Bottom),
            new PointF(rectangle.Right, rectangle.Bottom)
        })
        {
            graphics.FillRectangle(brush, point.X - size / 2, point.Y - size / 2, size, size);
        }
    }

    private RectangleF EntityRectangle(EntityDocument entity) => new(
        origin.X + entity.X * scale,
        origin.Y + entity.Y * scale,
        entity.Width * scale,
        entity.Height * scale);

    private PointF ScreenToWorld(Point point) => new(
        (point.X - origin.X) / scale,
        (point.Y - origin.Y) / scale);

    private static float Snap(float value) =>
        MathF.Round(value / GridSize) * GridSize;

    private static Color ParseColor(string value)
    {
        if (value.Length == 9 &&
            uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var rgba))
        {
            return Color.FromArgb(
                (int)(rgba & 0xFF),
                (int)((rgba >> 24) & 0xFF),
                (int)((rgba >> 16) & 0xFF),
                (int)((rgba >> 8) & 0xFF));
        }
        return Color.Magenta;
    }
}
