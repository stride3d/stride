//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineNode
    {
        private BezierPoint[] parameterizedBezierPoints;
        private int bezierPointCount = 3;

        [DataMemberIgnore]
        public Vector3 WorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TangentOutWorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TangentInWorldPosition { get; set; }

        [DataMemberIgnore]

        public Vector3 TargetTangentInWorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TargetWorldPosition { get; set; }

        #region Segments
        private int segments = 1;
        /// <summary>
        /// A minimum of 1
        /// </summary>
        /// <userdoc>The amount of segments the curve exists out of</userdoc>
        [Display(1, "Segments")]
        public int Segments
        {
            get { return segments; }
            set
            {
                if (value < 1)
                {
                    segments = 1;
                }
                else
                {
                    segments = value;
                    bezierPointCount = segments + 1;
                }
                
                InvokeOnDirty();
            }
        }
        #endregion

        #region Out
        private Vector3 tangentOut { get; set; }
        [Display(2, "Tangent out")]
        public Vector3 TangentOutLocal
        {
            get { return tangentOut; }
            set
            {
                tangentOut = value;
                InvokeOnDirty();
            }
        }
        #endregion

        #region In
        private Vector3 tangentIn { get; set; }
        [Display(3, "Tangent in")]
        public Vector3 TangentInLocal

        {
            get { return tangentIn; }
            set
            {
                tangentIn = value;
                InvokeOnDirty();
            }
        }
        #endregion



        public BoundingBox BoundingBox { get; set; }

        public float Length { get; private set; } = 0;

        public delegate void BezierCurveDirtyEventHandler();
        public event BezierCurveDirtyEventHandler OnSplineNodeDirty;

        public SplineNode()
        {
        }

        public void InvokeOnDirty()
        {
            OnSplineNodeDirty?.Invoke();
        }

        public BezierPoint[] GetBezierPoints()
        {
            return parameterizedBezierPoints;
        }

        /// <summary>
        /// This method is used internally to set the updated bezier points. This is done after a parameterization update in the BezierCurveBuilder
        /// </summary>
        /// <param name="bezierPoints"></param>
        /// <param name="updatedParameterizedBezierPoints"></param>
        public void SetCalculatedBezierCurveValues(float length, BezierPoint[] updatedParameterizedBezierPoints, BoundingBox box)
        {
            Length = length;
            parameterizedBezierPoints = updatedParameterizedBezierPoints;
            BoundingBox = box;
        }
        
        
        /// <summary>
        /// Updates the last bezierpoint rotation
        /// </summary>
        public void UpdateLastBezierPointRotation(Quaternion rotation)
        {
            ////Point last bezier point to next bezier point from the next bezier curve
            //var normalDif = Vector3.Normalize(position - baseBezierPoints[baseBezierPointCount - 1].Position);
            //baseBezierPoints[baseBezierPointCount - 1].Rotation = Quaternion.LookRotation(normalDif, Vector3.UnitY);
            parameterizedBezierPoints[bezierPointCount - 1].Rotation = rotation;
        }

        /// <summary>
        /// Returns the World position by a given percentage
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public Vector3 GetPositionOnBezierCurve(float percentage)
        {
            var distance = Length / 100 * Math.Clamp(percentage, 0, 100);
            return GetBezierPointForDistance(distance).Position;
        }

        /// <summary>
        /// Retrieves information about the closest point on the spline in relation to the given world position
        /// </summary>
        /// <param name="originPosition">A Vector3 world position </param>
        /// <returns></returns>
        public SplinePositionInfo GetClosestPointOnBezierCurve(Vector3 originPosition)
        {
            SplinePositionInfo info = null;
            for (var i = 0; i < bezierPointCount; i++)
            {
                var currentBezierPoint = GetBezierPoints()[i];
                var curSplinePointDistance = Vector3.Distance(currentBezierPoint.Position, originPosition);

                if (info == null || curSplinePointDistance < info.DistanceToOrigin)
                {
                    info ??= new SplinePositionInfo();
                    info.ClosestBezierPoint = currentBezierPoint;
                    info.Position = currentBezierPoint.Position;
                    info.ClosestBezierPointIndex = i;
                    info.DistanceToOrigin = curSplinePointDistance;
                    info.LengthOnCurve = currentBezierPoint.TotalLengthOnCurve;
                }
            }
            return info;
        }
        
        private BezierPoint GetBezierPointForDistance(float estimatedExpectedDistance)
        {
            for (var j = 0; j < bezierPointCount; j++)
            {
                var curPoint = parameterizedBezierPoints[j];
                if (curPoint.TotalLengthOnCurve >= estimatedExpectedDistance)
                {
                    return curPoint;
                }
            }
            return parameterizedBezierPoints[^1];
        }
    }
}
