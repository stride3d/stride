// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;
    
    public enum ColorComponent
    {
        /// <summary>
        /// The red component.
        /// </summary>
        R,
        /// <summary>
        /// The green component.
        /// </summary>
        G,
        /// <summary>
        /// The blue component.
        /// </summary>
        B,
        /// <summary>
        /// The alpha component.
        /// </summary>
        A
    }

    /// <summary>
    /// Base class for color component curve view model.
    /// </summary>
    /// <typeparam name="TValue">The data type of the curve.</typeparam>
    /// <typeparam name="TKeyFrameControlPointViewModel">The type of a view model to represent a control point for a keyframe of the curve.</typeparam>
    public abstract class ColorComponentCurveViewModel<TValue, TKeyFrameControlPointViewModel> : KeyFrameCurveViewModel<TValue>
        where TValue : struct
        where TKeyFrameControlPointViewModel : KeyFrameControlPointViewModel<TValue>
    {
        protected ColorComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<TValue> computeCurve, ColorComponent component)
            : base(editor, parent, computeCurve)
        {
            Component = component;
            Color = Editor.GetColor((VectorComponent)component);
        }

        /// <summary>
        /// The vector component.
        /// </summary>
        public ColorComponent Component { get; }

        /// <inheritdoc/>
        [NotNull]
        public sealed override string DisplayName => Component.ToString();

        /// <inheritdoc/>
        public sealed override void AddPoint(WindowsPoint point)
        {
            var realPoint = InverseTransformPoint(point);
            // Make sure the curve is sampled
            UnderlyingCurve.UpdateChanges();
            // Evaluate the current value
            var value = UnderlyingCurve.Evaluate(realPoint.X);
            UpdateComponent(ref value, realPoint);
            var keyFrame = new AnimationKeyFrame<TValue>
            {
                Key = realPoint.X,
                Value = value,
            };
            var index = GetInsertIndex(realPoint);
            KeyFramesNode.Add(keyFrame, index);
        }

        /// <summary>
        /// Updates the component of the <paramref name="value"/> that is handled by this curve.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="realPoint"></param>
        protected abstract void UpdateComponent(ref TValue value, Vector2 realPoint);

        /// <inheritdoc/>
        protected sealed override KeyFrameControlPointViewModel<TValue> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
        {
            return (TKeyFrameControlPointViewModel)Activator.CreateInstance(typeof(TKeyFrameControlPointViewModel), this, keyNode, valueNode, tangentTypeNode, Component);
        }
    }

    public sealed class Color4ComponentCurveViewModel : ColorComponentCurveViewModel<Color4, Color4ComponentControlPointViewModel>
    {
        public Color4ComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<Color4> computeCurve, ColorComponent component)
            : base(editor, parent, computeCurve, component)
        {
        }
        
        /// <inheritdoc/>
        protected override void UpdateComponent(ref Color4 value, Vector2 realPoint)
        {
            // Update the component that is handled by this curve
            switch (Component)
            {
                case ColorComponent.R:
                    value.R = realPoint.Y;
                    break;

                case ColorComponent.G:
                    value.G = realPoint.Y;
                    break;

                case ColorComponent.B:
                    value.B = realPoint.Y;
                    break;

                case ColorComponent.A:
                    value.A = realPoint.Y;
                    break;

                default:
                    throw new NotSupportedException(); // This should never happen
            }
        }
    }
}
