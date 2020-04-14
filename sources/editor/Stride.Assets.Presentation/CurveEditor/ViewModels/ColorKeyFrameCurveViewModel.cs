// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Animations;

namespace Xenko.Assets.Presentation.CurveEditor.ViewModels
{
    /// <summary>
    /// Represents an animation curve with <see cref="Color4"/> keyframes.
    /// Each component is represented by a child curve.
    /// </summary>
    public sealed class Color4KeyFrameCurveViewModel : DecomposedCurveViewModel<Color4>
    {
        public Color4KeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] ComputeAnimationCurve<Color4> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve,name)
        {
            Children.Add(new Color4ComponentCurveViewModel(editor, this, computeCurve, ColorComponent.R));
            Children.Add(new Color4ComponentCurveViewModel(editor, this, computeCurve, ColorComponent.G));
            Children.Add(new Color4ComponentCurveViewModel(editor, this, computeCurve, ColorComponent.B));
            Children.Add(new Color4ComponentCurveViewModel(editor, this, computeCurve, ColorComponent.A));
        }
    }
}
