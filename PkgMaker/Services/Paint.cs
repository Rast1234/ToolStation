using PkgMaker.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace PkgMaker.Services;

public static class Paint
{
    public static void CheckPngDimensions(byte[] data, uint expectedWidth, uint expectedHeight, string name)
    {
        var info = Image.Identify(data);
        if (info.Width != expectedWidth || info.Height != expectedHeight) throw new ArgumentException($"Expected {name} to be [{expectedWidth}*{expectedHeight}], got [{info.Width}*{info.Height}]");
    }

    /// <summary>
    /// Combine existing images together. Dimensions are already validated
    /// </summary>
    public static async Task<byte[]> RenderPreview(byte[] icon, byte[]? background, byte[]? overlay, CancellationToken token)
    {
        var img = Image.Load(Resources.GetEmbedded("wave.png"));
        var render = img.Clone(c =>
        {
            if (background != null) c.DrawImage(Image.Load(background), 1);

            c.DrawImage(Image.Load(Resources.GetEmbedded("xmb.png")), 1);
            c.DrawImage(Image.Load(icon), new Point(405, 417), 1);

            if (overlay != null) c.DrawImage(Image.Load(overlay), new Point(750, 416), 1);
        });

        await using var ms = new MemoryStream();
        await render.SaveAsync(ms, PngFormat.Instance, token);
        return ms.ToArray();
    }
}