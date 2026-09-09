using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;

namespace Agriloco.Api.Services;

// Unity's precompressed files need the original MIME type and encoding.
public static class UnityWebGlStaticFiles
{
    public static StaticFileOptions CreateOptions() => new()
    {
        ContentTypeProvider = new UnityContentTypeProvider(),
        OnPrepareResponse = context =>
        {
            var path = context.Context.Request.Path.Value ?? "";
            if (!(path.StartsWith("/Unity/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/viewer/", StringComparison.OrdinalIgnoreCase))) return;

            if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
                context.Context.Response.Headers.ContentEncoding = "br";
            else if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                context.Context.Response.Headers.ContentEncoding = "gzip";
        }
    };

    private sealed class UnityContentTypeProvider : IContentTypeProvider
    {
        private readonly FileExtensionContentTypeProvider defaults = new();

        public bool TryGetContentType(string subpath, out string contentType)
        {
            if (subpath.StartsWith("/Unity/", StringComparison.OrdinalIgnoreCase) || subpath.StartsWith("/viewer/", StringComparison.OrdinalIgnoreCase))
            {
                var originalPath = subpath;
                if (subpath.EndsWith(".br", StringComparison.OrdinalIgnoreCase) ||
                    subpath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    originalPath = subpath[..^3];

                switch (Path.GetExtension(originalPath).ToLowerInvariant())
                {
                    case ".wasm": contentType = "application/wasm"; return true;
                    case ".js": contentType = "application/javascript"; return true;
                    case ".data": contentType = "application/octet-stream"; return true;
                }
            }

            return defaults.TryGetContentType(subpath, out contentType!);
        }
    }
}
