// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Animations;
using Stride.Assets.Entities;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public abstract class EntityHierarchyViewModel : AssetCompositeHierarchyViewModel<EntityDesign, Entity>
{
    protected EntityHierarchyViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    public override EntityViewModel CreatePartViewModel(EntityDesign partDesign)
    {
        return new EntityViewModel(this, partDesign);
    }

    /// <inheritdoc />
    protected override bool ShouldConstructPropertyMember(IMemberNode member)
    {
        // Hide child nodes of a compute curve.
        if (member.Parent.Type.HasInterface(typeof(IComputeCurve<>)) && member.Name == nameof(ComputeAnimationCurve<int>.KeyFrames))
            return false;
        return base.ShouldConstructPropertyMember(member);
    }
}
