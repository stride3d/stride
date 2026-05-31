using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

// 5505 instead of 5555: the latter is Android ADB's daemon port, which the Android emulator
// already binds when running test pulls — they collide otherwise. Override with --port N
// when running a second instance against another checkout.
int port = 5505;
for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--port" || args[i] == "-p") && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
        port = p;
}

// --lan (or --bind 0.0.0.0) opens the server to the local network instead of binding loopback
// only. No auth — only flip this on a network you trust.
var lanMode = args.Contains("--lan") || args.Contains("--bind");
var bindHost = lanMode ? "0.0.0.0" : "localhost";

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://{bindHost}:{port}");
builder.Services.AddSingleton<SourceManager>();
builder.Services.AddSingleton<ForkManager>();
// Sidecar PSNR can be +Infinity on exact-match passes; opt into the named-literal
// extension. camelCase to match the producer + JS conventions.
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
var app = builder.Build();

const string UpstreamRepo = "stride3d/stride";

// Find Stride root
var strideRoot = FindStrideRoot(AppContext.BaseDirectory)
    ?? FindStrideRoot(Directory.GetCurrentDirectory())
    ?? throw new InvalidOperationException("Could not find Stride root (looking for 'tests/' + 'sources/' directories)");

var testsDir = Path.Combine(strideRoot, "tests");
var localDir = Path.Combine(testsDir, "local");
// Path → (mtime, hash). Cleared automatically by mtime mismatch; no eviction needed.
var hashCache = new System.Collections.Concurrent.ConcurrentDictionary<string, (long mtimeTicks, string hash)>();
// Runtime serialises +Infinity for exact-match PSNR; opt into the named-literal extension.
// camelCase to match the producer (ImageTester writes camelCase keys).
var sidecarReadOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
};
var ciCacheDir = Path.Combine(Path.GetTempPath(), "stride-compare-gold");

var sourceManager = app.Services.GetRequiredService<SourceManager>();
var forkManager = app.Services.GetRequiredService<ForkManager>();
forkManager.Load(Path.Combine(ciCacheDir, "forks.json"));

Console.WriteLine($"Stride root: {strideRoot}");
Console.WriteLine($"Gold images: {testsDir}");
Console.WriteLine($"Local output: {localDir}");
Console.WriteLine($"CI cache: {ciCacheDir}");
Console.WriteLine();
Console.WriteLine($"CompareGold running at http://localhost:{port}");
if (lanMode)
{
    foreach (var ip in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                 && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
        Console.WriteLine($"  LAN: http://{ip.Address}:{port}");
}
Console.WriteLine("Press Ctrl+C to stop.");

try { Process.Start(new ProcessStartInfo($"http://localhost:{port}") { UseShellExecute = true }); }
catch { }

// Warm hashCache + goldByNameCache off the request path. Without this, the first
// /identical-platforms call after startup synchronously walks/hashes the whole suite
// tree and blocks other requests behind it.
_ = Task.Run(() =>
{
    if (!Directory.Exists(testsDir)) return;
    var sw = System.Diagnostics.Stopwatch.StartNew();
    int files = 0;
    foreach (var suiteDir in Directory.GetDirectories(testsDir))
    {
        if (Path.GetFileName(suiteDir) == "local") continue;
        foreach (var (_, list) in GetSuiteGoldByName(suiteDir))
        foreach (var (_, path) in list) { CachedGoldHash(path); files++; }
    }
    Console.WriteLine($"Hash warm: {files} files in {sw.ElapsedMilliseconds}ms");
});

app.UseDefaultFiles();
// no-store on every static response so dev edits to html/js/css show up on next reload
// without needing a hard refresh; the assets are local-only and tiny so the lost cache
// hit doesn't matter.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-store",
});

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

// Branch caching: a slow git on this repo can take 10+s for rev-parse, which blocks
// every page load. Watch .git/HEAD for actual checkout changes (cheap, no polling) and
// recompute on miss; reads in between just return the cached string.
var headPath = Path.Combine(strideRoot, ".git", "HEAD");
string? cachedBranch = null;
long cachedBranchHeadTicks = 0;
string ReadBranchCached()
{
    long currentHeadTicks = 0;
    try { currentHeadTicks = File.GetLastWriteTimeUtc(headPath).Ticks; } catch { }
    if (cachedBranch != null && currentHeadTicks == cachedBranchHeadTicks) return cachedBranch;
    string branch = "";
    try
    {
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "git", Arguments = "rev-parse --abbrev-ref HEAD",
            WorkingDirectory = strideRoot,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        });
        if (proc != null && proc.WaitForExit(5000) && proc.ExitCode == 0)
            branch = proc.StandardOutput.ReadToEnd().Trim();
    }
    catch { }
    cachedBranch = branch;
    cachedBranchHeadTicks = currentHeadTicks;
    return branch;
}

app.MapGet("/api/info", () => new { StrideRoot = strideRoot, Hostname = Environment.MachineName, Branch = ReadBranchCached() });

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

// Gold metadata sidecar (renderer that baked this PNG). Returns 404 if absent.
app.MapGet("/api/gold/metadata", (string suite, string platform, string name) =>
{
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var metaPath = Path.ChangeExtension(Path.Combine(testsDir, suite, parts[0], parts[1], name), ".metadata.json");
    return File.Exists(metaPath) ? Results.File(metaPath, "application/json") : Results.NotFound();
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

    var repo = NormalizeRepo(body.Repo) ?? UpstreamRepo;
    var artifactName = body.ArtifactName ?? "test-artifacts-linux-vulkan";
    // Repo is scoped into the cache path so runs with the same id across forks don't collide.
    var destDir = Path.Combine(ciCacheDir, repo.Replace('/', '_'), body.RunId);
    var markerFile = Path.Combine(destDir, $".downloaded_{artifactName}");

    // Default label suffixes upstream with "CI", forks with their owner so the source list
    // makes the origin obvious at a glance.
    string DefaultLabel()
    {
        var shortId = body.RunId[..Math.Min(5, body.RunId.Length)];
        var prefix = repo == UpstreamRepo ? "CI" : repo.Split('/')[0];
        return $"{prefix} #{shortId}";
    }

    // Skip if already downloaded
    if (File.Exists(markerFile))
    {
        var cachedSrc = sourceManager.GetAll().FirstOrDefault(s => s.Path == destDir);
        var cachedLabel = !string.IsNullOrEmpty(body.Label) ? body.Label : DefaultLabel();
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
        Arguments = $"run download {body.RunId} --repo {repo} --name {artifactName} --dir \"{tmpDir}\"",
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
    var label = !string.IsNullOrEmpty(body.Label) ? body.Label : DefaultLabel();
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
    var primaryGoldDir = Path.Combine(testsDir, suite, parts[0], parts[1]);
    return Results.Ok(ListSourceItems(dir, primaryGoldDir));
});

// Per-name lists of other platforms whose gold has identical content hash to the
// current platform's primary gold — the data the frontend turns into "consolidate"
// hints. Split off from /images so initial page load isn't blocked by the whole-suite
// gold scan; the frontend calls this in the background after render.
app.MapGet("/api/identical-platforms", (string suite, string platform) =>
{
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var primaryDir = Path.Combine(testsDir, suite, parts[0], parts[1]);
    if (!Directory.Exists(primaryDir)) return Results.Ok(new Dictionary<string, List<string>>());
    var goldByName = GetSuiteGoldByName(Path.Combine(testsDir, suite));
    var result = new Dictionary<string, List<string>>();
    foreach (var f in Directory.GetFiles(primaryDir, "*.png"))
    {
        var name = Path.GetFileName(f);
        if (!goldByName.TryGetValue(name, out var twins)) continue;
        var primaryHash = CachedGoldHash(f);
        var twinPlats = twins.Where(t => t.platform != platform && CachedGoldHash(t.path) == primaryHash).Select(t => t.platform).ToList();
        if (twinPlats.Count > 0) result[name] = twinPlats;
    }
    return Results.Ok(result);
});

app.MapGet("/api/source/{id}/image", (string id, string suite, string platform, string name) =>
{
    var src = sourceManager.Get(id);
    if (src == null) return Results.NotFound();
    return ServeImage(src.Path, suite, platform, name);
});

// Per-source metadata sidecar (renderer that produced this run's PNG).
app.MapGet("/api/source/{id}/metadata", (string id, string suite, string platform, string name) =>
{
    var src = sourceManager.Get(id);
    if (src == null) return Results.NotFound();
    var parts = platform.Split('/', 2);
    if (parts.Length != 2) return Results.BadRequest("Invalid platform");
    var metaPath = Path.ChangeExtension(Path.Combine(src.Path, suite, parts[0], parts[1], name), ".metadata.json");
    return File.Exists(metaPath) ? Results.File(metaPath, "application/json") : Results.NotFound();
});

// === CI Runs API ===

app.MapGet("/api/ci/status", () => Results.Ok(new { Available = ghAvailable, Error = ghAvailable ? null : ghError }));

app.MapGet("/api/ci/artifacts", async (string runId, string? repo) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    var r = NormalizeRepo(repo) ?? UpstreamRepo;
    var artifacts = await FetchJsonLinesAsync(
        $"api repos/{r}/actions/runs/{runId}/artifacts --jq \".artifacts[] | {{name: .name, size_in_bytes: .size_in_bytes, expired: .expired}}\"");
    return Results.Ok(artifacts);
});

// Probe upstream + every configured fork for a run id. Returns the first repo whose API
// answers 200 for that run. Lets the client accept a manually-entered run id (or a pasted
// URL) without forcing the user to pick the repo separately.
app.MapGet("/api/ci/resolve", async (string runId) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    if (string.IsNullOrEmpty(runId)) return Results.BadRequest("runId required");
    var repos = new[] { UpstreamRepo }.Concat(forkManager.GetAll()).Distinct().ToArray();
    foreach (var repo in repos)
    {
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"api repos/{repo}/actions/runs/{runId} --jq .id",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        });
        if (proc == null) continue;
        await proc.WaitForExitAsync();
        if (proc.ExitCode == 0)
            return Results.Ok(new { repo });
    }
    return Results.NotFound();
});

app.MapGet("/api/ci/runs", async (string? branch, int? limit) =>
{
    if (!ghAvailable) return Results.BadRequest(ghError);
    var lim = limit ?? 10;
    var branchArg = !string.IsNullOrEmpty(branch) ? $"--branch {branch}" : "";

    // Fan out across upstream + every configured fork in parallel; tag each run with `repo`
    // so the UI can show a chip and the artifact-list / download calls can target the right repo.
    var repos = new[] { UpstreamRepo }.Concat(forkManager.GetAll()).Distinct().ToArray();
    var tasks = repos.Select(async r =>
    {
        var runs = await FetchJsonLinesAsync(
            $"api repos/{r}/actions/runs --jq \".workflow_runs[:{ lim}] | .[] | {{id: .id, run_number: .run_number, head_sha: .head_sha, head_branch: .head_branch, created_at: .created_at, conclusion: .conclusion, name: .name}}\" {branchArg}");
        // Re-emit each run with an added `repo` field so the client can disambiguate.
        return runs.Select(j =>
        {
            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in j.EnumerateObject()) dict[prop.Name] = prop.Value;
            dict["repo"] = JsonSerializer.SerializeToElement(r);
            return JsonSerializer.SerializeToElement(dict);
        }).ToList();
    });
    var all = (await Task.WhenAll(tasks)).SelectMany(x => x)
        .OrderByDescending(j => j.TryGetProperty("created_at", out var c) ? c.GetString() : "")
        .ToList();
    return Results.Ok(all);
});

// === Forks API ===

app.MapGet("/api/forks", () => Results.Ok(forkManager.GetAll()));
app.MapPost("/api/forks", async (HttpRequest request) =>
{
    var body = await JsonSerializer.DeserializeAsync<ForkRequest>(request.Body);
    var normalized = NormalizeRepo(body?.Repo);
    if (normalized == null) return Results.BadRequest("repo must be owner/name");
    if (normalized == UpstreamRepo) return Results.BadRequest($"{UpstreamRepo} is always included; no need to add it");
    forkManager.Add(normalized);
    return Results.Ok(new { repo = normalized });
});
app.MapDelete("/api/forks", (string repo) =>
{
    var normalized = NormalizeRepo(repo);
    if (normalized == null) return Results.BadRequest("repo must be owner/name");
    forkManager.Remove(normalized);
    return Results.Ok();
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
            // Carry .metadata.json next to the gold so it records the renderer that baked it.
            var srcMeta = Path.ChangeExtension(srcFile, ".metadata.json");
            if (File.Exists(srcMeta))
                File.Copy(srcMeta, Path.ChangeExtension(dstFile, ".metadata.json"), overwrite: true);
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
        // Reap .metadata.json with the PNG so we don't orphan stale renderer info.
        var metaFile = Path.ChangeExtension(file, ".metadata.json");
        if (File.Exists(metaFile))
        {
            File.Delete(metaFile);
            Console.WriteLine($"  Deleted: {metaFile}");
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

// Results sidecar (foo.results.json) lives next to each output PNG (or alone, on a passing
// test where the PNG is skipped). Union {*.png, *.results.json} by stem so passing tests
// still appear in the listing. Each item also carries the current SHA256 of its matched +
// primary gold; the frontend compares against the hashes the sidecar baked in at compare
// time to detect staleness (gold edited or copied after the test ran).
List<object> ListSourceItems(string dir, string primaryGoldDir)
{
    if (!Directory.Exists(dir)) return [];
    var byStem = new Dictionary<string, (bool png, Sidecar? sc)>(StringComparer.OrdinalIgnoreCase);
    foreach (var f in Directory.GetFiles(dir, "*.png"))
    {
        var stem = Path.GetFileNameWithoutExtension(f);
        byStem[stem] = (true, null);
    }
    foreach (var f in Directory.GetFiles(dir, "*.results.json"))
    {
        // Strip the .results suffix so the stem matches the PNG's bare name.
        var stem = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f));
        var sc = TryReadSidecar(f);
        byStem[stem] = (byStem.TryGetValue(stem, out var ex) ? ex.png : false, sc);
    }
    return byStem
        .Select(kv =>
        {
            var name = kv.Key + ".png";
            var matchedPath = ResolveMatchedGoldLocalPath(kv.Value.sc?.Matched);
            var primaryPath = Path.Combine(primaryGoldDir, name);
            var matchedGoldHash = matchedPath != null && File.Exists(matchedPath) ? CachedGoldHash(matchedPath) : null;
            var primaryGoldHash = File.Exists(primaryPath) ? CachedGoldHash(primaryPath) : null;
            return (object)new { Name = name, HasPng = kv.Value.png, Sidecar = kv.Value.sc, MatchedGoldHash = matchedGoldHash, PrimaryGoldHash = primaryGoldHash };
        })
        .ToList();
}

// The sidecar's matched path is whatever filesystem the test ran on (device path on
// Android, Windows path locally). Map back to the local checkout by taking the last
// 4 segments: <suite>/<Platform.API>/<Device>/<name>.png.
string? ResolveMatchedGoldLocalPath(string? matched)
{
    if (string.IsNullOrEmpty(matched)) return null;
    var parts = matched.Split('/', '\\');
    if (parts.Length < 4) return null;
    var n = parts.Length;
    return Path.Combine(testsDir, parts[n - 4], parts[n - 3], parts[n - 2], parts[n - 1]);
}

// Always rescan — the structure (which platforms have which gold files) can change via
// manual filesystem edits that don't go through our POST endpoints. Cheap because the
// hashes themselves are still memoised in CachedGoldHash; this is just dir enumeration.
Dictionary<string, List<(string platform, string path)>> GetSuiteGoldByName(string suiteDir)
{
    var goldByName = new Dictionary<string, List<(string platform, string path)>>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(suiteDir)) return goldByName;
    foreach (var pDir in Directory.GetDirectories(suiteDir))
    {
        var pName = Path.GetFileName(pDir);
        if (pName == "local") continue;
        foreach (var dDir in Directory.GetDirectories(pDir))
        {
            var plat = $"{pName}/{Path.GetFileName(dDir)}";
            foreach (var f in Directory.GetFiles(dDir, "*.png"))
            {
                var n = Path.GetFileName(f);
                if (!goldByName.TryGetValue(n, out var list)) goldByName[n] = list = [];
                list.Add((plat, f));
            }
        }
    }
    return goldByName;
}

// Cached SHA256 of a gold file's bytes. Cache key is path; entry invalidates when the
// file's mtime changes. First read is sync; subsequent reads are dictionary lookups.
string CachedGoldHash(string path)
{
    var mtime = File.GetLastWriteTimeUtc(path).Ticks;
    if (hashCache.TryGetValue(path, out var entry) && entry.mtimeTicks == mtime) return entry.hash;
    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
    hashCache[path] = (mtime, hash);
    return hash;
}

Sidecar? TryReadSidecar(string jsonPath)
{
    try { return JsonSerializer.Deserialize<Sidecar>(File.ReadAllText(jsonPath), sidecarReadOptions); }
    catch { return null; }
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

// Accepts "owner/name", returns null if the shape doesn't match. Trims whitespace.
// Used everywhere a repo flows in from the network or CLI so the rest of the pipeline
// can assume the value is safe to pass to `gh`.
static string? NormalizeRepo(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    var trimmed = input.Trim().Trim('/');
    var parts = trimmed.Split('/');
    if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) return null;
    return trimmed;
}

// Runs `gh` with the given args, parses stdout as JSON Lines, returns the parseable entries.
// Centralised so the runs / artifacts endpoints share the same plumbing.
static async Task<List<JsonElement>> FetchJsonLinesAsync(string ghArgs)
{
    var proc = Process.Start(new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = ghArgs,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    });
    if (proc == null) return new();
    var output = await proc.StandardOutput.ReadToEndAsync();
    await proc.WaitForExitAsync();
    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => { try { return JsonSerializer.Deserialize<JsonElement>(line); } catch { return default; } })
        .Where(j => j.ValueKind != JsonValueKind.Undefined)
        .ToList();
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
    [System.Text.Json.Serialization.JsonPropertyName("repo")]
    public string? Repo { get; set; }
}
record ForkRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("repo")]
    public string? Repo { get; set; }
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

// Mirrors Stride.Graphics.Regression.ImageTester.Sidecar so the JSON written by the test
// runtime can be deserialised here without an inter-project dependency.
record Sidecar(string Outcome, DateTime At, string? Matched, List<SidecarAttempt> Attempts);
record SidecarAttempt(string Gold, string Kind, bool Passed, int MaxDiff, double PsnrDb, Dictionary<string, int> Buckets, Dictionary<string, int>? Thresholds, string? GoldHash);

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

// === Fork Manager ===
// Configured "owner/name" repos whose runs we want to see alongside stride3d/stride.
// Upstream is always implicit (queried even when this list is empty) so we never need to
// store it. Persisted to a JSON file so the list survives across tool restarts.
class ForkManager
{
    private readonly List<string> _forks = new();
    private readonly Lock _lock = new();
    private string? _path;

    public void Load(string path)
    {
        _path = path;
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            if (list != null)
            {
                lock (_lock) { _forks.Clear(); _forks.AddRange(list); }
            }
        }
        catch { /* ignore corrupt config */ }
    }

    public void Add(string repo)
    {
        lock (_lock)
        {
            if (_forks.Contains(repo, StringComparer.OrdinalIgnoreCase)) return;
            _forks.Add(repo);
            Save();
        }
    }

    public void Remove(string repo)
    {
        lock (_lock)
        {
            _forks.RemoveAll(r => r.Equals(repo, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    public IEnumerable<string> GetAll()
    {
        lock (_lock) return _forks.ToArray();
    }

    private void Save()
    {
        if (_path == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_forks));
        }
        catch { /* best-effort */ }
    }
}
