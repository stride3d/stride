// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// Builds <see cref="NodeViewModel"/> hierarchies for plain objects, without a session.
/// </summary>
public sealed class NodeViewModelContext
{
    private readonly NodeContainer nodeContainer = new();

    public NodeViewModelContext(params INodePresenterUpdater[] updaters)
    {
        var graphViewModelService = new GraphViewModelService(nodeContainer);
        graphViewModelService.AvailableUpdaters.AddRange(updaters);
        ServiceProvider = new ViewModelServiceProvider([new NullDispatcherService(), graphViewModelService]);
    }

    public ViewModelServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Creates the view model of the given object and returns its root node.
    /// </summary>
    public NodeViewModel RootOf(object instance)
    {
        var rootNode = nodeContainer.GetOrCreateNode(instance);
        var viewModel = GraphViewModel.Create(ServiceProvider, [new PropertyProvider(rootNode)])!;
        return viewModel.RootNode;
    }

    /// <summary>
    /// Creates the view model of the given object and returns the node of one of its members.
    /// </summary>
    public NodeViewModel MemberOf(object instance, string member)
    {
        return RootOf(instance).GetChild(member) ?? throw new ArgumentException($"No member {member}.", nameof(member));
    }

    private sealed class PropertyProvider(IObjectNode rootNode) : IPropertyProviderViewModel
    {
        public bool CanProvidePropertiesViewModel => true;

        public IObjectNode GetRootNode() => rootNode;

        public bool ShouldConstructMember(IMemberNode member) => true;

        public bool ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }
}

public enum PlainEnum
{
    First,
    Second,
}

[Flags]
public enum FlagsEnum
{
    None = 0,
    One = 1,
    Two = 2,
}

/// <summary>
/// An object with one member per kind of template provider.
/// </summary>
public sealed class SampleObject
{
    public bool Flag { get; set; }

    public int Integer { get; set; } = 3;

    public int? NullableInteger { get; set; }

    public Guid? NullableStruct { get; set; }

    public string Text { get; set; } = "text";

    public PlainEnum Plain { get; set; }

    public FlagsEnum Flags { get; set; }

    public List<int> Numbers { get; set; } = [1, 2];

    public int[] Array { get; set; } = [1];

    public HashSet<string> Set { get; set; } = ["a"];

    public Dictionary<string, int> Map { get; set; } = new() { ["a"] = 1 };
}
