// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public enum RotationDisplayMode
    {
        Quaternion,
        Euler,
        AxisAngle
    }

    /// <summary>
    /// Represents a <see cref="Quaternion"/> curve.
    /// Three modes of display (<see cref="RotationDisplayMode"/>):
    /// <list type="bullet">
    /// <item><see cref="RotationDisplayMode.Quaternion"/>: displays each component of the quaternion in a separate curve.</item>
    /// <item><see cref="RotationDisplayMode.Euler"/>: displays each euler angles in a separate curve.</item>
    /// <item><see cref="RotationDisplayMode.AxisAngle"/>: displays the axis of the rotation as three curves (one for each component of the axis) and one curve for the angle.</item>
    /// </list>
    /// </summary>
    public sealed class RotationCurveViewModel : CurveViewModelBase<Quaternion>
    {
        private struct RotationData
        {
            private readonly float t;
            private readonly float x;
            private readonly float y;
            private readonly float z;
            private readonly float w;

            public RotationData(float t, float x, float y, float z, float w)
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
        
        private readonly List<RotationData> sampleData = new List<RotationData>();
        private RotationDisplayMode displayMode = RotationDisplayMode.Euler;

        public RotationCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Quaternion> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        public RotationDisplayMode DisplayMode { get { return displayMode; } set { SetValue(ref displayMode, value, Refresh); } }

        /// <inheritdoc/>
        protected override void RenderPoints(IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            var lineCount = DisplayMode == RotationDisplayMode.Euler ? 3 : 4;
            for (var i = 0; i < lineCount; ++i)
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

                Vector3 vector;
                switch (DisplayMode)
                {
                    case RotationDisplayMode.Quaternion:
                        sampleData.Add(new RotationData(ft, sample.X, sample.Y, sample.Z, sample.W));
                        break;

                    case RotationDisplayMode.Euler:
                        var matrix = Matrix.RotationQuaternion(sample);
                        DecomposeXYZ(matrix, out vector);
                        //matrix.DecomposeXYZ(out decomposed);
                        sampleData.Add(new RotationData(ft, MathUtil.RadiansToDegrees(vector.X), MathUtil.RadiansToDegrees(vector.Y), MathUtil.RadiansToDegrees(vector.Z), float.NaN));
                        break;

                    case RotationDisplayMode.AxisAngle:
                        vector = sample.Axis;
                        vector.Normalize();
                        sampleData.Add(new RotationData(ft, vector.X, vector.Y, vector.Z, MathUtil.RadiansToDegrees(sample.Angle)));
                        break;

                    default:
                        continue;
                }
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

        /// <summary>
        /// This method should have better precision than <see cref="Matrix.DecomposeXYZ(out Vector3)"/>, especially on the y-axis.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="rotation"></param>
        private static void DecomposeXYZ(Matrix matrix, out Vector3 rotation)
        {
            var rotY = Math.Abs(matrix.M13) - 1 < 1e-6f ? Math.Acos(Math.Max(Math.Abs(matrix.M11), Math.Abs(matrix.M33))) : Math.Asin(-matrix.M13);
            rotation.Y = (float)(Math.Sign(-matrix.M13)*rotY);
            var test = Math.Cos(rotY);
            if (test > 1e-6f)
            {
                rotation.Z = (float)Math.Atan2(matrix.M12, matrix.M11);
                rotation.X = (float)Math.Atan2(matrix.M23, matrix.M33);
            }
            else
            {
                rotation.Z = (float)Math.Atan2(-matrix.M21, matrix.M31);
                rotation.X = 0.0f;
            }
        }
    }
}
