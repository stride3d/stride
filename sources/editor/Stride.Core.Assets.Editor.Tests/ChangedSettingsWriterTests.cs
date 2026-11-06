// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Two editors that share a settings file keep each other's changes: each one writes only the settings it changed.
/// </summary>
public sealed class ChangedSettingsWriterTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
    private readonly string filePath;

    public ChangedSettingsWriterTests()
    {
        Directory.CreateDirectory(directory);
        filePath = Path.Combine(directory, "GameStudioInternal.conf");
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void AWriteKeepsTheValuesAnotherEditorWroteSince()
    {
        var first = new Editor(filePath);
        first.Recent.SetValue(["A.sln"]);
        first.Window.SetValue(100);
        Assert.True(first.Writer.Write());

        // The other editor starts, adds a recent session and writes
        var second = new Editor(filePath);
        Assert.Equal(["A.sln"], second.Recent.GetValue());
        second.Recent.SetValue(["B.sln", "A.sln"]);
        Assert.True(second.Writer.Write());

        // The first one only moved its window: the recent sessions of the second stay
        first.Window.SetValue(200);
        Assert.True(first.Writer.Write());

        var file = new Editor(filePath);
        Assert.Equal(["B.sln", "A.sln"], file.Recent.GetValue());
        Assert.Equal(200, file.Window.GetValue());
    }

    [Fact]
    public void AValueChangedInPlaceIsWritten()
    {
        var editor = new Editor(filePath);
        editor.Recent.SetValue(["A.sln"]);
        Assert.True(editor.Writer.Write());

        // The list was written; adding to it changes it again
        editor.Recent.GetValue().Add("B.sln");
        Assert.True(editor.Writer.Write());

        Assert.Equal(["A.sln", "B.sln"], new Editor(filePath).Recent.GetValue());
    }

    [Fact]
    public void TheSettingsOfTheFileThisEditorDoesNotKnowStay()
    {
        var other = new SettingsContainer();
        var otherKey = new SettingsKey<string>("Internal/Wpf/Layout", other, "");
        var otherProfile = other.CreateSettingsProfile(true);
        otherKey.SetValue("docked");
        Assert.True(other.SaveSettingsProfile(otherProfile, filePath));

        var editor = new Editor(filePath);
        editor.Window.SetValue(300);
        Assert.True(editor.Writer.Write());

        Assert.Equal("docked", otherKey.GetValue(other.LoadSettingsProfile(filePath, false)!, false));
    }

    /// <summary>
    /// An editor with its own settings container over the shared file.
    /// </summary>
    private sealed class Editor
    {
        public Editor(string filePath)
        {
            Container = new SettingsContainer();
            Recent = new SettingsKey<List<string>>("Internal/RecentSessions", Container, () => new List<string>());
            Window = new SettingsKey<int>("Internal/WindowWidth", Container, 0);
            var profile = Container.LoadSettingsProfile(filePath, true) ?? Container.CreateSettingsProfile(true);
            Writer = new ChangedSettingsWriter(Container, profile, filePath);
        }

        public SettingsContainer Container { get; }

        public SettingsKey<List<string>> Recent { get; }

        public SettingsKey<int> Window { get; }

        public ChangedSettingsWriter Writer { get; }
    }
}
