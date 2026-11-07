// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.Services;
using Stride.Audio.Editor;
using Stride.Core.Reflection;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Navigation;
using Stride.Particles.Components;
using Stride.Particles.Editor;
using Stride.Physics;
using Stride.Physics.Editor;
using Stride.Rendering.Voxels;
using Stride.Voxels.Editor;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The Editor packages of the plugins are toolkit-neutral: once their assemblies are registered, the Avalonia scene editor
/// finds their gizmos, their Add entity entries and their editor game services as the WPF one does.
/// </summary>
public sealed class PluginEditorsTests
{
    static PluginEditorsTests()
    {
        PluginEditorAssemblies.Register();
    }

    [AvaloniaTheory]
    [InlineData(typeof(AudioEmitterComponent), typeof(AudioEmitterGizmo), false)]
    [InlineData(typeof(AudioListenerComponent), typeof(AudioListenerGizmo), false)]
    [InlineData(typeof(ParticleSystemComponent), typeof(ParticleSystemGizmo), false)]
    [InlineData(typeof(PhysicsComponent), typeof(PhysicsGizmo), true)]
    [InlineData(typeof(PhysicsConstraintComponent), typeof(PhysicsConstraintGizmo), true)]
    [InlineData(typeof(NavigationBoundingBoxComponent), typeof(NavigationBoundingBoxGizmo), false)]
    [InlineData(typeof(VoxelVolumeComponent), typeof(VoxelVolumeGizmo), false)]
    public void ThePluginGizmosAreFound(Type componentType, Type gizmoType, bool hiddenByDefault)
    {
        Assert.Equal(gizmoType, StrideEditorPlugin.GizmoTypeDictionary[componentType]);
        Assert.Equal(hiddenByDefault, gizmoType.GetCustomAttribute<GizmoComponentAttribute>()!.HiddenByDefault);
    }

    [AvaloniaTheory]
    [InlineData("Audio", "Audio emitter")]
    [InlineData("Audio", "Audio listener")]
    [InlineData("Navigation", "Bounding box")]
    [InlineData("Particle system", "Simple particle system")]
    [InlineData("2D", "SpriteStudio")]
    [InlineData("Light", "Voxel light")]
    [InlineData("Light", "Voxel volume")]
    public void ThePluginEntityFactoriesAreInTheAddEntityMenu(string category, string name)
    {
        var factories = StrideEditorPlugin.EntityFactoryCategories.Single(x => x.Name == category).Factories;

        Assert.Single(factories, x => x.Name == name);
    }

    [AvaloniaFact]
    public void TheNavigationMeshServiceIsCreatedForTheSceneEditorAndShowsInTheOverlaysPopup()
    {
        var type = typeof(EditorGameNavigationMeshService);

        Assert.Contains(type, AssemblyRegistry.GetScanTypes(type.Assembly, typeof(EditorGameServiceAttribute)));
        Assert.Equal(typeof(ISceneEditorController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
        // The overlays popup asks the controller for its overlay services (GetServices needs a view model service)
        Assert.True(typeof(IEditorGameOverlayService).IsAssignableFrom(type));
        Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(type));
    }

    [AvaloniaFact]
    public void TheWireframeStageThatDrawsTheColliderShapesIsCreatedForTheSceneAndPrefabEditors()
    {
        var type = typeof(WireframeGizmoStageService);

        Assert.Contains(type, AssemblyRegistry.GetScanTypes(type.Assembly, typeof(EditorGameServiceAttribute)));
        Assert.Equal(typeof(EntityHierarchyEditorController), type.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType);
        // The gizmo options popup asks the controller for the component gizmo service
        Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(typeof(EditorGameComponentGizmoService)));
    }
}
