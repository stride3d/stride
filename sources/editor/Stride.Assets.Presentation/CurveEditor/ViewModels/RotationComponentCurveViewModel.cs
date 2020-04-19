// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;

    /// <summary>
    /// Represents the curve of one component of a <see cref="Quaternion"/>.
    /// </summary>
    public sealed class RotationComponentCurveViewModel : KeyFrameCurveViewModel<Quaternion>
    {
        public RotationComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<Quaternion> computeCurve, VectorComponent component)
            : base(editor, parent, computeCurve)
        {
            Component = component;
            Color = Editor.GetColor(component);
        }

        public VectorComponent Component { get; }

        /// <inheritdoc/>
        [NotNull]
        public override string DisplayName => Component.ToString();

        /// <inheritdoc/>
        public override void AddPoint(WindowsPoint point)
        {
            var realPoint = InverseTransformPoint(point);
            // Make sure the curve is sampled
            UnderlyingCurve.UpdateChanges();
            // Evaluate the current value
            var value = UnderlyingCurve.Evaluate(realPoint.X);
            // Update the component that is handled by this curve
            switch (Component)
            {
                case VectorComponent.X:
                    value.X = realPoint.Y;
                    break;

                case VectorComponent.Y:
                    value.Y = realPoint.Y;
                    break;

                case VectorComponent.Z:
                    value.Z = realPoint.Y;
                    break;

                case VectorComponent.W:
                    value.Z = realPoint.Y;
                    break;

                default:
                    throw new NotSupportedException(); // This should never happen
            }
            // Create a new keyframe
            var keyFrame = new AnimationKeyFrame<Quaternion>
            {
                Key = realPoint.X,
                Value = value,
            };
            var index = GetInsertIndex(realPoint);
            KeyFramesNode.Add(keyFrame, index);
        }

        /// <inheritdoc/>
        [NotNull]
        protected override KeyFrameControlPointViewModel<Quaternion> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
        {
            return new RotationComponentControlPointViewModel(this, keyNode, valueNode, tangentTypeNode, Component);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Vector2> SampleControlPoints(KeyFrameControlPointViewModel<Quaternion> point1, KeyFrameControlPointViewModel<Quaternion> point2)
        {
            if (!point1.IsSynchronized || !point2.IsSynchronized)
            {
                // During an edit operation, linearize the curve for better performance.
                // The other alternative would be to create a temporary curve and evaluate it (we cannot use the underlying curve since it is not synchronized).
                yield return point1.Point;
            }
            else
            {
                var curve = UnderlyingCurve;
                if (curve == null)
                    yield break;

                double start, end, increment;
                if (!GetSamplingParameters(XAxis, out start, out end, out increment))
                    yield break;

                // Limit to float precision
                //if (increment < 1E-6f)
                //    increment = 1E-6f;

                start = Math.Max(start, point1.ActualKey);
                end = Math.Min(end, point2.ActualKey);

                // Make sure the curve is up-to-date
                curve.UpdateChanges();
                // FIXME: instead of evaluating for each quaternion component, optimize by querying the parent curve (that should be a rotation curve that knows all components of a quaternion)
                for (var t = start; t < end; t += increment)
                {
                    var ft = (float)t;
                    var sample = curve.Evaluate(ft);
                    switch (Component)
                    {
                        case VectorComponent.X:
                            yield return new Vector2(ft, sample.X);
                            break;

                        case VectorComponent.Y:
                            yield return new Vector2(ft, sample.Y);
                            break;

                        case VectorComponent.Z:
                            yield return new Vector2(ft, sample.Z);
                            break;

                        case VectorComponent.W:
                            yield return new Vector2(ft, sample.W);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
