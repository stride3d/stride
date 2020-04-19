// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;
    using WindowsRect = System.Windows.Rect;
    using WindowsVector = System.Windows.Vector;

    public partial class CurveEditorViewModel : DispatcherViewModel
    {
        private readonly ObservableList<CurveViewModelBase> curves = new ObservableList<CurveViewModelBase>();
        private bool isControlPointHovered;
        private CurveViewModelBase selectedCurve;
        private ControlPointViewModelBase singleSelectedControlPoint;

        public CurveEditorViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] SessionViewModel session)
            : base(serviceProvider.SafeArgument(nameof(serviceProvider)))
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            Session = session;

            AddPointCommand = new AnonymousCommand<WindowsPoint>(serviceProvider, AddPoint);
            ClearCurveCommand = new AnonymousCommand(serviceProvider, ClearSelectedCurve);
            DeleteSelectedPointsCommand = new AnonymousCommand(serviceProvider, DeleteSelectedPoints);
            FocusCommand = new AnonymousCommand(serviceProvider, Focus);
            NavigateToControlPointCommand = new AnonymousCommand<int>(serviceProvider, NavigateToControlPoint);
            PreviewClickCommand = new AnonymousCommand<WindowsPoint>(serviceProvider, Click);
            RemoveSelectedCurveCommand = new AnonymousCommand(serviceProvider, RemoveSelectedCurve);
            ResetViewCommand = new AnonymousCommand<int>(serviceProvider, ResetAxes);
            SelectCommand = new AnonymousCommand<WindowsRect>(serviceProvider, Select);

            SelectedControlPoints.CollectionChanged += SelectedControlPointsCollectionChanged;

            InitializeRendering();
        }

        public IReadOnlyObservableCollection<CurveViewModelBase> Curves => curves;

        public bool IsControlPointHovered { get { return isControlPointHovered; } set { SetValue(ref isControlPointHovered, value); } }

        public CurveViewModelBase SelectedCurve { get { return selectedCurve; } set { SetValue(ref selectedCurve, value, OnSelectedCurveChanged); } }

        // TODO: move this property to EditableCurveViewModel? or merge selection from different curves?
        public ObservableList<object> SelectedControlPoints { get; } = new ObservableList<object>();

        public SessionViewModel Session { get; }

        public ControlPointViewModelBase SingleSelectedControlPoint { get { return singleSelectedControlPoint; } set { SetValue(ref singleSelectedControlPoint, value); } }

        public ICommandBase AddPointCommand { get; }

        public ICommandBase ClearCurveCommand { get; }

        public ICommandBase DeleteSelectedPointsCommand { get; }

        public ICommandBase FocusCommand { get; }

        public ICommandBase NavigateToControlPointCommand { get; }

        public ICommandBase PreviewClickCommand { get; }

        public ICommandBase RemoveSelectedCurveCommand { get; }

        public ICommandBase ResetViewCommand { get; }

        public ICommandBase SelectCommand { get; }

        internal IUndoRedoService UndoRedoService => Session.UndoRedoService;

        /// <summary>
        /// Adds the <paramref name="computeCurve"/> to the curve editor.
        /// </summary>
        /// <typeparam name="TValue">The type of the curve node values.</typeparam>
        /// <param name="computeCurve">The curve.</param>
        /// <param name="name">The name of the curve.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="computeCurve"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The <typeparamref name="TValue"/> is not supported by the editor.</exception>
        public void AddCurve<TValue>([NotNull] IComputeCurve<TValue> computeCurve, string name)
            where TValue : struct
        {
            if (computeCurve == null) throw new ArgumentNullException(nameof(computeCurve));

            // Check if the curve is already here
            if (SelectCurveIfExists(computeCurve))
                return;

            var curve = CreateCurveHierarchy(computeCurve, name: name);
            AddCurvePrivate(curve);
        }

        public override void Destroy()
        {
            var curvesCopy = curves.ToList();
            // clear before destroying
            curves.Clear();
            SelectedControlPoints.Clear();
            selectedCurve = null;

            foreach (var curve in curvesCopy)
            {
                curve.Destroy();
            }
            base.Destroy();
        }

        partial void InitializeRendering();

        private void AddCurvePrivate([NotNull] CurveViewModelBase curve)
        {
            curve.EnsureAxes();
            curves.Add(curve);
            SelectedCurve = curve;
        }

        private void AddPoint(WindowsPoint point)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var editableCurve = selectedCurve as IEditableCurveViewModel;
                editableCurve?.AddPoint(point); 

                UndoRedoService.SetName(transaction, "Add control point");
            }
        }

        private void ClearSelectedCurve()
        {
            var editableCurve = selectedCurve as IEditableCurveViewModel;
            if (editableCurve == null)
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                DeletePoints(editableCurve.ControlPoints, editableCurve);
                UndoRedoService.SetName(transaction, $"Clear curve '{selectedCurve.DisplayName}'");
            }
        }

        private void ClearPointSelection()
        {
            foreach (var p in SelectedControlPoints.OfType<ControlPointViewModelBase>())
            {
                p.IsSelected = false;
            }
            SelectedControlPoints.Clear();
        }

        private void Click(WindowsPoint point)
        {
            var editableCurve = SelectedCurve as IEditableCurveViewModel;
            if (editableCurve == null)
            {
                IsControlPointHovered = false;
                return;
            }
            // TODO: move this to the curve (especially hit testing)
            var radius = editableCurve.ControlPointRadius;
            var clickedPoint = editableCurve.GetClosestPoint(point, 2 * radius);
            if (clickedPoint == null)
            {
                if (Keyboard.Modifiers == ModifierKeys.None)
                    ClearPointSelection();
                IsControlPointHovered = false;
                return;
            }

            if (!clickedPoint.IsSelected)
            {
                // FIXME: do this atomically in the curve
                Select(new WindowsRect(point.X - 2 * radius, point.Y - 2 * radius, 4 * radius, 4 * radius), true);
            }

            IsControlPointHovered = true;
        }

        private void DeleteSelectedPoints()
        {
            var editableCurve = selectedCurve as IEditableCurveViewModel;
            if (editableCurve == null)
                return;

            var count = SelectedControlPoints.Count;
            if (count == 0)
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                DeletePoints(SelectedControlPoints.Cast<ControlPointViewModelBase>(), editableCurve);
                UndoRedoService.SetName(transaction, $"Delete {SelectedControlPoints.Count} point{(count > 1 ? "s" : "")} from curve '{selectedCurve.DisplayName}'");
            }
        }

        private static void DeletePoints([NotNull] IEnumerable<ControlPointViewModelBase> points, IEditableCurveViewModel editableCurve)
        {
            // Need to create a copy of the list to prevent exception when iterating and modifying the sane collection
            foreach (var point in points.ToList())
            {
                editableCurve.RemovePoint(point);
            }
        }

        public void Focus()
        {
            var count = SelectedControlPoints.Count;
            switch (count)
            {
                case 0:
                    // Focus on whole curve
                    ResetAxes();
                    break;

                default:
                    Debug.Assert(SelectedCurve != null);
                    // Focus on the selection
                    var center = (WindowsPoint)(SelectedControlPoints
                        .Select(s => (WindowsVector?)(s as ControlPointViewModelBase)?.ActualPoint)
                        .Aggregate(new WindowsVector(), (c, s) => s.HasValue ? s.Value+ c : c) / count);
                    SelectedCurve.XAxis?.Center(center);
                    SelectedCurve.YAxis?.Center(center);
                    break;
            }
        }

        private void NavigateToControlPoint(int navigation)
        {
            if (SelectedControlPoints.Count == 0)
                return;

            var point = SelectedControlPoints[0] as ControlPointViewModelBase;
            if (point == null)
                return;

#if !DEBUG
            var wasSelected = point.IsSelected; 
#endif
            point.IsSelected = false;
            if (navigation == int.MinValue)
            {
                // navigate to first point
                while (point.Previous != null)
                {
                    point = point.Previous;
                }
            }
            else if (navigation == int.MaxValue)
            {
                // navigate to last point
                while (point.Next != null)
                {
                    point = point.Next;
                }
            }
            else if (navigation == -1)
            {
                // navigate to previous point (works also with multi selection)
                point = point.Previous ?? point;
            }
            else if (navigation == 1)
            {
                // if multiple points are selected go to the next one following the selection
                point = SelectedControlPoints[SelectedControlPoints.Count - 1] as ControlPointViewModelBase ?? point;
                // navigate to next point
                point = point.Next ?? point;
            }
            else
            {
                // invalid (shouldn't happen)
#if DEBUG
                throw new ArgumentException($"Expected parameter navigation to be one of {{{int.MinValue}, {int.MaxValue}, {-1} or {1}}} but got {navigation} instead.", nameof(navigation));
#else
                point.IsSelected = wasSelected;
                return;
#endif
            }

            // FIXME: do this atomically in the curve
            ClearPointSelection();
            SelectedControlPoints.Add(point);
            point.IsSelected = true;
        }

        private void OnSelectedCurveChanged()
        {
            ClearPointSelection();
            InvalidateView();
        }

        private void RemoveCurve(CurveViewModelBase curve)
        {
            if (curve == null)
                return;

            if (curves.Remove(curve))
            {
                curve.Destroy();
            }
        }

        private void RemoveSelectedCurve()
        {
            RemoveCurve(SelectedCurve);
        }

        private void ResetAxes(int option = 0)
        {
            foreach (var axis in Axes)
            {
                if (axis.IsHorizontal() && option <= 0)
                {
                    axis.Reset();
                    axis.SetViewMaxMinToActualMaxMin();
                }

                if (axis.IsVertical() && option >= 0)
                {
                    axis.Reset();
                    axis.SetViewMaxMinToActualMaxMin();
                }
            }

            InvalidateView();
        }

        private void Select(WindowsRect selectionRect)
        {
            Select(selectionRect, false);
        }

        private void Select(WindowsRect selectionRect, bool isSingleSelection)
        {
            var editableCurve = SelectedCurve as IEditableCurveViewModel;
            if (editableCurve == null)
                return;

            // TODO: move this to the curve (especially hit testing)
            IList<ControlPointViewModelBase> selectedPoints = editableCurve.ControlPoints.Where(c => selectionRect.Contains(c.ActualPoint)).ToList();
            if (selectedPoints.Count > 1 && isSingleSelection)
            {
                var rectCenter = new WindowsPoint(selectionRect.Left + selectionRect.Width*0.5, selectionRect.Top + selectionRect.Height * 0.5);
                var closest = CurveHelper.GetClosestPoint(selectedPoints, rectCenter);
                selectedPoints = new[] { closest };
            }
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.None:
                    ClearPointSelection();
                    goto case ModifierKeys.Shift;

                case ModifierKeys.Shift:
                    // FIXME: do this atomically in the curve
                    foreach (var p in selectedPoints.Where(p => !p.IsSelected))
                    {
                        SelectedControlPoints.Add(p);
                        p.IsSelected = true;
                    }
                    break;

                case ModifierKeys.Control:
                    // FIXME: do this atomically in the curve
                    foreach (var p in selectedPoints.Where(p => p.IsSelected))
                    {
                        SelectedControlPoints.Remove(p);
                        p.IsSelected = false;
                    }
                    break;
            }
        }

        private void SelectedControlPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update single selection
            SingleSelectedControlPoint = (SelectedControlPoints.Count == 1 ? SelectedControlPoints[0] : null) as ControlPointViewModelBase;
        }

        private bool SelectCurveIfExists<TValue>(IComputeCurve<TValue> computeCurve) where TValue : struct
        {
            var curveNode = Session.AssetNodeContainer.GetNode(computeCurve);
            if (curveNode == null)
                return false;

            var curve = Curves.OfType<CurveViewModelBase<TValue>>().FirstOrDefault(c => c.CurveId == curveNode.Guid);
            if (curve == null)
                return false;

            SelectedCurve = curve;
            return true;
        }
    }
}
