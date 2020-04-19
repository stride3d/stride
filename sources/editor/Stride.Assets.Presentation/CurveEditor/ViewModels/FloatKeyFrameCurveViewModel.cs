// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;

    public sealed class FloatKeyFrameCurveViewModel : KeyFrameCurveViewModel<float>
    {
        public FloatKeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] ComputeAnimationCurve<float> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        public override void AddPoint(WindowsPoint point)
        {
            var realPoint = InverseTransformPoint(point);
            var keyFrame = new AnimationKeyFrame<float>
            {
                Key = realPoint.X,
                Value = realPoint.Y,
            };
            var index = GetInsertIndex(realPoint);
            KeyFramesNode.Add(keyFrame, index);
        }

        [NotNull]
        protected override KeyFrameControlPointViewModel<float> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
        {
            return new FloatControlPointViewModel(this, keyNode, valueNode, tangentTypeNode);
        }
    }
}
