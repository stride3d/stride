// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    public interface IEntityFactory
    {
        Task<Entity> CreateEntity([NotNull] EntityHierarchyItemViewModel parent);
    }
}
