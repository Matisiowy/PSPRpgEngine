using System.Globalization;
using PSPRpgEditor.Core;

namespace PSPRpgEditor.App;

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
            return;

        CalculateViewport();
        var worldWidth = Scene.Width * scale;
        var worldHeight = Scene.Height * scale;
        using var worldBrush = new SolidBrush(Color.FromArgb(66, 91, 48));
        e.Graphics.FillRectangle(worldBrush, origin.X, origin.Y, worldWidth, worldHeight);
        DrawGrid(e.Graphics);

        foreach (var entity in Scene.Entities.Where(
                     entity => entity.SpriteRenderer.Visible))
        {
            var rectangle = EntityRectangle(entity);
            using var brush = new SolidBrush(ParseColor(entity.SpriteRenderer.Color));
            e.Graphics.FillRectangle(brush, rectangle);
            if (ReferenceEquals(entity, SelectedEntity))
            {
                using var pen = new Pen(Color.FromArgb(255, 230, 90), 2);
                e.Graphics.DrawRectangle(pen, rectangle.X - 2, rectangle.Y - 2,
                    rectangle.Width + 4, rectangle.Height + 4);
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
            return;

        var entity = Scene.Entities.LastOrDefault(item =>
            EntityRectangle(item).Contains(e.Location));
        SelectedEntity = entity;
        SelectionChanged?.Invoke(entity);
        if (entity is not null)
        {
            draggingEntity = entity;
            var world = ScreenToWorld(e.Location);
            dragOffset = new PointF(
                world.X - entity.Transform.X, world.Y - entity.Transform.Y);
            Cursor = Cursors.SizeAll;
        }
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (draggingEntity is null || Scene is null)
            return;

        var world = ScreenToWorld(e.Location);
        var transform = draggingEntity.Transform;
        transform.X = Math.Clamp(Snap(world.X - dragOffset.X),
            0, Math.Max(0, Scene.Width - transform.Width));
        transform.Y = Math.Clamp(Snap(world.Y - dragOffset.Y),
            0, Math.Max(0, Scene.Height - transform.Height));
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
            return;
        scale = Math.Clamp(Math.Min(
            Math.Max(1, ClientSize.Width - MarginSize * 2f) / Scene.Width,
            Math.Max(1, ClientSize.Height - MarginSize * 2f) / Scene.Height),
            0.1f, 3f);
        origin = new PointF(
            (ClientSize.Width - Scene.Width * scale) / 2f,
            (ClientSize.Height - Scene.Height * scale) / 2f);
    }

    private void DrawGrid(Graphics graphics)
    {
        if (Scene is null || GridSize * scale < 5)
            return;
        using var pen = new Pen(Color.FromArgb(28, 255, 255, 255));
        for (var x = 0; x <= Scene.Width; x += GridSize)
        {
            var screenX = origin.X + x * scale;
            graphics.DrawLine(pen, screenX, origin.Y,
                screenX, origin.Y + Scene.Height * scale);
        }
        for (var y = 0; y <= Scene.Height; y += GridSize)
        {
            var screenY = origin.Y + y * scale;
            graphics.DrawLine(pen, origin.X, screenY,
                origin.X + Scene.Width * scale, screenY);
        }
    }

    private void DrawPspFrame(Graphics graphics)
    {
        if (Scene is null || Scene.Width < 480 || Scene.Height < 272)
            return;
        using var pen = new Pen(Color.FromArgb(110, 90, 180, 255), 2)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
        };
        graphics.DrawRectangle(pen, origin.X, origin.Y, 480 * scale, 272 * scale);
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8);
        using var brush = new SolidBrush(Color.FromArgb(190, 150, 210, 255));
        graphics.DrawString("PSP viewport 480x272", font, brush,
            origin.X + 5, origin.Y + 5);
    }

    private RectangleF EntityRectangle(EntityDocument entity) => new(
        origin.X + entity.Transform.X * scale,
        origin.Y + entity.Transform.Y * scale,
        entity.Transform.Width * scale,
        entity.Transform.Height * scale);

    private PointF ScreenToWorld(Point point) => new(
        (point.X - origin.X) / scale, (point.Y - origin.Y) / scale);

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
