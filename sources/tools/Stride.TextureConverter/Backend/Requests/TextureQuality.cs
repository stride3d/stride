// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// The desired texture quality.
    /// </summary>
    public enum TextureQuality // Matches PvrttWrapper.ECompressorQuality
    {
        Fast,
        Normal,
        High,
        Best,
    }
}
