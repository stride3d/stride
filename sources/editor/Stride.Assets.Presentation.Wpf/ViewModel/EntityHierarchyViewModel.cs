// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Assets.Entities;
using Stride.Core.Quantum;
using Stride.Animations;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModel
{
    public abstract class EntityHierarchyViewModel : AssetCompositeHierarchyViewModel<EntityDesign, Entity>
    {
        protected EntityHierarchyViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
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
}
