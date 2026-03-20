using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace ASPNET.Services;

public interface IWatermarkService
{
    byte[] ApplyWatermark(byte[] imageBytes, string text);
}

public class WatermarkService : IWatermarkService
{
    public byte[] ApplyWatermark(byte[] imageBytes, string text)
    {
        using var image = Image.Load(imageBytes);
        
        // Font setup (using a system font for simplicity in this demo)
        // In a real app, you might bundle a specific .ttf file
        var font = SystemFonts.CreateFont("Arial", 25, FontStyle.Italic);

        image.Mutate(x => {
            // Center the watermark text with low opacity
            var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
            var location = new PointF((image.Width - size.Width) / 2, (image.Height - size.Height) / 2);
            
            x.DrawText(text, font, Color.White.WithAlpha(0.3f), location);
        });

        using var ms = new MemoryStream();
        image.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return ms.ToArray();
    }

    private byte[] CreateDummyImage()
    {
        // Tạo 1 ảnh JPEG trắng đơn giản để demo watermark
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(800, 1200);
        using var outMs = new MemoryStream();
        image.Save(outMs, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
        return outMs.ToArray();
    }
}
