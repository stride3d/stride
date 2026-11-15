// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Stride.Tests.ScreenshotComparator;

/// <summary>
/// Calls Claude Sonnet vision with the baseline(s) + capture and asks "is this the same scene?".
/// Used as a second-opinion fallback when LPIPS is over threshold but the test opted into
/// <c>claudeFallback</c>. When more than one baseline is provided they're framed as the
/// acceptable variance range for the frame. An API key (see <see cref="ApiKey"/>) is required; if missing,
/// the fallback fails closed (returns Pass=false) so the regression sticks.
/// </summary>
public static class ClaudeVisionFallback
{
    private const string Model = "claude-sonnet-5-5";
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static readonly Regex VerdictLine = new(@"^(YES|NO)\s*:", RegexOptions.IgnoreCase);

    public readonly record struct Verdict(bool Pass, string Reason);

    /// <summary>The environment variables <see cref="ApiKey"/> reads, for messages.</summary>
    public const string ApiKeyVariables = "STRIDE_ANTHROPIC_API_KEY or ANTHROPIC_API_KEY";

    /// <summary>
    /// The API key: STRIDE_ANTHROPIC_API_KEY, else ANTHROPIC_API_KEY. The Stride-specific name lets a
    /// developer keep the key out of ANTHROPIC_API_KEY, which other tools (Claude Code among them) pick
    /// up and bill; CI sets the standard name.
    /// </summary>
    public static string? ApiKey
    {
        get
        {
            var key = Environment.GetEnvironmentVariable("STRIDE_ANTHROPIC_API_KEY");
            return string.IsNullOrEmpty(key) ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") : key;
        }
    }

    /// <summary>Single-baseline overload — back-compat shim around the multi-baseline form.</summary>
    public static Verdict Compare(string baselinePath, string capturePath, ComparisonPrompt prompt)
        => Compare(new[] { baselinePath }, capturePath, prompt);

    public static Verdict Compare(IReadOnlyList<string> baselinePaths, string capturePath, ComparisonPrompt prompt)
    {
        var apiKey = ApiKey;
        if (string.IsNullOrEmpty(apiKey))
            return new Verdict(false, $"{ApiKeyVariables} not set");
        if (baselinePaths.Count == 0)
            return new Verdict(false, "no baselines provided");

        var captureB64 = Convert.ToBase64String(File.ReadAllBytes(capturePath));
        var promptText = prompt.Build(baselinePaths.Count);

        var content = new List<object>();
        for (var i = 0; i < baselinePaths.Count; i++)
        {
            var label = baselinePaths.Count == 1 ? "BASELINE:" : $"BASELINE {i + 1} of {baselinePaths.Count}:";
            var b64 = Convert.ToBase64String(File.ReadAllBytes(baselinePaths[i]));
            content.Add(new { type = "text", text = label });
            content.Add(new { type = "image", source = new { type = "base64", media_type = "image/png", data = b64 } });
        }
        content.Add(new { type = "text", text = "CAPTURE:" });
        content.Add(new { type = "image", source = new { type = "base64", media_type = "image/png", data = captureB64 } });
        content.Add(new { type = "text", text = promptText });

        var body = JsonSerializer.Serialize(new
        {
            model = Model,
            // Room for the model's thinking (counted here) and a few lines of reasoning before the verdict
            max_tokens = 4000,
            messages = new[]
            {
                new { role = "user", content = content.ToArray() },
            },
        });

        // Retry transient failures (429/529/5xx, network) so brief Anthropic overload doesn't
        // surface as a fake "drift" regression. Backoff 2s, 4s between attempts. Non-transient
        // statuses (400/401/403) fail fast — retrying won't help.
        const int maxAttempts = 3;
        string lastTransient = "";
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
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

                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(respBody);
                    // Response shape: { content: [..., { type: "text", text: "<reasoning lines>\nYES: ..." | "...\nNO: ..." }] },
                    // other block types (e.g. thinking) are skipped
                    var text = string.Join("\n", doc.RootElement.GetProperty("content").EnumerateArray()
                        .Where(block => block.GetProperty("type").GetString() == "text")
                        .Select(block => block.GetProperty("text").GetString()));
                    // The verdict is the last line starting with "YES:" or "NO:" (markdown emphasis stripped), so a
                    // reasoning line such as "No change in ..." isn't one; without a verdict, the comparison fails.
                    var verdict = text.Split('\n')
                        .Select(line => line.Trim().Replace("**", "").TrimStart('#', ' '))
                        .LastOrDefault(line => VerdictLine.IsMatch(line));
                    if (verdict == null)
                        return new Verdict(false, $"no YES/NO verdict line (stop reason {doc.RootElement.GetProperty("stop_reason")}): {Truncate(text.Trim(), 300)}");
                    var pass = verdict.StartsWith("YES", StringComparison.OrdinalIgnoreCase);
                    return new Verdict(pass, verdict);
                }

                var code = (int)resp.StatusCode;
                var transient = code == 429 || code == 529 || (code >= 500 && code < 600);
                if (!transient)
                    return new Verdict(false, $"claude api error (status {code}): {Truncate(respBody, 200)}");

                lastTransient = $"status {code}: {Truncate(respBody, 150)}";
                if (attempt == maxAttempts)
                    return new Verdict(false, $"claude api unavailable after {maxAttempts} attempts (transient — likely Anthropic overload, retry the run): {lastTransient}");
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                lastTransient = $"network: {ex.Message}";
                if (attempt == maxAttempts)
                    return new Verdict(false, $"claude api unavailable after {maxAttempts} attempts (transient — retry the run): {lastTransient}");
            }
            catch (Exception ex)
            {
                return new Verdict(false, $"claude error: {ex.Message}");
            }

            // Exponential backoff: 2s after attempt 1, 4s after attempt 2.
            Thread.Sleep(TimeSpan.FromSeconds(1 << attempt));
        }

        // Unreachable — every loop path returns or backs off.
        return new Verdict(false, $"claude api unavailable: {lastTransient}");
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
}
