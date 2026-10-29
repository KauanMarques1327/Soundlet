using Microsoft.Maui.Graphics;

namespace Soundlet;

// Only the arc is drawn and rotated. The Bluetooth glyph stays in the original image.
internal sealed class LoadingArcDrawable(float referenceSize, float strokeWidth) : IDrawable
{
    private static readonly Color ArcColor = Color.FromArgb("#00F0FF");

    public float Rotation { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        var stroke = strokeWidth * size / referenceSize;
        var x = dirtyRect.X + (dirtyRect.Width - size) / 2;
        var y = dirtyRect.Y + (dirtyRect.Height - size) / 2;

        canvas.SaveState();
        canvas.Antialias = true;
        canvas.StrokeColor = ArcColor;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.Rotate(Rotation, x + size / 2, y + size / 2);
        canvas.DrawArc(x + stroke, y + stroke, size - 2 * stroke, size - 2 * stroke,
            0, 270, clockwise: false, closed: false);
        canvas.RestoreState();
    }
}
