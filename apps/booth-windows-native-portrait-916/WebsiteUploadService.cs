using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Photobooth.BoothNative;

public sealed class WebsiteUploadResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Visibility { get; set; } = "public";
    public string LayoutFormat { get; set; } = "original";
    public string? AccessUrl { get; set; }
    public bool RequiresPassword { get; set; }
    public string? PrivatePassword { get; set; }
}

public sealed class WebsiteUploadService
{
    private readonly HttpClient _httpClient;

    public WebsiteUploadService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(40)
        };
    }

    public async Task<WebsiteUploadResult> UploadAsync(
        string endpointBaseUrl,
        string imagePath,
        string code,
        string eventName,
        string visibility,
        string layoutFormat,
        string? privatePassword)
    {
        if (string.IsNullOrWhiteSpace(endpointBaseUrl))
        {
            return new WebsiteUploadResult
            {
                Ok = false,
                Message = "Upload endpoint is empty."
            };
        }

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return new WebsiteUploadResult
            {
                Ok = false,
                Message = "Selected image does not exist."
            };
        }

        var requestUrl = endpointBaseUrl.TrimEnd('/') + "/api/share/upload";
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(code.Trim()), "code");
        if (!string.IsNullOrWhiteSpace(eventName))
        {
            form.Add(new StringContent(eventName.Trim()), "eventName");
        }

        form.Add(new StringContent(string.Equals(visibility, "private", StringComparison.OrdinalIgnoreCase) ? "private" : "public"), "visibility");
        form.Add(new StringContent(string.IsNullOrWhiteSpace(layoutFormat) ? "original" : layoutFormat.Trim()), "layoutFormat");

        if (!string.IsNullOrWhiteSpace(privatePassword))
        {
            form.Add(new StringContent(privatePassword.Trim()), "privatePassword");
        }

        var fileBytes = await File.ReadAllBytesAsync(imagePath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(imagePath));
        form.Add(fileContent, "photos", Path.GetFileName(imagePath));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(requestUrl, form);
        }
        catch (Exception ex)
        {
            return new WebsiteUploadResult
            {
                Ok = false,
                Message = $"Upload request failed: {ex.Message}"
            };
        }

        string payloadText;
        try
        {
            payloadText = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return new WebsiteUploadResult
            {
                Ok = false,
                Message = $"Upload response read failed: {ex.Message}"
            };
        }

        try
        {
            using var document = JsonDocument.Parse(payloadText);
            var root = document.RootElement;
            var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
            var result = new WebsiteUploadResult
            {
                Ok = ok && response.IsSuccessStatusCode,
                Message = TryGetString(root, "message") ?? (ok ? "Upload completed." : $"Upload failed ({(int)response.StatusCode})."),
                Code = TryGetString(root, "code") ?? code,
                Visibility = TryGetString(root, "visibility") ?? "public",
                LayoutFormat = TryGetString(root, "layoutFormat") ?? layoutFormat,
                AccessUrl = TryGetString(root, "accessUrl"),
                RequiresPassword = root.TryGetProperty("requiresPassword", out var passwordRequiredEl) && passwordRequiredEl.ValueKind == JsonValueKind.True,
                PrivatePassword = TryGetString(root, "privatePassword")
            };
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message))
            {
                result.Message = $"Upload failed ({(int)response.StatusCode}).";
            }

            return result;
        }
        catch
        {
            return new WebsiteUploadResult
            {
                Ok = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode
                    ? "Upload completed, but response parsing failed."
                    : $"Upload failed ({(int)response.StatusCode}).",
                Code = code,
                Visibility = visibility,
                LayoutFormat = layoutFormat
            };
        }
    }

    private static string GetMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
