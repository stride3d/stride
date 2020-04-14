// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public abstract class ColorComponentControlPointViewModel<TValue> : KeyFrameControlPointViewModel<TValue>
        where TValue : struct
    {
        protected ColorComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, ColorComponent component) 
            : base(curve, keyNode, valueNode, tangentTypeNode)
        {
            Component = component;
            SynchronizePoint();
        }

        public ColorComponent Component { get; }
    }

    public sealed class Color4ComponentControlPointViewModel : ColorComponentControlPointViewModel<Color4>
    {
        public Color4ComponentControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode, ColorComponent component)
            : base(curve, keyNode, valueNode, tangentTypeNode, component)
        {
        }
        
        protected override double GetValue()
        {
            switch (Component)
            {
                case ColorComponent.R:
                    return ValueBinding.Value.R;

                case ColorComponent.G:
                    return ValueBinding.Value.G;

                case ColorComponent.B:
                    return ValueBinding.Value.B;

                case ColorComponent.A:
                    return ValueBinding.Value.A;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void SetValue(double value)
        {
            var current = ValueBinding.Value;
            switch (Component)
            {
                case ColorComponent.R:
                    ValueBinding.Value = new Color4((float)value, current.G, current.B, current.A);
                    break;

                case ColorComponent.G:
                    ValueBinding.Value = new Color4(current.R, (float)value, current.B, current.A);
                    break;

                case ColorComponent.B:
                    ValueBinding.Value = new Color4(current.R, current.G, (float)value, current.A);
                    break;

                case ColorComponent.A:
                    ValueBinding.Value = new Color4(current.R, current.G, current.B, (float)value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
