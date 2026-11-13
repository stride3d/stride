// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Assets.Editor.ViewModels.CurveEditor;

namespace Stride.Assets.Editor.ViewModels.CurveEditor.ControlPoints
{
    public sealed class FloatControlPointViewModel : KeyFrameControlPointViewModel<float>
    {
        public FloatControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode)
            : base(curve, keyNode, valueNode, tangentTypeNode)
        {
            SynchronizePoint();
        }

        protected override double GetValue()
        {
            return ValueBinding.Value;
        }

        protected override void SetValue(double value)
        {
            ValueBinding.Value = (float)value;
        }
    }
}
