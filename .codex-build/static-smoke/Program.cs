using Agriloco.Api.Services;
using System.IO.Compression;
using System.Net;

var root = Path.Combine(AppContext.BaseDirectory, "fixtures");
Directory.CreateDirectory(Path.Combine(root, "Unity", "Build"));
var payload = System.Text.Encoding.UTF8.GetBytes("sample Unity content");
foreach (var extension in new[] { "js", "wasm", "data" })
foreach (var encoding in new[] { "br", "gz" })
{
    using var file = File.Create(Path.Combine(root, "Unity", "Build", $"test.{extension}.{encoding}"));
    using Stream compressor = encoding == "br" ? new BrotliStream(file, CompressionMode.Compress) : new GZipStream(file, CompressionMode.Compress);
    compressor.Write(payload);
}
File.WriteAllText(Path.Combine(root, "index.html"), "hello");
File.WriteAllBytes(Path.Combine(root, "private.data.br"), payload);
File.WriteAllBytes(Path.Combine(root, "Unity", "Build", "private.config.br"), payload);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = root });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
await using var app = builder.Build();
app.UseStaticFiles(UnityWebGlStaticFiles.CreateOptions());
await app.StartAsync();
try
{
    using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.None });
    client.BaseAddress = new Uri(app.Urls.Single());
    foreach (var extension in new[] { "js", "wasm", "data" })
    foreach (var encoding in new[] { "br", "gz" })
    {
        using var response = await client.GetAsync($"/Unity/Build/test.{extension}.{encoding}");
        response.EnsureSuccessStatusCode();
        var expectedType = extension switch { "js" => "application/javascript", "wasm" => "application/wasm", _ => "application/octet-stream" };
        if (response.Content.Headers.ContentType?.MediaType != expectedType ||
            !response.Content.Headers.ContentEncoding.Contains(encoding == "br" ? "br" : "gzip"))
            throw new Exception("Incorrect headers: " + extension + "." + encoding);
        using var body = await response.Content.ReadAsStreamAsync();
        using Stream decompressor = encoding == "br" ? new BrotliStream(body, CompressionMode.Decompress) : new GZipStream(body, CompressionMode.Decompress);
        using var decoded = new MemoryStream();
        await decompressor.CopyToAsync(decoded);
        if (!decoded.ToArray().SequenceEqual(payload)) throw new Exception("Incorrect decoded bytes");
        Console.WriteLine("PASS " + extension + "." + encoding + " headers and decompression");
    }
    using var html = await client.GetAsync("/index.html");
    if (await html.Content.ReadAsStringAsync() != "hello" || html.Content.Headers.ContentEncoding.Any()) throw new Exception("Ordinary files changed");
    foreach (var path in new[] { "/private.data.br", "/Unity/Build/private.config.br", "/Unity/Build/missing.wasm.br" })
        if ((await client.GetAsync(path)).StatusCode != HttpStatusCode.NotFound) throw new Exception("Unexpected file exposure: " + path);
    Console.WriteLine("PASS ordinary files and unknown/missing file restrictions");
}
finally { await app.StopAsync(); }
