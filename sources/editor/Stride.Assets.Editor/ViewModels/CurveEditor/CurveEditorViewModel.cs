// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Animations;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Editor.ViewModels.CurveEditor.ControlPoints;

namespace Stride.Assets.Editor.ViewModels.CurveEditor
{

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

            AddPointCommand = new AnonymousCommand<DrawingPoint>(serviceProvider, AddPoint);
            ClearCurveCommand = new AnonymousCommand(serviceProvider, ClearSelectedCurve);
            DeleteSelectedPointsCommand = new AnonymousCommand(serviceProvider, DeleteSelectedPoints);
            FocusCommand = new AnonymousCommand(serviceProvider, Focus);
            NavigateToControlPointCommand = new AnonymousCommand<int>(serviceProvider, NavigateToControlPoint);
            PreviewClickCommand = new AnonymousCommand<CurveEditorClick>(serviceProvider, Click);
            RemoveSelectedCurveCommand = new AnonymousCommand(serviceProvider, RemoveSelectedCurve);
            ResetViewCommand = new AnonymousCommand<int>(serviceProvider, ResetAxes);
            SelectCommand = new AnonymousCommand<CurveEditorSelection>(serviceProvider, Select);

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
        /// Whether the curve editor can show and edit a property of the given type: an <see cref="IComputeCurve{T}"/> of
        /// <see cref="float"/>, <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, <see cref="Quaternion"/> or
        /// <see cref="Color4"/>.
        /// </summary>
        public static bool CanEdit([NotNull] Type type)
        {
            return type.HasInterface(typeof(IComputeCurve<Color4>))
                || type.HasInterface(typeof(IComputeCurve<float>))
                || type.HasInterface(typeof(IComputeCurve<Quaternion>))
                || type.HasInterface(typeof(IComputeCurve<Vector2>))
                || type.HasInterface(typeof(IComputeCurve<Vector3>))
                || type.HasInterface(typeof(IComputeCurve<Vector4>));
        }

        /// <summary>
        /// Adds the given curve to the curve editor and selects it, or only selects it if it is already listed.
        /// </summary>
        /// <param name="curve">The curve, of a type the editor supports (<see cref="CanEdit"/>).</param>
        /// <param name="name">The name of the curve.</param>
        /// <returns><c>true</c> if the curve is supported, <c>false</c> otherwise (nothing changes then).</returns>
        public bool AddCurve([NotNull] object curve, string name)
        {
            if (curve == null) throw new ArgumentNullException(nameof(curve));

            switch (curve)
            {
                case IComputeCurve<Color4> color4Curve:
                    AddCurve(color4Curve, name);
                    return true;
                case IComputeCurve<float> floatCurve:
                    AddCurve(floatCurve, name);
                    return true;
                case IComputeCurve<Quaternion> quaternionCurve:
                    AddCurve(quaternionCurve, name);
                    return true;
                case IComputeCurve<Vector2> vector2Curve:
                    AddCurve(vector2Curve, name);
                    return true;
                case IComputeCurve<Vector3> vector3Curve:
                    AddCurve(vector3Curve, name);
                    return true;
                case IComputeCurve<Vector4> vector4Curve:
                    AddCurve(vector4Curve, name);
                    return true;
                default:
                    return false;
            }
        }

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

        private void AddPoint(DrawingPoint point)
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

        private void Click(CurveEditorClick click)
        {
            var point = click.Position;
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
                if (click.Mode == CurveSelectionMode.Replace)
                    ClearPointSelection();
                IsControlPointHovered = false;
                return;
            }

            if (!clickedPoint.IsSelected)
            {
                // FIXME: do this atomically in the curve
                Select(new DrawingRect(point.X - 2 * radius, point.Y - 2 * radius, 4 * radius, 4 * radius), click.Mode, true);
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
                    var center = (DrawingPoint)(SelectedControlPoints
                        .Select(s => (DrawingVector?)(s as ControlPointViewModelBase)?.ActualPoint)
                        .Aggregate(new DrawingVector(), (c, s) => s.HasValue ? s.Value+ c : c) / count);
                    SelectedCurve.XAxis?.Center(center);
                    SelectedCurve.YAxis?.Center(center);
                    // The axes moved: draw again now, not on the next input
                    InvalidateView();
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
            // The ranges of the data are computed when the view updates the model: a curve that was just added (the "Edit curve"
            // command adds and focuses it at once) has none yet, and the view would fit the empty default range
            UpdateOverride(true);
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

        private void Select(CurveEditorSelection selection)
        {
            Select(selection.Rectangle, selection.Mode, false);
        }

        private void Select(DrawingRect selectionRect, CurveSelectionMode mode, bool isSingleSelection)
        {
            var editableCurve = SelectedCurve as IEditableCurveViewModel;
            if (editableCurve == null)
                return;

            // TODO: move this to the curve (especially hit testing)
            IList<ControlPointViewModelBase> selectedPoints = editableCurve.ControlPoints.Where(c => selectionRect.Contains(c.ActualPoint)).ToList();
            if (selectedPoints.Count > 1 && isSingleSelection)
            {
                var rectCenter = new DrawingPoint(selectionRect.Left + selectionRect.Width*0.5, selectionRect.Top + selectionRect.Height * 0.5);
                var closest = CurveHelper.GetClosestPoint(selectedPoints, rectCenter);
                selectedPoints = new[] { closest };
            }
            switch (mode)
            {
                case CurveSelectionMode.Replace:
                    ClearPointSelection();
                    goto case CurveSelectionMode.Add;

                case CurveSelectionMode.Add:
                    // FIXME: do this atomically in the curve
                    foreach (var p in selectedPoints.Where(p => !p.IsSelected))
                    {
                        SelectedControlPoints.Add(p);
                        p.IsSelected = true;
                    }
                    break;

                case CurveSelectionMode.Remove:
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
