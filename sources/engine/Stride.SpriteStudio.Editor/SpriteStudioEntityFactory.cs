// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Engine;

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// Add entity > 2D > SpriteStudio.
/// </summary>
[Display(40, "SpriteStudio", "2D")]
public class SpriteStudioEntityFactory : EntityFactory
{
    public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "SpriteStudio");
        var component = new SpriteStudioComponent();
        return CreateEntityWithComponent(name, component);
    }
}
