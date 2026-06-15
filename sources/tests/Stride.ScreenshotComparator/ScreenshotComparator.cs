// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Stride.Tests.ScreenshotComparator;

/// <summary>
/// Pixel-perceptual screenshot comparator. Reads new captures from <paramref name="newDir"/> and
/// matching baselines from <paramref name="baselineDir"/>, scores each pair with LPIPS-AlexNet
/// (via ONNX Runtime), and returns one <see cref="ComparisonResult"/> per <c>(sample, frame)</c>
/// pair with the distance and a coarse status (<c>ok</c> / <c>drift</c> / <c>new</c> / <c>missing</c>).
///
/// Layout assumed:
///   newDir       &lt;dir&gt;/&lt;sample&gt;/screenshots/&lt;frame&gt;.png
///   baselineDir  &lt;dir&gt;/&lt;sample&gt;/&lt;frame&gt;.png
/// </summary>
public static class ScreenshotComparator
{
    public const float DefaultThreshold = 0.05f;

    /// <summary>
    /// LPIPS canonical eval resolution. Resizing to a square here also dodges a NaN-on-tall-aspect
    /// issue with the ONNX-exported AlexNet at native portrait sizes (640×1136 → NaN; 256×256 → 0)
    /// — the L2-normalize step loses numerical safety through the dynamo export.
    /// </summary>
    private const int InputSize = 256;

    /// <summary>
    /// Compare every <paramref name="newDir"/>/&lt;sample&gt;/screenshots/&lt;frame&gt;.png against the matching
    /// baseline. <paramref name="sampleFilter"/> restricts to one sample by name. Per-frame thresholds
    /// emitted by the harness in done.json win over <paramref name="defaultThreshold"/>. Pass
    /// <paramref name="modelPath"/> to override where lpips_alex.onnx is read from (default looks in
    /// the executing assembly's <c>models/</c> sibling — works when the file is CopyToOutputDirectory'd
    /// into the consumer's bin).
    /// </summary>
    public static List<ComparisonResult> Compare(string newDir, string baselineDir, string? sampleFilter = null, float defaultThreshold = DefaultThreshold, string? modelPath = null, ComparisonPrompt? defaultPrompt = null)
    {
        defaultPrompt ??= GameplayComparisonPrompt.Default;
        modelPath ??= Path.Combine(AppContext.BaseDirectory, "models", "lpips_alex.onnx");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"LPIPS model not found at {modelPath}", modelPath);

        using var session = new InferenceSession(modelPath, new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL });

        var results = new List<ComparisonResult>();
        if (!Directory.Exists(newDir))
            throw new DirectoryNotFoundException($"--new dir not found: {newDir}");

        foreach (var sampleDir in Directory.EnumerateDirectories(newDir))
        {
            var sample = Path.GetFileName(sampleDir);
            if (sampleFilter is not null && !string.Equals(sample, sampleFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            var screenshotsDir = Path.Combine(sampleDir, "screenshots");
            if (!Directory.Exists(screenshotsDir))
                continue;

            // Per-frame metadata emitted by the harness in done.json (threshold + optional Claude
            // fallback). Falls back to defaultThreshold for samples whose harness predates the schema.
            var perFrameMetadata = LoadPerFrameMetadata(Path.Combine(sampleDir, "done.json"));

            foreach (var newPng in Directory.EnumerateFiles(screenshotsDir, "*.png"))
            {
                var frame = Path.GetFileNameWithoutExtension(newPng);
                var baselines = FindBaselineVariants(Path.Combine(baselineDir, sample), frame);
                perFrameMetadata.TryGetValue(frame, out var meta);
                var frameThreshold = meta.Threshold ?? defaultThreshold;

                if (baselines.Count == 0)
                {
                    results.Add(new ComparisonResult(sample, frame, null, frameThreshold, "new", "no baseline yet"));
                    continue;
                }

                float distance;
                try
                {
                    // Multi-baseline: take the closest match — captures only need to align with ONE
                    // of the curated acceptable variants for LPIPS to pass.
                    distance = baselines.Min(b => ComputeLpips(session, b, newPng));
                }
                catch (Exception ex)
                {
                    results.Add(new ComparisonResult(sample, frame, null, frameThreshold, "error", ex.Message));
                    continue;
                }

                if (distance < frameThreshold)
                {
                    results.Add(new ComparisonResult(sample, frame, distance, frameThreshold, "ok", null));
                    continue;
                }

                if (meta.ClaudeFallbackEnabled)
                {
                    var prompt = defaultPrompt with { ExtraHint = meta.ClaudeFallbackHint };
                    var verdict = ClaudeVisionFallback.Compare(baselines, newPng, prompt);
                    var detail = $"lpips drift (vs {baselines.Count} baseline(s)); claude: {verdict.Reason}";
                    results.Add(new ComparisonResult(sample, frame, distance, frameThreshold,
                        verdict.Pass ? "ok-via-claude" : "drift", detail));
                    continue;
                }

                results.Add(new ComparisonResult(sample, frame, distance, frameThreshold, "drift", null));
            }
        }

        // Walk baselines that have no matching new capture (missing — capture probably failed).
        // Variant files like "main.dark-scene.png" share the same frame name "main", so collapse
        // them to a set of unique frame names before checking the capture dir.
        if (Directory.Exists(baselineDir))
        {
            foreach (var sampleDir in Directory.EnumerateDirectories(baselineDir))
            {
                var sample = Path.GetFileName(sampleDir);
                if (sampleFilter is not null && !string.Equals(sample, sampleFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                var frames = Directory.EnumerateFiles(sampleDir, "*.png")
                    .Select(p => Path.GetFileNameWithoutExtension(p).Split('.')[0])
                    .Distinct(StringComparer.Ordinal);
                foreach (var frame in frames)
                {
                    var newPng = Path.Combine(newDir, sample, "screenshots", frame + ".png");
                    if (File.Exists(newPng))
                        continue;
                    results.Add(new ComparisonResult(sample, frame, null, defaultThreshold, "missing", "no capture for this baseline"));
                }
            }
        }

        results.Sort((a, b) =>
        {
            var c = string.Compare(a.Sample, b.Sample, StringComparison.Ordinal);
            return c != 0 ? c : string.Compare(a.Frame, b.Frame, StringComparison.Ordinal);
        });
        return results;
    }

    /// <summary>
    /// Returns the set of acceptable baselines for <paramref name="frame"/> in <paramref name="sampleDir"/>:
    /// the canonical <c>frame.png</c> plus any variants named <c>frame.&lt;tag&gt;.png</c>. Empty list if
    /// the sample directory or no matching files exist.
    /// </summary>
    private static List<string> FindBaselineVariants(string sampleDir, string frame)
    {
        var list = new List<string>();
        if (!Directory.Exists(sampleDir)) return list;
        var canonical = Path.Combine(sampleDir, frame + ".png");
        if (File.Exists(canonical)) list.Add(canonical);
        // <frame>.<anything>.png — Win32 glob treats the last "." segment as the extension.
        foreach (var f in Directory.EnumerateFiles(sampleDir, frame + ".*.png"))
            list.Add(f);
        return list;
    }

    private static float ComputeLpips(InferenceSession session, string pathA, string pathB)
    {
        using var imgA = Image.Load<Rgb24>(pathA);
        using var imgB = Image.Load<Rgb24>(pathB);
        imgA.Mutate(c => c.Resize(InputSize, InputSize));
        imgB.Mutate(c => c.Resize(InputSize, InputSize));

        var tensorA = ToTensor(imgA);
        var tensorB = ToTensor(imgB);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("a", tensorA),
            NamedOnnxValue.CreateFromTensor("b", tensorB),
        };

        using var results = session.Run(inputs);
        var output = (DenseTensor<float>)results[0].Value;
        return output[0];
    }

    /// <summary>PNG bytes → NCHW float32 in [-1, 1], matching the preprocessing the lpips Python lib does.</summary>
    private static DenseTensor<float> ToTensor(Image<Rgb24> img)
    {
        int w = img.Width, h = img.Height;
        var data = new float[3 * h * w];
        img.ProcessPixelRows(rows =>
        {
            for (int y = 0; y < h; y++)
            {
                var row = rows.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    var p = row[x];
                    var idx = y * w + x;
                    data[0 * h * w + idx] = p.R / 127.5f - 1f;
                    data[1 * h * w + idx] = p.G / 127.5f - 1f;
                    data[2 * h * w + idx] = p.B / 127.5f - 1f;
                }
            }
        });
        return new DenseTensor<float>(data, new[] { 1, 3, h, w });
    }

    /// <summary>
    /// Reads <c>done.json</c> for one sample and returns a frame-name → threshold map. The harness
    /// emits <c>screenshots: [{name, threshold}]</c>; older runs may have <c>screenshots: [string]</c>
    /// in which case we return an empty map and let the caller fall back to the default threshold.
    /// </summary>
    private static Dictionary<string, FrameMetadata> LoadPerFrameMetadata(string donePath)
    {
        var result = new Dictionary<string, FrameMetadata>(StringComparer.Ordinal);
        if (!File.Exists(donePath))
            return result;
        try
        {
            using var stream = File.OpenRead(donePath);
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("screenshots", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var entry in arr.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object) continue;
                if (!entry.TryGetProperty("Name", out var nameEl) && !entry.TryGetProperty("name", out nameEl)) continue;
                var name = nameEl.GetString();
                if (name is null) continue;
                float? threshold = null;
                if (entry.TryGetProperty("Threshold", out var thrEl) || entry.TryGetProperty("threshold", out thrEl))
                {
                    if (thrEl.ValueKind == JsonValueKind.Number)
                        threshold = thrEl.GetSingle();
                }
                // ClaudeFallback: null absent / true (generic prompt) / string (extra guidance).
                bool fallbackEnabled = false;
                string? fallbackHint = null;
                if (entry.TryGetProperty("ClaudeFallback", out var fbEl) || entry.TryGetProperty("claudeFallback", out fbEl))
                {
                    if (fbEl.ValueKind == JsonValueKind.True) fallbackEnabled = true;
                    else if (fbEl.ValueKind == JsonValueKind.String) { fallbackEnabled = true; fallbackHint = fbEl.GetString(); }
                }
                result[name] = new FrameMetadata(threshold, fallbackEnabled, fallbackHint);
            }
        }
        catch
        {
            // Malformed done.json — fall back to default threshold for everything in this sample.
        }
        return result;
    }

    private readonly record struct FrameMetadata(float? Threshold, bool ClaudeFallbackEnabled, string? ClaudeFallbackHint);
}

/// <summary>One row per (sample, frame) compared. <see cref="Lpips"/> is null for status="new" / "missing" / "error".</summary>
public sealed record ComparisonResult(string Sample, string Frame, float? Lpips, float Threshold, string Status, string? Detail);
