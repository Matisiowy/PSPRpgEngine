using PSPRpgEditor.Core;

namespace PSPRpgEditor.App;

public sealed class MainForm : Form
{
    private readonly TreeView hierarchy = new();
    private readonly InspectorPanel inspector = new();
    private readonly SceneView sceneView = new();
    private readonly AssetBrowserPanel assetBrowser = new();
    private readonly RichTextBox buildLog = new();
    private readonly ToolStripStatusLabel status = new("Ready");
    private readonly EditorSettings settings = ToolchainService.LoadSettings();

    private GameProject project = GameProject.CreateDefault();
    private string? projectPath;
    private bool dirty;

    public MainForm(string? startupProject = null)
    {
        Text = "PSP RPG Studio";
        Width = 1360;
        Height = 820;
        MinimumSize = new Size(1000, 650);
        BackColor = EditorTheme.Window;
        ForeColor = EditorTheme.Text;

        Controls.Add(BuildWorkspace());
        Controls.Add(BuildStatusStrip());
        Controls.Add(BuildToolbar());
        Controls.Add(BuildMenu());

        hierarchy.AfterSelect += (_, e) => SelectObject(e.Node?.Tag);
        inspector.ObjectChanged += ProjectChanged;
        sceneView.SelectionChanged += entity =>
        {
            inspector.SelectedObject = entity;
            SelectHierarchyObject(entity);
        };
        sceneView.SceneChanged += ProjectChanged;
        FormClosing += (_, e) => e.Cancel = !ConfirmDiscardChanges();

        if (startupProject is not null)
            OpenProject(startupProject);
        else
            ShowProject(project, null);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            BackColor = EditorTheme.Header,
            ForeColor = EditorTheme.Text,
            Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
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
        edit.DropDownItems.Add(new ToolStripMenuItem(
            "&Delete Selected", null, (_, _) => DeleteSelected())
        { ShortcutKeys = Keys.Delete });

        var build = new ToolStripMenuItem("&Build");
        build.DropDownItems.Add("Build game.pak", null, (_, _) => BuildProject(false));
        build.DropDownItems.Add("Build && Run PPSSPP", null, (_, _) => BuildProject(true));
        build.DropDownItems.Add(new ToolStripSeparator());
        build.DropDownItems.Add("Toolchain settings...", null, (_, _) => ConfigureTools());

        menu.Items.AddRange([file, edit, build]);
        MainMenuStrip = menu;
        return menu;
    }

    private ToolStrip BuildToolbar()
    {
        var bar = new ToolStrip
        {
            BackColor = EditorTheme.Header,
            ForeColor = EditorTheme.Text,
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(7, 4, 7, 4),
            Renderer = new ToolStripProfessionalRenderer(new DarkColorTable())
        };
        bar.Items.Add(Button("＋", "New project", (_, _) => NewProject()));
        bar.Items.Add(Button("⌂", "Open project", (_, _) => OpenProject()));
        bar.Items.Add(Button("▣", "Save project", (_, _) => SaveProject(false)));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(Button("＋ Object", "Add entity", (_, _) => AddEntity()));
        bar.Items.Add(Button("Delete", "Delete selected", (_, _) => DeleteSelected()));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(Button("▶  Play", "Build and launch PPSSPP",
            (_, _) => BuildProject(true)));
        bar.Items.Add(Button("Build PSP", "Build distributable game",
            (_, _) => BuildProject(false)));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(new ToolStripLabel("  2D  |  Grid 16  |  PSP 480x272")
        {
            ForeColor = Color.FromArgb(150, 157, 169)
        });
        return bar;
    }

    private static ToolStripButton Button(
        string text, string tooltip, EventHandler handler)
    {
        var button = new ToolStripButton(text)
        {
            ToolTipText = tooltip,
            ForeColor = EditorTheme.Text,
            Margin = new Padding(2, 0, 2, 0)
        };
        button.Click += handler;
        return button;
    }

    private StatusStrip BuildStatusStrip()
    {
        var strip = new StatusStrip
        {
            BackColor = EditorTheme.Header,
            ForeColor = EditorTheme.Text
        };
        strip.Items.Add(status);
        strip.Items.Add(new ToolStripStatusLabel { Spring = true });
        strip.Items.Add(new ToolStripStatusLabel("PSP 480 x 272 | Grid 16"));
        return strip;
    }

    private Control BuildWorkspace()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = EditorTheme.Window,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

        hierarchy.Dock = DockStyle.Fill;
        hierarchy.BackColor = EditorTheme.Panel;
        hierarchy.ForeColor = EditorTheme.Text;
        hierarchy.BorderStyle = BorderStyle.None;
        hierarchy.HideSelection = false;

        inspector.Dock = DockStyle.Fill;

        layout.Controls.Add(BuildLeftSidebar(), 0, 0);
        layout.Controls.Add(BuildCenterWorkspace(), 1, 0);
        layout.Controls.Add(EditorTheme.CreatePanel("OBJECT INSPECTOR", inspector), 2, 0);
        return layout;
    }

    private Control BuildLeftSidebar()
    {
        var tabs = new StudioTabControl { Dock = DockStyle.Fill };
        var scenePage = NewTab("Scene");
        scenePage.Controls.Add(hierarchy);

        var layersPage = NewTab("Layers");
        layersPage.Controls.Add(CreateInfoList(
            "Ground", "Objects", "Collision", "Entities", "Triggers"));

        var settingsPage = NewTab("Settings");
        settingsPage.Controls.Add(CreateInfoList(
            "Project", "Rendering", "Input", "Audio", "PSP Build"));

        tabs.TabPages.Add(scenePage);
        tabs.TabPages.Add(layersPage);
        tabs.TabPages.Add(settingsPage);
        return tabs;
    }

    private Control BuildCenterWorkspace()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 480,
            BackColor = Color.FromArgb(20, 22, 26),
            Panel1MinSize = 250,
            Panel2MinSize = 130
        };

        var documents = new StudioTabControl { Dock = DockStyle.Fill };
        var viewport = NewTab("2D Viewport");
        sceneView.Dock = DockStyle.Fill;
        viewport.Controls.Add(sceneView);
        viewport.Controls.Add(BuildViewportToolbar());
        var graph = NewTab("Event Graph");
        graph.Controls.Add(new EventGraphPreview { Dock = DockStyle.Fill });
        documents.TabPages.Add(viewport);
        documents.TabPages.Add(graph);

        var bottom = new StudioTabControl
        {
            Dock = DockStyle.Fill,
            ItemSize = new Size(100, 25)
        };
        var files = NewTab("Files");
        assetBrowser.Dock = DockStyle.Fill;
        files.Controls.Add(assetBrowser);
        var log = NewTab("Log");
        buildLog.Dock = DockStyle.Fill;
        buildLog.ReadOnly = true;
        buildLog.BackColor = Color.FromArgb(25, 28, 33);
        buildLog.ForeColor = Color.FromArgb(187, 194, 204);
        buildLog.BorderStyle = BorderStyle.None;
        buildLog.Font = new Font("Consolas", 9);
        log.Controls.Add(buildLog);
        bottom.TabPages.Add(files);
        bottom.TabPages.Add(log);

        split.Panel1.Controls.Add(documents);
        split.Panel2.Controls.Add(bottom);
        return split;
    }

    private ToolStrip BuildViewportToolbar()
    {
        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Top,
            Height = 28,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(39, 42, 49),
            ForeColor = EditorTheme.Text,
            Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()),
            Padding = new Padding(4, 1, 4, 1)
        };
        toolbar.Items.Add(new ToolStripButton("Select") { Checked = true });
        toolbar.Items.Add(new ToolStripButton("Move"));
        toolbar.Items.Add(new ToolStripButton("Paint"));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripButton("Grid") { Checked = true });
        toolbar.Items.Add(new ToolStripButton("PSP Frame") { Checked = true });
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(new ToolStripLabel("Camera: Main"));
        return toolbar;
    }

    private static TabPage NewTab(string text) => new(text)
    {
        BackColor = Color.FromArgb(27, 30, 35),
        ForeColor = EditorTheme.Text,
        Padding = new Padding(0)
    };

    private static Control CreateInfoList(params string[] items)
    {
        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = EditorTheme.Panel,
            ForeColor = Color.FromArgb(188, 194, 204),
            BorderStyle = BorderStyle.None,
            ItemHeight = 24,
            IntegralHeight = false
        };
        list.Items.AddRange(items);
        return list;
    }

    private void NewProject()
    {
        if (ConfirmDiscardChanges())
            ShowProject(GameProject.CreateDefault(), null);
    }

    private void OpenProject()
    {
        if (!ConfirmDiscardChanges())
            return;
        using var dialog = new OpenFileDialog { Filter = ProjectSerializer.FileFilter };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            OpenProject(dialog.FileName);
    }

    private void OpenProject(string path)
    {
        try
        {
            var loaded = ProjectSerializer.Load(path);
            ShowProject(loaded, path);
            status.Text = loaded.WasMigrated
                ? "Legacy project loaded. Save to migrate it to format v2."
                : $"Opened {Path.GetFileName(path)}";
            dirty = loaded.WasMigrated;
            UpdateTitle();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Cannot open project",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool SaveProject(bool choosePath)
    {
        if (choosePath || projectPath is null)
        {
            using var dialog = new SaveFileDialog
            {
                FileName = $"{SafeName(project.Name)}{ProjectSerializer.FileExtension}",
                DefaultExt = "psprpg",
                AddExtension = true,
                Filter = ProjectSerializer.FileFilter
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;
            projectPath = dialog.FileName;
        }

        try
        {
            ProjectSerializer.Save(project, projectPath);
            dirty = false;
            assetBrowser.SetProject(projectPath);
            UpdateTitle();
            status.Text = $"Saved {Path.GetFileName(projectPath)}";
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Cannot save project",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void AddEntity()
    {
        if (sceneView.Scene is not { } scene)
            return;
        var entity = new EntityDocument
        {
            Name = $"Entity {scene.Entities.Count + 1}",
            Transform = new Transform2D { X = 224, Y = 120 },
            SpriteRenderer = new SpriteRenderer { Color = "#FFB347FF" }
        };
        scene.Entities.Add(entity);
        sceneView.SelectedEntity = entity;
        inspector.SelectedObject = entity;
        ProjectChanged();
    }

    private void DeleteSelected()
    {
        if (inspector.SelectedObject is not EntityDocument entity ||
            sceneView.Scene is not { } scene)
            return;
        scene.Entities.Remove(entity);
        sceneView.SelectedEntity = null;
        inspector.SelectedObject = scene;
        ProjectChanged();
    }

    private async void BuildProject(bool run)
    {
        if (projectPath is null && !SaveProject(true))
            return;
        if (dirty && !SaveProject(false))
            return;

        try
        {
            var tools = ToolchainService.Detect(settings);
            var result = BuildService.Build(project, projectPath!, tools);
            if (result.EbootPath is null && tools.HasPspToolchain)
            {
                var projectDirectory = Path.GetDirectoryName(
                    Path.GetFullPath(projectPath!))!;
                var repositoryRoot = BuildService.FindRepositoryRoot(projectDirectory);
                if (repositoryRoot is not null)
                {
                    status.Text = "Building PSP runtime...";
                    var runtimeBuild = await BuildService.BuildRuntimeAsync(repositoryRoot);
                    if (runtimeBuild.ExitCode != 0)
                        throw new InvalidOperationException(
                            "PSP runtime build failed:\n\n" + runtimeBuild.Output);
                    result = BuildService.Build(project, projectPath!, tools);
                }
            }
            status.Text = string.Join(" ", result.Messages);
            AppendLog(result.Messages);

            if (!run)
            {
                MessageBox.Show(string.Join(Environment.NewLine, result.Messages) +
                    $"\n\nOutput: {result.OutputDirectory}", "Build complete");
                return;
            }

            if (result.EbootPath is null)
                throw new InvalidOperationException(
                    "game.pak was built, but EBOOT.PBP is missing. Build the PSP runtime first.");
            if (!tools.HasPpsspp)
                throw new InvalidOperationException(
                    "PPSSPP was not found. Configure its path in Build > Toolchain settings.");
            ToolchainService.LaunchPpsspp(tools.PpssppPath, result.EbootPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Build failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ConfigureTools()
    {
        using var dialog = new ToolchainSettingsForm(settings);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ToolchainService.SaveSettings(settings);
            var detected = ToolchainService.Detect(settings);
            status.Text = $"PSPSDK: {(detected.HasPspToolchain ? "found" : "missing")} | " +
                          $"PPSSPP: {(detected.HasPpsspp ? "found" : "missing")}";
        }
    }

    private void ShowProject(GameProject value, string? path)
    {
        project = value;
        projectPath = path;
        dirty = false;
        sceneView.Scene = project.Scenes.FirstOrDefault(
            scene => scene.Id == project.StartupScene) ?? project.Scenes.FirstOrDefault();
        sceneView.SelectedEntity = null;
        inspector.SelectedObject = project;
        assetBrowser.SetProject(path);
        ProjectChanged(markDirty: false);
    }

    private void SelectObject(object? value)
    {
        if (value is null)
            return;
        inspector.SelectedObject = value;
        if (value is SceneDocument scene)
        {
            sceneView.Scene = scene;
            sceneView.SelectedEntity = null;
        }
        else if (value is EntityDocument entity)
            sceneView.SelectedEntity = entity;
        sceneView.Invalidate();
    }

    private void ProjectChanged() => ProjectChanged(true);

    private void ProjectChanged(bool markDirty)
    {
        dirty |= markDirty;
        var selected = inspector.SelectedObject;
        hierarchy.BeginUpdate();
        hierarchy.Nodes.Clear();
        var root = new TreeNode(project.Name) { Tag = project };
        foreach (var scene in project.Scenes)
        {
            var sceneNode = new TreeNode($"Scene: {scene.Name}") { Tag = scene };
            foreach (var entity in scene.Entities)
                sceneNode.Nodes.Add(new TreeNode(entity.Name) { Tag = entity });
            root.Nodes.Add(sceneNode);
        }
        hierarchy.Nodes.Add(root);
        root.ExpandAll();
        hierarchy.EndUpdate();
        SelectHierarchyObject(selected);
        sceneView.Invalidate();
        inspector.RefreshView();
        UpdateTitle();
    }

    private void SelectHierarchyObject(object? value)
    {
        if (value is null)
            return;
        foreach (TreeNode node in hierarchy.Nodes)
        {
            var match = FindNode(node, value);
            if (match is not null)
            {
                hierarchy.SelectedNode = match;
                break;
            }
        }
    }

    private static TreeNode? FindNode(TreeNode node, object value)
    {
        if (ReferenceEquals(node.Tag, value))
            return node;
        foreach (TreeNode child in node.Nodes)
        {
            var result = FindNode(child, value);
            if (result is not null)
                return result;
        }
        return null;
    }

    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
            return true;
        var result = MessageBox.Show("Save changes before continuing?",
            "PSP RPG Studio", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        return result switch
        {
            DialogResult.Yes => SaveProject(false),
            DialogResult.No => true,
            _ => false
        };
    }

    private void UpdateTitle() =>
        Text = $"PSP RPG Studio - {project.Name}{(dirty ? " *" : "")}";

    private void AppendLog(IEnumerable<string> messages)
    {
        foreach (var message in messages)
            buildLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static string SafeName(string value)
    {
        foreach (var character in Path.GetInvalidFileNameChars())
            value = value.Replace(character, '_');
        return string.IsNullOrWhiteSpace(value) ? "Game" : value;
    }
}

internal sealed class ToolchainSettingsForm : Form
{
    public ToolchainSettingsForm(EditorSettings settings)
    {
        Text = "Toolchain settings";
        Width = 620;
        Height = 190;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var pspDev = new TextBox { Text = settings.PspDevPath, Dock = DockStyle.Fill };
        var ppsspp = new TextBox { Text = settings.PpssppPath, Dock = DockStyle.Fill };
        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK };
        ok.Click += (_, _) =>
        {
            settings.PspDevPath = pspDev.Text.Trim();
            settings.PpssppPath = ppsspp.Text.Trim();
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "PSPDEV folder:", AutoSize = true }, 0, 0);
        layout.Controls.Add(pspDev, 1, 0);
        layout.Controls.Add(new Label { Text = "PPSSPP exe:", AutoSize = true }, 0, 1);
        layout.Controls.Add(ppsspp, 1, 1);
        layout.Controls.Add(ok, 1, 2);
        Controls.Add(layout);
        AcceptButton = ok;
    }
}
