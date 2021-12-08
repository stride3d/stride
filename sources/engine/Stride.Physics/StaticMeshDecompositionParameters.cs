// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshDecompositionParameters>))]
    [DataContract("StaticMeshDecompositionParameters")]
    [Display("StaticMeshDecompositionParameters")]
    public class StaticMeshDecompositionParameters
    {
        /// <userdoc>
        /// If this is unchecked the following parameters are totally ignored, as only a simple convex hull of the whole model will be generated.
        /// </userdoc>
        ///

        public bool Match(object obj)
        {
            var other = obj as StaticMeshDecompositionParameters;

            if (other == null)
            {
                return false;
            }

            return true;
        }
    }
}
