// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Physics
{
    [DataContract]
    public struct HeightfieldCenteringParameters
    {
        [DataMember(10)]
        public bool Enabled { get; set; }

        /// <summary>
        /// The height to be centered.
        /// </summary>
        [DataMember(20)]
        [InlineProperty]
        public float CenterHeight { get; set; }

        public bool Match(HeightfieldCenteringParameters other)
        {
            return other.Enabled == Enabled &&
                Math.Abs(other.CenterHeight - CenterHeight) < float.Epsilon;
        }
    }
}
