using Microsoft.Maui.Graphics;

namespace Soundlet;

// Native graphics keep both selection states crisp and switch without loading images.
internal sealed class ConnectionOptionsDrawable : IDrawable
{
    internal static readonly Rect BluetoothBounds = new(110, 1175, 632, 125);
    internal static readonly Rect WifiBounds = new(110, 1335, 632, 127);
    public bool WifiSelected { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var scale = Math.Max(dirtyRect.Width / 850, dirtyRect.Height / 1850);
        canvas.SaveState();
        canvas.Translate((dirtyRect.Width - 850 * scale) / 2, (dirtyRect.Height - 1850 * scale) / 2);
        canvas.Scale(scale, scale);
        canvas.Antialias = true;
        DrawOption(canvas, 1175, false, !WifiSelected);
        DrawOption(canvas, 1335, true, WifiSelected);
        canvas.RestoreState();
    }

    private static void DrawOption(ICanvas canvas, float y, bool wifi, bool selected)
    {
        canvas.SaveState();
        var bounds = new RectF(110, y, 632, 126);
        if (selected)
            canvas.SetShadow(new SizeF(0, 0), 22, Color.FromArgb("#6600DFFF"));
        canvas.SetFillPaint(new LinearGradientPaint
        {
            StartColor = Color.FromArgb(selected ? "#00DAEB" : "#091B2B"),
            EndColor = Color.FromArgb(selected ? "#0096FF" : "#0B2C49"),
            StartPoint = new Point(0, 0), EndPoint = new Point(1, 1)
        }, bounds);
        canvas.FillRoundedRectangle(bounds, 63);
        canvas.RestoreState();
        if (!selected)
        {
            canvas.StrokeColor = Color.FromArgb("#4D78A6");
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(bounds, 63);
        }

        var ink = Color.FromArgb(selected ? "#001725" : "#89A2C5");
        canvas.FontColor = ink;
        canvas.Font = new Microsoft.Maui.Graphics.Font("sans-serif", selected ? 700 : 500);
        canvas.FontSize = 33;
        canvas.DrawString(wifi ? "Conectar via Wi-Fi" : "Conectar via Bluetooth",
            271, y, 375, 126, HorizontalAlignment.Left, VerticalAlignment.Center);
        var iconColor = selected ? ink : Color.FromArgb("#375A7E");
        canvas.StrokeColor = iconColor;
        canvas.StrokeSize = wifi ? 8 : 5;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        var cy = y + 63;
        if (wifi)
        {
            for (var i = 0; i < 3; i++)
            {
                var half = 32 - i * 10;
                var top = cy - 19 + i * 14;
                var path = new PathF();
                path.MoveTo(201 - half, top + 9);
                path.QuadTo(201, top - half * 0.65f, 201 + half, top + 9);
                canvas.DrawPath(path);
            }
            canvas.FillColor = iconColor;
            canvas.FillCircle(201, cy + 24, 5);
        }
        else
        {
            var path = new PathF();
            path.MoveTo(188, cy - 16); path.LineTo(219, cy + 15);
            path.LineTo(204, cy + 29); path.LineTo(204, cy - 29);
            path.LineTo(219, cy - 15); path.LineTo(188, cy + 16);
            canvas.DrawPath(path);
        }
        canvas.StrokeColor = Color.FromArgb(selected ? "#FFEE00" : "#557BA7");
        canvas.StrokeSize = selected ? 6 : 4;
        var arrow = new PathF();
        arrow.MoveTo(668, cy - 13); arrow.LineTo(680, cy); arrow.LineTo(668, cy + 13);
        canvas.DrawPath(arrow);
    }
}
