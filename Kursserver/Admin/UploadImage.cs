using Kursserver.Extracts;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace Kursserver.Admin
{
    [Authorize]
    public static class UploadImage
    { 
        public static void UploadImageEndpoints(this WebApplication app, string connectionString)
        {
            app.MapPost("api/upload-image", async (HttpContext context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<ExtractImage>();
                if (request == null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request payload." });
                    return;
                }
                try
                {
                    // Step 1: Parse the base64 string (format: "data:image/png;base64,actualBase64Data")

                    var base64data = request.Image.Split(',')[1];
                    var mimeType = request.Image.Split(',')[0].Split(':')[1].Split(';')[0];

                    // Step 2: Decode base64 to byte array
                    byte[] imageBytes = Convert.FromBase64String(base64data);

                    // Step 3: Determine file extension from MIME type
                    string extension = GetExtensionFromMimeType(mimeType);
                    if (extension == null)
                    {
                        context.Response.StatusCode = 400; // Bad Request
                        await context.Response.WriteAsJsonAsync(new { error = "Unsupported image format." });
                        return;
                    }

                    // Step 4: Generate a unique filename
                    string fileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine("wwwroot", "images", fileName);
                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Step 5: Save the file
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                    // NEW: Return success response with URL
                    string imageUrl = $"{context.Request.Scheme}://{context.Request.Host}/images/{fileName}";
                    Debug.WriteLine(imageUrl);
                    await context.Response.WriteAsJsonAsync(new { url = imageUrl });

                }
                catch (Exception ex)
                {
                    // Implementation for image upload goes here
                    context.Response.StatusCode = 501; // Not Implemented
                    await context.Response.WriteAsJsonAsync(new { error = "Image upload not implemented yet." });
                }
            });
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                _ => null
            };
        }
    }
}
