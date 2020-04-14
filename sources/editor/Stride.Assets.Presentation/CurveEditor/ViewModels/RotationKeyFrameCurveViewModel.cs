// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Animations;

namespace Xenko.Assets.Presentation.CurveEditor.ViewModels
{
    /// <summary>
    /// Represents an animation curve with <see cref="Quaternion"/> keyframes.
    /// Each component is represented by a child curve.
    /// </summary>
    public sealed class RotationKeyFrameCurveViewModel : DecomposedCurveViewModel<Quaternion>
    {
        public RotationKeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] ComputeAnimationCurve<Quaternion> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
            Children.Add(new RotationComponentCurveViewModel(editor, this, computeCurve, VectorComponent.X));
            Children.Add(new RotationComponentCurveViewModel(editor, this, computeCurve, VectorComponent.Y));
            Children.Add(new RotationComponentCurveViewModel(editor, this, computeCurve, VectorComponent.Z));
            Children.Add(new RotationComponentCurveViewModel(editor, this, computeCurve, VectorComponent.W));
        }
    }
}
