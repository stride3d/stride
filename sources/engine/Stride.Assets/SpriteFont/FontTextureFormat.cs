// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Assets.SpriteFont
{
    [DataContract]
    public enum FontTextureFormat
    {
        //Auto, -> currently not supported on all platforms 
        Rgba32,
        //CompressedMono, -> currently not supported on all platforms 
    }
}
