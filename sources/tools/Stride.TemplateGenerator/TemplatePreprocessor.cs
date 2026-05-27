// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Stride.Core.Diagnostics;

namespace Stride.TemplateGenerator;

/// <summary>
/// Transforms a raw sample dir into a staged tree that the dotnet new template engine can pack.
/// See <see cref="Run"/> for the inline-documented step sequence.
/// </summary>
internal class TemplatePreprocessor
{
    /// <summary>Standard dashed GUID format: 8-4-4-4-12 hex digits.</summary>
    private static readonly Regex GuidRegex = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Well-known constant GUIDs that look like instance identifiers but are actually fixed type
    /// markers — replacing them with per-instantiation values would break tooling. Skipped by both
    /// the scan and the substitution passes.
    /// </summary>
    private static readonly HashSet<Guid> ReservedGuids = new()
    {
        // .sln project type GUIDs (Microsoft well-knowns).
        new("9A19103F-16F7-4668-BE54-9A1E7A4F7556"), // SDK-style C# project
        new("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"), // Legacy C# project
        new("2150E333-8FDC-42A3-9474-1A3956D46DE8"), // Solution folder
    };

    /// <summary>
    /// Non-Stride file extensions treated as text for the GUID placeholder + line-ending passes.
    /// Stride assets (any <c>.sd*</c> extension) are picked up via <see cref="IsTextFile"/> so
    /// plugin-defined asset types stay covered without explicit listing.
    /// </summary>
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".targets", ".props", ".config",
        ".json", ".yaml", ".yml",
        ".xml", ".html", ".htm",
        ".md", ".txt", ".gitignore", ".gitattributes", ".editorconfig",
    };

    /// <summary>
    /// Stride asset extensions that store binary payload (rather than the typical YAML).
    /// Anything <c>.sd*</c> not in this set is treated as text.
    /// </summary>
    private static readonly HashSet<string> BinaryStrideExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sdimg",
    };

    /// <summary>
    /// True for files whose content the preprocessor may safely read + rewrite (GUID substitution,
    /// line-ending normalization). Covers non-Stride text extensions plus all <c>.sd*</c> asset
    /// extensions except the known-binary ones.
    /// </summary>
    private static bool IsTextFile(string path)
    {
        var ext = Path.GetExtension(path);
        if (TextExtensions.Contains(ext))
            return true;
        if (ext.StartsWith(".sd", StringComparison.OrdinalIgnoreCase))
            return !BinaryStrideExtensions.Contains(ext);
        return false;
    }

    public string? InputPath { get; set; }
    public string? OutputDirectory { get; set; }
    public string? TemplateName { get; set; }

    /// <summary>
    /// When set, every literal <c>$EngineVersion$</c> in <c>.csproj</c> output files is rewritten
    /// to this value (typically pack-time <c>PackageVersion</c>). No-op for samples that hardcode
    /// their version; required for the NewGame starter so its <c>PackageReference</c> versions
    /// pin to the matching engine release.
    /// </summary>
    public string? EngineVersion { get; set; }

    /// <summary>
    /// Parsed from the input's <c>.sdtpl</c> file (if present). Drives template.json metadata
    /// (Name, Description, identity, etc.) and per-template parameter opt-in via the
    /// <see cref="SdtplMetadata.Parameters"/> list. Null when the input dir has no .sdtpl —
    /// preprocessor falls back to default behavior in that case.
    /// </summary>
    public SdtplMetadata? Sdtpl { get; private set; }

    /// <summary>
    /// Original literal name to rename to <c>MyTemplate</c> across all staged content (file
    /// contents, file names, dir names). When null, auto-detected from the first <c>.sdpkg</c>'s
    /// <c>Name</c> field with any <c>.Game</c> / <c>.Windows</c> / etc. suffix stripped. Set
    /// explicitly via <c>--source-name=X</c> to override.
    ///
    /// No-op when the detected name is already <c>MyTemplate</c> (e.g. NewGame scaffold case).
    /// Required for sample templates: their content is full of literal <c>CSharpBeginner</c>
    /// (or whatever) refs in .cs namespaces, .sd* script type refs, .csproj RootNamespace, etc.,
    /// none of which the dotnet new template engine's <c>sourceName</c> mechanism can substitute
    /// because they don't match the sourceName literal.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// When true, skip the asset prune step. Templates ship larger (unreachable assets included);
    /// escape hatch for the "minimal" pack mode and for diagnostics.
    /// </summary>
    public bool SkipPrune { get; set; }

    /// <summary>
    /// Map of original GUID → placeholder index (1-based). Populated by the GUID scan pass,
    /// consumed by the placeholder substitution pass and template.json emission.
    /// </summary>
    public Dictionary<Guid, int> GuidMap { get; } = new();

    public bool Run(ILogger logger)
    {
        if (string.IsNullOrEmpty(InputPath))
        {
            logger.Error("--preprocess-template requires --input-path=<dir>");
            return false;
        }
        if (string.IsNullOrEmpty(OutputDirectory))
        {
            logger.Error("--preprocess-template requires --output-path=<dir>");
            return false;
        }
        if (!Directory.Exists(InputPath))
        {
            logger.Error($"Input path does not exist: {InputPath}");
            return false;
        }

        // Parse the sample's .sdtpl if present. Drives template.json metadata + per-template
        // parameter opt-in. Absent .sdtpl is fine — defaults apply.
        var sdtplPath = Directory.EnumerateFiles(InputPath, "*.sdtpl", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sdtplPath != null)
        {
            Sdtpl = SdtplMetadata.Parse(sdtplPath);
            logger.Info($"Loaded metadata from {Path.GetFileName(sdtplPath)} (Name='{Sdtpl.Name}', Parameters=[{string.Join(", ", Sdtpl.Parameters)}])");
        }

        // Stage: recursively mirror input → output. Clean output first to avoid leftover files
        // from prior runs polluting the staged tree.
        if (Directory.Exists(OutputDirectory))
            Directory.Delete(OutputDirectory, recursive: true);
        CopyDirectory(InputPath, OutputDirectory);
        logger.Info($"Staged {InputPath} → {OutputDirectory}");

        // .sdtpl is metadata for the package, not content for the user. Strip from the staged
        // output (the orchestrator's aggregation step reads .sdtpl directly from sample inputs).
        foreach (var stagedSdtpl in Directory.EnumerateFiles(OutputDirectory, "*.sdtpl", SearchOption.TopDirectoryOnly))
            File.Delete(stagedSdtpl);

        // Icon/Screenshot files declared in .sdtpl may point OUTSIDE the sample dir (e.g. the
        // genre starters share samples/Templates/.sdtpl/Icon2*.png). The recursive copy above
        // only mirrors what's inside the sample dir, so external assets need an explicit copy
        // into the staged tree. We deposit them under <output>/.sdtpl/<filename> — sibling to
        // sample-local screenshots — and the GameStudio bridge tries that fallback location
        // when the as-declared relative path doesn't resolve. Sample-local paths need no work
        // (already mirrored by CopyDirectory).
        CopyExternalSdtplAssets(sdtplPath, logger);

        // Dep collapse: for sample templates whose .csproj references shared external asset packs
        // (e.g. Templates/Packs/PrototypingBlocks), inline those packs' Assets/ and Resources/
        // content into the staged tree and strip the ProjectReference. Intra-template refs
        // (e.g. MyTemplate.Windows → MyTemplate) are preserved. No-op when no .csproj contains
        // an external ProjectReference (e.g. the NewGame scaffold).
        CollapseProjectReferences(logger);

        // Asset pruning: drop any asset under the staged tree that isn't reachable from a root
        // (RootAsset, always-mark-as-root type, or transitively depended on by such). For
        // dep-collapsed sample templates this typically prunes 80%+ of inlined-pack assets that
        // the sample doesn't actually use. Pure-text scan (no PackageSession.Load), so it has no
        // engine-assembly dependency and runs in milliseconds.
        if (!SkipPrune)
            DumbPruneUnreachableAssets(logger);
        else
            logger.Info("Skipping asset prune");

        // Clean up obj/ and bin/ dirs before the rename pass — the obj/project.nuget.cache files
        // contain stale sample-name references that would confuse the diff, and walking them is
        // slow.
        CleanBuildArtifacts(logger);

        // Generate MyTemplate.sln if absent. Samples typically don't include their sln (input is
        // the inner dir, not the sample root); synthesize one referencing only the csprojs that
        // exist in staging. Per-platform exec csprojs get wrapped in #if (XActive) conditional
        // regions so template.json's SpecialCustomOperations strips unselected platforms at
        // instantiation.
        GenerateSlnIfMissing(logger);

        // Sample-name → MyTemplate rename. Replaces the sample's literal name (CSharpBeginner /
        // SpriteStudioDemo / ...) with the sourceName placeholder "MyTemplate" across file
        // contents, file names, and directory names. The template engine then sourceName-
        // substitutes "MyTemplate" → user's -n value at instantiation. No-op when the detected
        // source name is already "MyTemplate" (NewGame scaffold case).
        RenameSourceName(logger);

        // Inject a BasicCameraController placeholder component into the Camera entity, BEFORE
        // the GUID scan so the injected component's Id gets caught by the placeholder pass
        // (fresh GUID per dotnet new instantiation). The type/assembly prefix is the literal
        // "MyTemplate" which the template engine substitutes via sourceName.
        InjectCameraScript(logger);

        // Scan all text files for unique GUIDs, then rewrite with placeholders. A pre-pass
        // indexes every Id explicitly declared in the staged .sd* tree (the asset's own Id at
        // the top of each file, plus sub-asset Ids like entity / component / render-stage Ids
        // declared within); during the GUID rewrite pass, only Ids in that locally-defined set
        // are placeholdered. Anything else is a reference to something external (engine
        // archetypes like `Archetype: 823a81bf...:DefaultGraphicsCompositorLevel10`, engine
        // compositor camera slot Ids referenced bare from `Slot:` fields, etc.) and must survive
        // intact through instantiation — otherwise the reference dangles at runtime.
        ScanLocallyDefinedIds(logger);
        ScanGuids();
        ApplyPlaceholders();
        logger.Info($"Replaced {GuidMap.Count} unique GUIDs with placeholders");

        // Emit .template.config/template.json with one generated/guid symbol per placeholder,
        // plus multichoice Platforms, HDR/LDR choice, and per-platform sources/modifiers.
        EmitTemplateJson();
        logger.Info($"Wrote template.json with {GuidMap.Count} generated/guid symbols");

        // $EngineVersion$ substitution. Only NewGame's csprojs use this literal; samples
        // hardcode their version and pass through unchanged.
        if (!string.IsNullOrEmpty(EngineVersion))
            SubstituteEngineVersion(logger);

        // Normalize line endings so the nupkg is byte-identical regardless of build host
        // (Windows checkout = CRLF, Linux CI = LF) and regardless of which step wrote each
        // file. Binary files are excluded via the TextExtensions whitelist.
        NormalizeLineEndings(logger);

        return true;
    }

    /// <summary>
    /// Final-pass line-ending normalization. <c>.sln</c> → CRLF (VS/dotnet sln write CRLF on
    /// edit; matching avoids the "Inconsistent line endings" prompt on first save). Everything
    /// else → LF (modern cross-platform convention, matches what <c>dotnet/sdk</c> templates
    /// ship). Only files in the <see cref="TextExtensions"/> whitelist are touched — binaries
    /// (.dds/.png/.fbx/.wav/...) are left untouched.
    /// </summary>
    private void NormalizeLineEndings(ILogger logger)
    {
        var rewritten = 0;
        foreach (var path in EnumerateTextFiles(OutputDirectory!))
        {
            var content = File.ReadAllText(path);
            var normalized = content.Replace("\r\n", "\n");
            if (Path.GetExtension(path).Equals(".sln", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Replace("\n", "\r\n");
            if (normalized != content)
            {
                File.WriteAllText(path, normalized);
                rewritten++;
            }
        }
        logger.Info($"Normalized line endings in {rewritten} text file(s)");
    }

    /// <summary>
    /// Copies any Icon/Screenshot file referenced by the parsed <see cref="Sdtpl"/> whose
    /// canonical location is OUTSIDE <see cref="InputPath"/> into the staged output's
    /// <c>.sdtpl/</c> dir. Sample-local references (path resolves inside the sample dir) are
    /// already covered by the prior recursive copy and need no further action.
    /// </summary>
    private void CopyExternalSdtplAssets(string? sdtplPath, ILogger logger)
    {
        if (Sdtpl == null || sdtplPath == null)
            return;
        var baseDir = Path.GetFullPath(InputPath!);
        var sdtplDir = Path.GetDirectoryName(sdtplPath)!;
        var destDir = Path.Combine(OutputDirectory!, ".sdtpl");

        void Copy(string? relPath)
        {
            if (string.IsNullOrEmpty(relPath))
                return;
            var src = Path.GetFullPath(Path.Combine(sdtplDir, relPath));
            if (!File.Exists(src))
            {
                logger.Warning($"sdtpl asset not found: {relPath} (resolved {src})");
                return;
            }
            // If the asset already lives inside the sample dir, the recursive stage already
            // copied it — skip.
            if (src.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                src.StartsWith(baseDir + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return;
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, Path.GetFileName(src));
            File.Copy(src, dest, overwrite: true);
            logger.Info($"Copied external sdtpl asset {Path.GetFileName(src)} → .sdtpl/");
        }

        Copy(Sdtpl.Icon);
        foreach (var screenshot in Sdtpl.Screenshots)
            Copy(screenshot);
    }

    private void SubstituteEngineVersion(ILogger logger)
    {
        var replaced = 0;
        foreach (var csproj in Directory.EnumerateFiles(OutputDirectory!, "*.csproj", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(csproj);
            if (content.IndexOf("$EngineVersion$", StringComparison.Ordinal) < 0)
                continue;
            File.WriteAllText(csproj, content.Replace("$EngineVersion$", EngineVersion));
            replaced++;
        }
        if (replaced > 0)
            logger.Info($"Substituted $EngineVersion$ → {EngineVersion} in {replaced} .csproj file(s)");
    }

    /// <summary>
    /// Every Guid the staged tree declares via an <c>Id:</c> line — the asset's own Id at
    /// the top of each .sd* file plus every sub-asset Id nested inside (entity, component,
    /// render-stage, etc.). Populated by <see cref="ScanLocallyDefinedIds"/>; consulted by
    /// <see cref="ScanGuids"/> to distinguish sample-internal Ids (which get rotated per
    /// instantiation via template.json's generated/guid symbols) from external references
    /// (engine archetypes, engine camera-slot Ids, anything else this sample doesn't own —
    /// kept intact so the reference still resolves at runtime).
    /// </summary>
    private readonly HashSet<Guid> LocallyDefinedIds = new();

    /// <summary>
    /// Walks every .sd* file in the staged output and records every Guid declared via an
    /// <c>Id:</c> line (any indent). The top-level <c>Id:</c> declares the asset itself; the
    /// nested ones declare sub-assets that are sample-internal but addressable by Id from
    /// other parts of the file (or other files in the same sample). Both flavors need
    /// placeholdering at instantiation time; everything else (engine refs, cross-pack refs)
    /// stays intact.
    /// </summary>
    private void ScanLocallyDefinedIds(ILogger logger)
    {
        foreach (var path in EnumerateTextFiles(OutputDirectory!))
        {
            // Only .sd* asset files declare assets / sub-assets via "Id:". Other text files
            // (.csproj, .sln, .json) carry GUIDs but they're not declarations — anything
            // there is treated as an external reference and preserved.
            if (!Path.GetExtension(path).StartsWith(".sd", StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith("Id:", StringComparison.Ordinal))
                    continue;
                var idStr = trimmed.Substring(3).Trim();
                if (Guid.TryParse(idStr, out var g))
                    LocallyDefinedIds.Add(g);
            }
        }
        logger.Info($"Indexed {LocallyDefinedIds.Count} locally-defined Id(s)");
    }

    private void ScanGuids()
    {
        // Sort for determinism: same input layout produces the same placeholder assignment.
        // Only Ids declared locally (asset's own + sub-asset Ids) get placeholdered. GUIDs
        // not in LocallyDefinedIds are external references (engine archetypes / slots / etc.)
        // and stay intact through preprocessing.
        foreach (var path in EnumerateTextFiles(OutputDirectory!))
        {
            var content = File.ReadAllText(path);
            foreach (Match m in GuidRegex.Matches(content))
            {
                if (Guid.TryParse(m.Value, out var g)
                    && !ReservedGuids.Contains(g)
                    && LocallyDefinedIds.Contains(g)
                    && !GuidMap.ContainsKey(g))
                    GuidMap[g] = GuidMap.Count + 1;
            }
        }
    }

    private void ApplyPlaceholders()
    {
        foreach (var path in EnumerateTextFiles(OutputDirectory!))
        {
            var content = File.ReadAllText(path);
            var rewritten = GuidRegex.Replace(content, m =>
                Guid.TryParse(m.Value, out var g) && GuidMap.TryGetValue(g, out var idx)
                    ? MakePlaceholder(idx)
                    : m.Value);
            if (!ReferenceEquals(rewritten, content))
                File.WriteAllText(path, rewritten);
        }
    }

    /// <summary>
    /// Walks all <c>.csproj</c> files under the staged output, removes any
    /// <c>&lt;ProjectReference&gt;</c> whose target resolves outside the staging dir, and copies
    /// the referenced project's <c>Assets/</c> and <c>Resources/</c> content into the staged
    /// root's top-level <c>Assets/</c> / <c>Resources/</c>. Intra-template references (e.g. exec
    /// → game library, same staging tree) are left untouched.
    /// </summary>
    private void CollapseProjectReferences(ILogger logger)
    {
        var stagedRoot = new DirectoryInfo(OutputDirectory!).FullName;
        var inputRoot = new DirectoryInfo(InputPath!).FullName;
        var totalCopied = 0;
        var totalRefsRemoved = 0;

        foreach (var csprojPath in Directory.EnumerateFiles(OutputDirectory!, "*.csproj", SearchOption.AllDirectories))
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                logger.Warning($"Could not parse {csprojPath} as XML; skipping dep-collapse: {ex.Message}");
                continue;
            }

            // ProjectReference is always under the default namespace (i.e. no xmlns prefix on SDK
            // csprojs). Match by local name to be robust against either form.
            var refsToRemove = new List<XElement>();
            foreach (var refElem in doc.Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
            {
                var include = refElem.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(include))
                    continue;

                // .csproj files conventionally use backslash separators (Windows convention) even
                // when authored cross-platform. Normalize to the current platform so Path.Combine
                // resolves them correctly on Linux/macOS.
                var normalizedInclude = include.Replace('\\', Path.DirectorySeparatorChar);
                // Resolve relative to the ORIGINAL csproj location, not the staged one. The
                // staged copy strips parent dirs, so a sample's "..\..\..\..\Packs\Foo" reference
                // can't resolve from staging. Map the staged path back to its input counterpart.
                var relCsproj = Path.GetRelativePath(stagedRoot, csprojPath);
                var originalCsproj = Path.Combine(inputRoot, relCsproj);
                var refDir = Path.GetDirectoryName(originalCsproj)!;
                var resolved = Path.GetFullPath(Path.Combine(refDir, normalizedInclude));
                var resolvedDir = Path.GetDirectoryName(resolved)!;

                if (resolvedDir.StartsWith(inputRoot, StringComparison.OrdinalIgnoreCase))
                {
                    // Intra-template reference — keep. The pre-collapse input tree may contain
                    // multiple csprojs that ref each other (e.g. Foo.Windows → Foo.Game); those
                    // are part of the template, not external packs.
                    continue;
                }

                if (!File.Exists(resolved))
                {
                    logger.Warning($"ProjectReference target does not exist on disk: {resolved} (in {csprojPath})");
                    refsToRemove.Add(refElem);
                    continue;
                }

                // Copy the referenced project dir's Assets/ and Resources/ subdirs into the staged
                // root's top-level Assets/ and Resources/.
                var copiedCount = CopyContentSubdir(resolvedDir, stagedRoot, "Assets", logger)
                                + CopyContentSubdir(resolvedDir, stagedRoot, "Resources", logger);
                totalCopied += copiedCount;
                logger.Info($"Inlined ProjectReference '{include}' → {copiedCount} files");
                refsToRemove.Add(refElem);
            }

            if (refsToRemove.Count == 0)
                continue;

            // Remove the ProjectReference nodes (and their immediate trailing whitespace, if any,
            // to keep the .csproj XML tidy).
            foreach (var elem in refsToRemove)
            {
                if (elem.NextNode is XText whitespace && string.IsNullOrWhiteSpace(whitespace.Value))
                    whitespace.Remove();
                elem.Remove();
                totalRefsRemoved++;
            }
            doc.Save(csprojPath);
        }

        if (totalRefsRemoved > 0)
            logger.Info($"Dep-collapse: removed {totalRefsRemoved} external ProjectReference(s), inlined {totalCopied} files");
    }

    /// <summary>
    /// Maps a per-platform exec project's suffix to the template parameter name guarding its
    /// inclusion. Mirrors the *Active computed bools emitted by <see cref="EmitParameterSymbols"/>;
    /// the <c>iOS</c> entry uses <c>iOsActive</c> (mixed case) to dodge the customOperations
    /// C++-evaluator clash with the <c>iOS</c> quoteless choice literal.
    /// </summary>
    private static readonly Dictionary<string, string> PlatformActiveSymbol = new(StringComparer.Ordinal)
    {
        { "Windows", "WindowsActive" },
        { "Linux",   "LinuxActive"   },
        { "macOS",   "MacOSActive"   },
        { "iOS",     "iOsActive"     },
        { "Android", "AndroidActive" },
    };

    /// <summary>
    /// Synthesizes a <c>MyTemplate.sln</c> at the staged root when one isn't already present.
    /// Walks <c>*.csproj</c> under the tree and emits a Project block for each, wrapping
    /// per-platform exec projects (those whose dir name ends in a known platform suffix) in
    /// <c>#if (XActive)</c> markers so the template engine's SpecialCustomOperations strips
    /// unselected platforms at instantiation. Instance GUIDs are random but flow through the
    /// subsequent GUID-placeholder pass and get fresh values per dotnet new invocation.
    /// </summary>
    private void GenerateSlnIfMissing(ILogger logger)
    {
        var existing = Directory.EnumerateFiles(OutputDirectory!, "*.sln", SearchOption.TopDirectoryOnly).ToList();
        if (existing.Count > 0)
            return;

        // Discover csprojs and classify.
        // Each entry: (csprojPath, dirName, platformSuffix-or-null, instanceGuid)
        var projects = new List<(string Csproj, string DirName, string? Platform, Guid InstanceId)>();
        foreach (var csproj in Directory.EnumerateFiles(OutputDirectory!, "*.csproj", SearchOption.AllDirectories))
        {
            var dirName = Path.GetFileName(Path.GetDirectoryName(csproj)!);
            string? platform = null;
            foreach (var p in PlatformActiveSymbol.Keys)
            {
                if (dirName.EndsWith("." + p, StringComparison.Ordinal))
                {
                    platform = p;
                    break;
                }
            }
            projects.Add((csproj, dirName, platform, Guid.NewGuid()));
        }
        if (projects.Count == 0)
            return;

        // Per-platform execs sort first (in PlatformActiveSymbol order, so Windows leads),
        // game library after. VS / dotnet picks the first project in the .sln as the startup
        // project when no .vs/ user state exists; putting Windows first means the user can
        // F5 / dotnet run straight after instantiation without manually picking a startup.
        var ordered = projects
            .OrderBy(p => p.Platform == null ? 1 : 0)
            .ThenBy(p => p.Platform == null ? 0 : new List<string>(PlatformActiveSymbol.Keys).IndexOf(p.Platform))
            .ToList();

        const string SdkProjectTypeGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        var sb = new StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine("VisualStudioVersion = 17.0.0.0");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
        foreach (var p in ordered)
        {
            var relCsproj = Path.GetRelativePath(OutputDirectory!, p.Csproj).Replace('/', '\\');
            if (p.Platform != null)
                sb.AppendLine($"#if ({PlatformActiveSymbol[p.Platform]})");
            sb.AppendLine($"Project(\"{{{SdkProjectTypeGuid}}}\") = \"{p.DirName}\", \"{relCsproj}\", \"{{{p.InstanceId.ToString().ToUpperInvariant()}}}\"");
            sb.AppendLine("EndProject");
            if (p.Platform != null)
                sb.AppendLine("#endif");
        }
        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var p in ordered)
        {
            if (p.Platform != null)
                sb.AppendLine($"#if ({PlatformActiveSymbol[p.Platform]})");
            var idStr = p.InstanceId.ToString().ToUpperInvariant();
            sb.AppendLine($"\t\t{{{idStr}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{idStr}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{idStr}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"\t\t{{{idStr}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            if (p.Platform != null)
                sb.AppendLine("#endif");
        }
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        sb.AppendLine("\t\tHideSolutionNode = FALSE");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
        sb.AppendLine($"\t\tSolutionGuid = {{{Guid.NewGuid().ToString().ToUpperInvariant()}}}");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");

        var slnPath = Path.Combine(OutputDirectory!, "MyTemplate.sln");
        File.WriteAllText(slnPath, sb.ToString());
        logger.Info($"Generated MyTemplate.sln with {ordered.Count} project(s)");
    }

    /// <summary>
    /// Removes <c>obj/</c> and <c>bin/</c> dirs anywhere under the staged tree so stale build
    /// artifacts don't get walked by the rename pass and don't ship in the final template.
    /// </summary>
    private void CleanBuildArtifacts(ILogger logger)
    {
        var removed = 0;
        foreach (var dir in Directory.EnumerateDirectories(OutputDirectory!, "*", SearchOption.AllDirectories).ToList())
        {
            var name = Path.GetFileName(dir);
            if ((name.Equals("obj", StringComparison.OrdinalIgnoreCase) || name.Equals("bin", StringComparison.OrdinalIgnoreCase))
                && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
                removed++;
            }
        }
        if (removed > 0)
            logger.Info($"Removed {removed} obj/bin build artifact dir(s) from staged tree");
    }

    /// <summary>
    /// Replaces the sample's original literal name with <c>MyTemplate</c> across the staged
    /// tree: text file contents (word-boundary match so prefixed identifiers aren't mangled),
    /// file names, and directory names. Auto-detected from the first <c>.sdpkg</c>'s <c>Name</c>
    /// field if <see cref="SourceName"/> is unset.
    /// </summary>
    private void RenameSourceName(ILogger logger)
    {
        var sourceName = SourceName ?? DetectSourceNameFromSdpkg(logger);
        if (string.IsNullOrEmpty(sourceName))
        {
            logger.Info("No source name detected; skipping rename pass");
            return;
        }
        if (sourceName == "MyTemplate")
        {
            logger.Info("Detected source name is already 'MyTemplate'; skipping rename pass");
            return;
        }

        // Word-boundary regex with optional Stride exec-project suffix lookahead. Matches:
        //   - SampleName             (followed by non-word — covered by \b in lookahead)
        //   - SampleNameApp          (App suffix preserved)
        //   - SampleNameAppDelegate  (iOS pattern)
        //   - SampleNameActivity     (Android pattern)
        // The lookahead means only "SampleName" is consumed; the suffix stays intact, so
        // CSharpBeginnerApp.cs → MyTemplateApp.cs (then sourceName substitutes at instantiation).
        // CSharpBeginnerExtraFoo stays untouched because no boundary follows "SampleName" there.
        // Escape sourceName in case it contains regex-special chars.
        var pattern = new Regex($@"\b{Regex.Escape(sourceName)}(?=(App|AppDelegate|Activity)?\b)", RegexOptions.Compiled);

        // Rewrite file contents first (paths still valid during this pass).
        var filesRewritten = 0;
        foreach (var path in EnumerateTextFiles(OutputDirectory!))
        {
            var content = File.ReadAllText(path);
            var rewritten = pattern.Replace(content, "MyTemplate");
            if (!ReferenceEquals(rewritten, content) && rewritten != content)
            {
                File.WriteAllText(path, rewritten);
                filesRewritten++;
            }
        }

        // Rename files. Collect first, mutate after — modifying a dir while enumerating it is
        // implementation-defined.
        var fileRenames = new List<(string from, string to)>();
        foreach (var path in Directory.EnumerateFiles(OutputDirectory!, "*", SearchOption.AllDirectories))
        {
            var basename = Path.GetFileName(path);
            var newBasename = pattern.Replace(basename, "MyTemplate");
            if (newBasename != basename)
                fileRenames.Add((path, Path.Combine(Path.GetDirectoryName(path)!, newBasename)));
        }
        foreach (var (from, to) in fileRenames)
            File.Move(from, to, overwrite: true);

        // Rename directories bottom-up so parent renames don't invalidate child paths mid-walk.
        var dirRenames = new List<(string from, string to)>();
        foreach (var dir in Directory.EnumerateDirectories(OutputDirectory!, "*", SearchOption.AllDirectories))
        {
            var basename = Path.GetFileName(dir);
            var newBasename = pattern.Replace(basename, "MyTemplate");
            if (newBasename != basename)
                dirRenames.Add((dir, Path.Combine(Path.GetDirectoryName(dir)!, newBasename)));
        }
        // Sort by descending path depth so deepest dirs rename first.
        dirRenames.Sort((a, b) => b.from.Length.CompareTo(a.from.Length));
        foreach (var (from, to) in dirRenames)
            Directory.Move(from, to);

        logger.Info($"Renamed '{sourceName}' → 'MyTemplate' in {filesRewritten} file(s), {fileRenames.Count} file name(s), {dirRenames.Count} dir name(s)");
    }

    /// <summary>
    /// Reads the first <c>.sdpkg</c>'s <c>Name:</c> field and strips a trailing <c>.Game</c> /
    /// <c>.Windows</c> / <c>.Linux</c> / <c>.macOS</c> / <c>.iOS</c> / <c>.Android</c> suffix.
    /// Stride convention is <c>SampleName.Game</c> for the library package; the base name is
    /// what gets referenced in code namespaces.
    /// </summary>
    private string? DetectSourceNameFromSdpkg(ILogger logger)
    {
        var sdpkg = Directory.EnumerateFiles(OutputDirectory!, "*.sdpkg", SearchOption.AllDirectories).FirstOrDefault();
        if (sdpkg == null)
            return null;

        var nameLine = File.ReadLines(sdpkg)
            .FirstOrDefault(l => l.TrimStart().StartsWith("Name:", StringComparison.Ordinal));
        if (nameLine == null)
            return null;

        var name = nameLine.Substring(nameLine.IndexOf(':') + 1).Trim();
        foreach (var suffix in new[] { ".Game", ".Windows", ".Linux", ".macOS", ".iOS", ".Android" })
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name.Substring(0, name.Length - suffix.Length);
        }
        return name;
    }

    /// <summary>
    /// Asset type tags (the YAML <c>!Foo</c> first-line marker) that are always treated as roots,
    /// mirroring the engine's <c>[AssetDescription(AlwaysMarkAsRoot = true)]</c>-decorated types
    /// plus the canonical scene/prefab/compositor entry points that usually anchor a sample.
    /// Hardcoded so the dumb-scan pruner doesn't need an engine ProjectReference to query
    /// <see cref="AssetRegistry"/> at runtime.
    /// </summary>
    private static readonly HashSet<string> AlwaysRootAssetTypeTags = new(StringComparer.Ordinal)
    {
        "GameSettingsAsset",       // [AssetDescription(AlwaysMarkAsRoot = true)]
        "EffectShader",            // [AssetDescription(AlwaysMarkAsRoot = true)]
        "EffectLibrary",           // [AssetDescription(AlwaysMarkAsRoot = true)] (EffectLogAsset)
        "EffectCompositorAsset",   // [AssetDescription(AlwaysMarkAsRoot = true)]
        "ScriptSourceFileAsset",   // [AssetDescription(AlwaysMarkAsRoot = true)] (not normally a .sd file)
    };

    /// <summary>Matches a top-level <c>Id: &lt;dashed-guid&gt;</c> field (no indent).</summary>
    private static readonly Regex TopLevelIdRegex = new(
        @"^Id:\s+([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Matches a Stride asset reference in <c>&lt;guid&gt;:&lt;location&gt;</c> form. Location can
    /// contain letters, digits, <c>/</c>, <c>.</c>, <c>_</c>, <c>-</c>, and spaces; we stop at
    /// whitespace, comma, or YAML container closers so we don't accidentally swallow trailing
    /// punctuation. The <c>ref!! &lt;guid&gt;</c> intra-asset object reference form has no
    /// <c>:location</c> suffix and therefore can't match.
    /// </summary>
    private static readonly Regex AssetRefRegex = new(
        @"\b([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}):[^\s,}\]]+",
        RegexOptions.Compiled);

    /// <summary>
    /// Pure-text asset pruner: walks every <c>*.sd*</c> file under the staged tree (anything with
    /// a leading <c>!Type</c> YAML tag and top-level <c>Id:</c>), parses outgoing
    /// <c>&lt;guid&gt;:&lt;location&gt;</c> references with a regex, BFS-marks reachable from
    /// RootAssets + always-root types, deletes the unmarked. No engine assemblies, no MSBuild
    /// project resolution. False negatives (pruning a still-referenced asset) are bounded because
    /// Stride YAML uses exactly two cross-asset reference forms (the <c>&lt;guid&gt;:&lt;location&gt;</c>
    /// asset ref and the intra-file <c>ref!! &lt;guid&gt;</c>); only the first is cross-asset and
    /// both <c>BasePartAsset:</c>-style derived refs use the same syntax.
    /// </summary>
    private void DumbPruneUnreachableAssets(ILogger logger)
    {
        var sdpkgPaths = Directory.EnumerateFiles(OutputDirectory!, "*.sdpkg", SearchOption.AllDirectories).ToList();
        var rootIds = new HashSet<Guid>();
        foreach (var sdpkg in sdpkgPaths)
            CollectRootAssets(sdpkg, rootIds);

        // Discover every asset file: any *.sd* file other than .sdpkg that opens with a !Type YAML
        // tag and has a top-level Id field. Filter on extension prefix to avoid reading binaries
        // (and ignore .sdpkg itself, which is the package manifest, not an asset).
        var assetFiles = new Dictionary<Guid, string>();
        var assetTypes = new Dictionary<Guid, string>();
        var outgoing = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var path in Directory.EnumerateFiles(OutputDirectory!, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(path);
            if (ext.Length < 3 || !ext.StartsWith(".sd", StringComparison.OrdinalIgnoreCase))
                continue;
            if (ext.Equals(".sdpkg", StringComparison.OrdinalIgnoreCase))
                continue;
            if (ext.Equals(".sdtpl", StringComparison.OrdinalIgnoreCase))
                continue;

            string content;
            try { content = File.ReadAllText(path); }
            catch { continue; }

            // Type tag: very first line, must start with !.
            var newlineIdx = content.IndexOf('\n');
            if (newlineIdx <= 1 || content[0] != '!')
                continue;
            var typeTag = content.Substring(1, newlineIdx - 1).Trim();

            var idMatch = TopLevelIdRegex.Match(content);
            if (!idMatch.Success)
                continue;
            var selfId = Guid.Parse(idMatch.Groups[1].Value);

            assetFiles[selfId] = path;
            assetTypes[selfId] = typeTag;
            if (AlwaysRootAssetTypeTags.Contains(typeTag))
                rootIds.Add(selfId);

            var refs = new HashSet<Guid>();
            foreach (Match m in AssetRefRegex.Matches(content))
            {
                if (Guid.TryParse(m.Groups[1].Value, out var refId) && refId != selfId)
                    refs.Add(refId);
            }
            outgoing[selfId] = refs;
        }

        // BFS from roots, restricted to ids we actually have files for (refs may point to engine
        // assets that aren't in the staged tree — that's expected).
        var reachable = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        foreach (var id in rootIds)
        {
            if (assetFiles.ContainsKey(id) && reachable.Add(id))
                queue.Enqueue(id);
        }
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!outgoing.TryGetValue(id, out var refs))
                continue;
            foreach (var refId in refs)
            {
                if (assetFiles.ContainsKey(refId) && reachable.Add(refId))
                    queue.Enqueue(refId);
            }
        }

        var deleted = 0;
        foreach (var (id, path) in assetFiles)
        {
            if (reachable.Contains(id))
                continue;
            File.Delete(path);
            deleted++;
        }
        if (deleted > 0)
            logger.Info($"Pruned {deleted} unreachable asset file(s) from staged tree");
    }

    /// <summary>
    /// Parses the <c>RootAssets:</c> section of a <c>.sdpkg</c> and adds each entry's leading GUID
    /// to <paramref name="rootIds"/>. Entries are YAML list items of form
    /// <c>- &lt;guid&gt;:&lt;location&gt;</c>; we stop on the next top-level key.
    /// </summary>
    private static void CollectRootAssets(string sdpkgPath, HashSet<Guid> rootIds)
    {
        var inSection = false;
        foreach (var rawLine in File.ReadLines(sdpkgPath))
        {
            if (!inSection)
            {
                if (rawLine.StartsWith("RootAssets:", StringComparison.Ordinal))
                    inSection = true;
                continue;
            }
            // End of section: any non-blank line that's not a list-item entry (indented dash).
            if (rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0]) && !rawLine.StartsWith("- ", StringComparison.Ordinal))
                break;
            var m = AssetRefRegex.Match(rawLine);
            if (m.Success && Guid.TryParse(m.Groups[1].Value, out var id))
                rootIds.Add(id);
        }
    }

    private static int CopyContentSubdir(string sourceProjectDir, string stagedRoot, string subdirName, ILogger logger)
    {
        var sourceSubdir = Path.Combine(sourceProjectDir, subdirName);
        if (!Directory.Exists(sourceSubdir))
            return 0;

        var destSubdir = Path.Combine(stagedRoot, subdirName);
        Directory.CreateDirectory(destSubdir);

        var copied = 0;
        foreach (var srcFile in Directory.EnumerateFiles(sourceSubdir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceSubdir, srcFile);
            var dest = Path.Combine(destSubdir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(srcFile, dest, overwrite: true);
            copied++;
        }
        return copied;
    }

    /// <summary>
    /// Injects a BasicCameraController script component into the Camera entity's Components dict
    /// of the staged MainScene.sdscene. The type/assembly is written as
    /// <c>!MyTemplate.BasicCameraController,MyTemplate</c>; <c>MyTemplate</c> is the
    /// <c>sourceName</c> declared in template.json, which the template engine substitutes with the
    /// user's <c>-n</c> value at instantiation. The component's <c>Id</c> uses a hardcoded GUID
    /// that will be caught by the subsequent GUID-placeholder pass and replaced per-instance; the
    /// 32-hex Components dict key stays literal (file-scoped uniqueness is all that's required).
    /// Skipped silently if no <c>MainScene.sdscene</c> exists (e.g. when preprocessing a sample
    /// that doesn't have a Camera entity).
    /// </summary>
    private void InjectCameraScript(ILogger logger)
    {
        // Inject into every MainScene variant — the dual-pass HDR/LDR orchestrator emits both
        // MainScene.sdscene (HDR) and MainScene.LDR.sdscene (LDR). Each variant has its own
        // Camera entity that needs the script wired up; template.json sources/modifiers picks
        // one variant at instantiation.
        foreach (var scenePath in Directory.EnumerateFiles(OutputDirectory!, "MainScene*.sdscene", SearchOption.AllDirectories))
            InjectCameraScriptInto(scenePath, logger);
    }

    // 32-hex form for the Components-dict key, dashed form for the entity Id field. Caught and
    // replaced per-instance by the subsequent GUID-placeholder pass.
    private const string CameraScriptKeyHex = "ca3e7a5c012aff45b1d7e89014bd58cf";
    private const string CameraScriptIdDashed = "ca3e7a5c-012a-ff45-b1d7-e89014bd58cf";

    private static void InjectCameraScriptInto(string scenePath, ILogger logger)
    {
        // Normalize CRLF → LF so the LF-only markers below work uniformly on both Windows
        // (autocrlf checkouts) and Unix sources.
        var content = File.ReadAllText(scenePath).Replace("\r\n", "\n");

        // Find "Name: Camera" at the canonical entity-field indent (16 spaces) inside Parts.
        const string nameMarker = "\n                Name: Camera\n";
        var nameIdx = content.IndexOf(nameMarker, StringComparison.Ordinal);
        if (nameIdx < 0)
        {
            logger.Warning($"{Path.GetFileName(scenePath)} has no Camera entity at expected indent; skipping camera-script injection");
            return;
        }

        // The Camera's Components block runs from after Name: through to the next entity in Parts
        // (or end of file if Camera is last).
        const string nextEntityMarker = "\n        -   Entity:";
        var nextEntityIdx = content.IndexOf(nextEntityMarker, nameIdx + nameMarker.Length, StringComparison.Ordinal);
        var insertPoint = nextEntityIdx > 0 ? nextEntityIdx : content.Length;

        // Components-dict entries are at indent 20, sub-fields at indent 24.
        var injection =
            $"\n                    {CameraScriptKeyHex}: !MyTemplate.BasicCameraController,MyTemplate"
            + $"\n                        Id: {CameraScriptIdDashed}";

        var rewritten = content.Substring(0, insertPoint) + injection + content.Substring(insertPoint);
        File.WriteAllText(scenePath, rewritten);
        logger.Info($"Injected BasicCameraController placeholder into {Path.GetFileName(scenePath)}");
    }

    private static IEnumerable<string> EnumerateTextFiles(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsTextFile(path))
                yield return path;
        }
    }

    /// <summary>
    /// Returns a GUID-shaped placeholder string for the given 1-based index, e.g.
    /// <c>00000000-0000-0000-0000-000000000001</c>. Guaranteed parseable as a Guid; matches the
    /// dashed format the engine YAML uses.
    /// </summary>
    private static string MakePlaceholder(int index) =>
        $"00000000-0000-0000-0000-{index.ToString("x12", CultureInfo.InvariantCulture)}";

    /// <summary>Minimal escaping for embedding a string into a JSON string literal.</summary>
    private static string JsonEscape(string s) => s
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r")
        .Replace("\t", "\\t");

    private void EmitTemplateJson()
    {
        var configDir = Path.Combine(OutputDirectory!, ".template.config");
        Directory.CreateDirectory(configDir);

        var shortName = TemplateName ?? "stride-template";
        // Identity stable across reinstalls — prefer the .sdtpl Id (UI state survives engine
        // version bumps) and fall back to a name-derived literal for templates without an
        // explicit Id.
        var identity = Sdtpl?.Id?.ToString() ?? $"Stride.Templates.{shortName}";
        var displayName = JsonEscape(Sdtpl?.Name ?? shortName);
        var description = JsonEscape(Sdtpl?.Description ?? string.Empty);
        var defaultName = JsonEscape(Sdtpl?.DefaultOutputName ?? "MyGame");
        // Classifications: split "Samples/Graphics" into ["Samples", "Graphics"]; falls back to
        // a flat ["Stride", "Game"] tag when no Group is declared.
        var classifications = Sdtpl?.Group is { Length: > 0 } g
            ? string.Join(", ", g.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(p => $"\"{JsonEscape(p.Trim())}\""))
            : "\"Stride\", \"Game\"";

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"$schema\": \"http://json.schemastore.org/template\",");
        sb.AppendLine("  \"author\": \"Stride\",");
        sb.AppendLine($"  \"classifications\": [{classifications}],");
        sb.AppendLine($"  \"identity\": \"{identity}\",");
        sb.AppendLine($"  \"name\": \"{displayName}\",");
        sb.AppendLine($"  \"shortName\": \"{shortName}\",");
        if (description.Length > 0)
            sb.AppendLine($"  \"description\": \"{description}\",");
        sb.AppendLine($"  \"defaultName\": \"{defaultName}\",");
        sb.AppendLine("  \"tags\": { \"language\": \"C#\", \"type\": \"project\" },");
        sb.AppendLine("  \"preferNameDirectory\": true,");
        // sourceName: every literal occurrence of "MyTemplate" in template content gets replaced
        // with the user's -n value at instantiation. The preprocessor injects "MyTemplate" into
        // the Camera entity's script-component type/assembly ref; users get a working camera
        // script bound to their own namespace.
        sb.AppendLine("  \"sourceName\": \"MyTemplate\",");
        sb.AppendLine("  \"symbols\": {");

        EmitBaseParameterSymbols(sb);
        if (Sdtpl?.HasParameter("HDR") == true)             EmitHDRSymbol(sb);
        if (Sdtpl?.HasParameter("graphicsProfile") == true) EmitGraphicsProfileSymbol(sb);
        if (Sdtpl?.HasParameter("orientation") == true)     EmitOrientationSymbol(sb);
        // EffectiveHDR depends on both — only meaningful when both are opted-in.
        if (Sdtpl?.HasParameter("HDR") == true && Sdtpl?.HasParameter("graphicsProfile") == true)
            EmitEffectiveHDRComputed(sb);
        EmitGuidSymbols(sb);

        sb.AppendLine("  },");
        EmitSourcesModifiers(sb);
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(configDir, "template.json"), sb.ToString());
    }

    /// <summary>
    /// Always-emitted: Platforms multichoice + the env-bind / computed-bool chain that turns the
    /// "Host" sentinel into per-platform Active bools used by sources/modifiers. Every template
    /// has per-platform exec projects, so this set applies universally.
    /// </summary>
    private static void EmitBaseParameterSymbols(StringBuilder sb)
    {
        sb.AppendLine("""
                "platforms": {
                  "type": "parameter",
                  "datatype": "choice",
                  "allowMultipleValues": true,
                  "enableQuotelessLiterals": true,
                  "defaultValue": "host",
                  "choices": [
                    { "choice": "host",    "description": "Detect host OS automatically" },
                    { "choice": "windows", "description": "Windows x64 desktop" },
                    { "choice": "linux",   "description": "Linux x64 desktop" },
                    { "choice": "macos",   "description": "macOS arm64/x64" },
                    { "choice": "ios",     "description": "iOS arm64" },
                    { "choice": "android", "description": "Android arm64/x64" }
                  ]
                },
                "envOS":     { "type": "bind", "binding": "env:OS",     "defaultValue": "" },
                "envOSTYPE": { "type": "bind", "binding": "env:OSTYPE", "defaultValue": "" },
                "envHOME":   { "type": "bind", "binding": "env:HOME",   "defaultValue": "" },
                "IsMacOS":   { "type": "computed", "value": "(envOSTYPE == \"darwin\")" },
                "IsLinux":   { "type": "computed", "value": "(!IsMacOS && envHOME != \"\" && envOS != \"Windows_NT\")" },
                "IsWindows": { "type": "computed", "value": "(!IsMacOS && !IsLinux)" },
                "WindowsActive": { "type": "computed", "value": "(platforms == \"windows\") || ((platforms == \"host\") && IsWindows)" },
                "LinuxActive":   { "type": "computed", "value": "(platforms == \"linux\")   || ((platforms == \"host\") && IsLinux)"   },
                "MacOSActive":   { "type": "computed", "value": "(platforms == \"macos\")   || ((platforms == \"host\") && IsMacOS)"   },
                "iOsActive":     { "type": "computed", "value": "(platforms == \"ios\")"     },
                "AndroidActive": { "type": "computed", "value": "(platforms == \"android\")" },
        """);
    }

    /// <summary>Per-template opt-in HDR bool parameter.</summary>
    private static void EmitHDRSymbol(StringBuilder sb) => sb.AppendLine("""
                "HDR": {
                  "type": "parameter",
                  "datatype": "bool",
                  "description": "Use HDR rendering pipeline (requires graphicsProfile >= 10.0)",
                  "defaultValue": "true"
                },
    """);

    /// <summary>Per-template opt-in GraphicsProfile choice.</summary>
    private static void EmitGraphicsProfileSymbol(StringBuilder sb) => sb.AppendLine("""
                "graphicsProfile": {
                  "type": "parameter",
                  "datatype": "choice",
                  "description": "Graphics feature level",
                  "defaultValue": "10.0",
                  "choices": [
                    { "choice": "9.0",  "description": "Shader Model 3 (D3D9, no HDR)" },
                    { "choice": "10.0", "description": "Shader Model 4 (D3D10)" },
                    { "choice": "11.0", "description": "Shader Model 5 (D3D11)" }
                  ]
                },
    """);

    /// <summary>Per-template opt-in mobile display Orientation choice.</summary>
    private static void EmitOrientationSymbol(StringBuilder sb) => sb.AppendLine("""
                "orientation": {
                  "type": "parameter",
                  "datatype": "choice",
                  "description": "Display orientation (mobile)",
                  "defaultValue": "Default",
                  "choices": [
                    { "choice": "Default"        },
                    { "choice": "LandscapeLeft"  },
                    { "choice": "LandscapeRight" },
                    { "choice": "Portrait"       }
                  ]
                },
    """);

    /// <summary>Computed bool used by HDR/LDR scene-variant sources/modifiers. Requires both HDR + GraphicsProfile.</summary>
    private static void EmitEffectiveHDRComputed(StringBuilder sb) => sb.AppendLine("""
                "EffectiveHDR":  { "type": "computed", "value": "(HDR && (graphicsProfile != \"9.0\"))" },
    """);

    /// <summary>
    /// Emits the <c>sources/modifiers</c> block that excludes per-platform exec-project
    /// directories when their corresponding <c>*Active</c> computed bool is false. Each modifier
    /// fires independently, so any combination of <c>--Platforms</c> selections produces a tree
    /// containing exactly the chosen per-platform projects (plus the platform-agnostic library
    /// project with its nested <c>Assets/</c> / <c>Resources/</c>). Per-platform <c>.sln</c>
    /// Project blocks are stripped at instantiation by the conditional <c>#if</c> markers
    /// processed via SpecialCustomOperations.
    /// </summary>
    private void EmitSourcesModifiers(StringBuilder sb)
    {
        sb.AppendLine("""
          "sources": [
            {
              "modifiers": [
                { "condition": "(!WindowsActive)", "exclude": [ "MyTemplate.Windows/**" ] },
                { "condition": "(!LinuxActive)",   "exclude": [ "MyTemplate.Linux/**"   ] },
                { "condition": "(!MacOSActive)",   "exclude": [ "MyTemplate.macOS/**"   ] },
                { "condition": "(!iOsActive)",     "exclude": [ "MyTemplate.iOS/**"     ] },
                { "condition": "(!AndroidActive)", "exclude": [ "MyTemplate.Android/**" ] }
        """);
        if (Sdtpl?.HasParameter("HDR") == true && Sdtpl?.HasParameter("GraphicsProfile") == true)
        {
            // HDR/LDR scene-variant selection. References EffectiveHDR computed bool; only
            // emitted when both HDR + GraphicsProfile are opted in (EffectiveHDR depends on
            // both).
            sb.AppendLine("""
                ,
                { "condition": "(EffectiveHDR)",  "exclude": [ "**/*.LDR.*" ] },
                { "condition": "(!EffectiveHDR)",
                  "exclude": [
                    "MyTemplate/Assets/Sphere Material.sdmat",
                    "MyTemplate/Assets/Ground Material.sdmat",
                    "MyTemplate/Assets/Sphere.sdpromodel",
                    "MyTemplate/Assets/Ground.sdpromodel",
                    "MyTemplate/Assets/Skybox texture.sdtex",
                    "MyTemplate/Assets/Skybox.sdsky",
                    "MyTemplate/Assets/MainScene.sdscene",
                    "MyTemplate/Assets/GameSettings.sdgamesettings",
                    "MyTemplate/Assets/GraphicsCompositor.sdgfxcomp"
                  ],
                  "rename": {
                    "MyTemplate/Assets/Sphere Material.LDR.sdmat":            "MyTemplate/Assets/Sphere Material.sdmat",
                    "MyTemplate/Assets/Ground Material.LDR.sdmat":            "MyTemplate/Assets/Ground Material.sdmat",
                    "MyTemplate/Assets/Sphere.LDR.sdpromodel":                "MyTemplate/Assets/Sphere.sdpromodel",
                    "MyTemplate/Assets/Ground.LDR.sdpromodel":                "MyTemplate/Assets/Ground.sdpromodel",
                    "MyTemplate/Assets/Skybox texture.LDR.sdtex":             "MyTemplate/Assets/Skybox texture.sdtex",
                    "MyTemplate/Assets/Skybox.LDR.sdsky":                     "MyTemplate/Assets/Skybox.sdsky",
                    "MyTemplate/Assets/MainScene.LDR.sdscene":                "MyTemplate/Assets/MainScene.sdscene",
                    "MyTemplate/Assets/GameSettings.LDR.sdgamesettings":      "MyTemplate/Assets/GameSettings.sdgamesettings",
                    "MyTemplate/Assets/GraphicsCompositor.LDR.sdgfxcomp":     "MyTemplate/Assets/GraphicsCompositor.sdgfxcomp"
                  }
                }
            """);
        }
        sb.AppendLine("""
              ]
            }
          ],
          "SpecialCustomOperations": {
            "**/*.sln": {
              "operations": [
                {
                  "type": "conditional",
                  "configuration": {
                    "if":     [ "#if" ],
                    "else":   [ "#else" ],
                    "elseif": [ "#elseif" ],
                    "endif":  [ "#endif" ],
                    "trim":      true,
                    "wholeLine": true,
                    "evaluator": "C++"
                  }
                }
              ]
            }
          }
        """);
    }

    private void EmitGuidSymbols(StringBuilder sb)
    {
        // One generated/guid symbol per GUID in the map. Each gets a fresh GUID at instantiation,
        // applied consistently everywhere the placeholder appears (the template engine does a
        // literal string replace).
        var entries = new List<KeyValuePair<int, string>>();
        foreach (var kv in GuidMap)
            entries.Add(new KeyValuePair<int, string>(kv.Value, MakePlaceholder(kv.Value)));
        entries.Sort((a, b) => a.Key.CompareTo(b.Key));
        for (var i = 0; i < entries.Count; i++)
        {
            var idx = entries[i].Key;
            var placeholder = entries[i].Value;
            var trailingComma = i < entries.Count - 1 ? "," : "";
            sb.AppendLine($"    \"g{idx}\": {{ \"type\": \"generated\", \"generator\": \"guid\", \"replaces\": \"{placeholder}\", \"parameters\": {{ \"format\": \"D\" }} }}{trailingComma}");
        }
    }

    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        }
        foreach (var subdir in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(subdir, Path.Combine(dest, Path.GetFileName(subdir)));
        }
    }
}
