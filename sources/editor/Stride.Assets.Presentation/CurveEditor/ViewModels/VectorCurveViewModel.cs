// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    /// <summary>
    /// Represents a <see cref="Vector2"/> curve.
    /// Displays each component of the vector in a separate curve.
    /// </summary>
    public sealed class Vector2CurveViewModel : CurveViewModelBase<Vector2>
    {
        private struct VectorData
        {
            private readonly float t;
            private readonly float x;
            private readonly float y;

            public VectorData(float t, float x, float y)
            {
                this.t = t;
                this.x = x;
                this.y = y;
            }

            public float T => t;

            public float this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return x;
                        case 1:
                            return y;
                        default:
                            return T;
                    }
                }
            }
        }

        private readonly List<VectorData> sampleData = new List<VectorData>();

        public Vector2CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector2> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        /// <inheritdoc/>
        protected override void RenderPoints(IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            for (var i = 0; i < 2; ++i)
            {
                var drawPoints = sampleData.Select(d => TransformPoint(new Vector2(d.T, d[i]))).ToList();
                // Render the curve
                RenderLine(drawingContext, ref clippingRect, drawPoints, Editor.GetColor((VectorComponent)i));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateData()
        {
            var curve = UnderlyingCurve;
            if (curve == null)
                return;

            double start, end, increment;
            if (!GetSamplingParameters(XAxis, out start, out end, out increment))
                return;

            // Limit to float precision
            //if (increment < 1E-6f)
            //    increment = 1E-6f;

            // Make sure the curve is up-to-date
            curve.UpdateChanges();
            // Clear the previous samples
            sampleData.Clear();
            // Calculate the new samples
            for (var t = start; t < end; t += increment)
            {
                var ft = (float)t;
                var sample = curve.Evaluate(ft);
                sampleData.Add(new VectorData(ft, sample.X, sample.Y));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
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

            foreach (var data in sampleData)
            {
                var x = data.T;
                if (!XAxis?.IsValidValue(x) ?? true)
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                for (var i = 0; i < 2; ++i)
                {
                    var y = data[i];
                    if (!YAxis?.IsValidValue(y) ?? true)
                        continue;

                    if (y < miny)
                        miny = y;

                    if (y > maxy)
                        maxy = y;
                }
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

    /// <summary>
    /// Represents a <see cref="Vector3"/> curve.
    /// Displays each component of the vector in a separate curve.
    /// </summary>
    public sealed class Vector3CurveViewModel : CurveViewModelBase<Vector3>
    {
        private struct VectorData
        {
            private readonly float t;
            private readonly float x;
            private readonly float y;
            private readonly float z;

            public VectorData(float t, float x, float y, float z)
            {
                this.t = t;
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public float T => t;

            public float this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return x;
                        case 1:
                            return y;
                        case 2:
                            return z;
                        default:
                            return T;
                    }
                }
            }
        }

        private readonly List<VectorData> sampleData = new List<VectorData>();

        public Vector3CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector3> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        /// <inheritdoc/>
        protected override void RenderPoints(IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            for (var i = 0; i < 3; ++i)
            {
                var drawPoints = sampleData.Select(d => TransformPoint(new Vector2(d.T, d[i]))).ToList();
                // Render the curve
                RenderLine(drawingContext, ref clippingRect, drawPoints, Editor.GetColor((VectorComponent)i));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateData()
        {
            var curve = UnderlyingCurve;
            if (curve == null)
                return;

            double start, end, increment;
            if (!GetSamplingParameters(XAxis, out start, out end, out increment))
                return;

            // Limit to float precision
            //if (increment < 1E-6f)
            //    increment = 1E-6f;

            // Make sure the curve is up-to-date
            curve.UpdateChanges();
            // Clear the previous samples
            sampleData.Clear();
            // Calculate the new samples
            for (var t = start; t < end; t += increment)
            {
                var ft = (float)t;
                var sample = curve.Evaluate(ft);
                sampleData.Add(new VectorData(ft, sample.X, sample.Y, sample.Z));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
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

            foreach (var data in sampleData)
            {
                var x = data.T;
                if (!XAxis?.IsValidValue(x) ?? true)
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                for (var i = 0; i < 3; ++i)
                {
                    var y = data[i];
                    if (!YAxis?.IsValidValue(y) ?? true)
                        continue;

                    if (y < miny)
                        miny = y;

                    if (y > maxy)
                        maxy = y;
                }
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

    /// <summary>
    /// Represents a <see cref="Vector4"/> curve.
    /// Displays each component of the vector in a separate curve.
    /// </summary>
    public sealed class Vector4CurveViewModel : CurveViewModelBase<Vector4>
    {
        private struct VectorData
        {
            private readonly float t;
            private readonly float x;
            private readonly float y;
            private readonly float z;
            private readonly float w;

            public VectorData(float t, float x, float y, float z, float w)
            {
                this.t = t;
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public float T => t;

            public float this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return x;
                        case 1:
                            return y;
                        case 2:
                            return z;
                        case 3:
                            return w;
                        default:
                            return T;
                    }
                }
            }
        }
        
        private readonly List<VectorData> sampleData = new List<VectorData>();

        public Vector4CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector4> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        /// <inheritdoc/>
        protected override void RenderPoints(IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            for (var i = 0; i < 4; ++i)
            {
                var drawPoints = sampleData.Select(d => TransformPoint(new Vector2(d.T, d[i]))).ToList();
                // Render the curve
                RenderLine(drawingContext, ref clippingRect, drawPoints, Editor.GetColor((VectorComponent)i));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateData()
        {
            var curve = UnderlyingCurve;
            if (curve == null)
                return;

            double start, end, increment;
            if (!GetSamplingParameters(XAxis, out start, out end, out increment))
                return;

            // Limit to float precision
            //if (increment < 1E-6f)
            //    increment = 1E-6f;

            // Make sure the curve is up-to-date
            curve.UpdateChanges();
            // Clear the previous samples
            sampleData.Clear();
            // Calculate the new samples
            for (var t = start; t < end; t += increment)
            {
                var ft = (float)t;
                var sample = curve.Evaluate(ft);
                sampleData.Add(new VectorData(ft, sample.X, sample.Y, sample.Z, sample.W));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
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

            foreach (var data in sampleData)
            {
                var x = data.T;
                if (!XAxis?.IsValidValue(x) ?? true)
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                for (var i = 0; i < 4; ++i)
                {
                    var y = data[i];
                    if (!YAxis?.IsValidValue(y) ?? true)
                        continue;

                    if (y < miny)
                        miny = y;

                    if (y > maxy)
                        maxy = y;
                }
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
