// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The marker an editor writes while the plugins join a session (<see cref="PluginLoadingMarker"/>), and the offer of safe
/// mode at the next start when a marker was left behind.
/// </summary>
public sealed class TestPluginLoadingMarker : IDisposable
{
    // No process gets an id that high: the marker of an editor that is gone
    private const int GoneProcessId = int.MaxValue - 7;

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.PluginLoading." + Guid.NewGuid().ToString("N"));
    private readonly PluginLoadingMarker marker;

    public TestPluginLoadingMarker()
    {
        marker = new PluginLoadingMarker(directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Fact]
    public void TheMarkerOfThisEditorIsWrittenThenRemoved()
    {
        var game = new UFile("/Projects/Game/Game.sln");

        marker.Write(game);

        Assert.True(File.Exists(marker.FilePath));
        Assert.Equal(Path.Combine(directory, Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".marker"), marker.FilePath);
        Assert.Equal(game.ToOSPath(), File.ReadAllLines(marker.FilePath)[0]);
        // This editor is still running: its marker is not a crash
        Assert.Empty(marker.FindLeftBehind());

        marker.Remove();

        Assert.False(File.Exists(marker.FilePath));
        // Removing it again (a session that failed to open after the plugins joined) does nothing
        marker.Remove();
    }

    [Fact]
    public void TheMarkerOfAnEditorThatIsGoneIsLeftBehindUntilCleared()
    {
        WriteMarker(GoneProcessId, "/Projects/Game/Game.sln", 1);
        WriteMarker(GoneProcessId - 1, "", 1);
        File.WriteAllText(Path.Combine(directory, "notes.txt"), "not a marker");
        File.WriteAllText(Path.Combine(directory, "other.marker"), "not a process id");

        var left = marker.FindLeftBehind().OrderBy(x => x.FilePath, StringComparer.Ordinal).ToList();

        Assert.Equal(2, left.Count);
        Assert.Null(left[0].SessionPath);
        Assert.Equal("Game.sln", left[1].SessionPath?.GetFileName());

        PluginLoadingMarker.Clear(left);

        Assert.Empty(marker.FindLeftBehind());
        Assert.False(File.Exists(left[0].FilePath));
        Assert.False(File.Exists(left[1].FilePath));
    }

    [Fact]
    public void AMarkerWithTheIdOfThisEditorButAnotherStartTimeIsLeftBehind()
    {
        // An editor that died, and this one started with its process id (a restarted container or Wine prefix)
        WriteMarker(Environment.ProcessId, "/Projects/Game/Game.sln", 1);

        var left = Assert.Single(marker.FindLeftBehind());

        Assert.Equal(marker.FilePath, left.FilePath);
        Assert.Equal("Game.sln", left.SessionPath?.GetFileName());
        // Once this editor writes its own, it is its own again
        marker.Write(null);
        Assert.Empty(marker.FindLeftBehind());
        marker.Remove();
    }

    [Fact]
    public void AnotherProcessThatGotTheSameIdIsNotTheEditorThatLeftTheMarker()
    {
        var other = FindOtherProcess();
        Assert.NotNull(other);
        var (processId, startTime) = other.Value;

        WriteMarker(processId, "/Projects/Game/Game.sln", startTime);
        // That editor is still loading its plugins
        Assert.Empty(marker.FindLeftBehind());

        WriteMarker(processId, "/Projects/Game/Game.sln", startTime - 1);
        Assert.Single(marker.FindLeftBehind());
    }

    [Fact]
    public async Task NoMarkerLeftMeansNoOffer()
    {
        var dialog = SafeModeDialog.Create(1);

        Assert.False(await marker.OfferSafeModeAsync(dialog));

        Assert.Empty(((SafeModeDialog)(object)dialog).Messages);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task SafeModeIsOfferedOnceAfterACrashWhileThePluginsLoaded(int answer, bool safeMode)
    {
        WriteMarker(GoneProcessId, "/Projects/Game/Game.sln", 1);
        var dialog = SafeModeDialog.Create(answer);
        var recorder = (SafeModeDialog)(object)dialog;

        Assert.Equal(safeMode, await marker.OfferSafeModeAsync(dialog));

        var message = Assert.Single(recorder.Messages);
        Assert.Contains("Game Studio stopped while it was opening the project **Game.sln**.", message, StringComparison.Ordinal);
        Assert.Contains("safe mode", message, StringComparison.Ordinal);
        Assert.Equal(["Start in safe mode", "Start normally"], recorder.Buttons);
        // Cleared whatever the answer: the next start does not ask again
        Assert.Empty(marker.FindLeftBehind());
        Assert.False(await marker.OfferSafeModeAsync(dialog));
        Assert.Single(recorder.Messages);
    }

    [Fact]
    public void TheHostRemovesTheMarkerOnceThePluginsHaveInitializedTheSession()
    {
        var host = new RecordingEditorPluginHost([]) { LoadingMarker = marker };
        var session = new FakeSession();
        session.ServiceProvider.RegisterService(host);
        host.RegisterSession(session, new LoggerResult());

        Assert.Same(marker, ((IAssetsPluginService)host).LoadingMarker);
        marker.Write(null);
        Assert.True(File.Exists(marker.FilePath));

        host.InitializeSession(session);

        Assert.False(File.Exists(marker.FilePath));
    }

    [Fact]
    public void TheHostTellsWhetherItRunsInSafeMode()
    {
        IAssetsPluginService host = new RecordingEditorPluginHost([]) { SafeMode = true };

        Assert.True(host.SafeMode);
        Assert.Null(host.LoadingMarker);
        Assert.False(((IAssetsPluginService)new RecordingEditorPluginHost([])).SafeMode);
    }

    private void WriteMarker(int processId, string sessionPath, long startTime)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, processId.ToString(CultureInfo.InvariantCulture) + ".marker"), [sessionPath, startTime.ToString(CultureInfo.InvariantCulture)]);
    }

    private static (int ProcessId, long StartTime)? FindOtherProcess()
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (process.Id == Environment.ProcessId)
                    continue;
                try
                {
                    return (process.Id, process.StartTime.ToUniversalTime().Ticks);
                }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Not readable: try the next one
                }
            }
        }
        return null;
    }
}

/// <summary>
/// A dialog service that records the message boxes with buttons and answers the given button; the other dialogs are not supported.
/// </summary>
public class SafeModeDialog : DispatchProxy
{
    private int answer;

    public List<string> Messages { get; } = [];

    public List<string> Buttons { get; } = [];

    public static IDialogService Create(int answer)
    {
        var service = Create<IDialogService, SafeModeDialog>();
        ((SafeModeDialog)(object)service).answer = answer;
        return service;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == nameof(IDialogService.MessageBoxAsync) && targetMethod.ReturnType == typeof(Task<int>))
        {
            Messages.Add((string)args![0]!);
            Buttons.Clear();
            Buttons.AddRange(((IReadOnlyCollection<DialogButtonInfo>)args[1]!).Select(x => (string)x.Content!));
            return Task.FromResult(answer);
        }
        throw new NotSupportedException($"{targetMethod?.Name} is not supported by the fake dialog service.");
    }
}
