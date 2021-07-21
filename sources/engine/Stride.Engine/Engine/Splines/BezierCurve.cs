using System;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines
{
    public class BezierCurve
    {
        private int _segments = 2;
        private int bezierPointCount = 3;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        public Vector3 Position { get; private set; }
        public Vector3 TangentPosition { get; private set; }

        public Vector3 TargetPosition { get; private set; }
        public Vector3 TargetTangentPosition { get; private set; }

        private BezierPoint[] _tempBezierPoints;
        private BezierPoint[] _redistributedBezierPoints;


        public float Distance { get; private set; } = 0;

        public BezierCurve(int segments, Vector3 position, Vector3 tangentPosition, Vector3 targetPosition, Vector3 targetTangentPosition)
        {
            _segments = segments;
            bezierPointCount = _segments + 1;

            Position = position;
            TangentPosition = tangentPosition;
            TargetPosition = targetPosition;
            TargetTangentPosition = targetTangentPosition;

            _tempBezierPoints = new BezierPoint[bezierPointCount];

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
            //return _redistributedBezierPoints;
            return _tempBezierPoints;
        }

        public struct BezierPoint
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public float PointDistance;
            public float TotalNodeDistance;
        }

        public void Update()
        {
            p0 = Position;
            p1 = TangentPosition;
            p2 = TargetTangentPosition;
            p3 = TargetPosition;

            //var oriPivot = Pivot:Create();
            //var targetPivot = Pivot:Create();

            float t = 1.0f / _segments;
            for (var i = 0; i < bezierPointCount; i++)
            {
                var p = CalculateBezierPoint(t * (i));
                _tempBezierPoints[i].Position = p;

                if (i > 0)
                {
                    //oriPivot.SetPosition(_splinePoints[i - 1], true);
                    //targetPivot.SetPosition(p, true);
                    //oriPivot.Point(targetPivot);

                    //todo fix rotation
                    //_splinePoints[i - 1].rotation = oriPivot:GetRotation(true);

                    if (i == bezierPointCount)
                    {
                        _tempBezierPoints[i].Rotation = _tempBezierPoints[i - 1].Rotation;
                    }

                    var distance = Vector3.Distance(_tempBezierPoints[i].Position, _tempBezierPoints[i - 1].Position);
                    Console.WriteLine(i + " - " + distance);
                    _tempBezierPoints[i].PointDistance = distance;
                    _tempBezierPoints[i].TotalNodeDistance = _tempBezierPoints[i - 1].TotalNodeDistance + distance;
                }
                else
                {
                    _tempBezierPoints[i].PointDistance = 0;
                    _tempBezierPoints[i].TotalNodeDistance = 0;
                }
            }

            for (int i = 0; i < bezierPointCount; i++)
            {
                Distance += _tempBezierPoints[i].PointDistance;
            }

            Console.WriteLine("disc - " + Distance);


            //Redistribute();
        }

        /// <summary>
        /// polynominal curve has incorrect arc length parameterization. Use approximated estimated position
        /// </summary>
        private void Redistribute()
        {
            _redistributedBezierPoints = new BezierPoint[bezierPointCount];


            for (var i = 1; i < bezierPointCount-2; i++)
            {
                Console.WriteLine("Distribute - " + i);

                var estimatedExptedDistance = (Distance / bezierPointCount) * i;
                
                var bezierPoint = _tempBezierPoints[i];

                //if lower than total current node distance
                Vector3 estimatedPos;

                if (estimatedExptedDistance < bezierPoint.TotalNodeDistance)
                {
                    Console.WriteLine("Shorter - ");

                    var prevBezierPoint = _tempBezierPoints[i-1];
                    estimatedPos = Vector3.Lerp(prevBezierPoint.Position, bezierPoint.Position, estimatedExptedDistance / bezierPoint.TotalNodeDistance);
                }
                else
                {
                    Console.WriteLine("Longer- ");
                    var nextBezierPoint = _tempBezierPoints[i+1];
                    estimatedPos = Vector3.Lerp(bezierPoint.Position, nextBezierPoint.Position, estimatedExptedDistance / nextBezierPoint.TotalNodeDistance);

                }

                _redistributedBezierPoints[i].Position = estimatedPos;
                _redistributedBezierPoints[i].PointDistance = estimatedExptedDistance;
                _redistributedBezierPoints[i].TotalNodeDistance = _redistributedBezierPoints[i - 1].TotalNodeDistance + estimatedExptedDistance;
            }
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
