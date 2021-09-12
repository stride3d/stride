using System;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines
{
    public class BezierCurve
    {
        private int bezierPointCount = 3;
        private int baseBezierPointCount = 100;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        public Vector3 Position { get; private set; }
        public Vector3 TangentPosition { get; private set; }

        public Vector3 TargetPosition { get; private set; }
        public Vector3 TargetTangentPosition { get; private set; }

        private BezierPoint[] _baseBezierPoints;
        private BezierPoint[] _parameterizedBezierPoints;

        public BoundingBox BoundingBox { get; private set; }

        public float Distance { get; private set; } = 0;

        public BezierCurve(int segments, Vector3 position, Vector3 tangentPosition, Vector3 targetPosition, Vector3 targetTangentPosition)
        {
            bezierPointCount = segments + 1;
            baseBezierPointCount = bezierPointCount > baseBezierPointCount ? baseBezierPointCount + 10 : baseBezierPointCount;

            Position = position;
            TangentPosition = tangentPosition;
            TargetPosition = targetPosition;
            TargetTangentPosition = targetTangentPosition;

            Update();
        }

        public void SetPosition(Vector3 position, bool update = false)
        {
            Position = position;

            if (update)
                Update();
        }

        public void SetTangentPosition(Vector3 tangetPosition, bool update = false)
        {
            TangentPosition = tangetPosition;

            if (update)
                Update();
        }

        public void SetTargetPosition(Vector3 targetPosition, bool update = false)
        {
            TargetPosition = targetPosition;

            if (update)
                Update();
        }

        public void SetTargetTangentPosition(Vector3 targetTangentPosition, bool update = false)
        {
            TargetTangentPosition = targetTangentPosition;

            if (update)
                Update();
        }

        public BezierPoint[] GetBezierPoints()
        {
            return _parameterizedBezierPoints;
        }

        public class BezierPoint
        {
            public Vector3 WorldPosition;
            public Vector3 Rotation;
            public float PointDistance;
            public float Distance;
        }

        public void Update()
        {
            _baseBezierPoints = new BezierPoint[baseBezierPointCount];
            _parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            p0 = Position;
            p1 = TangentPosition;
            p2 = TargetTangentPosition;
            p3 = TargetPosition;

            // 2 methods of arc length parameterization: Use a larger amount of pre calculated points or devide segments per distance
            // We create a base spline that contains a large amount of segments.
            // Later on we can distill this as a way of determining arc length parameterization
            float t = 1.0f / (baseBezierPointCount - 1);
            for (var i = 0; i < baseBezierPointCount; i++)
            {
                _baseBezierPoints[i] = new BezierPoint { WorldPosition = CalculateBezierPoint(t * i) };

                if (i == 0)
                {
                    _baseBezierPoints[i].PointDistance = 0;
                    _baseBezierPoints[i].Distance = 0;
                }
                else
                {
                    var distance = Vector3.Distance(_baseBezierPoints[i].WorldPosition, _baseBezierPoints[i - 1].WorldPosition);
                    //_baseBezierPoints[i].PointDistance = distance;
                    _baseBezierPoints[i].Distance = _baseBezierPoints[i - 1].Distance + distance;
                }
            }

            Distance += _baseBezierPoints[baseBezierPointCount - 1].Distance;

            ArcLengthParameterization();
        }

        public Vector3 GetPositionOnCurve(float percentage)
        {
            var distance = (Distance / 100) * Math.Clamp(percentage, 0, 100);
            return GetBezierPointForDistance(distance).WorldPosition;
        }

        /// <summary>
        /// polynominal curve has incorrect arc length parameterization. Use approximated estimated positions
        /// </summary>
        private void ArcLengthParameterization()
        {
            _parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            if (Distance <= 0)
                return;

            for (var i = 0; i < bezierPointCount; i++)
            {
                var estimatedExptedDistance = (Distance / (bezierPointCount - 1)) * i;
                _parameterizedBezierPoints[i] = GetBezierPointForDistance(estimatedExptedDistance);
            }

            _parameterizedBezierPoints[bezierPointCount - 1] = _baseBezierPoints[baseBezierPointCount - 1];
        }



        private BezierPoint GetBezierPointForDistance(float distance)
        {
            for (int j = 0; j < baseBezierPointCount; j++)
            {
                var curPoint = _baseBezierPoints[j];
                if (curPoint.Distance >= distance)
                {
                    return curPoint;
                }
            }
            return _baseBezierPoints[_baseBezierPoints.Length - 1];
        }

        private Vector3 CalculateBezierPoint(float t)
        {
            var tPower3 = t * t * t;
            var tPower2 = t * t;
            var oneMinusT = 1 - t;
            var oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
            var oneMinusTPower2 = oneMinusT * oneMinusT;
            var x = oneMinusTPower3 * p0.X + (3 * oneMinusTPower2 * t * p1.X) + (3 * oneMinusT * tPower2 * p2.X) + tPower3 * p3.X;
            var y = oneMinusTPower3 * p0.Y + (3 * oneMinusTPower2 * t * p1.Y) + (3 * oneMinusT * tPower2 * p2.Y) + tPower3 * p3.Y;
            var z = oneMinusTPower3 * p0.Z + (3 * oneMinusTPower2 * t * p1.Z) + (3 * oneMinusT * tPower2 * p2.Z) + tPower3 * p3.Z;
            return new Vector3(x, y, z);
        }

        //    public ClosestPointInfo GetClosestPointOnCurve(Vector3 otherPosition)
        //    {
        //        //determine closest splinepoint
        //        var shortestSplinePointIndex = 1;
        //        float shortestSplinePointDistance = 0f;
        //        for (var i = 0; i < bezierPointCount; i++)//do	
        //        {
        //            var curSplinePointDistance = Vector3.Distance(_bezierPoints[i].Position, otherPosition);

        //            if (curSplinePointDistance < shortestSplinePointDistance)
        //            {
        //                shortestSplinePointDistance = curSplinePointDistance;
        //                shortestSplinePointIndex = i;
        //            }
        //        }

        //        //determine previous or current splinepoint
        //        float shortestPreviousDistance = 0f;
        //        float shortestNextDistance = 0f;
        //        if (shortestSplinePointIndex - 1 > 0)
        //        {
        //            shortestPreviousDistance = Vector3.Distance(_bezierPoints[shortestSplinePointIndex - 1].Position, otherPosition);
        //        }

        //        if (shortestSplinePointIndex + 1 <= _bezierPoints.Length)
        //        {
        //            shortestNextDistance = Vector3.Distance(_bezierPoints[shortestSplinePointIndex + 1].Position, otherPosition);
        //        }

        //        if (shortestPreviousDistance < shortestNextDistance)
        //        {
        //            shortestSplinePointIndex -= 1;
        //        }

        //        //Gather info
        //        var info = new ClosestPointInfo()
        //        {
        //            APosition = _bezierPoints[shortestSplinePointIndex].Position,
        //            AIndex = shortestSplinePointIndex
        //        };

        //        if (shortestSplinePointIndex + 1 <= _bezierPoints.Length)
        //        {
        //            info.BPosition = _bezierPoints[shortestSplinePointIndex + 1].Position;
        //            info.BIndex = shortestSplinePointIndex + 1;
        //        }

        //        if (info.BPosition != null)
        //        {
        //            info.ClosestPoint = ProjectPointOnLineSegment(info.APosition, info.BPosition, otherPosition);
        //        }
        //        else
        //        {
        //            info.ClosestPoint = info.APosition;
        //        }

        //        return info;
        //    }

        //    public struct ClosestPointInfo
        //    {
        //        public Vector3 ClosestPoint;
        //        public Vector3 APosition;
        //        public int AIndex;
        //        public Vector3 BPosition;
        //        public int BIndex;
        //    }

        //    public Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        //    {
        //        var vector = linePoint2 - linePoint1;
        //        vector.Normalize();
        //        var projectedPoint = ProjectPointOnLine(linePoint1, vector, point);
        //        var side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

        //        if (side == 0)
        //        {
        //            return projectedPoint;
        //        }


        //        if (side == 1)
        //        {

        //            return linePoint1;
        //        }


        //        //if (side == 2)
        //        {

        //            return linePoint2;
        //        }
        //    }

        //    public Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
        //    {
        //        var t = Vector3.Dot(point, linePoint);
        //        return linePoint + lineVec * t;
        //    }

        //    public int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        //    {
        //        var lineVec = linePoint2 - linePoint1;
        //        var pointVec = point - linePoint1;
        //        var dot = Vector3.Dot(linePoint1, linePoint2);

        //        if (dot > 0)
        //        {

        //            if (pointVec.Length() * 2 <= lineVec.Length() * 2)
        //            {
        //                return 0;
        //            }
        //            else
        //            {
        //                return 2;
        //            }
        //        }
        //        else
        //        {
        //            return 1;
        //        }
        //    }
        //}
    }
}
