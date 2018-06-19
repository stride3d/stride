// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Assets.Entities;
using Xenko.Core.Quantum;
using Xenko.Animations;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.ViewModel
{
    public abstract class EntityHierarchyViewModel : AssetCompositeHierarchyViewModel<EntityDesign, Entity>
    {
        protected EntityHierarchyViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        internal new EntityHierarchyEditorViewModel Editor => (EntityHierarchyEditorViewModel)base.Editor;

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
