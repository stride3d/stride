// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public sealed class RotationComponentControlPointViewModel : KeyFrameControlPointViewModel<Quaternion>
    {
        public RotationComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, VectorComponent component)
            : base(curve, keyNode, valueNode, tangentTypeNode)
        {
            Component = component;
            SynchronizePoint();
        }

        public VectorComponent Component { get; }

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
                    ValueBinding.Value = new Quaternion((float)value, current.Y, current.Z, current.W);
                    break;

                case VectorComponent.Y:
                    ValueBinding.Value = new Quaternion(current.X, (float)value, current.Z, current.W);
                    break;

                case VectorComponent.Z:
                    ValueBinding.Value = new Quaternion(current.X, current.Y, (float)value, current.W);
                    break;

                case VectorComponent.W:
                    ValueBinding.Value = new Quaternion(current.X, current.Y, current.Z, (float)value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
