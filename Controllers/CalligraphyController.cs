using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalligraphyController : ControllerBase
    {
        private const string CacheDir = "/mnt/books/calligraphy-cache";
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        // Simple concurrency guard: prevent duplicate Gemini calls for same bookId
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        private readonly string _apiKey;
        private readonly string _model;

        public CalligraphyController(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey missing");
            _model = config["Gemini:Model"] ?? "gemini-2.0-flash-exp-image-generation";

            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);
        }

        [HttpGet("generate")]
        public async Task<IActionResult> Generate(
            [FromQuery] int bookId,
            [FromQuery] string title,
            [FromQuery] string? language = null)
        {
            if (string.IsNullOrEmpty(title))
                return BadRequest(new { message = "title required" });

            var cachePath = Path.Combine(CacheDir, $"{bookId}.png");

            // 1) Return cached image if exists
            if (System.IO.File.Exists(cachePath))
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(cachePath);
                return File(bytes, "image/png");
            }

            // 2) Concurrency: only one call per bookId
            var sem = _locks.GetOrAdd(bookId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (System.IO.File.Exists(cachePath))
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(cachePath);
                    return File(bytes, "image/png");
                }

                // 3) Detect language
                var isArabic = !string.IsNullOrEmpty(language)
                    ? language.StartsWith("ar") || language.StartsWith("ur")
                    : System.Text.RegularExpressions.Regex.IsMatch(title, @"[\u0600-\u06FF]");

                // 4) Build the Gemini prompt
                var prompt = BuildPrompt(title, isArabic);

                // 5) Call Gemini image generation API
                var imageBytes = await CallGeminiForImageAsync(prompt);

                if (imageBytes == null || imageBytes.Length == 0)
                    return StatusCode(500, new { message = "Gemini returned no image" });

                // 6) Cache to disk
                await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);

                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Calligraphy generation failed: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
            finally
            {
                sem.Release();
                _locks.TryRemove(bookId, out _);
            }
        }

        // ============================================
        // Build the AI prompt
        // ============================================
        private string BuildPrompt(string title, bool isArabic)
        {
            if (isArabic)
            {
                return $@"Generate a highly ornate, dramatic Thuluth or Diwani style Arabic calligraphy artwork 
containing the text: ""{title}""

Style requirements:
- Traditional Islamic manuscript calligraphy
- Dramatic flowing curves with thick and thin stroke contrast
- Deep black ink on aged cream/parchment paper
- Elegant gold or red ornamental flourishes around the text
- Wide composition suitable for a book cover
- Rich, high-contrast, painting-like quality
- No English text, no Latin letters
- Vertical orientation, aspect ratio 2:3
- Background: warm cream paper texture with subtle golden decoration
- Text must be in correct Arabic script, right-to-left";
            }
            else
            {
                return $@"Generate a highly ornate, dramatic calligraphy artwork containing the text: ""{title}""

Style requirements:
- Ornate medieval manuscript calligraphy in the style of a medieval illuminated book cover
- Elaborate flourishing letters with dramatic thick and thin stroke contrast
- Deep black ink with gold leaf accents
- Elegant decorative flourishes and vines around the text
- Rich, high-contrast, painting-like quality
- Vertical orientation, aspect ratio 2:3
- Background: aged parchment or cream paper with subtle texture
- No Arabic text, all letters must be Latin script
- Suitable as a book cover title";
            }
        }

        // ============================================
        // Gemini API call (image generation)
        // ============================================
        private async Task<byte[]?> CallGeminiForImageAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "TEXT", "IMAGE" }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Gemini error {response.StatusCode}: {responseJson}");
                return null;
            }

            // Parse response — image is base64 inline
            using var doc = JsonDocument.Parse(responseJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return null;

            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData))
                {
                    var base64 = inlineData.GetProperty("data").GetString();
                    if (!string.IsNullOrEmpty(base64))
                        return Convert.FromBase64String(base64);
                }
            }

            return null;
        }
    }
}