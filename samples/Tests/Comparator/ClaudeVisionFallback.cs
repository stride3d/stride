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

        var prompt =
            "Compare these two Stride engine screenshots — BASELINE (expected) vs CAPTURE " +
            "(this run). Both were produced by the same engine code; visible differences are " +
            "almost always caused by test-harness timing nondeterminism (variable frame pacing " +
            "across graphics APIs), NOT by a rendering regression. A rendering regression looks " +
            "broken, not just 'different'. Be tolerant.\n\n" +
            "YES (not a regression):\n" +
            "- HUD numeric values differ (ammo, score, timer, FPS, health). This is gameplay state.\n" +
            "- Character / weapon / camera pose, aim angle, or hand-bob phase differs. Animation phase.\n" +
            "- Particle / smoke / fire / cloth / water / lighting / post-process noise differs.\n" +
            "- Same overall scene with one element in a slightly different position or animation state.\n" +
            "\n" +
            "NO (real regression):\n" +
            "- Whole-frame color / gamma / brightness shift (capture noticeably darker, desaturated, " +
            "washed-out, or with wrong sRGB encoding).\n" +
            "- Missing or corrupt geometry (broken meshes, distorted models, holes).\n" +
            "- Missing or wrong textures (pink/purple checkerboard, all-black surfaces, wrong materials).\n" +
            "- Missing post-process pass (no bloom / shadow / SSAO / tonemapping where baseline has them).\n" +
            "- Different UI page, missing UI elements, wrong UI text labels (label text, not numeric values).\n" +
            "- Wrong scene entirely (different level, different camera angle by 90°+, missing major objects).\n" +
            "\n" +
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
