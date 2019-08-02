// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Drawing;
using Xenko.Core.Presentation.Extensions;
using Xenko.Core.Quantum;
using Xenko.Animations;

namespace Xenko.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;

    public abstract class KeyFrameCurveViewModel<TValue> : EditableCurveViewModel<TValue>
        where TValue : struct
    {
        private readonly ObservableList<KeyFrameControlPointViewModel<TValue>> controlPoints = new ObservableList<KeyFrameControlPointViewModel<TValue>>();

        protected KeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] ComputeAnimationCurve<TValue> computeCurve, string name = null)
            : base(editor, parent, computeCurve, name)
        {
            KeyFramesNode = CurveNode[nameof(computeCurve.KeyFrames)].Target;
            KeyFramesNode.ItemChanged += KeyFramesContentChanged;
            controlPoints.CollectionChanged += ControlPointsCollectionChanged;
        }

        public override IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints => controlPoints;

        protected IObjectNode KeyFramesNode { get; }

        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(KeyFrameCurveViewModel<TValue>));

            controlPoints.CollectionChanged -= ControlPointsCollectionChanged;
            foreach (var point in controlPoints)
            {
                point.PropertyChanged -= ControlPointPropertyChanged;
                point.Destroy();
            }
            controlPoints.Clear();
            KeyFramesNode.ItemChanged -= KeyFramesContentChanged;

            base.Destroy();
        }

        public override bool RemovePoint(ControlPointViewModelBase point)
        {
            var kfcp = point as KeyFrameControlPointViewModel<TValue>;
            if (kfcp == null)
                return false;
            var index = new NodeIndex(controlPoints.IndexOf(kfcp));
            if (index.Int == -1)
                return false;

            var kf = KeyFramesNode.Retrieve(index);
            KeyFramesNode.Remove(kf, index);
            return true;
        }

        protected virtual KeyFrameControlPointViewModel<TValue> CreateControlPoint([NotNull] IObjectNode node)
        {
            var keyNode = node[nameof(AnimationKeyFrame<TValue>.Key)];
            var valueNode = node[nameof(AnimationKeyFrame<TValue>.Value)];
            var tangentTypeNode = node[nameof(AnimationKeyFrame<TValue>.TangentType)];
            return CreateKeyFrameControlPoint(keyNode, valueNode, tangentTypeNode);
        }

        protected abstract KeyFrameControlPointViewModel<TValue> CreateKeyFrameControlPoint([NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode);

        protected NodeIndex GetInsertIndex(Vector2 point)
        {
            // Assuming the key frames are ordered
            var index = controlPoints.FindIndex(kf => (kf.IsSynchronized ? kf.ActualKey : kf.Key) > point.X);
            return new NodeIndex(index >= 0 ? index : controlPoints.Count);
        }

        protected sealed override void InitializeOverride()
        {
            var keyFrames = KeyFramesNode.Retrieve() as ICollection<AnimationKeyFrame<TValue>>;
            if (keyFrames != null)
            {
                KeyFrameControlPointViewModel<TValue> previous = null;
                foreach (var keyFrame in keyFrames)
                {
                    var node = Editor.Session.AssetNodeContainer.GetOrCreateNode(keyFrame);
                    // create a control point for the current node
                    var controlPoint = CreateControlPoint(node);
                    if (controlPoint != null)
                    {
                        if (previous != null)
                        {
                            // build the double-linked references
                            previous.Next = controlPoint;
                            controlPoint.Previous = previous;
                        }
                        previous = controlPoint;
                        // add at the end of the list
                        controlPoints.Add(controlPoint);
                    }
                }
            }
        }

        protected virtual void KeyFramesContentChanged(object sender, ItemChangeEventArgs e)
        {
            if (!IsInitialized)
                return;

            if (!e.Index.IsInt)
                return;

            var index = e.Index.Int;
            KeyFrameControlPointViewModel<TValue> controlPoint;
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                case ContentChangeType.CollectionUpdate:
                    return;

                case ContentChangeType.CollectionAdd:
                    var node = Editor.Session.AssetNodeContainer.GetNode(e.NewValue);
                    controlPoint = CreateControlPoint(node);
                    if (controlPoint != null)
                    {
                        // update the double-linked references
                        if (index > 0)
                        {
                            controlPoint.Previous = controlPoints[index - 1];
                            controlPoints[index - 1].Next = controlPoint;
                        }
                        if (index < controlPoints.Count)
                        {
                            controlPoint.Next = controlPoints[index];
                            controlPoints[index].Previous = controlPoint;
                        }
                        // insert into the list
                        controlPoints.Insert(index, controlPoint);
                    }
                    break;

                case ContentChangeType.CollectionRemove:
                    // update the double-linked references
                    controlPoint = controlPoints[index];
                    if (index > 0)
                        controlPoints[index - 1].Next = controlPoint.Next;
                    if (index + 1 < controlPoints.Count)
                        controlPoints[index + 1].Previous = controlPoint.Previous;
                    // remove from the list
                    controlPoints.RemoveAt(index);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected sealed override void RenderPoints([NotNull] IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            var controlPointCount = controlPoints.Count;
            if (controlPointCount == 0)
            {
                drawingContext.DrawTexts(new[] { clippingRect.GetCenterLocation() }, Color.LightGray, new[] { $"{Resources.Strings.KeyGestures.GestureAddPoint} to add a keyframe" },
                    XAxis.FontFamily, XAxis.FontSize * 2, FontWeights.Bold, HorizontalAlignment.Center, VerticalAlignment.Center);

                return;
            }

#if DEBUG_POINTS_ORDER
            var sortedPoints = new List<KeyFrameControlPointViewModel<TValue>>(controlPoints.Count);
            var firstPoint = controlPoints[0];
            while (firstPoint != null)
            {
                sortedPoints.Add(firstPoint);
                firstPoint = firstPoint.Next as KeyFrameControlPointViewModel<TValue>;
            }
#else
            // control points can temporarily be unordered during the dragging of one or more points
            var sortedPoints = controlPoints.OrderBy(p => p.ActualPoint.X).ToList();
#endif
            // sample the curve
            var drawPoints = new List<WindowsPoint>();
            for (var i = 0; i < controlPointCount - 1; ++i)
            {
                var samples = SampleControlPoints(sortedPoints[i], sortedPoints[i + 1]);
                drawPoints.AddRange(samples.Select(TransformPoint));
            }
            // last point
            drawPoints.Add(sortedPoints[controlPoints.Count - 1].ActualPoint);
            // render the curve
            RenderLine(drawingContext, ref clippingRect, drawPoints, Color);

            if (!isCurrentCurve)
                return;

            // Retrieve actual points (and selected actual points)
            var actualPoints = new List<WindowsPoint>();
            var selectedActualPoints = new List<WindowsPoint>();
            foreach (var controlPoint in controlPoints)
            {
                var actualPoint = controlPoint.ActualPoint;
                actualPoints.Add(actualPoint);
                if (controlPoint.IsSelected)
                    selectedActualPoints.Add(actualPoint);
            }
            // Render the control points
            var radius = ControlPointRadius;
            drawingContext.DrawCircles(actualPoints, radius, ControlPointColor, ControlPointColor);
            // Render the selection circles over the selected control points
            radius *= 2;
            drawingContext.DrawCircles(selectedActualPoints, radius, Color.Transparent, Color.WhiteSmoke);
            // Render the tangents
            // TODO: also render tangents
        }

        /// <summary>
        /// Samples the curve between the given control points.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        protected virtual IEnumerable<Vector2> SampleControlPoints([NotNull] KeyFrameControlPointViewModel<TValue> point1, KeyFrameControlPointViewModel<TValue> point2)
        {
            // linear interpolation by default
            yield return point1.Point;
        }

        protected internal sealed override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            var minx = MinX;
            var miny = MinY;
            var maxx = MaxX;
            var maxy = MaxY;

            if (double.IsNaN(minx))
                minx = double.MaxValue;

            if (double.IsNaN(miny))
                miny = double.MaxValue;

            if (double.IsNaN(maxx))
                maxx = double.MinValue;

            if (double.IsNaN(maxy))
                maxy = double.MinValue;

            foreach (var point in controlPoints)
            {
                var x = point.IsSynchronized ? point.ActualKey : point.Key;
                var y = point.IsSynchronized ? point.ActualValue : point.Value;
                
                if (!IsValidPoint(x, y))
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                if (y < miny)
                    miny = y;

                if (y > maxy)
                    maxy = y;
            }

            if (minx < double.MaxValue)
                MinX = minx;

            if (miny < double.MaxValue)
                MinY = miny;

            if (maxx > double.MinValue)
                MaxX = maxx;

            if (maxy > double.MinValue)
                MaxY = maxy;
        }
    }
}
