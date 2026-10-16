// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsers.Tests.Corpus;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing.Upgrade;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The shader source upgrade (<see cref="ShaderUpgrader"/>): the engine and samples are upgraded by it, and it gives the same
/// result as the steps done by hand in the past.
/// </summary>
public class ShaderUpgradeTests
{
    static string RepositoryRoot => ShaderCorpus.RepositoryRoot;

    // The engine shaders are one project; each sample is a project using them
    static readonly string[] EngineRoots = ["sources/engine", "sources/plugins", "sources/editor"];

    static string EngineShaderClassesFile => Path.Combine(RepositoryRoot, "sources", "shaders", "Stride.Shaders.Parsers", "Upgrade", "EngineShaders.txt");

    // Written back with their byte order mark
    static readonly HashSet<string> FilesWithBom = [];

    static Dictionary<string, string> ReadShaders(params string[] roots)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in roots)
        {
            var rootPath = Path.Combine(RepositoryRoot, root);
            if (!Directory.Exists(rootPath))
                continue;
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.sdsl", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(RepositoryRoot, file).Replace('\\', '/');
                if (relative.Contains("/bin/") || relative.Contains("/obj/"))
                    continue;
                if (File.ReadAllBytes(file).AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
                    FilesWithBom.Add(relative);
                files[relative] = File.ReadAllText(file);
            }
        }
        return files;
    }

    // samples/<category>/<sample>
    static IEnumerable<string> SampleRoots() => Directory.EnumerateDirectories(Path.Combine(RepositoryRoot, "samples"))
        .SelectMany(Directory.EnumerateDirectories)
        .Select(x => Path.GetRelativePath(RepositoryRoot, x).Replace('\\', '/'))
        .Where(x => ReadShaders(x).Count > 0)
        .Order(StringComparer.Ordinal);

    static void Write(IReadOnlyDictionary<string, string> before, ShaderUpgradeResult result)
    {
        foreach (var (path, text) in result.Files)
        {
            if (before[path] != text)
            {
                File.WriteAllText(Path.Combine(RepositoryRoot, path), text, new System.Text.UTF8Encoding(FilesWithBom.Contains(path)));
                Console.WriteLine($"Upgraded {path}");
            }
        }
        foreach (var note in result.Notes)
            Console.WriteLine($"Note: {note}");
    }

    /// <summary>
    /// Upgrades the engine, editor and sample shaders in place, and rewrites <c>EngineShaders.txt</c>.
    /// </summary>
    [Fact(Explicit = true)]
    public void UpgradeRepository()
    {
        var engine = ReadShaders(EngineRoots);
        var upgraded = ShaderUpgrader.Upgrade(engine, engine: null);
        Write(engine, upgraded);

        var engineClasses = EngineShaderClasses.Write(upgraded.Files);
        File.WriteAllText(EngineShaderClassesFile, engineClasses);

        foreach (var sampleRoot in SampleRoots())
        {
            var sample = ReadShaders(sampleRoot);
            Write(sample, ShaderUpgrader.Upgrade(sample, EngineShaderClasses.Parse(engineClasses)));
        }
    }

    /// <summary>
    /// Rewrites <c>EngineShaders.txt</c> from the engine shaders.
    /// </summary>
    [Fact(Explicit = true)]
    public void UpdateEngineShaderClasses()
    {
        File.WriteAllText(EngineShaderClassesFile, EngineShaderClasses.Write(ReadShaders(EngineRoots)));
    }

    // The list used to upgrade projects describes today's engine
    [Fact]
    public void EngineShaderClassesAreCurrent()
    {
        var expected = EngineShaderClasses.Write(ReadShaders(EngineRoots));
        Assert.True(File.ReadAllText(EngineShaderClassesFile).ReplaceLineEndings("\n") == expected,
            $"{EngineShaderClassesFile} doesn't match the engine shaders: run the explicit test {nameof(UpdateEngineShaderClasses)}");
    }

    static string FixturesDirectory => Path.Combine(RepositoryRoot, "tests", "shader-upgrade");

    // tests/shader-upgrade/<step>/<case>: user-style shaders in the old syntax (before/) and their upgrade (after/), with the
    // sources of a library they use (dependencies/, already upgraded) when there is one. A case with a solution.txt has several projects,
    // which the project upgrade goes through one by one (Stride.Assets.Tests, TestShaderSourceUpgrade)
    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (var directory in Directory.EnumerateDirectories(FixturesDirectory).SelectMany(Directory.EnumerateDirectories)
            .Where(x => !File.Exists(Path.Combine(x, "solution.txt"))).Order(StringComparer.Ordinal))
            data.Add(Path.GetRelativePath(FixturesDirectory, directory).Replace('\\', '/'));
        return data;
    }

    // tests/shader-upgrade/dependencies/<case>/solution.txt: the projects of a solution and the plugins they use (TestShaderSourceUpgrade)
    public static TheoryData<string> SolutionFixtures()
    {
        var data = new TheoryData<string>();
        foreach (var directory in Directory.EnumerateDirectories(Path.Combine(FixturesDirectory, "dependencies")).Where(x => File.Exists(Path.Combine(x, "solution.txt"))).Order(StringComparer.Ordinal))
            data.Add(Path.GetFileName(directory));
        return data;
    }

    // The upgraded projects of a solution compile with the plugins and the projects they reference; a project the user didn't upgrade stays in
    // the old language
    [Theory]
    [MemberData(nameof(SolutionFixtures))]
    public void SolutionFixtureCompiles(string fixture)
    {
        var directory = Path.Combine(FixturesDirectory, "dependencies", fixture);
        var members = File.ReadAllLines(Path.Combine(directory, "solution.txt")).Where(x => x.Length > 0 && x[0] != '#').Select(x => x.Split(' ')).ToList();
        var plugins = members.Where(x => x[0] == "plugin").Select(x => PluginFolder(fixture, Path.Combine(directory, x[1]), x[2])).ToList();
        var projects = members.Where(x => x[0] != "plugin" && !(x.Length > 1 && x[1] == "refused")).Select(x => Path.Combine(directory, x[0], "after")).ToList();
        var loader = new ShaderLoader([.. projects, .. plugins, "./assets/Stride/SDSL"]);
        foreach (var file in projects.SelectMany(x => Directory.EnumerateFiles(x, "*.sdsl")))
            Assert.True(loader.LoadExternalBuffer(Path.GetFileNameWithoutExtension(file), [], out _, out _, out _), file);
    }

    // A plugin (<folder>) written for Stride <version>: one written before modules is compiled as the upgrade of a project that uses it reads it
    static string PluginFolder(string fixture, string folder, string version)
    {
        if (new System.Version(version) >= new System.Version(4, 5, 5))
            return folder;
        var upgraded = Path.Combine(Path.GetTempPath(), "stride-solution-fixtures", fixture, Path.GetFileName(folder));
        Directory.CreateDirectory(upgraded);
        var files = Directory.EnumerateFiles(folder, "*.sdsl").ToDictionary(Path.GetFileName, File.ReadAllText)!;
        foreach (var (name, text) in ShaderUpgrader.Upgrade(files!, EngineShaderClasses.Current).Files)
            File.WriteAllText(Path.Combine(upgraded, name), text);
        return upgraded;
    }

    static Dictionary<string, string> ReadFixture(string fixture, string part)
    {
        var directory = Path.Combine(FixturesDirectory, fixture, part);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.sdsl").ToDictionary(Path.GetFileName, File.ReadAllText, StringComparer.Ordinal)!
            : [];
    }

    /// <summary>
    /// Writes the after/ folder of every fixture from its before/ folder (review the result before committing it).
    /// </summary>
    [Fact(Explicit = true)]
    public void UpdateFixtures()
    {
        foreach (var fixture in Fixtures().Select(x => (string)x.Data))
        {
            var result = ShaderUpgrader.Upgrade(ReadFixture(fixture, "before"), EngineShaderClasses.Current, dependencies: ReadFixture(fixture, "dependencies"));
            var directory = Path.Combine(FixturesDirectory, fixture, "after");
            Directory.CreateDirectory(directory);
            foreach (var (name, text) in result.Files)
                File.WriteAllText(Path.Combine(directory, name), text);
            foreach (var note in result.Notes)
                Console.WriteLine($"{fixture}: {note}");
        }
    }

    // The chain gives the expected sources, changes nothing when run again, and the result compiles
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void FixtureUpgrade(string fixture)
    {
        var expected = ReadFixture(fixture, "after");
        var dependencies = ReadFixture(fixture, "dependencies");
        var result = ShaderUpgrader.Upgrade(ReadFixture(fixture, "before"), EngineShaderClasses.Current, dependencies: dependencies);
        Assert.Equal(expected.Keys.Order(), result.Files.Keys.Order());
        foreach (var (name, text) in result.Files)
            Assert.Equal(expected[name].ReplaceLineEndings("\n"), text.ReplaceLineEndings("\n"));
        Assert.Empty(result.Notes.Where(x => x.Contains("dependency written for an older Stride version")));

        var again = ShaderUpgrader.Upgrade(expected, EngineShaderClasses.Current, dependencies: dependencies);
        Assert.Empty(again.Files.Where(x => expected[x.Key] != x.Value).Select(x => x.Key));

        var loader = new ShaderLoader(Path.Combine(FixturesDirectory, fixture, "after"), Path.Combine(FixturesDirectory, fixture, "dependencies"), "./assets/Stride/SDSL");
        foreach (var name in expected.Keys)
            Assert.True(loader.LoadExternalBuffer(Path.GetFileNameWithoutExtension(name), [], out _, out _, out _), name);
    }

    // A dependency is read, never changed; one written for an older version is read as if upgraded, with a note
    [Fact]
    public void DependencyIsReadNotChanged()
    {
        var dependencies = new Dictionary<string, string> { ["MyLibSettings.sdsl"] = "shader MyLibSettings { stage float Scale; };" };
        var files = new Dictionary<string, string> { ["MyUser.sdsl"] = "shader MyUser : MyLibSettings { float Get() { return Scale; } };" };
        var result = ShaderUpgrader.Upgrade(files, EngineShaderClasses.Current, dependencies: dependencies);
        Assert.Equal(["MyUser.sdsl"], result.Files.Keys);
        Assert.Contains(result.Notes, x => x.StartsWith("MyLibSettings.sdsl: dependency written for an older Stride version", StringComparison.Ordinal));
        // Not a module: only its own upgrade decides that
        Assert.Contains(": MyLibSettings", result.Files["MyUser.sdsl"]);
    }

    // The upgraded feature works in a composition: its helpers are module code like the code calling them
    [Fact]
    public void UpgradedFeatureHelpersMixInComposition()
    {
        var loader = new ShaderLoader(Path.Combine(FixturesDirectory, "module-helpers", "feature-helpers", "after"), "./assets/Stride/SDSL");
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("MyDisplacementRoot") },
            Compositions = { ["Surface"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("MyStormDisplacement") } } },
        };
        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // The upgraded node reads the compute thread streams in a composition (ComputeShaderBase is [Root], SDSL0119 there)
    [Fact]
    public void UpgradedComputeStreamsMixInComposition()
    {
        var loader = new ShaderLoader(Path.Combine(FixturesDirectory, "module-units", "compute-streams", "after"), "./assets/Stride/SDSL");
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("MyComputeRoot") },
            Compositions = { ["Color"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("MyThreadColor") } } },
        };
        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // The upgraded node calls its parent's helper without `base.` (a base call to a module method fails in a composition, SDSL0208)
    [Fact]
    public void UpgradedBaseHelperCallMixesInComposition()
    {
        var loader = new ShaderLoader(Path.Combine(FixturesDirectory, "module-units", "base-helper", "after"), "./assets/Stride/SDSL");
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("MyBrightRoot") },
            Compositions = { ["Color"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("MyBrightColor") } } },
        };
        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    // Master before the `module` work (A13a): engine shaders still written with `stage`
    const string EngineBeforeModules = "195392b6d9";

    // Edits made by hand since then, not by the upgrade: file → the text the chain gives, and the text today
    static readonly Dictionary<string, (string Upgraded, string Today)[]> ManualEdits = new()
    {
        // The upgrade makes the light group a parent (it was used by name); the comment and the shorter access were written by hand
        ["sources/engine/Stride.Rendering/Rendering/Shadows/ShadowMapReceiverPointCubeMap.sdsl"] =
        [
            ("    /// Selects the shadow map and computes the shadow factor.\n", "    /// Selects the shadow map and computes the shadow factor.\n    /// Mixed into the point light group, whose light positions it reads.\n"),
            ("LightPointGroup<TLightCount>.Lights[lightIndex]", "Lights[lightIndex]"),
        ],
        // The thread streams moved to ComputeShaderStreams, which shaders used in compositions import ([Root] is checked there)
        ["sources/engine/Stride.Rendering/Rendering/ComputeEffect/ComputeShaderBase.sdsl"] =
        [
            ("/// ThreadNumberZ: Macro - number of threads on the Z axis.\n", "/// ThreadNumberZ: Macro - number of threads on the Z axis.\n/// A shader used in a composition reads the thread streams by importing <see cref=\"ComputeShaderStreams\"/>.\n"),
            ("{\n    stream uint3 GroupId : SV_GroupID;\n    stream uint3 DispatchThreadId : SV_DispatchThreadID;\n    stream uint3 GroupThreadId : SV_GroupThreadID;\n"
                + "    stream uint  GroupIndex : SV_GroupIndex;\n\n    stream uint3 ThreadGroupCount;\n    stream uint  ThreadCountPerGroup;\n"
                + "    stream uint  ThreadGroupIndex;\n    \n    stream int ThreadCountX;\n    stream int ThreadCountY;\n    stream int ThreadCountZ;\n\n",
                "{\n    import ComputeShaderStreams;\n\n"),
            ("    }\n\n    bool IsFirstThreadOfGroup()\n    {\n        return streams.GroupThreadId.x == 0 && streams.GroupThreadId.y == 0 && streams.GroupThreadId.z == 0;\n    }\n};", "    }\n};"),
        ],
    };

    // The engine shaders of that commit, upgraded, are today's engine shaders apart from the edits made by hand
    [Fact]
    public void EngineBeforeModulesUpgradesToToday()
    {
        var old = ReadShadersAt(EngineBeforeModules, EngineRoots);
        if (old == null)
            Assert.Skip($"Commit {EngineBeforeModules} is not available (shallow clone?)");

        var today = ReadShaders(EngineRoots);
        var upgraded = ShaderUpgrader.Upgrade(old, engine: null).Files;
        var differences = new List<string>();
        foreach (var path in today.Keys.Intersect(upgraded.Keys).Order(StringComparer.Ordinal))
        {
            var text = upgraded[path].ReplaceLineEndings("\n");
            foreach (var (upgradedText, todayText) in ManualEdits.GetValueOrDefault(path) ?? [])
                text = text.Replace(upgradedText, todayText);
            // [Root] and [Pure] are written by hand (the upgrade takes the engine's [Root] markers from EngineShaders.txt)
            var expected = System.Text.RegularExpressions.Regex.Replace(today[path].ReplaceLineEndings("\n"), @"^[ \t]*\[(Root|Pure)\]\n", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (text != expected)
                differences.Add(path);
        }
        Assert.True(differences.Count == 0, $"Upgraded from {EngineBeforeModules}, differs from today: {string.Join(", ", differences)}");
    }

    // The .sdsl files under the roots at a commit (null if git or the commit is not available)
    static Dictionary<string, string>? ReadShadersAt(string commit, string[] roots)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git", ["archive", "--format=zip", commit, "--", .. roots])
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo)!;
            var archive = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(archive);
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                return null;

            archive.Position = 0;
            using var zip = new System.IO.Compression.ZipArchive(archive);
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in zip.Entries)
            {
                if (!entry.FullName.EndsWith(".sdsl") || entry.FullName.Contains("/bin/") || entry.FullName.Contains("/obj/"))
                    continue;
                using var reader = new StreamReader(entry.Open());
                files[entry.FullName] = reader.ReadToEnd();
            }
            return files;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    // Shaders that need the macros of their effect to compile
    static readonly Dictionary<string, Stride.Shaders.ShaderMacro[]> ShaderMacros = new()
    {
        // Override methods taking 12 control points (InputControlPointCount defaults to 3)
        ["TessellationAE2"] = [new("InputControlPointCount", "12")],
        ["TessellationAE3"] = [new("InputControlPointCount", "12")],
        ["TessellationAE4"] = [new("InputControlPointCount", "12")],
    };

    // Shaders that don't compile alone, for a reason not related to the upgrade
    static readonly HashSet<string> KnownFailures =
    [
        // Casts a float4 to a struct, which the compiler doesn't support
        "LightTiling",
    ];

    // Every engine and sample shader compiles on its own, also those no known effect uses (the corpus doesn't see them), with no
    // warning (the corpus loader reports them as errors)
    [Fact]
    public void EveryShaderCompiles()
    {
        var loader = ShaderCorpus.CreateLoader();
        var failures = new List<string>();
        foreach (var (path, text) in ReadShaders([.. EngineRoots, "samples"]).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (KnownFailures.Contains(name))
                continue;
            try
            {
                if (!loader.LoadExternalBuffer(name, ShaderMacros.GetValueOrDefault(name) ?? [], out _, out _, out _))
                    failures.Add(path);
            }
            catch (Exception e)
            {
                failures.Add($"{path}: {e.Message.Split('\n')[0]}");
            }
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    // The engine and samples are upgraded already: the upgrade changes nothing
    [Fact]
    public void RepositoryIsUpgraded()
    {
        var engine = ReadShaders(EngineRoots);
        var result = ShaderUpgrader.Upgrade(engine, engine: null);
        Assert.Empty(result.Files.Where(x => engine[x.Key] != x.Value).Select(x => x.Key));

        foreach (var sampleRoot in SampleRoots())
        {
            var sample = ReadShaders(sampleRoot);
            var sampleResult = ShaderUpgrader.Upgrade(sample, EngineShaderClasses.Current);
            Assert.Empty(sampleResult.Files.Where(x => sample[x.Key] != x.Value).Select(x => x.Key));
        }
    }

    // Two files of the project declare one class (a stale copy): the upgrade can't tell which one the build uses, so it changes neither, and
    // says so, whatever the names of the files
    [Theory]
    [InlineData("MyHelpers - Copy.sdsl")]
    [InlineData("MyHelpers2.sdsl")]
    public void AClassDeclaredInTwoFilesIsLeftAlone(string copy)
    {
        const string helpers = "shader MyHelpers { float3 Tint(float3 color) { return color; } };\n";
        var files = new Dictionary<string, string>
        {
            ["MyHelpers.sdsl"] = helpers,
            [copy] = helpers,
            ["MyFx.sdsl"] = "shader MyFx : ImageEffectShader, MyHelpers { float3 Apply(float3 color) { return Tint(color); } };\n",
        };

        var result = ShaderUpgrader.Upgrade(files, EngineShaderClasses.Current);

        Assert.Equal(helpers, result.Files["MyHelpers.sdsl"]);
        Assert.Equal(helpers, result.Files[copy]);
        Assert.Contains("shader MyFx : MyHelpers { import ImageEffectShader;", result.Files["MyFx.sdsl"]);
        Assert.Contains(result.Notes, x => x.Contains("MyHelpers.sdsl", StringComparison.Ordinal) && x.Contains(copy, StringComparison.Ordinal)
            && x.Contains("class MyHelpers is declared in each of these files", StringComparison.Ordinal));
    }

    // A helper that module code calls becomes module, also when it reads a member with one value per instance, which a class used in
    // compositions can't do: a note says so
    [Fact]
    public void AModuleHelperThatReadsInstanceStateGetsANote()
    {
        var files = new Dictionary<string, string>
        {
            ["MyColor.sdsl"] = "shader MyColor : ComputeColor { float Value; stage float RunIt() { return Helper(); } float Helper() { return Value; } override float4 Compute() { return RunIt(); } };\n",
        };

        var result = ShaderUpgrader.Upgrade(files, EngineShaderClasses.Current);

        Assert.Contains("module float Helper()", result.Files["MyColor.sdsl"]);
        Assert.Contains(result.Notes, x => x.Contains("MyColor.Helper becomes module", StringComparison.Ordinal) && x.Contains("it reads Value", StringComparison.Ordinal));
    }

    // The keyword of a class declaration, in each form the parser takes it, and nothing else
    [Theory]
    [InlineData("class A { };", "shader A { };")]
    [InlineData("namespace N.M\n{\n    class A { };\n    class B : A { };\n}\n", "namespace N.M\n{\n    shader A { };\n    shader B : A { };\n}\n")]
    [InlineData("partial class A { };", "partial shader A { };")]
    [InlineData("internal partial class A { };", "internal partial shader A { };")]
    [InlineData("[Root]\nclass A { };", "[Root]\nshader A { };")]
    [InlineData("class A<float Scale> : B<Scale> { };", "shader A<float Scale> : B<Scale> { };")]
    // Not a declaration: a comment, a string, a name
    [InlineData("// class A is old\nshader A { };", null)]
    [InlineData("shader A { [Link(\"class A\")] float Value; };", null)]
    [InlineData("shader A { float myclass; float class2; };", null)]
    // An HLSL class inside a class
    [InlineData("shader A\n{\n    interface I { float F(); };\n    class C : I { float F() { return 1; } };\n};", null)]
    public void TheClassKeywordOfADeclarationBecomesShader(string text, string? expected)
    {
        var result = ShaderUpgrader.Upgrade(new Dictionary<string, string> { ["A.sdsl"] = text }, engine: null, lastStep: "class-to-shader");

        Assert.Equal(expected ?? text, result.Files["A.sdsl"]);
    }

    // base.X() ran the version of X mixed before the caller, and X() the last one: an override of X in a library makes them differ, so base. stays
    [Fact]
    public void TheBaseCallOfAModuleMethodThatALibraryOverridesStays()
    {
        var dependencies = new Dictionary<string, string>
        {
            ["LibHelpers.sdsl"] = "module LibHelpers { float3 Tone(float3 color) { return color; } };\n",
            ["LibWarm.sdsl"] = "[Root] shader LibWarm : ShadingBase { import LibHelpers; module override float3 Tone(float3 color) { return color * 2; } };\n",
        };
        var files = new Dictionary<string, string> { ["MyNode.sdsl"] = "shader MyNode : ComputeColor, LibHelpers { override float4 Compute() { return float4(base.Tone(1), 1); } };\n" };

        var result = ShaderUpgrader.Upgrade(files, EngineShaderClasses.Current, dependencies: dependencies);

        Assert.Contains("base.Tone(1)", result.Files["MyNode.sdsl"]);
        Assert.Contains(result.Notes, x => x.Contains("MyNode calls base.Tone() of the module LibHelpers, and Tone is overridden", StringComparison.Ordinal));
    }
}
