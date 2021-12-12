using Stride.Core;

namespace Stride.Engine.Splines
{
    [DataContract]
    public struct SplineDebugInfo
    {
        private bool _segments;
        private bool _points;
        private bool _boundingBox;

        public bool Points
        {
            get { return _points; }
            set
            {
                _points = value;
            }
        }

        public bool Segments
        {
            get { return _segments; }
            set
            {
                _segments = value;
            }
        }

        public bool BoundingBox
        {
            get { return _boundingBox; }
            set
            {
                _boundingBox = value;
            }
        }
    }
}
