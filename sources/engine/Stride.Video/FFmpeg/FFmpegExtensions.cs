#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_VIDEO_FFMPEG

using System;
using System.Runtime.CompilerServices;
using FFmpeg.AutoGen;
using Silk.NET.Direct3D11;

namespace Stride.Video.FFmpeg
{
    public static class FFmpegExtensions
    {
        public static _GUID ToGUID(this Guid guid)
        {
            return Unsafe.As<Guid, _GUID>(ref guid);
        }

        public static D3D11_VIDEO_DECODER_CONFIG ToFFmpegDecoderConfig(this VideoDecoderConfig configuration)
        {
            return new D3D11_VIDEO_DECODER_CONFIG
            {
                guidConfigBitstreamEncryption = configuration.GuidConfigBitstreamEncryption.ToGUID(),
                Config4GroupedCoefs = configuration.Config4GroupedCoefs,
                ConfigSpecificIDCT = configuration.ConfigSpecificIDCT,
                ConfigHostInverseScan = configuration.ConfigHostInverseScan,
                ConfigResidDiffAccelerator = configuration.ConfigResidDiffAccelerator,
                ConfigIntraResidUnsigned = configuration.ConfigIntraResidUnsigned,
                ConfigSpatialResidInterleaved = configuration.ConfigSpatialResidInterleaved,
                ConfigMinRenderTargetBuffCount = (ushort)configuration.ConfigMinRenderTargetBuffCount,
                ConfigSpatialHost8or9Clipping = configuration.ConfigSpatialHost8or9Clipping,
                ConfigSpatialResid8 = configuration.ConfigSpatialResid8,
                ConfigResidDiffHost = configuration.ConfigResidDiffHost,
                ConfigMBcontrolRasterOrder = configuration.ConfigMBcontrolRasterOrder,
                ConfigBitstreamRaw = configuration.ConfigBitstreamRaw,
                guidConfigResidDiffEncryption = configuration.GuidConfigResidDiffEncryption.ToGUID(),
                guidConfigMBcontrolEncryption = configuration.GuidConfigMBcontrolEncryption.ToGUID(),
                ConfigResid8Subtraction = configuration.ConfigResid8Subtraction,
                ConfigDecoderSpecific = configuration.ConfigDecoderSpecific
            };
        }
    }
}

#endif
