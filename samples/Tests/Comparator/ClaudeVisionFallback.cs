// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Stride.SampleScreenshotComparator;

/// <summary>
/// Calls Claude Haiku 4.5 vision with the baseline + capture and asks "is this the same scene?".
/// Used as a second-opinion fallback when LPIPS is over threshold but the test opted into
/// <c>claudeFallback</c>. ANTHROPIC_API_KEY env var is required; if missing, the fallback fails
/// closed (returns Pass=false) so the regression sticks.
/// </summary>
public static class ClaudeVisionFallback
{
    private const string Model = "claude-haiku-4-5";
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public readonly record struct Verdict(bool Pass, string Reason);

    public static Verdict Compare(string baselinePath, string capturePath, string? extraHint)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            return new Verdict(false, "ANTHROPIC_API_KEY not set");

        var baselineB64 = Convert.ToBase64String(File.ReadAllBytes(baselinePath));
        var captureB64 = Convert.ToBase64String(File.ReadAllBytes(capturePath));

        var prompt = "Compare these two game screenshots — first is the BASELINE (expected), " +
                     "second is the CAPTURE (this run). Reply YES if they show the same UI state " +
                     "and same visible content (same text, buttons, characters, scene layout). " +
                     "Treat noise (particle positions, animation cycle phase, lighting flicker) " +
                     "as acceptable. Reply NO if there is a meaningful regression (different UI " +
                     "page, missing element, wrong text, different scene). " +
                     "Format: \"YES: <one-line reason>\" or \"NO: <one-line reason>\".";
        if (!string.IsNullOrEmpty(extraHint))
            prompt += " Additional context for this specific frame: " + extraHint;

        var body = JsonSerializer.Serialize(new
        {
            model = Model,
            max_tokens = 80,
            temperature = 0.0,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "BASELINE:" },
                        new { type = "image", source = new { type = "base64", media_type = "image/png", data = baselineB64 } },
                        new { type = "text", text = "CAPTURE:" },
                        new { type = "image", source = new { type = "base64", media_type = "image/png", data = captureB64 } },
                        new { type = "text", text = prompt },
                    },
                },
            },
        });

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", ApiVersion);

            using var resp = http.Send(req);
            var respBody = resp.Content.ReadAsStringAsync().Result;
            if (!resp.IsSuccessStatusCode)
                return new Verdict(false, $"claude api {(int)resp.StatusCode}: {Truncate(respBody, 200)}");

            using var doc = JsonDocument.Parse(respBody);
            // Response shape: { content: [{ type: "text", text: "YES: ..." | "NO: ..." }] }
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
            text = text.Trim();
            // Accept "YES" or "NO" prefix (case-insensitive).
            var pass = text.StartsWith("YES", StringComparison.OrdinalIgnoreCase);
            return new Verdict(pass, text);
        }
        catch (Exception ex)
        {
            return new Verdict(false, $"claude error: {ex.Message}");
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
}
