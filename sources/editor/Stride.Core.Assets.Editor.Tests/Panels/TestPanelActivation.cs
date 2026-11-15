// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestPanelActivation
{
    private const string Output = "Tests.Output";

    /// <summary>
    /// A shell that records the panels brought forward.
    /// </summary>
    private sealed class RecordingPanelService : IPanelService
    {
        public List<string> Shown { get; } = [];

        public PanelRegistry Registry { get; } = new();

        public event EventHandler<PanelChangedEventArgs>? PanelChanged { add { } remove { } }

        public bool IsVisible(string panelId) => true;

        public void Show(string panelId, bool activate = true) => Shown.Add(panelId);

        public void Hide(string panelId) { }

        public IPanelViewModel? GetPanel(string panelId) => null;

        public IReadOnlyList<IPanelViewModel> GetInstances(string panelId) => [];

        public Task<IPanelViewModel> OpenInstanceAsync(string panelId) => throw new NotSupportedException();

        public void Activate(IPanelViewModel instance) { }

        public Task<bool> CloseInstanceAsync(IPanelViewModel instance) => Task.FromResult(false);

        public Task ResetLayoutAsync() => Task.CompletedTask;
    }

    private static LogMessage Message(LogMessageType type) => new("Build", type, type.ToString());

    [Fact]
    public void AnErrorBringsThePanelForwardAndALowerLevelDoesNot()
    {
        var panels = new RecordingPanelService();
        var messages = new ObservableList<ILogMessage>();
        using var activation = PanelActivation.OnLog(panels, Output, messages);

        messages.Add(Message(LogMessageType.Info));
        messages.Add(Message(LogMessageType.Warning));
        Assert.Empty(panels.Shown);

        messages.Add(Message(LogMessageType.Error));
        Assert.Equal([Output], panels.Shown);
        messages.Add(Message(LogMessageType.Fatal));
        Assert.Equal([Output, Output], panels.Shown);
    }

    [Fact]
    public void AfterAnErrorEveryMessageBringsThePanelForwardUntilTheLogIsCleared()
    {
        var panels = new RecordingPanelService();
        var messages = new ObservableList<ILogMessage>();
        using var activation = PanelActivation.OnLog(panels, Output, messages);

        messages.Add(Message(LogMessageType.Error));
        messages.Add(Message(LogMessageType.Info));
        Assert.Equal(2, panels.Shown.Count);

        // A new build clears its log: an error starts it again
        messages.Clear();
        messages.Add(Message(LogMessageType.Info));
        Assert.Equal(2, panels.Shown.Count);
        messages.Add(Message(LogMessageType.Error));
        Assert.Equal(3, panels.Shown.Count);
    }

    [Fact]
    public void ADisposedActivationNoLongerFollowsTheLog()
    {
        var panels = new RecordingPanelService();
        var messages = new ObservableList<ILogMessage>();
        var activation = PanelActivation.OnLog(panels, Output, messages);

        activation.Dispose();
        messages.Add(Message(LogMessageType.Error));
        Assert.Empty(panels.Shown);
    }
}
