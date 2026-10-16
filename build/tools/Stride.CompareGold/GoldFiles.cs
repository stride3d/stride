// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Stride.Graphics.Regression;

// Gold file operations shared by the headless promote/dedup and the web UI, so both treat variants
// (<name>.variantN.png, see GoldVariant) the same way.
internal static class GoldFiles
{
    // Where a render that is added as a variant to a bucket with golds goes: over the variant made on the same
    // CPU, else to a new variant (a gold with no CPU recorded counts as another CPU's).
    public static string VariantTarget(string renderPng, string name, List<string> bucketGolds)
    {
        if (ReadCpu(renderPng) is { } cpu && bucketGolds.FirstOrDefault(g => ReadCpu(g) is { } goldCpu && SameCpu(cpu, goldCpu)) is { } sameCpu)
            return sameCpu;
        var next = bucketGolds.Max(g => GoldVariant.Index(Path.GetFileName(g))) + 1;
        return Path.Combine(Path.GetDirectoryName(bucketGolds[0])!, GoldVariant.FileName(name, next));
    }

    // Same model, and the same instruction sets when both recorded them (one model can expose different ones,
    // e.g. under a hypervisor; golds made before cpuFeatures existed match on the model).
    private static bool SameCpu((string Model, string? Features) a, (string Model, string? Features) b) =>
        a.Model == b.Model && (a.Features is null || b.Features is null || a.Features == b.Features);

    // The CPU from the .metadata.json next to an image: its model and, when recorded, its instruction sets. Null
    // when unknown, including when the model is only the architecture name the runtime falls back to (X64,
    // Arm64…), which many hosts share.
    public static (string Model, string? Features)? ReadCpu(string pngPath)
    {
        var meta = Path.ChangeExtension(pngPath, ".metadata.json");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(meta));
            if (!doc.RootElement.TryGetProperty("cpu", out var cpu) || cpu.GetString() is not { Length: > 0 } model
                || Enum.TryParse<System.Runtime.InteropServices.Architecture>(model, ignoreCase: true, out _))
                return null;
            return (model, doc.RootElement.TryGetProperty("cpuFeatures", out var features) ? features.GetString() : null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException) { return null; }
    }

    // Deletes golds with their .metadata.json.
    public static void Remove(IEnumerable<string> paths)
    {
        foreach (var path in paths.ToList())
        {
            if (File.Exists(path)) File.Delete(path);
            var meta = Path.ChangeExtension(path, ".metadata.json");
            if (File.Exists(meta)) File.Delete(meta);
        }
    }
}
