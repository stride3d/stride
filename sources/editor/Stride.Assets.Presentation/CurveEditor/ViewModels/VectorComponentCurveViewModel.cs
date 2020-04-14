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
    
    public enum VectorComponent
    {
        /// <summary>
        /// The x component.
        /// </summary>
        X,
        /// <summary>
        /// The y component.
        /// </summary>
        Y,
        /// <summary>
        /// The z component.
        /// </summary>
        Z,
        /// <summary>
        /// The w component.
        /// </summary>
        W
    }

    /// <summary>
    /// Base class for vector component curve view model.
    /// </summary>
    /// <typeparam name="TValue">The data type of the curve.</typeparam>
    /// <typeparam name="TKeyFrameControlPointViewModel">The type of a view model to represent a control point for a keyframe of the curve.</typeparam>
    public abstract class VectorComponentCurveViewModel<TValue, TKeyFrameControlPointViewModel> : KeyFrameCurveViewModel<TValue>
        where TValue : struct
        where TKeyFrameControlPointViewModel : KeyFrameControlPointViewModel<TValue>
    {
        protected VectorComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<TValue> computeCurve, VectorComponent component)
            : base(editor, parent, computeCurve)
        {
            Component = component;
            Color = Editor.GetColor(component);
        }

        /// <summary>
        /// The vector component.
        /// </summary>
        public VectorComponent Component { get; }

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

    /// <summary>
    /// Represents the curve of one component of a <see cref="Vector2"/>.
    /// </summary>
    public sealed class Vector2ComponentCurveViewModel : VectorComponentCurveViewModel<Vector2, Vector2ComponentControlPointViewModel>
    {
        public Vector2ComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<Vector2> computeCurve, VectorComponent component)
            : base(editor, parent, computeCurve, component)
        {
            if (component == VectorComponent.Z) throw new ArgumentException("Vector component 'Z' is invalid, only 'X' and 'Y' are supported.", nameof(component));
            if (component == VectorComponent.W) throw new ArgumentException("Vector component 'W' is invalid, only 'X' and 'Y' are supported.", nameof(component));
        }

        /// <inheritdoc/>
        protected override void UpdateComponent(ref Vector2 value, Vector2 realPoint)
        {
            // Update the component that is handled by this curve
            switch (Component)
            {
                case VectorComponent.X:
                    value.X = realPoint.Y;
                    break;

                case VectorComponent.Y:
                    value.Y = realPoint.Y;
                    break;

                default:
                    throw new NotSupportedException(); // This should never happen
            }
        }
    }


    /// <summary>
    /// Represents the curve of one component of a <see cref="Vector3"/>.
    /// </summary>
    public sealed class Vector3ComponentCurveViewModel : VectorComponentCurveViewModel<Vector3, Vector3ComponentControlPointViewModel>
    {
        public Vector3ComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<Vector3> computeCurve, VectorComponent component)
            : base(editor, parent, computeCurve, component)
        {
            if (component == VectorComponent.W) throw new ArgumentException("Vector component 'W' is invalid, only 'X', 'Y' and 'Z' are supported.", nameof(component));
        }
        
        /// <inheritdoc/>
        protected override void UpdateComponent(ref Vector3 value, Vector2 realPoint)
        {
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

                default:
                    throw new NotSupportedException(); // This should never happen
            }
        }
    }


    /// <summary>
    /// Represents the curve of one component of a <see cref="Vector4"/>.
    /// </summary>
    public sealed class Vector4ComponentCurveViewModel : VectorComponentCurveViewModel<Vector4, Vector4ComponentControlPointViewModel>
    {
        public Vector4ComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<Vector4> computeCurve, VectorComponent component)
            : base(editor, parent, computeCurve, component)
        {
        }
        
        /// <inheritdoc/>
        protected override void UpdateComponent(ref Vector4 value, Vector2 realPoint)
        {
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
                    value.W = realPoint.Y;
                    break;

                default:
                    throw new NotSupportedException(); // This should never happen
            }
        }
    }
}
