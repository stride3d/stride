// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;
    using WindowsVector = System.Windows.Vector;

    public abstract class ControlPointViewModelBase : DispatcherViewModel, IResizingTarget
    {
        private Vector2 point;
        private bool isSelected;

        /// <summary>
        /// Starting point of the resizing, in screen coordinates.
        /// </summary>
        private WindowsPoint startingPoint;

        protected ControlPointViewModelBase([NotNull] CurveViewModelBase curve)
            : base(curve.SafeArgument(nameof(curve)).ServiceProvider)
        {
            Curve = curve;
            
            DependentProperties.Add(nameof(Point), new[] { nameof(ActualKey), nameof(ActualPoint), nameof(ActualValue) });
        }

        /// <summary>
        /// Gets the actual key, even when resizing (e.g. during a drag operation).
        /// Set the key (in this case it is equivalent to directly setting the <see cref="Key"/> property.
        /// </summary>
        /// <remarks>
        /// This property is intended to be used by XAML bindings for editing, so that the value displayed in the input control is always correct. 
        /// </remarks>
        public double ActualKey { get { return Point.X; } set { Key = value; } }
        
        /// <summary>
        /// Gets the actual point (in screen coordinates) that represents the Key/Value pair.
        /// </summary>
        public WindowsPoint ActualPoint
        {
            get
            {
                // FIXME (performance) cache the value and invalidate it when the point or the axis transform changed (not that easy)
                return Curve.TransformPoint(Point);
            }
        }

        /// <summary>
        /// Gets the actual value, even when resizing (e.g. during a drag operation).
        /// Set the value (in this case it is equivalent to directly setting the <see cref="Value"/> property.
        /// </summary>
        /// <remarks>
        /// This property is intended to be used by WPF binding for editing, so that the value displayed in the input control is always correct.
        /// Other classes should use the <see cref="Value"/> property instead.
        /// </remarks>
        public double ActualValue { get { return Point.Y; } set { Value = value; } }

        /// <summary>
        /// True if this control point is currently part of a selection, false otherwise.
        /// </summary>
        public bool IsSelected { get { return isSelected; } set { SetValue(ref isSelected, value); } }

        /// <summary>
        /// True if the <see cref="Point"/>, <see cref="ActualKey"/> and <see cref="ActualValue"/> are synchronized
        /// with the <see cref="Key"/> and <see cref="Value"/> properties.
        /// </summary>
        /// <remarks>
        /// During a resizing operation, these values are not synchronized.
        /// </remarks>
        public bool IsSynchronized { get; protected set; }

        /// <summary>
        /// Gets the underlying key.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>This property is not intended to be accessed by XAML bindings. <see cref="ActualKey"/> should be used instead.</item>
        /// <item>When <see cref="IsSynchronized"/> is <c>True</c>, it is usually faster and safer to read <see cref="ActualKey"/> instead.</item>
        /// </list>
        /// </remarks>
        public abstract double Key { get; protected set; }

        /// <summary>
        /// Gets the underlying value.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>This property is not intended to be accessed by XAML bindings. <see cref="ActualValue"/> should be used instead.</item>
        /// <item>When <see cref="IsSynchronized"/> is <c>True</c>, it is usually faster and safer to read <see cref="ActualValue"/> instead.</item>
        /// </list>
        /// </remarks>
        public abstract double Value { get; protected set; }

        public Vector2 Point { get { return point; } protected set { SetValue(ref point, value, () => IsSynchronized = false); } }

        /// <summary>
        /// Gets the undo/redo service used by this view model.
        /// </summary>
        protected IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

        protected internal ControlPointViewModelBase Next { get; set; }

        protected internal ControlPointViewModelBase Previous { get; set; }

        protected CurveViewModelBase Curve { get; }

        void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                OnResizingCompleted(direction, horizontalChange, verticalChange);
                UndoRedoService.SetName(transaction, "Move control point");
            }
        }

        void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            OnResizingDelta(direction, horizontalChange, verticalChange);
        }

        void IResizingTarget.OnResizingStarted(ResizingDirection direction)
        {
            // Makes sure the point is synchronized with the underlying key/value pair.
            SynchronizePoint();
            // Remember the starting point
            startingPoint = ActualPoint;
        }

        protected virtual void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            if (Math.Abs(horizontalChange) + Math.Abs(verticalChange) < MathUtil.ZeroToleranceDouble)
                return;
            
            // Zooming might have change during the resizing, so we need to calculate from the original position
            var vector = Curve.InverseTransformPoint(startingPoint + new WindowsVector(horizontalChange, verticalChange));
            CoerceResizing(ref vector, false);
            Key = vector.X;
            Value = vector.Y;
        }

        protected virtual void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            var changeVector = Curve.InverseTransformVector(new WindowsVector(horizontalChange, verticalChange));
            CoerceResizing(ref changeVector, true);
            switch (direction)
            {
                case ResizingDirection.Center:
                    Point = Point + changeVector;
                    break;
            }
        }

        /// <summary>
        /// Coerces the resizing with custom rules. The default implementation does nothing.
        /// </summary>
        /// <param name="changeVector">The change vector.</param>
        /// <param name="isDelta">True when called from <see cref="OnResizingDelta"/>. False when called from <see cref="OnResizingCompleted"/>.</param>
        protected virtual void CoerceResizing(ref Vector2 changeVector, bool isDelta)
        {
            // default implementation does nothing
        }

        [NotNull]
        protected MemberGraphNodeBinding<TValue> CreateBinding<TValue>(IMemberNode node, string propertyName)
        {
            return new MemberGraphNodeBinding<TValue>(node, propertyName, OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        }

        protected override void OnPropertyChanged([NotNull] params string[] propertyNames)
        {
            base.OnPropertyChanged(propertyNames);

            if (propertyNames.Any(p => string.Equals(p, nameof(Key)) || string.Equals(p, nameof(Value))))
            {
                IsSynchronized = false;
                SynchronizePoint();
            }
        }

        protected void SynchronizePoint()
        {
            if (IsSynchronized)
            {
                // Already synchronized, prevent unnecessary change notification
                Debug.Assert(Math.Abs(ActualKey - Key) < double.Epsilon && Math.Abs(ActualValue - Value) < double.Epsilon);
                return;
            }

            OnPropertyChanging(nameof(Point));
            point = new Vector2((float)Key, (float)Value);
            IsSynchronized = true;
            OnPropertyChanged(nameof(Point));
        }
    }
}
