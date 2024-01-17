using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract]
    public struct FilterByDistance
    {
        /// <summary>
        /// 0 == Feature disabled
        /// </summary>
        public ushort Id;
        /// <summary>
        /// Collision occurs if delta > 1
        /// </summary>
        public ushort XAxis;
        /// <summary>
        /// Collision occurs if delta > 1
        /// </summary>
        public ushort YAxis;
        /// <summary>
        /// Collision occurs if delta > 1
        /// </summary>
        public ushort ZAxis;
    }
}