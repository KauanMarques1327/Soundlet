namespace Soundlet;

// Coordinates refer to the 850 x 1850 reference composition. Both the artwork
// and this overlay use the same centered AspectFill transform at every size.
public sealed class SplashProgressDrawable : IDrawable
{
    public float Progress { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var scale = Math.Max(dirtyRect.Width / 850f, dirtyRect.Height / 1850f);
        canvas.SaveState();
        canvas.Translate((dirtyRect.Width - 850 * scale) / 2, (dirtyRect.Height - 1850 * scale) / 2);
        canvas.Scale(scale, scale);
        canvas.FillColor = Color.FromArgb("#0B385C");
        canvas.FillRoundedRectangle(293, 1247, 274, 8, 4);
        var width = 274 * Math.Clamp(Progress, 0, 1);
        if (width > 0)
        {
            // The glow follows the animated fill, including its rounded leading edge.
            canvas.SetShadow(new SizeF(0, 0), 14, Color.FromArgb("#00DCEB"));
            canvas.FillColor = Color.FromArgb("#00DCEB");
            canvas.FillRoundedRectangle(293, 1247, width, 8, Math.Min(4, width / 2));
        }
        canvas.RestoreState();
    }
}
