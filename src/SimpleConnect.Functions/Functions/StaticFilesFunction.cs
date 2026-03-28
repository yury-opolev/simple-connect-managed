using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SimpleConnect.Functions.Functions;

public class StaticFilesFunction
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "application/javascript",
        [".json"] = "application/json",
        [".wasm"] = "application/wasm",
        [".dll"] = "application/octet-stream",
        [".dat"] = "application/octet-stream",
        [".blat"] = "application/octet-stream",
        [".pdb"] = "application/octet-stream",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".ico"] = "image/x-icon",
        [".gif"] = "image/gif",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".map"] = "application/json"
    };

    [Function("StaticFiles")]
    public async Task<HttpResponseData> Serve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*path}")] HttpRequestData req,
        string? path)
    {
        // Default to index.html for root and SPA routes
        path = string.IsNullOrEmpty(path) ? "index.html" : path;

        // Find the wwwroot directory relative to the function app
        var basePath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var filePath = Path.GetFullPath(Path.Combine(basePath, path));

        // Prevent directory traversal
        if (!filePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return req.CreateResponse(HttpStatusCode.Forbidden);

        // If file doesn't exist, serve index.html for SPA routing (Blazor)
        if (!File.Exists(filePath))
        {
            var indexPath = Path.Combine(basePath, "index.html");
            if (File.Exists(indexPath))
                filePath = indexPath;
            else
                return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var extension = Path.GetExtension(filePath);
        var contentType = MimeTypes.GetValueOrDefault(extension, "application/octet-stream");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", contentType);

        // Cache strategy:
        // - HTML: always revalidate (SPA entry point)
        // - _framework/: no-cache (changes between deployments, same URLs; Blazor has its own Cache API)
        // - Everything else (css/, js/, fonts/): immutable (truly static assets)
        if (extension == ".html")
            response.Headers.Add("Cache-Control", "no-cache");
        else if (filePath.Contains($"{Path.DirectorySeparatorChar}_framework{Path.DirectorySeparatorChar}") ||
                 filePath.Contains("/_framework/"))
            response.Headers.Add("Cache-Control", "no-cache");
        else
            response.Headers.Add("Cache-Control", "public, max-age=31536000, immutable");

        var fileBytes = await File.ReadAllBytesAsync(filePath);
        await response.Body.WriteAsync(fileBytes);

        return response;
    }
}
