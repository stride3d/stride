// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The component categories of the Add component list when several entities are selected: every category that has a component
/// all the entities can take is listed, with those components only.
/// </summary>
public sealed class EntityComponentCategoriesTests
{
    private static readonly AbstractNodeType Light = new(typeof(LightComponent));
    private static readonly AbstractNodeType LightShaft = new(typeof(LightShaftComponent));
    private static readonly AbstractNodeType Model = new(typeof(ModelComponent));
    private static readonly AbstractNodeType Instancing = new(typeof(InstancingComponent));
    private static readonly AbstractNodeType Camera = new(typeof(CameraComponent));

    [Fact]
    public void ACategoryKeepsTheComponentsEveryEntityCanTake()
    {
        // The first entity has a model and a light shaft already, the second a camera
        AbstractNodeTypeGroup[] first = [new("Lights", [Light]), new("Miscellaneous", [Camera]), new("Model", [Instancing])];
        AbstractNodeTypeGroup[] second = [new("Lights", [Light, LightShaft]), new("Model", [Instancing, Model])];

        var combined = EntityHierarchyData.CombineTypeGroups([first, second]).ToList();

        Assert.Equal(["Lights", "Model"], combined.Select(x => x.GroupName));
        Assert.Equal([Light], combined[0].Types);
        Assert.Equal([Instancing], combined[1].Types);
    }

    [Fact]
    public void OneEntityKeepsItsCategories()
    {
        AbstractNodeTypeGroup[] groups = [new("Lights", [Light, LightShaft]), new("Model", [Model])];

        Assert.Equal(groups, EntityHierarchyData.CombineTypeGroups([groups]));
        Assert.Empty(EntityHierarchyData.CombineTypeGroups([]));
    }

    [Fact]
    public void TheCategoriesOfTheNodeCombineWithTheCommonComponents()
    {
        AbstractNodeTypeGroup[] first = [new("Lights", [Light, LightShaft])];
        AbstractNodeTypeGroup[] second = [new("Lights", [Light])];
        var combiner = EntityHierarchyData.EntityComponentAvailableTypeGroupsKey.Metadatas.OfType<PropertyCombinerMetadata>().Single();

        var combined = Assert.IsAssignableFrom<IEnumerable<AbstractNodeTypeGroup>>(combiner.Combiner([first, second]));

        Assert.Equal([Light], Assert.Single(combined).Types);
    }
}
