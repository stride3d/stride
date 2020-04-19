// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Drawing;
using Stride.Animations;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using Color = Core.Mathematics.Color;
    using WindowsPoint = System.Windows.Point;

    public interface IEditableCurveViewModel
    {
        [ItemNotNull, NotNull]
        IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints { get; }

        // FIXME: might remove this later when hit testing is done on the curve
        double ControlPointRadius { get; }

        void AddPoint(WindowsPoint point);

        ControlPointViewModelBase GetClosestPoint(WindowsPoint position, double maximumDistance = double.PositiveInfinity);

        bool RemovePoint(ControlPointViewModelBase point);
    }

    public abstract class EditableCurveViewModel<TValue> : CurveViewModelBase<TValue>, IEditableCurveViewModel, IResizingTarget
        where TValue : struct
    {
        private Color controlPointColor = Color.CadetBlue;
        private double controlPointRadius = 4;

        protected EditableCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] IComputeCurve<TValue> computeCurve, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        public abstract IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints { get; }

        /// <summary>
        /// Gets or sets the color of a control point.
        /// </summary>
        /// <remarks>
        /// Changing the color will not trigger a refresh of the curve.
        /// </remarks>
        /// <value>The color of a control point.</value>
        public Color ControlPointColor { get { return controlPointColor; } protected internal set { SetValue(ref controlPointColor, value); } }

        /// <summary>
        /// Gets or sets the radius of a control point.
        /// </summary>
        /// <remarks>
        /// Changing the radius will not trigger a refresh of the curve.
        /// </remarks>
        /// <value>The radius of a control point.</value>
        public double ControlPointRadius { get { return controlPointRadius; } protected internal set { SetValue(ref controlPointRadius, value); } }

        public bool IsInitialized { get; private set; }

        public bool IsInitializing { get; private set; }

        public abstract void AddPoint(WindowsPoint point);

        public ControlPointViewModelBase GetClosestPoint(WindowsPoint position, double maximumDistance)
        {
            return CurveHelper.GetClosestPoint(ControlPoints, position, maximumDistance);
        }

        /// <inheritdoc/>
        public sealed override void Initialize()
        {
            IsInitializing = true;
            try
            {
                InitializeOverride();
                IsInitialized = true;
            }
            finally
            {
                IsInitializing = false;
            }
        }

        public abstract bool RemovePoint(ControlPointViewModelBase point);

        public sealed override void Render(IDrawingContext drawingContext, bool isCurrentCurve)
        {
            var decomposedCurve = Parent as DecomposedCurveViewModel<TValue>;
            if (decomposedCurve != null && isCurrentCurve)
            {
                // Render siblings of decomposed curve
                foreach (var sibling in Parent.Children.Where(s => !ReferenceEquals(s, this)))
                {
                    // get the color
                    var color = sibling.Color;
                    // darken
                    sibling.Color = color * 0.666f;
                    sibling.Render(drawingContext, false);
                    // restore color
                    sibling.Color = color;
                }
            }

            // Render itself
            base.Render(drawingContext, isCurrentCurve);
        }

        void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                OnResizingCompleted(direction, horizontalChange, verticalChange);
                Editor.UndoRedoService.SetName(transaction, "Move control points");
            }
        }

        void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            OnResizingDelta(direction, horizontalChange, verticalChange);
        }

        void IResizingTarget.OnResizingStarted(ResizingDirection direction)
        {
            OnResizingStarted(direction);
        }

        /// <summary>
        /// Called when the content of the <see cref="ControlPoints"/> collection has changed.
        /// </summary>
        /// <remarks>Inheriting classes have the responsability of calling this method whenever suitable.</remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ControlPointsCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
                return;

            if (e.OldItems != null)
            {
                foreach (ControlPointViewModelBase c in e.OldItems)
                {
                    c.PropertyChanged -= ControlPointPropertyChanged;
                    c.Destroy();
                }
            }

            if (e.NewItems != null)
            {
                foreach (ControlPointViewModelBase c in e.NewItems)
                {
                    c.PropertyChanged += ControlPointPropertyChanged;
                }
            }

            Refresh();
        }

        protected void ControlPointPropertyChanged(object sender, [NotNull] PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ControlPointViewModelBase.ActualPoint) || e.PropertyName == nameof(ControlPointViewModelBase.IsSelected))
            {
                Refresh();
            }
        }

        protected virtual void InitializeOverride()
        {
            // default implementation does nothing
        }

        protected virtual void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
            foreach (IResizingTarget c in selectedPoints)
            {
                c.OnResizingCompleted(direction, horizontalChange, verticalChange);
            }
        }

        protected virtual void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
            foreach (IResizingTarget c in selectedPoints)
            {
                c.OnResizingDelta(direction, horizontalChange, verticalChange);
            }
        }

        protected virtual void OnResizingStarted(ResizingDirection direction)
        {
            var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
            foreach (IResizingTarget c in selectedPoints)
            {
                c.OnResizingStarted(direction);
            }
        }

        protected override void Refresh()
        {
            if (!IsInitialized || IsInitializing)
                return;

            base.Refresh();
        }
    }
}
