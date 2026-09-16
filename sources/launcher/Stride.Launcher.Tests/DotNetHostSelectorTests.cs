// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;
using static Stride.Core.Assets.DotNetHostSelector;

namespace Stride.Launcher.Tests;

public class DotNetHostSelectorTests
{
    static readonly string[] Wpf = ["Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App"];

    static DotNetVersion V(string text) => DotNetVersion.TryParse(text, out var version) ? version : throw new FormatException(text);

    static DotNetInstall Install(string[] netcore, string[] desktop, string[] sdks) => new("root", "root/dotnet",
        new Dictionary<string, IReadOnlyList<DotNetVersion>>
        {
            ["Microsoft.NETCore.App"] = netcore.Select(V).ToList(),
            ["Microsoft.WindowsDesktop.App"] = desktop.Select(V).ToList(),
        },
        sdks.Select(V).ToList());

    static readonly DotNetInstall Full = Install(["10.0.11", "11.0.2", "12.0.0"], ["10.0.11", "11.0.2", "12.0.0"], ["10.0.400", "11.0.100", "12.0.100"]);

    [Fact]
    public void NoRequirementUsesCurrent()
    {
        var decision = Resolve(10, null, null, null, false, Wpf, Full);
        Assert.Equal(DecisionKind.UseCurrent, decision.Kind);
        Assert.Equal(10, decision.Major);
    }

    [Fact]
    public void RequirementAtOrBelowCurrentUsesCurrent()
    {
        Assert.Equal(DecisionKind.UseCurrent, Resolve(10, 10, null, null, false, Wpf, Full).Kind);
        Assert.Equal(DecisionKind.UseCurrent, Resolve(10, 8, null, null, false, Wpf, Full).Kind);
    }

    [Fact]
    public void RelaunchesOnTheRequiredMajor()
    {
        var decision = Resolve(10, 11, null, null, false, Wpf, Full);
        Assert.Equal(DecisionKind.Relaunch, decision.Kind);
        Assert.Equal(11, decision.Major);
    }

    [Fact]
    public void SkipsAMajorWithoutSdk()
    {
        var install = Install(["10.0.11", "11.0.2", "12.0.0"], ["10.0.11", "11.0.2", "12.0.0"], ["10.0.400", "12.0.100"]);
        var decision = Resolve(10, 11, null, null, false, Wpf, install);
        Assert.Equal(DecisionKind.Relaunch, decision.Kind);
        Assert.Equal(12, decision.Major);
    }

    [Fact]
    public void ReportsAMissingFramework()
    {
        var install = Install(["10.0.11", "11.0.2"], ["10.0.11"], ["10.0.400", "11.0.100"]);
        var decision = Resolve(10, 11, null, null, false, Wpf, install);
        Assert.Equal(DecisionKind.Missing, decision.Kind);
        Assert.Equal(11, decision.Major);
        Assert.Contains("Microsoft.WindowsDesktop.App", decision.Reason);
    }

    [Fact]
    public void ReportsAMissingSdk()
    {
        var install = Install(["10.0.11", "11.0.2"], ["10.0.11", "11.0.2"], ["10.0.400"]);
        var decision = Resolve(10, 11, null, null, false, Wpf, install);
        Assert.Equal(DecisionKind.Missing, decision.Kind);
        Assert.Contains("SDK", decision.Reason);
    }

    [Fact]
    public void GlobalJsonPinIsExact()
    {
        var install = Install(["10.0.11", "12.0.0"], ["10.0.11", "12.0.0"], ["10.0.400", "12.0.100"]);
        var decision = Resolve(10, 11, 11, null, false, Wpf, install);
        Assert.Equal(DecisionKind.Missing, decision.Kind);
        Assert.Equal(11, decision.Major);
    }

    [Fact]
    public void AChoiceOnlyRaisesTheProjectMajor()
    {
        Assert.Equal(12, Resolve(10, 12, null, 11, false, Wpf, Full).Major);
        Assert.Equal(11, Resolve(10, 10, null, 11, false, Wpf, Full).Major);
        Assert.Equal(11, Resolve(10, null, null, 11, false, Wpf, Full).Major);
        Assert.Equal(DecisionKind.UseCurrent, Resolve(10, null, null, 10, false, Wpf, Full).Kind);
    }

    [Fact]
    public void GlobalJsonPinWinsOverAChoice()
    {
        var decision = Resolve(10, 11, 11, 12, false, Wpf, Full);
        Assert.Equal(DecisionKind.Relaunch, decision.Kind);
        Assert.Equal(11, decision.Major);
    }

    [Fact]
    public void AChoiceRollsUpToAMajorWithSdk()
    {
        var install = Install(["10.0.11", "11.0.2", "12.0.0"], ["10.0.11", "11.0.2", "12.0.0"], ["10.0.400", "12.0.100"]);
        Assert.Equal(12, Resolve(10, null, null, 11, false, Wpf, install).Major);
    }

    [Fact]
    public void MissingMessageNamesWhatAskedForTheMajor()
    {
        var noSdkAbove10 = Install(["10.0.11", "11.0.2", "12.0.0"], ["10.0.11", "11.0.2", "12.0.0"], ["10.0.400"]);
        Assert.StartsWith("This project needs .NET 12,", Resolve(10, 12, null, 11, false, Wpf, noSdkAbove10).Reason);
        Assert.StartsWith("Game Studio was asked to run on .NET 13 or newer,", Resolve(10, 11, null, 13, false, Wpf, Full).Reason);
        Assert.StartsWith("This project's global.json asks for the .NET 11 SDK,", Resolve(10, 12, 11, null, false, Wpf, noSdkAbove10).Reason);
    }

    [Fact]
    public void NeverRelaunchesTwice()
    {
        var decision = Resolve(10, 11, null, null, true, Wpf, Full);
        Assert.Equal(DecisionKind.Missing, decision.Kind);
    }

    [Fact]
    public void NoInstallIsMissing()
    {
        Assert.Equal(DecisionKind.Missing, Resolve(10, 11, null, null, false, Wpf, null).Kind);
    }

    [Theory]
    [InlineData("net11.0", 11)]
    [InlineData("net11.0-windows7.0", 11)]
    [InlineData("NET8.0-android", 8)]
    [InlineData("11", 11)]
    [InlineData("11.0.100-rc.1.25451.107", 11)]
    [InlineData("net472", null)]
    [InlineData("netstandard2.1", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ParsesMajors(string? value, int? expected)
    {
        Assert.Equal(expected, ParseMajor(value));
    }

    [Fact]
    public void OrdersVersions()
    {
        Assert.True(V("10.0.11").CompareTo(V("10.0.10")) > 0);
        Assert.True(V("11.0.0").CompareTo(V("11.0.0-rc.1.25451.107")) > 0);
        Assert.True(V("11.0.0-preview.7").CompareTo(V("11.0.0-rc.1")) < 0);
        Assert.Equal(V("11.0.2"), Full.Highest("Microsoft.NETCore.App", 11));
        Assert.Null(Full.Highest("Microsoft.NETCore.App", 9));
    }

    [Fact]
    public void RequiredMajorComesFromTheExecutables()
    {
        using var temp = new TempDirectory();
        temp.Write("Game/Game.csproj", "<Project><PropertyGroup><TargetFramework>net12.0</TargetFramework></PropertyGroup></Project>");
        temp.Write("Game.Windows/Game.Windows.csproj", "<Project><PropertyGroup><OutputType>WinExe</OutputType><TargetFrameworks>net11.0;net11.0-windows</TargetFrameworks></PropertyGroup></Project>");
        temp.Write("Game.sln", """
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Game", "Game\Game.csproj", "{1}"
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Game.Windows", "Game.Windows\Game.Windows.csproj", "{2}"
            """);
        Assert.Equal(11, GetRequiredMajor(temp.Path("Game.sln")));
        Assert.Equal(12, GetRequiredMajor(temp.Path("Game/Game.csproj")));

        // The restored assets file wins over the csproj text.
        temp.Write("Game.Windows/obj/project.assets.json", """{ "project": { "frameworks": { "net13.0": {} } } }""");
        Assert.Equal(13, GetRequiredMajor(temp.Path("Game.sln")));
    }

    [Fact]
    public void RequiredMajorIsTheFirstTargetFramework()
    {
        // The editor loads a multi-targeting project's first framework, so that one decides.
        using var temp = new TempDirectory();
        temp.Write("Game.Windows/Game.Windows.csproj", "<Project><PropertyGroup><OutputType>Exe</OutputType><TargetFrameworks>net10.0;net11.0</TargetFrameworks></PropertyGroup></Project>");
        Assert.Equal(10, GetRequiredMajor(temp.Path("Game.Windows/Game.Windows.csproj")));

        temp.Write("Game.Windows/obj/project.assets.json", """{ "project": { "frameworks": { "net11.0": {}, "net10.0": {} } } }""");
        Assert.Equal(11, GetRequiredMajor(temp.Path("Game.Windows/Game.Windows.csproj")));
    }

    [Fact]
    public void AProjectEditedAfterItsRestoreUsesItsOwnTargetFramework()
    {
        // Moving a project to a newer .NET and opening it before any restore: the assets file still has the old framework.
        using var temp = new TempDirectory();
        temp.Write("Game.Windows/obj/project.assets.json", """{ "project": { "frameworks": { "net10.0": {} } } }""");
        File.SetLastWriteTimeUtc(temp.Path("Game.Windows/obj/project.assets.json"), DateTime.UtcNow.AddMinutes(-10));
        temp.Write("Game.Windows/Game.Windows.csproj", "<Project><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net12.0</TargetFramework></PropertyGroup></Project>");
        Assert.Equal(12, GetRequiredMajor(temp.Path("Game.Windows/Game.Windows.csproj")));

        // A framework the csproj only names through a property still comes from the assets file.
        temp.Write("Game.Windows/Game.Windows.csproj", "<Project><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>$(GameFramework)</TargetFramework></PropertyGroup></Project>");
        Assert.Equal(10, GetRequiredMajor(temp.Path("Game.Windows/Game.Windows.csproj")));
    }

    [Fact]
    public void HostSelectionSupportIsReadFromTheAssembly()
    {
        // This test assembly references the selector; the core library does not; a missing file is "no".
        Assert.True(SupportsHostSelection(typeof(DotNetHostSelectorTests).Assembly.Location));
        Assert.False(SupportsHostSelection(typeof(object).Assembly.Location));
        Assert.False(SupportsHostSelection(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "missing.dll")));
    }

    [Fact]
    public void RequiredMajorReadsSlnx()
    {
        using var temp = new TempDirectory();
        temp.Write("Game.Windows/Game.Windows.csproj", "<Project><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
        temp.Write("Game.slnx", """<Solution><Folder Name="/Platforms/"><Project Path="Game.Windows/Game.Windows.csproj" /></Folder></Solution>""");
        Assert.Equal(11, GetRequiredMajor(temp.Path("Game.slnx")));
        Assert.Null(GetRequiredMajor(temp.Path("Missing.sln")));
    }

    [Fact]
    public void GlobalJsonWalksUp()
    {
        using var temp = new TempDirectory();
        temp.Write("global.json", """{ "sdk": { "version": "11.0.100" } }""");
        Directory.CreateDirectory(temp.Path("a/b"));
        Assert.Equal(11, GetGlobalJsonSdkMajor(temp.Path("a/b")));

        temp.Write("a/global.json", """{ "sdk": { "version": "12.0.100", "rollForward": "latestMajor" } }""");
        Assert.Null(GetGlobalJsonSdkMajor(temp.Path("a/b")));
    }

    [Fact]
    public void RuntimeConfigIsRewrittenForEveryFramework()
    {
        using var temp = new TempDirectory();
        var previousDirectory = RuntimeConfigDirectory;
        RuntimeConfigDirectory = temp.Path("host");
        try
        {
        temp.Write("app/App.runtimeconfig.json", """
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" },
                "configProperties": { "System.GC.Server": true }
              }
            }
            """);
        var install = Install(["10.0.11", "11.0.1", "11.0.3"], ["11.0.3"], ["11.0.100"]);
        var config = WriteRuntimeConfig(temp.Path("app/App.dll"), 11, install);
        var text = File.ReadAllText(config);
        Assert.Contains("\"frameworks\"", text);
        Assert.Contains("\"version\": \"11.0.3\"", text);
        Assert.Contains("\"rollForward\": \"LatestPatch\"", text);
        Assert.Contains("\"System.GC.Server\": true", text);
        Assert.DoesNotContain("\"framework\":", text);

        var startInfo = RelaunchStartInfo(temp.Path("app/App.dll"), 11, ["a", RelaunchedArg], install);
        Assert.Equal(["exec", "--runtimeconfig", config, temp.Path("app/App.dll"), "a", RelaunchedArg], startInfo.ArgumentList);
        Assert.Equal(["Microsoft.NETCore.App"], ReadFrameworks(temp.Path("app/App.dll")));
        Assert.Equal(10, ReadNativeMajor(temp.Path("app/App.dll")));
        }
        finally
        {
            RuntimeConfigDirectory = previousDirectory;
        }
    }

    sealed class TempDirectory : IDisposable
    {
        readonly string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stride-host-tests", Guid.NewGuid().ToString("N"));

        public string Path(string relative) => System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));

        public void Write(string relative, string content)
        {
            var path = Path(relative);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
        }
    }
}
