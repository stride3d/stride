// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.TextureConverter
{
    /// <summary>
    /// Request type, used internally, to check whether a library can handle a request.
    /// </summary>
    internal enum RequestType
    {
        Loading,
        Rescaling,
        Compressing,
        Converting,
        SwitchingChannels,
        Flipping,
        FlippingSub,
        Swapping,
        Export,
        Decompressing,
        MipMapsGeneration,
        ExportToStride,
        NormalMapGeneration,
        GammaCorrection,
        PreMultiplyAlpha,
        ColorKey,
        AtlasCreation,
        AtlasExtraction,
        ArrayCreation,
        ArrayExtraction,
        AtlasUpdate,
        ArrayUpdate,
        ArrayInsertion,
        ArrayElementRemoval,
        CubeCreation,
        InvertYUpdate
    }
}
