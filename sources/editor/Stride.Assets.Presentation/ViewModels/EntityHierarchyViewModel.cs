// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyViewModel : AssetCompositeHierarchyViewModel<EntityDesign, Entity>
{
    protected EntityHierarchyViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    public override EntityViewModel CreatePartViewModel(EntityDesign partDesign)
    {
        return new EntityViewModel(this, partDesign);
    }
}
