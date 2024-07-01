// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.PvrttWrapper;

internal enum EPVRTPixelFormat
{
    PVRTCI_2bpp_RGB,
    PVRTCI_2bpp_RGBA,
    PVRTCI_4bpp_RGB,
    PVRTCI_4bpp_RGBA,
    PVRTCII_2bpp,
    PVRTCII_4bpp,
    ETC1,
    DXT1,
    DXT2,
    DXT3,
    DXT4,
    DXT5,

    //These formats are identical to some DXT formats.
    BC1 = DXT1,
    BC2 = DXT3,
    BC3 = DXT5,
    BC4,
    BC5,
    /* Currently unsupported: */
    BC6,
    BC7,
    /* ~~~~~~~~~~~~~~~~~~ */

    // Packed YUV formats
    UYVY_422,
    YUY2_422,

    BW1bpp,
    SharedExponentR9G9B9E5,
    RGBG8888,
    GRGB8888,
    ETC2_RGB,
    ETC2_RGBA,
    ETC2_RGB_A1,
    EAC_R11,
    EAC_RG11,


    ASTC_4x4,
	ASTC_5x4,
	ASTC_5x5,
	ASTC_6x5,
	ASTC_6x6,
	ASTC_8x5,
	ASTC_8x6,
	ASTC_8x8,
	ASTC_10x5,
	ASTC_10x6,
	ASTC_10x8,
	ASTC_10x10,
	ASTC_12x10,
	ASTC_12x12,

	ASTC_3x3x3,
	ASTC_4x3x3,
	ASTC_4x4x3,
	ASTC_4x4x4,
	ASTC_5x4x4,
	ASTC_5x5x4,
	ASTC_5x5x5,
	ASTC_6x5x5,
	ASTC_6x6x5,
	ASTC_6x6x6,

	BASISU_ETC1S,
	BASISU_UASTC,

	RGBM,
	RGBD,

	PVRTCI_HDR_6bpp,
	PVRTCI_HDR_8bpp,
	PVRTCII_HDR_6bpp,
	PVRTCII_HDR_8bpp,

	// The memory layout for 10 and 12 bit YUV formats that are packed into a WORD (16 bits) is denoted by MSB or LSB:
	// MSB denotes that the sample is stored in the most significant <N> bits
	// LSB denotes that the sample is stored in the least significant <N> bits
	// All YUV formats are little endian

	// Packed YUV formats
	VYUA10MSB_444,
	VYUA10LSB_444,
	VYUA12MSB_444,
	VYUA12LSB_444,
	UYV10A2_444,	// Y410
	UYVA16_444,		// Y416
	YUYV16_422,		// Y216
	UYVY16_422,
	YUYV10MSB_422,	// Y210
	YUYV10LSB_422,
	UYVY10MSB_422,
	UYVY10LSB_422,
	YUYV12MSB_422,
	YUYV12LSB_422,
	UYVY12MSB_422,
	UYVY12LSB_422,

	/*
		Reserved for future expansion
	*/

	// 3 Plane (Planar) YUV formats
	YUV_3P_444 = 270,
	YUV10MSB_3P_444,
	YUV10LSB_3P_444,
	YUV12MSB_3P_444,
	YUV12LSB_3P_444,
	YUV16_3P_444,
	YUV_3P_422,
	YUV10MSB_3P_422,
	YUV10LSB_3P_422,
	YUV12MSB_3P_422,
	YUV12LSB_3P_422,
	YUV16_3P_422,
	YUV_3P_420,
	YUV10MSB_3P_420,
	YUV10LSB_3P_420,
	YUV12MSB_3P_420,
	YUV12LSB_3P_420,
	YUV16_3P_420,
	YVU_3P_420,

	/*
		Reserved for future expansion
	*/

	// 2 Plane (Biplanar/semi-planar) YUV formats
	YUV_2P_422 = 480,	// P208
	YUV10MSB_2P_422,	// P210
	YUV10LSB_2P_422,
	YUV12MSB_2P_422,
	YUV12LSB_2P_422,
	YUV16_2P_422,		// P216
	YUV_2P_420,			// NV12
	YUV10MSB_2P_420,	// P010
	YUV10LSB_2P_420,
	YUV12MSB_2P_420,
	YUV12LSB_2P_420,
	YUV16_2P_420,		// P016
	YUV_2P_444,
	YVU_2P_444,
	YUV10MSB_2P_444,
	YUV10LSB_2P_444,
	YVU10MSB_2P_444,
	YVU10LSB_2P_444,
	YVU_2P_422,
	YVU10MSB_2P_422,
	YVU10LSB_2P_422,
	YVU_2P_420,			// NV21
	YVU10MSB_2P_420,
	YVU10LSB_2P_420,

	//Invalid value
	NumCompressedPFs
}