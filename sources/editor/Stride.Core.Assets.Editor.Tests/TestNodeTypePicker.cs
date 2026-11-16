// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The list of the Add component button: the components grouped by category, and a search over every category.
/// </summary>
public class TestNodeTypePicker
{
    [Display("Ambient light")]
    public sealed class AmbientLightThing;

    [Display("Model")]
    public sealed class ModelThing;

    public sealed class SpotLightComponent;

    private static readonly AbstractNodeType Ambient = new(typeof(AmbientLightThing));
    private static readonly AbstractNodeType Model = new(typeof(ModelThing));
    private static readonly AbstractNodeType Spot = new(typeof(SpotLightComponent));

    [Fact]
    public void NothingIsListedUntilAGroupIsSelected()
    {
        var picker = Create();

        Assert.True(picker.HasTypes);
        Assert.Empty(picker.ShownTypes);

        picker.SelectedGroup = picker.Groups[0];
        Assert.Equal([Ambient, Spot], picker.ShownTypes);
        Assert.Null(picker.SelectedType);

        picker.SelectedGroup = picker.Groups[1];
        Assert.Equal([Model], picker.ShownTypes);
    }

    [Fact]
    public void TheSearchListsTheMatchesOfEveryGroupAndSelectsTheFirst()
    {
        var picker = Create();
        picker.SelectedGroup = picker.Groups[1];

        picker.SearchToken = "light";

        Assert.True(picker.IsSearching);
        Assert.Null(picker.SelectedGroup);
        Assert.Equal([Ambient, Spot], picker.ShownTypes);
        Assert.Equal(Ambient, picker.SelectedType);

        // Every word matches, as a part of the name or as its camel case initials, ignoring the case
        picker.SearchToken = "  LIGHT  amb ";
        Assert.Equal([Ambient], picker.ShownTypes);
        picker.SearchToken = "slc";
        Assert.Equal([Spot], picker.ShownTypes);
        picker.SearchToken = "nothing";
        Assert.Empty(picker.ShownTypes);
        Assert.Null(picker.SelectedType);

        // A blank search lists nothing again, as before the search
        picker.SearchToken = " ";
        Assert.False(picker.IsSearching);
        Assert.Empty(picker.ShownTypes);
    }

    [Fact]
    public void AddingRunsTheCommandWithTheTypeAndClearsTheList()
    {
        var command = new RecordingCommand();
        var picker = Create();
        picker.AddCommand = command;
        picker.SearchToken = "model";

        Assert.True(picker.Add(picker.SelectedType!));

        Assert.Equal([Model], command.Executed);
        Assert.Equal(string.Empty, picker.SearchToken);
        Assert.Null(picker.SelectedType);
        Assert.Empty(picker.ShownTypes);

        // A command that cannot run with the type does not
        command.CanRun = false;
        Assert.False(picker.Add(Spot));
        Assert.Equal([Model], command.Executed);
    }

    [Fact]
    public void ReplacingTheGroupsDropsASelectedGroupThatIsGone()
    {
        var picker = Create();
        picker.SelectedGroup = picker.Groups[1];

        picker.Groups = [picker.Groups[0]];

        Assert.Null(picker.SelectedGroup);
        Assert.Empty(picker.ShownTypes);
    }

    private static NodeTypePickerViewModel Create()
    {
        return new NodeTypePickerViewModel
        {
            Types = [Ambient, Model, Spot],
            Groups = [new AbstractNodeTypeGroup("Lights", [Ambient, Spot]), new AbstractNodeTypeGroup("Model", [Model])],
        };
    }

    private sealed class RecordingCommand : ICommand
    {
        public List<object?> Executed { get; } = [];

        public bool CanRun { get; set; } = true;

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => CanRun;

        public void Execute(object? parameter) => Executed.Add(parameter);
    }
}
