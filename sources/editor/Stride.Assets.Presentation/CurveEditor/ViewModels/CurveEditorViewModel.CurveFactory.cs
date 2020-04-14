// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    partial class CurveEditorViewModel
    {
        /// <summary>
        /// Creates a hierarchy of curves to represent the given <paramref name="computeCurve"/>.
        /// </summary>
        /// <typeparam name="TValue">The data type of the curve.</typeparam>
        /// <param name="computeCurve"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [NotNull]
        private CurveViewModelBase CreateCurveHierarchy<TValue>([NotNull] IComputeCurve<TValue> computeCurve, CurveViewModelBase parent = null, string name = null)
            where TValue : struct
        {
            var colorCurve = computeCurve as IComputeCurve<Color4>;
            if (colorCurve != null)
                return CreateCurveHierarchy<Color4, Color4CurveViewModel, Color4KeyFrameCurveViewModel>(colorCurve, parent, name);

            var floatCurve = computeCurve as IComputeCurve<float>;
            if (floatCurve != null)
                return CreateCurveHierarchy<float, FloatCurveViewModel, FloatKeyFrameCurveViewModel>(floatCurve, parent, name);

            var rotationCurve = computeCurve as IComputeCurve<Quaternion>;
            if (rotationCurve != null)
                return CreateCurveHierarchy<Quaternion, RotationCurveViewModel, RotationKeyFrameCurveViewModel>(rotationCurve, parent, name);

            var vector2Curve = computeCurve as IComputeCurve<Vector2>;
            if (vector2Curve != null)
                return CreateCurveHierarchy<Vector2, Vector2CurveViewModel, Vector2KeyFrameCurveViewModel>(vector2Curve, parent, name);

            var vector3Curve = computeCurve as IComputeCurve<Vector3>;
            if (vector3Curve != null)
                return CreateCurveHierarchy<Vector3, Vector3CurveViewModel, Vector3KeyFrameCurveViewModel>(vector3Curve, parent, name);

            var vector4Curve = computeCurve as IComputeCurve<Vector4>;
            if (vector4Curve != null)
                return CreateCurveHierarchy<Vector4, Vector4CurveViewModel, Vector4KeyFrameCurveViewModel>(vector4Curve, parent, name);

            throw new NotSupportedException($"The type IComputeCurve<{typeof(TValue).Name}> is not supported by this editor.");
        }

        /// <summary>
        /// Creates a hierarchy of curves to represent the given <paramref name="computeCurve"/>.
        /// </summary>
        /// <typeparam name="TValue">The data type of the curve.</typeparam>
        /// <typeparam name="TCurveViewModel">The type of a view model to display the curve.</typeparam>
        /// <typeparam name="TKeyFrameCurveViewModel">The type of a view model to display and edit the animation curve.</typeparam>
        /// <param name="computeCurve"></param>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [NotNull]
        private CurveViewModelBase<TValue> CreateCurveHierarchy<TValue, TCurveViewModel, TKeyFrameCurveViewModel>([NotNull] IComputeCurve<TValue> computeCurve, CurveViewModelBase parent = null, string name = null)
            where TValue : struct
            where TCurveViewModel : CurveViewModelBase<TValue>
            where TKeyFrameCurveViewModel : CurveViewModelBase<TValue>
        {
            var animationCurve = computeCurve as ComputeAnimationCurve<TValue>;
            if (animationCurve != null)
            {
                var curve =
                    (TKeyFrameCurveViewModel)Activator.CreateInstance(typeof(TKeyFrameCurveViewModel), this, (ComputeAnimationCurve<TValue>)computeCurve, parent, name);
                curve.Initialize();
                return curve;
            }

            var binaryCurve = computeCurve as ComputeBinaryCurve<TValue>;
            if (binaryCurve != null)
            {
                var curve = (TCurveViewModel)Activator.CreateInstance(typeof(TCurveViewModel), this, binaryCurve, parent, name);
                if (binaryCurve.LeftChild != null)
                {
                    var displayName = GetDisplayName(typeof(ComputeBinaryCurve<TValue>), nameof(ComputeBinaryCurve<TValue>.LeftChild));
                    curve.Children.Add(CreateCurveHierarchy<TValue, TCurveViewModel, TKeyFrameCurveViewModel>(binaryCurve.LeftChild, curve, displayName ?? "Left"));
                }
                if (binaryCurve.RightChild != null)
                {
                    // Retrieve the display attribute
                    var displayName = GetDisplayName(typeof(ComputeBinaryCurve<TValue>), nameof(ComputeBinaryCurve<TValue>.RightChild));
                    curve.Children.Add(CreateCurveHierarchy<TValue, TCurveViewModel, TKeyFrameCurveViewModel>(binaryCurve.RightChild, curve, displayName ?? "Right"));
                }
                curve.Initialize();
                return curve;
            }
            // Fallback curve
            else
            {
                var curve = (TCurveViewModel)Activator.CreateInstance(typeof(TCurveViewModel), this, computeCurve, parent, name);
                curve.Initialize();
                return curve;
            }
        }

        private static string GetDisplayName(Type type, string propertyName)
        {
            // Get the property
            var propertyInfo = type?.GetProperty(propertyName);
            // Retrieve the display attribute
            var displayAttribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(propertyInfo);
            // Return the name
            return displayAttribute?.Name;
        }
    }
}
