// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public abstract class VectorComponentControlPointViewModel<TValue> : KeyFrameControlPointViewModel<TValue>
        where TValue : struct
    {
        protected VectorComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, VectorComponent component) 
            : base(curve, keyNode, valueNode, tangentTypeNode)
        {
            Component = component;
            SynchronizePoint();
        }

        public VectorComponent Component { get; }
    }

    public sealed class Vector2ComponentControlPointViewModel : VectorComponentControlPointViewModel<Vector2>
    {
        public Vector2ComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, VectorComponent component)
            : base(curve, keyNode, valueNode, tangentTypeNode, component)
        {
            if (component == VectorComponent.Z) throw new ArgumentException("Vector component 'Z' is invalid, only 'X' and 'Y' are supported.", nameof(component));
            if (component == VectorComponent.W) throw new ArgumentException("Vector component 'W' is invalid, only 'X' and 'Y' are supported.", nameof(component));
        }

        protected override double GetValue()
        {
            switch (Component)
            {
                case VectorComponent.X:
                    return ValueBinding.Value.X;

                case VectorComponent.Y:
                    return ValueBinding.Value.Y;

                case VectorComponent.Z:
                case VectorComponent.W:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void SetValue(double value)
        {
            var current = ValueBinding.Value;
            switch (Component)
            {
                case VectorComponent.X:
                    ValueBinding.Value = new Vector2((float)value, current.Y);
                    break;

                case VectorComponent.Y:
                    ValueBinding.Value = new Vector2(current.X, (float)value);
                    break;

                case VectorComponent.Z:
                case VectorComponent.W:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed class Vector3ComponentControlPointViewModel : VectorComponentControlPointViewModel<Vector3>
    {
        public Vector3ComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, VectorComponent component)
            : base(curve, keyNode, valueNode, tangentTypeNode, component)
        {
            if (component == VectorComponent.W) throw new ArgumentException("Vector component 'W' is invalid, only 'X', 'Y' and 'Z' are supported.", nameof(component));
        }
        
        protected override double GetValue()
        {
            switch (Component)
            {
                case VectorComponent.X:
                    return ValueBinding.Value.X;

                case VectorComponent.Y:
                    return ValueBinding.Value.Y;

                case VectorComponent.Z:
                    return ValueBinding.Value.Z;

                case VectorComponent.W:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void SetValue(double value)
        {
            var current = ValueBinding.Value;
            switch (Component)
            {
                case VectorComponent.X:
                    ValueBinding.Value = new Vector3((float)value, current.Y, current.Z);
                    break;

                case VectorComponent.Y:
                    ValueBinding.Value = new Vector3(current.X, (float)value, current.Z);
                    break;

                case VectorComponent.Z:
                    ValueBinding.Value = new Vector3(current.X, current.Y, (float)value);
                    break;

                case VectorComponent.W:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed class Vector4ComponentControlPointViewModel : VectorComponentControlPointViewModel<Vector4>
    {
        public Vector4ComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, VectorComponent component)
            : base(curve, keyNode, valueNode, tangentTypeNode, component)
        {
        }
        
        protected override double GetValue()
        {
            switch (Component)
            {
                case VectorComponent.X:
                    return ValueBinding.Value.X;

                case VectorComponent.Y:
                    return ValueBinding.Value.Y;

                case VectorComponent.Z:
                    return ValueBinding.Value.Z;

                case VectorComponent.W:
                    return ValueBinding.Value.W;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void SetValue(double value)
        {
            var current = ValueBinding.Value;
            switch (Component)
            {
                case VectorComponent.X:
                    ValueBinding.Value = new Vector4((float)value, current.Y, current.Z, current.W);
                    break;

                case VectorComponent.Y:
                    ValueBinding.Value = new Vector4(current.X, (float)value, current.Z, current.W);
                    break;

                case VectorComponent.Z:
                    ValueBinding.Value = new Vector4(current.X, current.Y, (float)value, current.W);
                    break;

                case VectorComponent.W:
                    ValueBinding.Value = new Vector4(current.X, current.Y, current.Z, (float)value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
