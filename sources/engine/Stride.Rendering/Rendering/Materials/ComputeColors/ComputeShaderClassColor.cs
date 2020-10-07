// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A shader outputing a color/vector value.
    /// </summary>
    [DataContract("ComputeShaderClassColor")]
    [Display("Shader")]
    public class ComputeShaderClassColor : ComputeShaderClassBase<IComputeColor>, IComputeColor
    {
        private int hashCode = 0;

        /// <inheritdoc/>
        public bool HasChanged
        {
            get
            {
                if (hashCode != 0 && hashCode == (MixinReference?.GetHashCode() ?? 0))
                    return false;

                hashCode = (MixinReference?.GetHashCode() ?? 0);
                return true;
            }
        }
    }
}
