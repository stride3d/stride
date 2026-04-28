using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5555");
builder.Services.AddSingleton<SourceManager>();
var app = builder.Build();

// Find Stride root
var strideRoot = FindStrideRoot(AppContext.BaseDirectory)
    ?? FindStrideRoot(Directory.GetCurrentDirectory())
    ?? throw new InvalidOperationException("Could not find Stride root (looking for 'tests/' + 'sources/' directories)");

var testsDir = Path.Combine(strideRoot, "tests");
var localDir = Path.Combine(testsDir, "local");
var ciCacheDir = Path.Combine(Path.GetTempPath(), "stride-compare-gold");

var sourceManager = app.Services.GetRequiredService<SourceManager>();

Console.WriteLine($"Stride root: {strideRoot}");
Console.WriteLine($"Gold images: {testsDir}");
Console.WriteLine($"Local output: {localDir}");
Console.WriteLine($"CI cache: {ciCacheDir}");
Console.WriteLine();
Console.WriteLine("CompareGold running at http://localhost:5555");
Console.WriteLine("Press Ctrl+C to stop.");

try { Process.Start(new ProcessStartInfo("http://localhost:5555") { UseShellExecute = true }); }
catch { }

app.UseDefaultFiles();
app.UseStaticFiles();

// Disable caching for gold image responses (they change on promote)
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/gold") || ctx.Request.Path.StartsWithSegments("/api/thresholds"))
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers.CacheControl = "no-store";
            ctx.Response.Headers.Remove("ETag");
            ctx.Response.Headers.Remove("Last-Modified");
            return Task.CompletedTask;
        });
    }
    await next();
});

// Check gh CLI availability once at startup
bool ghAvailable = false;
string ghError = "gh CLI not found. Install from https://cli.github.com/";
try
{
    var ghCheck = Process.Start(new ProcessStartInfo { FileName = "gh", Arguments = "auth status", RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false });
    if (ghCheck != null)
    {
        ghCheck.WaitForExit(5000);
        ghAvailable = ghCheck.ExitCode == 0;
        if (!ghAvailable)
            ghError = "gh CLI not authenticated. Run: gh auth login";
    }
}
catch { }
Console.WriteLine(ghAvailable ? "GitHub CLI: authenticated" : $"GitHub CLI: {ghError}");

// === Info API ===

app.MapGet("/api/info", () => new { StrideRoot = strideRoot });

// === Gold API ===

app.MapGet("/api/suites", () =>
{
    var suites = new HashSet<string>();
    if (Directory.Exists(testsDir))
        foreach (var dir in Directory.GetDirectories(testsDir))
        {
            var name = Path.GetFileName(dir);
            if (name != "local") suites.Add(name);
        }
    // Also from sources
    foreach (var src in sourceManager.GetAll())
        if (Directory.Exists(src.Path))
            foreach (var dir in Directory.GetDirectories(src.Path))
                suites.Add(Path.GetFileName(dir));
    return suites.OrderBy(s => s);
});

app.MapGet("/api/platforms", (string suite) =>
{
    var platforms = new HashSet<string>();
    CollectPlatforms(Path.Combine(testsDir, suite), platforms);
    foreach (var src in sourceManager.GetAll())
        CollectPlatforms(Path.Combine(src.Path, suite), platforms);
    return platforms.OrderBy(p => p);
});

app.MapGet("/api/gold/images", (string suite, string platform) =>
{
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Platform format: Platform/Device");
    var dir = Path.Combine(testsDir, suite, parts[0], parts[1]);
    var primary = ListPngNames(dir);
    var primarySet = new HashSet<string>(primary);

    // Pick the best fallback gold for display. We mirror Graphics.Regression's
    // any-match semantics for pass/fail on the client — this score only decides
    // which gold the UI shows by default.
    var requestedApi = parts[0];
    var requestedDevice = parts[1];
    var requestedIsSw = IsSoftwareRenderer(requestedDevice);
    var fallbackBest = new Dictionary<string, (string platform, int score)>();
    var suiteDir = Path.Combine(testsDir, suite);
    if (Directory.Exists(suiteDir))
    {
        foreach (var pDir in Directory.GetDirectories(suiteDir))
        {
            var pName = Path.GetFileName(pDir);
            if (pName == "local") continue;
            foreach (var dDir in Directory.GetDirectories(pDir))
            {
                var device = Path.GetFileName(dDir);
                var fallbackPlatform = $"{pName}/{device}";
                if (fallbackPlatform == platform) continue;
                var candidateIsSw = IsSoftwareRenderer(device);
                var score = ScoreFallback(pName, device, candidateIsSw, requestedApi, requestedDevice, requestedIsSw);
                foreach (var f in Directory.GetFiles(dDir, "*.png"))
                {
                    var name = Path.GetFileName(f);
                    if (primarySet.Contains(name)) continue;
                    if (!fallbackBest.TryGetValue(name, out var existing) || score > existing.score)
                        fallbackBest[name] = (fallbackPlatform, score);
                }
            }
        }
    }
    var fallbacks = fallbackBest.Select(kv => (object)new { Name = kv.Key, FallbackPlatform = kv.Value.platform }).ToList();

    return Results.Ok(new
    {
        Images = primary.Select(n => new { Name = n, FallbackPlatform = (string?)null }),
        Fallbacks = fallbacks
    });
});

app.MapGet("/api/gold/image", (string suite, string platform, string name) =>
{
    // Try exact platform first, then fallback across all platforms
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var filePath = Path.Combine(testsDir, suite, parts[0], parts[1], name);
    if (File.Exists(filePath)) return Results.File(filePath, "image/png");

    // Fallback: search all platforms in this suite, preferring closest match
    var suiteDir = Path.Combine(testsDir, suite);
    if (Directory.Exists(suiteDir))
    {
        var requestedApi = parts[0];
        var requestedDevice = parts[1];
        var requestedIsSw = IsSoftwareRenderer(requestedDevice);
        string? bestPath = null;
        int bestScore = -1;
        foreach (var pDir in Directory.GetDirectories(suiteDir))
        {
            var pName = Path.GetFileName(pDir);
            if (pName == "local") continue;
            foreach (var dDir in Directory.GetDirectories(pDir))
            {
                var candidate = Path.Combine(dDir, name);
                if (!File.Exists(candidate)) continue;
                var device = Path.GetFileName(dDir);
                var candidateIsSw = IsSoftwareRenderer(device);
                var score = ScoreFallback(pName, device, candidateIsSw, requestedApi, requestedDevice, requestedIsSw);
                if (score > bestScore) { bestScore = score; bestPath = candidate; }
            }
        }
        if (bestPath != null) return Results.File(bestPath, "image/png");
    }
    return Results.NotFound();
});

// Thresholds
app.MapGet("/api/thresholds", (string suite) =>
{
    var path = Path.Combine(testsDir, suite, "thresholds.jsonc");
    if (!File.Exists(path)) return Results.Ok(Array.Empty<object>());
    var jsonc = File.ReadAllText(path);
    // Strip // comments
    var json = System.Text.RegularExpressions.Regex.Replace(jsonc, @"//.*?$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
    var rules = JsonSerializer.Deserialize<JsonElement>(json);
    return Results.Ok(rules);
});

// Also list all gold platforms that have a given image (for fallback info)
app.MapGet("/api/gold/all", (string suite, string name) =>
{
    var results = new List<object>();
    var suiteDir = Path.Combine(testsDir, suite);
    if (!Directory.Exists(suiteDir)) return Results.Ok(results);
    foreach (var pDir in Directory.GetDirectories(suiteDir))
    {
        if (Path.GetFileName(pDir) == "local") continue;
        foreach (var dDir in Directory.GetDirectories(pDir))
            if (File.Exists(Path.Combine(dDir, name)))
                results.Add(new { Platform = $"{Path.GetFileName(pDir)}/{Path.GetFileName(dDir)}" });
    }
    return Results.Ok(results);
});

// === Source API ===

app.MapGet("/api/sources", () => sourceManager.GetAll().Select(s => new { s.Id, s.Type, s.Label }));

app.MapPost("/api/sources/add-local", () =>
{
    if (!Directory.Exists(localDir))
        return Results.BadRequest("tests/local/ does not exist");
    var src = sourceManager.Add("local", "Local", localDir);
    return Results.Ok(new { src.Id, src.Label });
});

app.MapPost("/api/sources/add-ci", async (HttpRequest request) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    var body = await JsonSerializer.DeserializeAsync<CiDownloadRequest>(request.Body);
    if (body == null || string.IsNullOrEmpty(body.RunId)) return Results.BadRequest("runId required");

    var artifactName = body.ArtifactName ?? "test-artifacts-linux-vulkan";
    var destDir = Path.Combine(ciCacheDir, body.RunId);
    var markerFile = Path.Combine(destDir, $".downloaded_{artifactName}");

    // Skip if already downloaded
    if (File.Exists(markerFile))
    {
        var cachedSrc = sourceManager.GetAll().FirstOrDefault(s => s.Path == destDir);
        var cachedLabel = !string.IsNullOrEmpty(body.Label) ? body.Label : $"CI #{body.RunId[..Math.Min(5, body.RunId.Length)]}";
        cachedSrc ??= sourceManager.Add("ci", cachedLabel, destDir);
        return Results.Ok(new { cachedSrc.Id, Label = cachedSrc.Label });
    }

    // Download to a temp dir first, then merge into the cache dir
    var tmpDir = destDir + $"_tmp_{artifactName.GetHashCode():x}";
    if (Directory.Exists(tmpDir))
        Directory.Delete(tmpDir, true);
    Directory.CreateDirectory(tmpDir);

    // Download this artifact
    var proc = Process.Start(new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = $"run download {body.RunId} --repo stride3d/stride --name {artifactName} --dir \"{tmpDir}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    });
    if (proc != null)
    {
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
            return Results.Problem($"gh failed for {artifactName}: {err}");
        }
    }

    // Merge temp into cache dir (overwrite existing files)
    Directory.CreateDirectory(destDir);
    foreach (var file in Directory.GetFiles(tmpDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(tmpDir, file);
        var destFile = Path.Combine(destDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        File.Copy(file, destFile, overwrite: true);
    }
    if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);

    // Mark as downloaded so we skip next time
    File.WriteAllText(markerFile, DateTime.UtcNow.ToString("o"));

    // Reuse existing source for same run, or create new
    var label = $"CI #{body.RunId[..Math.Min(5, body.RunId.Length)]}";
    if (!string.IsNullOrEmpty(body.Label)) label = body.Label;
    var existing = sourceManager.GetAll().FirstOrDefault(s => s.Path == destDir);
    var src = existing ?? sourceManager.Add("ci", label, destDir);
    return Results.Ok(new { src.Id, src.Label });
});

app.MapPost("/api/sources/add-folder", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<FolderRequest>(request.Body);
    if (body == null || string.IsNullOrEmpty(body.Path)) return Results.BadRequest("path required");
    if (!Directory.Exists(body.Path)) return Results.BadRequest("Directory does not exist");
    var label = body.Label ?? Path.GetFileName(body.Path);
    var src = sourceManager.Add("folder", label, body.Path);
    return Results.Ok(new { src.Id, src.Label });
});

app.MapDelete("/api/sources/{id}", (string id) =>
{
    sourceManager.Remove(id);
    return Results.Ok();
});

app.MapGet("/api/source/{id}/images", (string id, string suite, string platform) =>
{
    var src = sourceManager.Get(id);
    if (src == null) return Results.NotFound();
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var dir = Path.Combine(src.Path, suite, parts[0], parts[1]);
    return Results.Ok(ListPngs(dir));
});

app.MapGet("/api/source/{id}/image", (string id, string suite, string platform, string name) =>
{
    var src = sourceManager.Get(id);
    if (src == null) return Results.NotFound();
    return ServeImage(src.Path, suite, platform, name);
});

// === CI Runs API ===

app.MapGet("/api/ci/status", () => Results.Ok(new { Available = ghAvailable, Error = ghAvailable ? null : ghError }));

app.MapGet("/api/ci/artifacts", async (string runId) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    var proc = Process.Start(new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = $"api repos/stride3d/stride/actions/runs/{runId}/artifacts --jq \".artifacts[] | {{name: .name, size_in_bytes: .size_in_bytes, expired: .expired}}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    });
    if (proc == null) return Results.Problem("Could not start gh");
    var output = await proc.StandardOutput.ReadToEndAsync();
    await proc.WaitForExitAsync();
    var artifacts = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => { try { return JsonSerializer.Deserialize<JsonElement>(line); } catch { return default; } })
        .Where(j => j.ValueKind != JsonValueKind.Undefined)
        .ToList();
    return Results.Ok(artifacts);
});

app.MapGet("/api/ci/runs", async (string? branch, int? limit) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    var lim = limit ?? 10;
    var branchArg = !string.IsNullOrEmpty(branch) ? $"--branch {branch}" : "";
    var proc = Process.Start(new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = $"api repos/stride3d/stride/actions/runs --jq \".workflow_runs[:{ lim}] | .[] | {{id: .id, head_sha: .head_sha, head_branch: .head_branch, created_at: .created_at, conclusion: .conclusion, name: .name}}\" {branchArg}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    });
    if (proc == null) return Results.Problem("Could not start gh");
    var output = await proc.StandardOutput.ReadToEndAsync();
    await proc.WaitForExitAsync();
    // Parse JSONL output
    var runs = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => { try { return JsonSerializer.Deserialize<JsonElement>(line); } catch { return default; } })
        .Where(j => j.ValueKind != JsonValueKind.Undefined)
        .ToList();
    return Results.Ok(runs);
});

// === Promote API ===

app.MapPost("/api/promote", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<PromoteRequest>(request.Body);
    if (body == null) return Results.BadRequest("Invalid body");

    var src = sourceManager.Get(body.SourceId);
    if (src == null) return Results.BadRequest("Source not found");

    var parts = body.Platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");

    var srcDir = Path.Combine(src.Path, body.Suite, parts[0], parts[1]);
    var goldDir = Path.Combine(testsDir, body.Suite, parts[0], parts[1]);
    Directory.CreateDirectory(goldDir);

    int promoted = 0;
    var details = new List<object>();
    foreach (var name in body.Names)
    {
        var srcFile = Path.Combine(srcDir, name);
        var dstFile = Path.Combine(goldDir, name);
        if (File.Exists(srcFile))
        {
            File.Copy(srcFile, dstFile, overwrite: true);
            promoted++;
            details.Add(new { Name = name, Src = srcFile, Dst = dstFile, SrcSize = new FileInfo(srcFile).Length, DstSize = new FileInfo(dstFile).Length });
        }
        else
        {
            details.Add(new { Name = name, Src = srcFile, Dst = dstFile, Error = "Source file not found" });
        }
    }
    Console.WriteLine($"Promote: {promoted}/{body.Names.Length} from {srcDir} -> {goldDir}");
    foreach (var d in details) Console.WriteLine($"  {d}");
    return Results.Ok(new { Promoted = promoted, Details = details });
});

app.MapPost("/api/gold/delete", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<DeleteGoldRequest>(request.Body);
    if (body == null) return Results.BadRequest("Invalid body");

    var parts = body.Platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");

    var goldDir = Path.Combine(testsDir, body.Suite, parts[0], parts[1]);
    int deleted = 0;
    foreach (var name in body.Names)
    {
        var file = Path.Combine(goldDir, name);
        if (File.Exists(file))
        {
            File.Delete(file);
            deleted++;
            Console.WriteLine($"  Deleted: {file}");
        }
    }
    Console.WriteLine($"Delete gold: {deleted}/{body.Names.Length} from {goldDir}");
    return Results.Ok(new { Deleted = deleted });
});

app.Run();

// === Helpers ===

static string? FindStrideRoot(string startDir)
{
    var dir = startDir;
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir, "tests")) && Directory.Exists(Path.Combine(dir, "sources")))
            return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return null;
}

static void CollectPlatforms(string suiteDir, HashSet<string> platforms)
{
    if (!Directory.Exists(suiteDir)) return;
    foreach (var pDir in Directory.GetDirectories(suiteDir))
        foreach (var dDir in Directory.GetDirectories(pDir))
            platforms.Add($"{Path.GetFileName(pDir)}/{Path.GetFileName(dDir)}");
}

static List<string> ListPngNames(string dir)
{
    if (!Directory.Exists(dir)) return [];
    return Directory.GetFiles(dir, "*.png")
        .Select(Path.GetFileName)
        .ToList()!;
}

static List<object> ListPngs(string dir)
{
    if (!Directory.Exists(dir)) return [];
    return Directory.GetFiles(dir, "*.png")
        .Select(f => (object)new { Name = Path.GetFileName(f) })
        .ToList();
}


static string GetGfxApi(string platformApi)
{
    // "Windows.Direct3D11" → "Direct3D11", "Linux.Vulkan" → "Vulkan"
    var dot = platformApi.IndexOf('.');
    return dot >= 0 ? platformApi[(dot + 1)..] : platformApi;
}

static string GetOS(string platformApi)
{
    // "Windows.Direct3D11" → "Windows", "Linux.Vulkan" → "Linux"
    var dot = platformApi.IndexOf('.');
    return dot >= 0 ? platformApi[..dot] : platformApi;
}

static int ScoreFallback(string candidatePlatApi, string candidateDevice, bool candidateIsSw,
                         string requestedPlatApi, string requestedDevice, bool requestedIsSw)
{
    // Higher = closer. Tiers, roughly in order of importance:
    //   exact OS+API > same OS > same gfx API across OS > same device/renderer >
    //   same renderer class (SW/HW).
    // Same-device (e.g. both WARP, both Lavapipe) matters so D3D12/WARP picks
    // D3D11/WARP over Windows.Vulkan/Lavapipe when they'd otherwise tie on OS.
    int score = 0;
    if (candidatePlatApi == requestedPlatApi) score += 16;
    if (GetOS(candidatePlatApi) == GetOS(requestedPlatApi)) score += 8;
    if (GetGfxApi(candidatePlatApi) == GetGfxApi(requestedPlatApi)) score += 4;
    if (string.Equals(candidateDevice, requestedDevice, StringComparison.OrdinalIgnoreCase)) score += 2;
    if (candidateIsSw == requestedIsSw) score += 1;
    return score;
}

static bool IsSoftwareRenderer(string device)
{
    var d = device.ToLowerInvariant();
    return d.Contains("warp") || d.Contains("swiftshader") || d.Contains("lavapipe") || d.Contains("llvmpipe");
}

static IResult ServeImage(string baseDir, string suite, string platform, string name)
{
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var filePath = Path.Combine(baseDir, suite, parts[0], parts[1], name);
    if (!File.Exists(filePath)) return Results.NotFound();
    return Results.File(filePath, "image/png");
}

// === Models ===

record CiDownloadRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("runId")]
    public string RunId { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("artifactName")]
    public string? ArtifactName { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string? Label { get; set; }
}
record FolderRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string? Label { get; set; }
}
record PromoteRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("suite")]
    public string Suite { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("platform")]
    public string Platform { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("names")]
    public string[] Names { get; set; } = [];
}

record DeleteGoldRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("suite")]
    public string Suite { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("platform")]
    public string Platform { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("names")]
    public string[] Names { get; set; } = [];
}

// === Source Manager ===

class Source
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = ""; // "local", "ci", "folder"
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
}

class SourceManager
{
    private readonly ConcurrentDictionary<string, Source> _sources = new();
    private int _nextId = 1;

    public Source Add(string type, string label, string path)
    {
        var id = $"src-{Interlocked.Increment(ref _nextId)}";
        var src = new Source { Id = id, Type = type, Label = label, Path = path };
        _sources[id] = src;
        return src;
    }

    public Source? Get(string id) => _sources.GetValueOrDefault(id);
    public void Remove(string id) => _sources.TryRemove(id, out _);
    public IEnumerable<Source> GetAll() => _sources.Values;
}
