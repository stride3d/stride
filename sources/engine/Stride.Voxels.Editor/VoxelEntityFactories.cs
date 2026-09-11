// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Engine;
using Stride.Rendering.Lights;
using Stride.Rendering.Voxels;
using Stride.Rendering.Voxels.VoxelGI;

namespace Stride.Voxels.Editor;

/// <summary>
/// Add entity > Light > Voxel light.
/// </summary>
[Display(60, "Voxel light", "Light")]
public class VoxelLightEntityFactory : EntityFactory
{
    public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "Voxel light");
        var component = new LightComponent { Type = new LightVoxel() };
        return CreateEntityWithComponent(name, component);
    }
}

/// <summary>
/// Add entity > Light > Voxel volume.
/// </summary>
[Display(65, "Voxel volume", "Light")]
public class VoxelVolumeEntityFactory : EntityFactory
{
    public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "Voxel volume");
        var component = new VoxelVolumeComponent { Attributes = { new VoxelAttributeEmissionOpacity() } };
        return CreateEntityWithComponent(name, component);
    }
}
