using System.Collections.Generic;
using Stride.Core.Mathematics;
namespace Stride.Engine.Splines
{
    public class Spline
    {
        private List<BezierCurve> _bezierCurves;
        public List<BezierCurve> BezierCurves
        {
            get
            {
                if (_bezierCurves == null)
                {
                    _bezierCurves = new List<BezierCurve>();
                }
                return _bezierCurves;
            }
            set
            {
                _bezierCurves = value;
            }
        }

        public void Update()
        {
            foreach (var curve in BezierCurves)
            {
                curve.Update();
            }
        }

        public void AddBezierCurve(BezierCurve bezierCurve)
        {
            BezierCurves.Add(bezierCurve);
        }

        public float GetDistance()
        {
            float distance = 0;
            foreach (var curve in BezierCurves)
            {
                distance += curve.Distance;
            }
            return distance;
        }
    }
}
