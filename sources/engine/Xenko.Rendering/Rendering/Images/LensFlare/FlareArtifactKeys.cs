// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="FlareArtifact"/> and FlareArtifactEffect xkfx.
    /// </summary>
    internal static class FlareArtifactKeys
    {
        public static readonly PermutationParameterKey<int> Count = ParameterKeys.NewPermutation<int>();
    }
}
