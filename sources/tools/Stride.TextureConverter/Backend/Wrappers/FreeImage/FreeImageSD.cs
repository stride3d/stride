// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace FreeImageAPI;
using StridePixelFormat = Stride.Graphics.PixelFormat;

public static class FreeImageSD
{
	/// <summary>
	/// Retrieves all parameters needed to create a new FreeImage bitmap from the pixel format.
	/// </summary>
	/// <param name="format">The <see cref="Stride.Graphics.PixelFormat"/> of the image.</param>
	/// <param name="type">Returns the type used for the new bitmap.</param>
	/// <param name="bpp">Returns the color depth for the new bitmap.</param>
	/// <param name="redMask">Returns the red_mask for the new bitmap.</param>
	/// <param name="greenMask">Returns the green_mask for the new bitmap.</param>
	/// <param name="blueMask">Returns the blue_mask for the new bitmap.</param>
	/// <returns>True in case a matching conversion exists; else false.
	/// </returns>
	public static bool GetFormatParameters(
		StridePixelFormat format,
		out FREE_IMAGE_TYPE type,
		out uint bpp,
		out uint redMask,
		out uint greenMask,
		out uint blueMask)
	{
		var result = true;
		type = FREE_IMAGE_TYPE.FIT_UNKNOWN;
		bpp = 0;
		redMask = 0;
		greenMask = 0;
		blueMask = 0;

		switch (format)
		{
			case StridePixelFormat.R32G32B32A32_Float:
				type = FREE_IMAGE_TYPE.FIT_RGBAF;
				bpp = 128;
				break;
			case StridePixelFormat.R32G32B32_Float:
				type = FREE_IMAGE_TYPE.FIT_RGBF;
				bpp = 96;
				break;
			case StridePixelFormat.R16G16B16A16_Typeless:
			case StridePixelFormat.R16G16B16A16_Float:
			case StridePixelFormat.R16G16B16A16_UNorm:
			case StridePixelFormat.R16G16B16A16_UInt:
			case StridePixelFormat.R16G16B16A16_SNorm:
			case StridePixelFormat.R16G16B16A16_SInt:
				type = FREE_IMAGE_TYPE.FIT_RGBA16;
				bpp = 64;
				break;
			case StridePixelFormat.D32_Float:
			case StridePixelFormat.R32_Float:
				type = FREE_IMAGE_TYPE.FIT_FLOAT;
				bpp = 32;
				break;
			case StridePixelFormat.R32_SInt:
				type = FREE_IMAGE_TYPE.FIT_INT32;
				bpp = 32;
				break;
			case StridePixelFormat.R32_UInt:
				type = FREE_IMAGE_TYPE.FIT_UINT32;
				bpp = 32;
				break;
			case StridePixelFormat.R16_SInt:
				type = FREE_IMAGE_TYPE.FIT_INT16;
				bpp = 16;
				break;
			case StridePixelFormat.R16_UInt:
				type = FREE_IMAGE_TYPE.FIT_UINT16;
				bpp = 16;
				break;
			case StridePixelFormat.R32_Typeless:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 32;
				break;
			case StridePixelFormat.R8G8B8A8_Typeless:
			case StridePixelFormat.R8G8B8A8_UNorm:
			case StridePixelFormat.R8G8B8A8_UNorm_SRgb:
			case StridePixelFormat.R8G8B8A8_UInt:
			case StridePixelFormat.R8G8B8A8_SNorm:
			case StridePixelFormat.R8G8B8A8_SInt:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 32;
				redMask = FreeImage.FI_RGBA_RED_MASK;
				greenMask = FreeImage.FI_RGBA_GREEN_MASK;
				blueMask = FreeImage.FI_RGBA_BLUE_MASK;
				break;
			case StridePixelFormat.R16G16_Typeless:
			case StridePixelFormat.R16G16_Float:
			case StridePixelFormat.R16G16_UNorm:
			case StridePixelFormat.R16G16_UInt:
			case StridePixelFormat.R16G16_SNorm:
			case StridePixelFormat.R16G16_SInt:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 32;
				redMask = 0xFFFF0000;
				greenMask = 0x0000FFFF;
				break;
			case StridePixelFormat.B8G8R8A8_Typeless:
			case StridePixelFormat.B8G8R8A8_UNorm_SRgb:
			case StridePixelFormat.B8G8R8X8_Typeless:
			case StridePixelFormat.B8G8R8X8_UNorm_SRgb:
			case StridePixelFormat.B8G8R8A8_UNorm:
			case StridePixelFormat.B8G8R8X8_UNorm:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 32;
				redMask = FreeImage.FI_RGBA_BLUE_MASK;
				greenMask = FreeImage.FI_RGBA_GREEN_MASK;
				blueMask = FreeImage.FI_RGBA_RED_MASK;
				break;

			case StridePixelFormat.B5G6R5_UNorm:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 16;
				redMask = FreeImage.FI16_565_RED_MASK;
				greenMask = FreeImage.FI16_565_GREEN_MASK;
				blueMask = FreeImage.FI16_565_BLUE_MASK;
				break;
			case StridePixelFormat.B5G5R5A1_UNorm:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 16;
				redMask = FreeImage.FI16_555_RED_MASK;
				greenMask = FreeImage.FI16_555_GREEN_MASK;
				blueMask = FreeImage.FI16_555_BLUE_MASK;
				break;
			case StridePixelFormat.R16_Typeless:
			case StridePixelFormat.R16_Float:
			case StridePixelFormat.D16_UNorm:
			case StridePixelFormat.R16_UNorm:
			case StridePixelFormat.R16_SNorm:
				type = FREE_IMAGE_TYPE.FIT_UINT16;
				bpp = 16;
				break;
			case StridePixelFormat.R8G8_Typeless:
			case StridePixelFormat.R8G8_UNorm:
			case StridePixelFormat.R8G8_UInt:
			case StridePixelFormat.R8G8_SNorm:
			case StridePixelFormat.R8G8_SInt:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 16;
				redMask = 0xFF00;
				greenMask= 0x00FF;
				break;
			case StridePixelFormat.R8_Typeless:
			case StridePixelFormat.R8_UNorm:
			case StridePixelFormat.R8_UInt:
			case StridePixelFormat.R8_SNorm:
			case StridePixelFormat.R8_SInt:
			case StridePixelFormat.A8_UNorm:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 8;
				break;
			case StridePixelFormat.R1_UNorm:
				type = FREE_IMAGE_TYPE.FIT_BITMAP;
				bpp = 1;
				break;
			default:
				result = false;
				break;
		}

		return result;
	}

}
