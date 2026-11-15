using Microsoft.Maui.Graphics;

namespace SmartPos.Mobile.Controls;

/// <summary>
/// Draws a floating white bottom bar with a center convex bump that wraps a raised cart medallion.
/// Shadow is painted in-canvas to avoid Android Border.Shadow black-square artifacts.
/// </summary>
public sealed class CurvedBottomBarDrawable : IDrawable
{
    private static readonly Color FillColor = Color.FromArgb("#FFFFFF");
    private static readonly Color StrokeColor = Color.FromArgb("#EAE2DA");
    private static readonly Color ShadowColor = Color.FromArgb("#1C1410");

    /// <summary>Horizontal inset from the GraphicsView edges (matches prior pill margin).</summary>
    public float HorizontalInset { get; set; } = 14f;

    /// <summary>Bottom inset so the bar sits slightly above the view edge.</summary>
    public float BottomInset { get; set; } = 8f;

    /// <summary>Height of the flat bar body (excluding the center bump).</summary>
    public float BarBodyHeight { get; set; } = 66f;

    /// <summary>Corner radius of the left/right ends of the bar.</summary>
    public float CornerRadius { get; set; } = 28f;

    /// <summary>Half-width of the center bump where it meets the flat top.</summary>
    public float BumpHalfWidth { get; set; } = 38f;

    /// <summary>How far the bump rises above the flat top edge of the bar.</summary>
    public float BumpRise { get; set; } = 24f;

    // Matches PageBackground so Android GraphicsView never leaves a black void outside the bar path.
    private static readonly Color CanvasClearColor = Color.FromArgb("#FAF7F4");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
        {
            return;
        }

        // Clear the full surface first — Android GraphicsView defaults to opaque black.
        canvas.FillColor = CanvasClearColor;
        canvas.FillRectangle(dirtyRect);

        var path = BuildBarPath(dirtyRect);
        if (path is null)
        {
            return;
        }

        // Soft layered shadow (no Border.Shadow — avoids opaque black squares on Android).
        canvas.SaveState();
        for (var i = 3; i >= 1; i--)
        {
            var offsetY = 2f + i * 1.5f;
            var alpha = 0.035f * i;
            canvas.Translate(0, offsetY);
            canvas.FillColor = ShadowColor.WithAlpha(alpha);
            canvas.FillPath(path);
            canvas.Translate(0, -offsetY);
        }

        canvas.FillColor = FillColor;
        canvas.FillPath(path);

        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = 1f;
        canvas.DrawPath(path);
        canvas.RestoreState();
    }

    private PathF? BuildBarPath(RectF dirtyRect)
    {
        var left = HorizontalInset;
        var right = dirtyRect.Width - HorizontalInset;
        var bottom = dirtyRect.Height - BottomInset;
        var top = bottom - BarBodyHeight;

        if (right - left < 40f || bottom - top < 20f)
        {
            return null;
        }

        var cx = dirtyRect.Width * 0.5f;
        var rise = Math.Min(BumpRise, Math.Max(0f, top - 2f));
        var half = Math.Min(BumpHalfWidth, (right - left) * 0.28f);
        var bumpLeft = cx - half;
        var bumpRight = cx + half;
        var bumpTop = top - rise;

        var cr = Math.Min(CornerRadius, Math.Min((right - left) * 0.5f, BarBodyHeight * 0.5f));

        // Cubic control offset for a smooth circular-ish bump.
        var cp = half * 0.55f;

        var path = new PathF();

        // Top-left after corner
        path.MoveTo(left + cr, top);

        // Flat top → left side of bump
        path.LineTo(bumpLeft, top);

        // Convex bump: left → peak → right (two cubics)
        path.CurveTo(
            bumpLeft + cp, top,
            cx - cp, bumpTop,
            cx, bumpTop);
        path.CurveTo(
            cx + cp, bumpTop,
            bumpRight - cp, top,
            bumpRight, top);

        // Flat top → top-right corner
        path.LineTo(right - cr, top);
        path.QuadTo(right, top, right, top + cr);

        // Right edge → bottom-right
        path.LineTo(right, bottom - cr);
        path.QuadTo(right, bottom, right - cr, bottom);

        // Bottom edge → bottom-left
        path.LineTo(left + cr, bottom);
        path.QuadTo(left, bottom, left, bottom - cr);

        // Left edge → top-left
        path.LineTo(left, top + cr);
        path.QuadTo(left, top, left + cr, top);

        path.Close();
        return path;
    }
}
