using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace Agriloco.Api.Services;

// Bounded, disposable derived cache. MarketDetails.Image remains the only stored source.
public sealed class DefinitionThumbnails : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 * 1024 });
    private readonly SemaphoreSlim gate = new(1, 1);
    public sealed record Thumbnail(byte[] Bytes, string ETag);

    public async Task<Thumbnail?> GetAsync(byte[] original)
    {
        var key = Convert.ToHexString(SHA256.HashData(original));
        if (cache.TryGetValue<Thumbnail>(key, out var hit)) return hit;
        await gate.WaitAsync();
        try
        {
            if (cache.TryGetValue<Thumbnail>(key, out hit)) return hit;
            using var data = SKData.CreateCopy(original);
            using var codec = SKCodec.Create(data);
            if (codec == null || codec.Info.Width <= 0 || codec.Info.Height <= 0) return null;
            var size = codec.GetScaledDimensions(Math.Min(1f, 160f / Math.Max(codec.Info.Width, codec.Info.Height)));
            // Bound decoding memory even for highly compressed oversized uploads.
            if (size.Width <= 0 || size.Height <= 0 || (long)size.Width * size.Height > 16_000_000) return null;
            using var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) return null;
            bool swap = (int)codec.EncodedOrigin >= 5;
            int w = swap ? size.Height : size.Width, h = swap ? size.Width : size.Height;
            float scale = Math.Min(1f, 160f / Math.Max(w, h));
            using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)Math.Round(w * scale)), Math.Max(1, (int)Math.Round(h * scale))));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent); canvas.Scale(scale);
            // Apply EXIF orientation, including mirrored photos, before encoding without metadata.
            switch (codec.EncodedOrigin)
            {
                case SKEncodedOrigin.TopRight: canvas.Translate(size.Width, 0); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.BottomRight: canvas.Translate(size.Width, size.Height); canvas.RotateDegrees(180); break;
                case SKEncodedOrigin.BottomLeft: canvas.Translate(0, size.Height); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.RightTop: canvas.Translate(size.Height, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(size.Height, size.Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, size.Width); canvas.RotateDegrees(270); break;
            }
            using var decoded = SKImage.FromBitmap(bitmap);
            canvas.DrawImage(decoded, new SKRect(0, 0, size.Width, size.Height), new SKSamplingOptions(SKFilterMode.Linear));
            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80);
            if (encoded == null) return null;
            var bytes = encoded.ToArray();
            var thumbnail = new Thumbnail(bytes, '"' + Convert.ToHexString(SHA256.HashData(bytes)) + '"');
            cache.Set(key, thumbnail, new MemoryCacheEntryOptions { Size = bytes.Length, SlidingExpiration = TimeSpan.FromMinutes(20) });
            return thumbnail;
        }
        finally { gate.Release(); }
    }
    public void Dispose() { cache.Dispose(); gate.Dispose(); }
}
