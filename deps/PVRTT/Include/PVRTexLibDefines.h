/*!****************************************************************************

 @file         PVRTexLibDefines.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        Public PVRTexLib defines header.

******************************************************************************/
#ifndef _PVRTEXLIBDEFINES_H_
#define _PVRTEXLIBDEFINES_H_

/****************************************************************************
** Integer types
****************************************************************************/
typedef char				PVRTchar8;
typedef signed char			PVRTint8;
typedef signed short		PVRTint16;
typedef signed int			PVRTint32;
typedef unsigned char		PVRTuint8;
typedef unsigned short		PVRTuint16;
typedef unsigned int		PVRTuint32;
typedef float				PVRTfloat32;
typedef signed long long	PVRTint64;
typedef unsigned long long	PVRTuint64;

#define PVRTEXLIBSIZEASSERT(T, size) typedef int (sizeof_##T)[sizeof(T) == (size)]
PVRTEXLIBSIZEASSERT(PVRTchar8, 1);
PVRTEXLIBSIZEASSERT(PVRTint8, 1);
PVRTEXLIBSIZEASSERT(PVRTuint8, 1);
PVRTEXLIBSIZEASSERT(PVRTint16, 2);
PVRTEXLIBSIZEASSERT(PVRTuint16, 2);
PVRTEXLIBSIZEASSERT(PVRTint32, 4);
PVRTEXLIBSIZEASSERT(PVRTuint32, 4);
PVRTEXLIBSIZEASSERT(PVRTint64, 8);
PVRTEXLIBSIZEASSERT(PVRTuint64, 8);
PVRTEXLIBSIZEASSERT(PVRTfloat32, 4);
#undef PVRTEXLIBSIZEASSERT

/*****************************************************************************
* Texture related constants and enumerations.
*****************************************************************************/
// V3 Header Identifiers.
#define PVRTEX3_IDENT			0x03525650U	// 'P''V''R'3
#define PVRTEX3_IDENT_REV		0x50565203U
// If endianness is backwards then PVR3 will read as 3RVP, hence why it is written as an int.

//Current version texture identifiers
#define PVRTEX_CURR_IDENT		PVRTEX3_IDENT
#define PVRTEX_CURR_IDENT_REV	PVRTEX3_IDENT_REV

// PVR Header file flags. Condition if true. If false, opposite is true unless specified.
#define PVRTEX3_FILE_COMPRESSED	(1U << 0U)	//	Texture has been file compressed using PVRTexLib (currently unused)
#define PVRTEX3_PREMULTIPLIED	(1U << 1U)	//	Texture has been premultiplied by alpha value.	

// Mip Map level specifier constants. Other levels are specified by 1,2...n
#define PVRTEX_TOPMIPLEVEL	0
#define PVRTEX_ALLMIPLEVELS	-1 //This is a special number used simply to return a total of all MIP levels when dealing with data sizes.

// A 64 bit pixel format ID & this will give you the high bits of a pixel format to check for a compressed format.
#define PVRTEX_PFHIGHMASK 0xffffffff00000000ull

/*
	Preprocessor definitions to generate a pixelID for use when consts are needed. For example - switch statements.
	These should be evaluated by the compiler rather than at run time - assuming that arguments are all constant.
*/

//Generate a 4 channel PixelID.
#define PVRTGENPIXELID4(C1Name, C2Name, C3Name, C4Name, C1Bits, C2Bits, C3Bits, C4Bits) ( ( (PVRTuint64)C1Name) + ( (PVRTuint64)C2Name<<8) + ( (PVRTuint64)C3Name<<16) + ( (PVRTuint64)C4Name<<24) + ( (PVRTuint64)C1Bits<<32) + ( (PVRTuint64)C2Bits<<40) + ( (PVRTuint64)C3Bits<<48) + ( (PVRTuint64)C4Bits<<56) )

//Generate a 1 channel PixelID.
#define PVRTGENPIXELID3(C1Name, C2Name, C3Name, C1Bits, C2Bits, C3Bits)( PVRTGENPIXELID4(C1Name, C2Name, C3Name, 0, C1Bits, C2Bits, C3Bits, 0) )

//Generate a 2 channel PixelID.
#define PVRTGENPIXELID2(C1Name, C2Name, C1Bits, C2Bits) ( PVRTGENPIXELID4(C1Name, C2Name, 0, 0, C1Bits, C2Bits, 0, 0) )

//Generate a 3 channel PixelID.
#define PVRTGENPIXELID1(C1Name, C1Bits) ( PVRTGENPIXELID4(C1Name, 0, 0, 0, C1Bits, 0, 0, 0))

/*!***********************************************************************
 @enum  PVRTexLibMetaData
 @brief Values for each meta data type that PVRTexLib knows about. 
        Texture arrays hinge on each surface being identical in all
        but content, including meta data. If the meta data varies even
        slightly then a new texture should be used.
        It is possible to write your own extension to get around this however.
*************************************************************************/
enum PVRTexLibMetaData
{
	PVRTLMD_TextureAtlasCoords = 0,
	PVRTLMD_BumpData,
	PVRTLMD_CubeMapOrder,
	PVRTLMD_TextureOrientation,
	PVRTLMD_BorderData,
	PVRTLMD_Padding,
	PVRTLMD_PerChannelType,
	PVRTLMD_SupercompressionGlobalData,
	PVRTLMD_MaxRange,
	PVRTLMD_NumMetaDataTypes
};

/*!***********************************************************************
 @enum  PVRTexLibAxis
 @brief Axis
*************************************************************************/
enum PVRTexLibAxis
{
	PVRTLA_X = 0,
	PVRTLA_Y = 1,
	PVRTLA_Z = 2
};

/*!***********************************************************************
 @enum  PVRTexLibOrientation
 @brief Image orientations per axis
*************************************************************************/
enum PVRTexLibOrientation
{
	PVRTLO_Left = 1 << PVRTexLibAxis::PVRTLA_X,
	PVRTLO_Right = 0,
	PVRTLO_Up = 1 << PVRTexLibAxis::PVRTLA_Y,
	PVRTLO_Down = 0,
	PVRTLO_Out = 1 << PVRTexLibAxis::PVRTLA_Z,
	PVRTLO_In = 0
};

/*!***********************************************************************
 @enum  PVRTexLibColourSpace
 @brief Describes the colour space of the texture
*************************************************************************/
enum PVRTexLibColourSpace
{
	PVRTLCS_Linear,
	PVRTLCS_sRGB,
	PVRTLCS_BT601,
	PVRTLCS_BT709,
	PVRTLCS_BT2020,
	PVRTLCS_NumSpaces
};

/*!***********************************************************************
 @enum  PVRTexLibChannelName
 @brief Channel names for non-compressed formats
*************************************************************************/
enum PVRTexLibChannelName
{
	PVRTLCN_NoChannel,
	PVRTLCN_Red,
	PVRTLCN_Green,
	PVRTLCN_Blue,
	PVRTLCN_Alpha,
	PVRTLCN_Luminance,
	PVRTLCN_Intensity,
	PVRTLCN_Depth,
	PVRTLCN_Stencil,
	PVRTLCN_Unspecified,
	PVRTLCN_NumChannels
};

/*!***********************************************************************
 @enum  PVRTexLibPixelFormat
 @brief Compressed pixel formats that PVRTexLib understands
*************************************************************************/
enum PVRTexLibPixelFormat
{
	PVRTLPF_PVRTCI_2bpp_RGB,
	PVRTLPF_PVRTCI_2bpp_RGBA,
	PVRTLPF_PVRTCI_4bpp_RGB,
	PVRTLPF_PVRTCI_4bpp_RGBA,
	PVRTLPF_PVRTCII_2bpp,
	PVRTLPF_PVRTCII_4bpp,
	PVRTLPF_ETC1,
	PVRTLPF_DXT1,
	PVRTLPF_DXT2,
	PVRTLPF_DXT3,
	PVRTLPF_DXT4,
	PVRTLPF_DXT5,

	//These formats are identical to some DXT formats.
	PVRTLPF_BC1 = PVRTLPF_DXT1,
	PVRTLPF_BC2 = PVRTLPF_DXT3,
	PVRTLPF_BC3 = PVRTLPF_DXT5,
	PVRTLPF_BC4,
	PVRTLPF_BC5,

	/* Currently unsupported: */
	PVRTLPF_BC6,
	PVRTLPF_BC7,
	/* ~~~~~~~~~~~~~~~~~~ */

	// Packed YUV formats
	PVRTLPF_UYVY_422, // https://www.fourcc.org/pixel-format/yuv-uyvy/
	PVRTLPF_YUY2_422, // https://www.fourcc.org/pixel-format/yuv-yuy2/

	PVRTLPF_BW1bpp,
	PVRTLPF_SharedExponentR9G9B9E5,
	PVRTLPF_RGBG8888,
	PVRTLPF_GRGB8888,
	PVRTLPF_ETC2_RGB,
	PVRTLPF_ETC2_RGBA,
	PVRTLPF_ETC2_RGB_A1,
	PVRTLPF_EAC_R11,
	PVRTLPF_EAC_RG11,

	PVRTLPF_ASTC_4x4,
	PVRTLPF_ASTC_5x4,
	PVRTLPF_ASTC_5x5,
	PVRTLPF_ASTC_6x5,
	PVRTLPF_ASTC_6x6,
	PVRTLPF_ASTC_8x5,
	PVRTLPF_ASTC_8x6,
	PVRTLPF_ASTC_8x8,
	PVRTLPF_ASTC_10x5,
	PVRTLPF_ASTC_10x6,
	PVRTLPF_ASTC_10x8,
	PVRTLPF_ASTC_10x10,
	PVRTLPF_ASTC_12x10,
	PVRTLPF_ASTC_12x12,

	PVRTLPF_ASTC_3x3x3,
	PVRTLPF_ASTC_4x3x3,
	PVRTLPF_ASTC_4x4x3,
	PVRTLPF_ASTC_4x4x4,
	PVRTLPF_ASTC_5x4x4,
	PVRTLPF_ASTC_5x5x4,
	PVRTLPF_ASTC_5x5x5,
	PVRTLPF_ASTC_6x5x5,
	PVRTLPF_ASTC_6x6x5,
	PVRTLPF_ASTC_6x6x6,

	PVRTLPF_BASISU_ETC1S,
	PVRTLPF_BASISU_UASTC,

	PVRTLPF_RGBM,
	PVRTLPF_RGBD,

	PVRTLPF_PVRTCI_HDR_6bpp,
	PVRTLPF_PVRTCI_HDR_8bpp,
	PVRTLPF_PVRTCII_HDR_6bpp,
	PVRTLPF_PVRTCII_HDR_8bpp,

	// The memory layout for 10 and 12 bit YUV formats that are packed into a WORD (16 bits) is denoted by MSB or LSB:
	// MSB denotes that the sample is stored in the most significant <N> bits
	// LSB denotes that the sample is stored in the least significant <N> bits
	// All YUV formats are little endian

	// Packed YUV formats
	PVRTLPF_VYUA10MSB_444,
	PVRTLPF_VYUA10LSB_444,
	PVRTLPF_VYUA12MSB_444,
	PVRTLPF_VYUA12LSB_444,
	PVRTLPF_UYV10A2_444,	// Y410
	PVRTLPF_UYVA16_444,		// Y416
	PVRTLPF_YUYV16_422,		// Y216
	PVRTLPF_UYVY16_422,
	PVRTLPF_YUYV10MSB_422,	// Y210
	PVRTLPF_YUYV10LSB_422,
	PVRTLPF_UYVY10MSB_422,
	PVRTLPF_UYVY10LSB_422,
	PVRTLPF_YUYV12MSB_422,
	PVRTLPF_YUYV12LSB_422,
	PVRTLPF_UYVY12MSB_422,
	PVRTLPF_UYVY12LSB_422,

	/*
		Reserved for future expansion
	*/

	// 3 Plane (Planar) YUV formats
	PVRTLPF_YUV_3P_444 = 270,
	PVRTLPF_YUV10MSB_3P_444,
	PVRTLPF_YUV10LSB_3P_444,
	PVRTLPF_YUV12MSB_3P_444,
	PVRTLPF_YUV12LSB_3P_444,
	PVRTLPF_YUV16_3P_444,
	PVRTLPF_YUV_3P_422,
	PVRTLPF_YUV10MSB_3P_422,
	PVRTLPF_YUV10LSB_3P_422,
	PVRTLPF_YUV12MSB_3P_422,
	PVRTLPF_YUV12LSB_3P_422,
	PVRTLPF_YUV16_3P_422,
	PVRTLPF_YUV_3P_420,
	PVRTLPF_YUV10MSB_3P_420,
	PVRTLPF_YUV10LSB_3P_420,
	PVRTLPF_YUV12MSB_3P_420,
	PVRTLPF_YUV12LSB_3P_420,
	PVRTLPF_YUV16_3P_420,
	PVRTLPF_YVU_3P_420,

	/*
		Reserved for future expansion
	*/

	// 2 Plane (Biplanar/semi-planar) YUV formats
	PVRTLPF_YUV_2P_422 = 480,	// P208
	PVRTLPF_YUV10MSB_2P_422,	// P210
	PVRTLPF_YUV10LSB_2P_422,
	PVRTLPF_YUV12MSB_2P_422,
	PVRTLPF_YUV12LSB_2P_422,
	PVRTLPF_YUV16_2P_422,		// P216
	PVRTLPF_YUV_2P_420,			// NV12
	PVRTLPF_YUV10MSB_2P_420,	// P010
	PVRTLPF_YUV10LSB_2P_420,
	PVRTLPF_YUV12MSB_2P_420,
	PVRTLPF_YUV12LSB_2P_420,
	PVRTLPF_YUV16_2P_420,		// P016
	PVRTLPF_YUV_2P_444,
	PVRTLPF_YVU_2P_444,
	PVRTLPF_YUV10MSB_2P_444,
	PVRTLPF_YUV10LSB_2P_444,
	PVRTLPF_YVU10MSB_2P_444,
	PVRTLPF_YVU10LSB_2P_444,
	PVRTLPF_YVU_2P_422,
	PVRTLPF_YVU10MSB_2P_422,
	PVRTLPF_YVU10LSB_2P_422,
	PVRTLPF_YVU_2P_420,			// NV21
	PVRTLPF_YVU10MSB_2P_420,
	PVRTLPF_YVU10LSB_2P_420,

	//Invalid value
	PVRTLPF_NumCompressedPFs
};

/*!***********************************************************************
 @enum  PVRTexLibVariableType
 @brief Data types. Describes how the data is interpreted by PVRTexLib and
        how the pointer returned by PVRTexLib_GetTextureDataPtr() should
		be interpreted.
*************************************************************************/
enum PVRTexLibVariableType
{
	PVRTLVT_UnsignedByteNorm,
	PVRTLVT_SignedByteNorm,
	PVRTLVT_UnsignedByte,
	PVRTLVT_SignedByte,
	PVRTLVT_UnsignedShortNorm,
	PVRTLVT_SignedShortNorm,
	PVRTLVT_UnsignedShort,
	PVRTLVT_SignedShort,
	PVRTLVT_UnsignedIntegerNorm,
	PVRTLVT_SignedIntegerNorm,
	PVRTLVT_UnsignedInteger,
	PVRTLVT_SignedInteger,
	PVRTLVT_SignedFloat,
	PVRTLVT_Float = PVRTLVT_SignedFloat, //the name Float is now deprecated.
	PVRTLVT_UnsignedFloat,
	PVRTLVT_NumVarTypes,

	PVRTLVT_Invalid = 255
};

/*!***********************************************************************
 @enum  PVRTexLibCompressorQuality
 @brief Quality level to compress the texture with. Applies to PVRTC, 
        ETC, ASTC, BASIS and IMGIC formats.
*************************************************************************/
enum PVRTexLibCompressorQuality
{
	PVRTLCQ_PVRTCFastest = 0,	//!< PVRTC fastest
	PVRTLCQ_PVRTCFast,			//!< PVRTC fast
	PVRTLCQ_PVRTCLow,			//!< PVRTC low
	PVRTLCQ_PVRTCNormal,		//!< PVRTC normal
	PVRTLCQ_PVRTCHigh,			//!< PVRTC high
	PVRTLCQ_PVRTCVeryHigh,		//!< PVRTC very high
	PVRTLCQ_PVRTCThorough,		//!< PVRTC thorough
	PVRTLCQ_PVRTCBest,			//!< PVRTC best
	PVRTLCQ_NumPVRTCModes,		//!< Number of PVRTC modes

	PVRTLCQ_ETCFast = 0,		//!< ETC fast
	PVRTLCQ_ETCNormal,			//!< ETC normal
	PVRTLCQ_ETCSlow,			//!< ETC slow
	PVRTLCQ_NumETCModes,		//!< Number of ETC modes

	PVRTLCQ_ASTCVeryFast = 0,	//!< ASTC very fast
	PVRTLCQ_ASTCFast,			//!< ASTC fast
	PVRTLCQ_ASTCMedium,			//!< ASTC medium
	PVRTLCQ_ASTCThorough,		//!< ASTC thorough
	PVRTLCQ_ASTCExhaustive,		//!< ASTC exhaustive
	PVRTLCQ_NumASTCModes,		//!< Number of ASTC modes

	PVRTLCQ_BASISULowest = 0,	//!< BASISU lowest quality
	PVRTLCQ_BASISULow,			//!< BASISU low quality
	PVRTLCQ_BASISUNormal,		//!< BASISU normal quality
	PVRTLCQ_BASISUHigh,			//!< BASISU high quality
	PVRTLCQ_BASISUBest,			//!< BASISU best quality
	PVRTLCQ_NumBASISUModes,		//!< Number of BASISU modes
};

/*!***********************************************************************
 @enum  PVRTexLibResizeMode
 @brief Filter to apply when resizing an image
*************************************************************************/
enum PVRTexLibResizeMode
{
	PVRTLRM_Nearest,	//!< Nearest filtering
	PVRTLRM_Linear,		//!< Linear filtering 
	PVRTLRM_Cubic,		//!< Cubic filtering, uses Catmull-Rom splines.
	PVRTLRM_Modes		//!< Number of resize modes
};

/*!***********************************************************************
 @enum      PVRTexLibFileContainerType
 @brief     File container type
*************************************************************************/
enum PVRTexLibFileContainerType
{
	PVRTLFCT_PVR,		//!< PVR: https://docs.imgtec.com/Specifications/PVR_File_Format_Specification/topics/pvr_intro.html
	PVRTLFCT_KTX,		//!< KTX version 1: https://www.khronos.org/registry/KTX/specs/1.0/ktxspec_v1.html 
	PVRTLFCT_KTX2,		//!< KTX version 2: https://github.khronos.org/KTX-Specification/
	PVRTLFCT_ASTC,		//!< ASTC compressed textures only: https://github.com/ARM-software/astc-encoder
	PVRTLFCT_BASIS,		//!< Basis Universal compressed textures only: https://github.com/BinomialLLC/basis_universal
	PVRTLFCT_DDS,		//!< DirectDraw Surface: https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-reference
	PVRTLFCT_CHeader	//!< C style header
};

/*!***********************************************************************
 @enum      PVRTexLibColourDiffMode
 @brief     The clamping mode to use when performing a colour diff
*************************************************************************/
enum PVRTexLibColourDiffMode
{
	PVRTLCDM_Abs,	//!< Absolute
	PVRTLCDM_Signed //!< Signed
};

/*!***********************************************************************
 @enum      PVRTexLibLegacyApi
 @brief     Legacy API enum.
*************************************************************************/
enum PVRTexLibLegacyApi
{
	PVRTLLAPI_OGLES = 1, //!< OpenGL ES 1.x
	PVRTLLAPI_OGLES2,    //!< OpenGL ES 2.0
	PVRTLLAPI_D3DM,      //!< Direct 3D M
	PVRTLLAPI_OGL,       //!< Open GL
	PVRTLLAPI_DX9,       //!< DirextX 9
	PVRTLLAPI_DX10,      //!< DirectX 10
	PVRTLLAPI_OVG,       //!< Open VG
	PVRTLLAPI_MGL,       //!< MGL
};

#define PVRT_MIN(a,b)       (((a) < (b)) ? (a) : (b))
#define PVRT_MAX(a,b)       (((a) > (b)) ? (a) : (b))
#define PVRT_CLAMP(x, l, h)	(PVRT_MIN((h), PVRT_MAX((x), (l))))

/*!***************************************************************************
 @def           TEXOFFSET2D
 @brief         2D texture offset
*****************************************************************************/
#define TEXOFFSET2D(x,y,width) (((x)+(y)*(width)))

/*!***************************************************************************
 @def          TEXOFFSET3D
 @brief        3D texture offset
*****************************************************************************/
#define TEXOFFSET3D(x,y,z,width,height) (((x)+(y)*(width)+(z)*(width)*(height)))

/*!***************************************************************************
 @struct        PVRTextureHeaderV3
 @brief      	A header for a PVR texture.
 @details       Contains everything required to read a texture accurately, and nothing more. Extraneous data is stored in a MetaDataBlock.
				Correct use of the texture may rely on MetaDataBlock, but accurate data loading can be done through the standard header alone.
*****************************************************************************/
#pragma pack(push,4)
struct PVRTextureHeaderV3
{
	PVRTuint32	u32Version;			///< Version of the file header, used to identify it.
	PVRTuint32	u32Flags;			///< Various format flags.
	PVRTuint64	u64PixelFormat;		///< The pixel format, 8cc value storing the 4 channel identifiers and their respective sizes.
	PVRTuint32	u32ColourSpace;		///< The Colour Space of the texture, currently either linear RGB or sRGB.
	PVRTuint32	u32ChannelType;		///< Variable type that the channel is stored in. Supports signed/unsigned int/short/byte or float for now.
	PVRTuint32	u32Height;			///< Height of the texture.
	PVRTuint32	u32Width;			///< Width of the texture.
	PVRTuint32	u32Depth;			///< Depth of the texture. (Z-slices)
	PVRTuint32	u32NumSurfaces;		///< Number of members in a Texture Array.
	PVRTuint32	u32NumFaces;		///< Number of faces in a Cube Map. Maybe be a value other than 6.
	PVRTuint32	u32MIPMapCount;		///< Number of MIP Maps in the texture - NB: Includes top level.
	PVRTuint32	u32MetaDataSize;	///< Size of the accompanying meta data.
};
#pragma pack(pop)
#define PVRTEX3_HEADERSIZE 52U
#endif /* _PVRTEXLIBDEFINES_H_ */

/*****************************************************************************
End of file (PVRTexLibDefines.h)
*****************************************************************************/