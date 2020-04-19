// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Animations;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public abstract class KeyFrameControlPointViewModel<TValue> : ControlPointViewModelBase, IDisposable
        where TValue : struct
    {
        private readonly MemberGraphNodeBinding<float> keyBinding;
        private readonly MemberGraphNodeBinding<AnimationKeyTangentType> tangentTypeBinding;

        protected KeyFrameControlPointViewModel([NotNull] CurveViewModelBase curve, [NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode)
           : base(curve)
        {
            if (keyNode == null) throw new ArgumentNullException(nameof(keyNode));
            if (valueNode == null) throw new ArgumentNullException(nameof(valueNode));
            if (tangentTypeNode == null) throw new ArgumentNullException(nameof(tangentTypeNode));

            keyBinding = CreateBinding<float>(keyNode, nameof(Key));
            ValueBinding = CreateBinding<TValue>(valueNode, nameof(Value));
            tangentTypeBinding = CreateBinding<AnimationKeyTangentType>(tangentTypeNode, nameof(TangentType));
        }

        public sealed override double Key { get { return keyBinding.Value; } protected set { keyBinding.Value = (float)value; } }

        public sealed override double Value { get { return GetValue(); } protected set { SetValue(value); } }

        public AnimationKeyTangentType TangentType { get { return tangentTypeBinding.Value; } protected set { tangentTypeBinding.Value = value; } }

        protected MemberGraphNodeBinding<TValue> ValueBinding { get; }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(KeyFrameControlPointViewModel<TValue>));
            Cleanup();
            base.Destroy();
        }

        protected override void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            base.OnResizingCompleted(direction, horizontalChange, verticalChange);
            Debug.Assert(IsSynchronized);
            // Maintain order
            if (Previous != null && ActualKey < (Previous.IsSynchronized ? Previous.ActualKey : Previous.Key) ||
                Next != null && ActualKey > (Next.IsSynchronized ? Next.ActualKey : Next.Key))
            {
                var editableCurve = Curve as EditableCurveViewModel<TValue>;
                if (editableCurve == null)
                    return;

                var actualPoint = ActualPoint;
                // remove
                editableCurve.RemovePoint(this);
                // re-add
                editableCurve.AddPoint(actualPoint);
            }
        }

        protected abstract double GetValue();

        protected abstract void SetValue(double value);

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            keyBinding.Dispose();
            ValueBinding.Dispose();
            tangentTypeBinding.Dispose();
        }
    }
}
