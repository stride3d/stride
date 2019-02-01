// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<SimpleConvexHullGenerator>))]
    [DataContract("SimpleConvexHullGenerator")]
    [Display("Simple")]
    public class SimpleConvexHullGenerator : IVhacdConvexHullGenerator
    {
        public bool SimpleHull => true;

        public int Depth => 0;

        public int PosSampling => 0;

        public int AngleSampling => 0;

        public int PosRefine => 0;

        public int AngleRefine => 0;

        public float Alpha => 0f;

        public float Threshold => 0f;

        public bool Match(object obj)
        {
            var other = obj as SimpleConvexHullGenerator;

            if (other == null)
            {
                return false;
            }

            return true;
        }
    }
}
