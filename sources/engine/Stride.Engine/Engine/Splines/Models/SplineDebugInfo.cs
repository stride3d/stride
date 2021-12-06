using Stride.Core;

namespace Stride.Engine.Splines
{
    [DataContract]
    public struct SplineDebugInfo
    {
        private bool _nodes;
        private bool _segments;
        private bool _points;
        private bool _boundingBox;

        [DataMemberIgnore]
        public bool IsDirty { get; set; }

        public bool Points
        {
            get { return _points; }
            set
            {
                _points = value;
                IsDirty = true;
            }
        }

        public bool Segments
        {
            get { return _segments; }
            set
            {
                _segments = value;
                IsDirty = true;
            }
        }

        public bool Nodes
        {
            get { return _nodes; }
            set
            {
                _nodes = value;
                IsDirty = true;
            }
        }

        public bool BoundingBox
        {
            get { return _boundingBox; }
            set
            {
                _boundingBox = value;
                IsDirty = true;
            }
        }
    }
}
