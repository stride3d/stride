// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Stride.SampleScreenshotRunner;

/// <summary>
/// Prepares the samples of a run before its first test: generates them from their template (with the upgrade) one at a
/// time, then builds them all in one build. Then gives each test the folder of its sample, and its part of the build.
/// </summary>
/// <remarks>
/// One build shares MSBuild and the compiler between the samples, which separate builds each start again. A test that
/// passes gives the folder to the next test of the same sample, which takes it as it is. The tests run one at a time, so
/// the state has no lock.
/// </remarks>
internal static class PreparedSamples
{
    // The samples of the run, in the order of their first test
    private static readonly List<string> planned = [];
    // The number of tests still to come for each sample
    private static readonly Dictionary<string, int> uses = new(StringComparer.OrdinalIgnoreCase);
    // The prepared folders a test gave back for the next test of the same sample
    private static readonly HashSet<string> kept = new(StringComparer.OrdinalIgnoreCase);
    // What the preparation gave for each sample no test took yet
    private static Dictionary<string, (SampleResult? Failure, SampleBuild? Build)>? prepared;

    /// <summary>Sets the sample of each coming test, in the order they run.</summary>
    internal static void Plan(IEnumerable<string> sampleNames)
    {
        planned.Clear();
        uses.Clear();
        kept.Clear();
        prepared = null;
        foreach (var name in sampleNames)
        {
            var count = uses.GetValueOrDefault(name) + 1;
            uses[name] = count;
            if (count == 1)
                planned.Add(name);
        }
    }

    /// <summary>
    /// Gives the folder of <paramref name="fixture"/>'s sample to the caller, prepared: as the previous test of the same
    /// sample left it, else as prepared before the first test, else generated now. The first call prepares the samples of
    /// the run. Returns null on success; a failure SampleResult on regen error.
    /// </summary>
    /// <param name="build">The build of the sample with the others of the run; null when the caller builds it.</param>
    internal static SampleResult? Take(DiscoveredFixture fixture, string worktreeRoot, string configuration, out SampleBuild? build)
    {
        prepared ??= PrepareAll(worktreeRoot, configuration);
        build = null;
        if (uses.TryGetValue(fixture.SampleName, out var count))
            uses[fixture.SampleName] = count - 1;

        if (kept.Remove(fixture.SampleName))
        {
            Console.WriteLine($"[{fixture.SampleName}] takes the folder of its previous test");
            return null;
        }

        if (prepared.Remove(fixture.SampleName, out var sample))
        {
            build = sample.Build;
            return sample.Failure;
        }

        return ScreenshotRunner.RegenerateSample(fixture, ScreenshotRunner.SampleDirectory(worktreeRoot, fixture.SampleName));
    }

    /// <summary>
    /// Gives back the folder of a test that passed. Returns false when a later test of the run uses the same sample: that
    /// test takes the folder; true when the caller may delete it.
    /// </summary>
    internal static bool Release(string sampleName)
    {
        if (uses.GetValueOrDefault(sampleName) <= 0)
            return true;
        kept.Add(sampleName);
        return false;
    }

    private static Dictionary<string, (SampleResult? Failure, SampleBuild? Build)> PrepareAll(string worktreeRoot, string configuration)
    {
        var fixtures = ScreenshotRunner.LoadFixtures(worktreeRoot);
        var toPrepare = planned
            .Select(name => fixtures.FirstOrDefault(x => string.Equals(x.SampleName, name, StringComparison.OrdinalIgnoreCase)))
            .OfType<DiscoveredFixture>()
            .ToList();

        var watch = Stopwatch.StartNew();
        var failures = new Dictionary<string, SampleResult?>(StringComparer.OrdinalIgnoreCase);
        foreach (var fixture in toPrepare)
            failures[fixture.SampleName] = ScreenshotRunner.RegenerateSample(fixture, ScreenshotRunner.SampleDirectory(worktreeRoot, fixture.SampleName));
        Console.WriteLine($"Prepared {failures.Count} samples in {watch.Elapsed.TotalSeconds:F0} s");

        var toBuild = failures.Where(x => x.Value is null).Select(x => x.Key).ToList();
        watch.Restart();
        var builds = toBuild.Count > 0 ? ScreenshotRunner.BuildSamples(worktreeRoot, toBuild, configuration) : null;
        if (builds is not null)
            Console.WriteLine($"Built {toBuild.Count} samples in one build in {watch.Elapsed.TotalSeconds:F0} s");
        else if (toBuild.Count > 0)
            Console.WriteLine($"The build of the {toBuild.Count} samples failed without naming a sample: each test builds its sample alone");

        return failures.ToDictionary(
            x => x.Key,
            x => (x.Value, builds?.GetValueOrDefault(x.Key)),
            StringComparer.OrdinalIgnoreCase);
    }
}
