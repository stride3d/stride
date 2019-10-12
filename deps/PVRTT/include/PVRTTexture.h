/*!****************************************************************************

 @file         PVRTTexture.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        Texture loading.

******************************************************************************/
#ifndef _PVRTTEXTURE_H_
#define _PVRTTEXTURE_H_

#include "PVRTGlobal.h"

/*****************************************************************************
* Texture related constants and enumerations. 
*****************************************************************************/
// V3 Header Identifiers.
const PVRTuint32 PVRTEX3_IDENT			= 0x03525650;	// 'P''V''R'3
const PVRTuint32 PVRTEX3_IDENT_REV		= 0x50565203;   
// If endianness is backwards then PVR3 will read as 3RVP, hence why it is written as an int.

//Current version texture identifiers
const PVRTuint32 PVRTEX_CURR_IDENT		= PVRTEX3_IDENT;
const PVRTuint32 PVRTEX_CURR_IDENT_REV	= PVRTEX3_IDENT_REV;

// PVR Header file flags.										Condition if true. If false, opposite is true unless specified.
const PVRTuint32 PVRTEX3_FILE_COMPRESSED	= (1<<0);		//	Texture has been file compressed using PVRTexLib (currently unused)
const PVRTuint32 PVRTEX3_PREMULTIPLIED		= (1<<1);		//	Texture has been premultiplied by alpha value.	

// Mip Map level specifier constants. Other levels are specified by 1,2...n
const PVRTint32 PVRTEX_TOPMIPLEVEL			= 0;
const PVRTint32 PVRTEX_ALLMIPLEVELS			= -1; //This is a special number used simply to return a total of all MIP levels when dealing with data sizes.

//values for each meta data type that we know about. Texture arrays hinge on each surface being identical in all but content, including meta data. 
//If the meta data varies even slightly then a new texture should be used. It is possible to write your own extension to get around this however.
enum EPVRTMetaData
{
	ePVRTMetaDataTextureAtlasCoords=0,
	ePVRTMetaDataBumpData,
	ePVRTMetaDataCubeMapOrder,
	ePVRTMetaDataTextureOrientation,
	ePVRTMetaDataBorderData,
	ePVRTMetaDataPadding,
	ePVRTMetaDataNumMetaDataTypes
};

enum EPVRTAxis
{
	ePVRTAxisX = 0,
	ePVRTAxisY = 1,
	ePVRTAxisZ = 2
};

enum EPVRTOrientation
{
	ePVRTOrientLeft	= 1<<ePVRTAxisX,
	ePVRTOrientRight= 0,
	ePVRTOrientUp	= 1<<ePVRTAxisY,
	ePVRTOrientDown	= 0,
	ePVRTOrientOut	= 1<<ePVRTAxisZ,
	ePVRTOrientIn	= 0
};

enum EPVRTColourSpace
{
	ePVRTCSpacelRGB,
	ePVRTCSpacesRGB,
	ePVRTCSpaceNumSpaces
};

//Compressed pixel formats
enum EPVRTPixelFormat
{
	ePVRTPF_PVRTCI_2bpp_RGB,
	ePVRTPF_PVRTCI_2bpp_RGBA,
	ePVRTPF_PVRTCI_4bpp_RGB,
	ePVRTPF_PVRTCI_4bpp_RGBA,
	ePVRTPF_PVRTCII_2bpp,
	ePVRTPF_PVRTCII_4bpp,
	ePVRTPF_ETC1,
	ePVRTPF_DXT1,
	ePVRTPF_DXT2,
	ePVRTPF_DXT3,
	ePVRTPF_DXT4,
	ePVRTPF_DXT5,

	//These formats are identical to some DXT formats.
	ePVRTPF_BC1 = ePVRTPF_DXT1,
	ePVRTPF_BC2 = ePVRTPF_DXT3,
	ePVRTPF_BC3 = ePVRTPF_DXT5,

	//These are currently unsupported:
	ePVRTPF_BC4,
	ePVRTPF_BC5,
	ePVRTPF_BC6,
	ePVRTPF_BC7,

	//These are supported
	ePVRTPF_UYVY,
	ePVRTPF_YUY2,
	ePVRTPF_BW1bpp,
	ePVRTPF_SharedExponentR9G9B9E5,
	ePVRTPF_RGBG8888,
	ePVRTPF_GRGB8888,
	ePVRTPF_ETC2_RGB,
	ePVRTPF_ETC2_RGBA,
	ePVRTPF_ETC2_RGB_A1,
	ePVRTPF_EAC_R11,
	ePVRTPF_EAC_RG11,

	ePVRTPF_ASTC_4x4,
	ePVRTPF_ASTC_5x4,
	ePVRTPF_ASTC_5x5,
	ePVRTPF_ASTC_6x5,
	ePVRTPF_ASTC_6x6,
	ePVRTPF_ASTC_8x5,
	ePVRTPF_ASTC_8x6,
	ePVRTPF_ASTC_8x8,
	ePVRTPF_ASTC_10x5,
	ePVRTPF_ASTC_10x6,
	ePVRTPF_ASTC_10x8,
	ePVRTPF_ASTC_10x10,
	ePVRTPF_ASTC_12x10,
	ePVRTPF_ASTC_12x12,

	ePVRTPF_ASTC_3x3x3,
	ePVRTPF_ASTC_4x3x3,
	ePVRTPF_ASTC_4x4x3,
	ePVRTPF_ASTC_4x4x4,
	ePVRTPF_ASTC_5x4x4,
	ePVRTPF_ASTC_5x5x4,
	ePVRTPF_ASTC_5x5x5,
	ePVRTPF_ASTC_6x5x5,
	ePVRTPF_ASTC_6x6x5,
	ePVRTPF_ASTC_6x6x6,

	//Invalid value
	ePVRTPF_NumCompressedPFs
};

//Variable Type Names
enum EPVRTVariableType
{
	ePVRTVarTypeUnsignedByteNorm,
	ePVRTVarTypeSignedByteNorm,
	ePVRTVarTypeUnsignedByte,
	ePVRTVarTypeSignedByte,
	ePVRTVarTypeUnsignedShortNorm,
	ePVRTVarTypeSignedShortNorm,
	ePVRTVarTypeUnsignedShort,
	ePVRTVarTypeSignedShort,
	ePVRTVarTypeUnsignedIntegerNorm,
	ePVRTVarTypeSignedIntegerNorm,
	ePVRTVarTypeUnsignedInteger,
	ePVRTVarTypeSignedInteger,
	ePVRTVarTypeSignedFloat,	ePVRTVarTypeFloat=ePVRTVarTypeSignedFloat, //the name ePVRTVarTypeFloat is now deprecated.
	ePVRTVarTypeUnsignedFloat,
	ePVRTVarTypeNumVarTypes
};

//A 64 bit pixel format ID & this will give you the high bits of a pixel format to check for a compressed format.
static const PVRTuint64 PVRTEX_PFHIGHMASK=0xffffffff00000000ull;

/*****************************************************************************
* Texture header structures.
*****************************************************************************/

/*!***********************************************************************
 @struct       	MetaDataBlock
 @brief      	A struct containing a block of extraneous meta data for a texture.
*************************************************************************/
struct MetaDataBlock
{
	PVRTuint32	DevFOURCC;		///< A 4cc descriptor of the data type's creator. Values equating to values between 'P' 'V' 'R' 0 and 'P' 'V' 'R' 255 will be used by our headers.
	PVRTuint32	u32Key;			///< A DWORD (enum value) identifying the data type, and thus how to read it.
	PVRTuint32	u32DataSize;	///< Size of the Data member.
	PVRTuint8*	Data;			///< Data array, can be absolutely anything, the loader needs to know how to handle it based on DevFOURCC and Key. Use new operator to assign to it.
		
	/*!***********************************************************************
		@fn       		MetaDataBlock
		@brief      	Meta Data Block Constructor.
	*************************************************************************/
	MetaDataBlock() : DevFOURCC(0), u32Key(0), u32DataSize(0), Data(NULL)
	{}
		
	/*!***********************************************************************
		@fn       		MetaDataBlock
		@brief      	Meta Data Block Copy Constructor.
	*************************************************************************/
	MetaDataBlock(const MetaDataBlock& rhs)  : DevFOURCC(rhs.DevFOURCC), u32Key(rhs.u32Key), u32DataSize(rhs.u32DataSize)
	{
		//Copy the data across.
		Data = new PVRTuint8[u32DataSize];
		for (PVRTuint32 uiDataAmt=0; uiDataAmt<u32DataSize; ++uiDataAmt)
		{
			Data[uiDataAmt]=rhs.Data[uiDataAmt];
		}
	}

	/*!***********************************************************************
		@fn       		~MetaDataBlock
		@brief      	Meta Data Block Destructor.
	*************************************************************************/
	~MetaDataBlock()
	{
		if (Data) 
			delete [] Data;
		Data = NULL;
	}

	/*!***********************************************************************
		@fn       		SizeOfBlock
		@return			size_t Size (in a file) of the block.
		@brief      	Returns the number of extra bytes this will add to any output files.
	*************************************************************************/
	size_t SizeOfBlock() const
	{
		return sizeof(DevFOURCC)+sizeof(u32Key)+sizeof(u32DataSize)+u32DataSize;
	}

	/*!***********************************************************************
		@brief      	Assigns one MetaDataBlock to the other.
		@return			MetaDataBlock This MetaDataBlock after the operation.
	*************************************************************************/
	MetaDataBlock& operator=(const MetaDataBlock& rhs)
	{
		if (&rhs==this)
			return *this;

		//Remove pre-existing data.
		if (Data)
			delete [] Data;
		Data=NULL;

		//Copy the basic parameters
		DevFOURCC=rhs.DevFOURCC;
		u32Key=rhs.u32Key;
		u32DataSize=rhs.u32DataSize;

		//Copy the data across.
		if (rhs.Data)
		{
			Data = new PVRTuint8[u32DataSize];
			for (PVRTuint32 uiDataAmt=0; uiDataAmt<u32DataSize; ++uiDataAmt)
			{
				Data[uiDataAmt]=rhs.Data[uiDataAmt];
			}
		}

		return *this;
	}

	/*!***************************************************************************
	@fn       		ReadFromPtr
	@param[in]		pDataCursor		The data to read
	@brief      	Reads from a pointer of memory in to the meta data block.
	*****************************************************************************/
	bool ReadFromPtr(const unsigned char** pDataCursor);
};

#pragma pack(push,4)

/*!***************************************************************************
 @struct        PVRTextureHeaderV3
 @brief      	A header for a PVR texture.
 @details       Contains everything required to read a texture accurately, and nothing more. Extraneous data is stored in a MetaDataBlock. 
                Correct use of the texture may rely on MetaDataBlock, but accurate data loading can be done through the standard header alone.
*****************************************************************************/
struct PVRTextureHeaderV3{
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

	/*!***************************************************************************
	@brief      	Constructor for the header - used to make sure that the header is initialised usefully. 
                    The initial pixel format is an invalid one and must be set.
	*****************************************************************************/
	PVRTextureHeaderV3() : 
		u32Version(PVRTEX3_IDENT),			        ///< Version of the file header.
        u32Flags(0),			                    ///< Format flags.
		u64PixelFormat(ePVRTPF_NumCompressedPFs),   ///< The pixel format.
		u32ColourSpace(0),		                    ///< The Colour Space of the texture.
        u32ChannelType(0),              		    ///< Variable type that the channel is stored in.
		u32Height(1),                           	///< Height of the texture.
        u32Width(1),    			                ///< Width of the texture.
        u32Depth(1),                		    	///< Depth of the texture. (Z-slices)
		u32NumSurfaces(1),                  		///< Number of members in a Texture Array.
        u32NumFaces(1),                        		///< Number of faces in a Cube Map. Maybe be a value other than 6.
		u32MIPMapCount(1),          		        ///< Number of MIP Maps in the texture - NB: Includes top level.
        u32MetaDataSize(0)                         	///< Size of the accompanying meta data.
	{}
};
#pragma pack(pop)
#define PVRTEX3_HEADERSIZE 52

/*!***************************************************************************
 @brief     Describes the Version 2 header of a PVR texture header.
*****************************************************************************/
struct PVR_Texture_Header
{
	PVRTuint32 dwHeaderSize;		/*!< size of the structure */
	PVRTuint32 dwHeight;			/*!< height of surface to be created */
	PVRTuint32 dwWidth;				/*!< width of input surface */
	PVRTuint32 dwMipMapCount;		/*!< number of mip-map levels requested */
	PVRTuint32 dwpfFlags;			/*!< pixel format flags */
	PVRTuint32 dwTextureDataSize;	/*!< Total size in bytes */
	PVRTuint32 dwBitCount;			/*!< number of bits per pixel  */
	PVRTuint32 dwRBitMask;			/*!< mask for red bit */
	PVRTuint32 dwGBitMask;			/*!< mask for green bits */
	PVRTuint32 dwBBitMask;			/*!< mask for blue bits */
	PVRTuint32 dwAlphaBitMask;		/*!< mask for alpha channel */
	PVRTuint32 dwPVR;				/*!< magic number identifying pvr file */
	PVRTuint32 dwNumSurfs;			/*!< the number of surfaces present in the pvr */
} ;

/*****************************************************************************
* Legacy (V2 and V1) ENUMS
*****************************************************************************/

    /*!***************************************************************************
     @brief     Legacy pixel type. DEPRECATED.
    *****************************************************************************/
	enum PVRTPixelType
	{
		MGLPT_ARGB_4444 = 0x00,
		MGLPT_ARGB_1555,
		MGLPT_RGB_565,
		MGLPT_RGB_555,
		MGLPT_RGB_888,
		MGLPT_ARGB_8888,
		MGLPT_ARGB_8332,
		MGLPT_I_8,
		MGLPT_AI_88,
		MGLPT_1_BPP,
		MGLPT_VY1UY0,
		MGLPT_Y1VY0U,
		MGLPT_PVRTC2,
		MGLPT_PVRTC4,

		// OpenGL version of pixel types
		OGL_RGBA_4444= 0x10,
		OGL_RGBA_5551,
		OGL_RGBA_8888,
		OGL_RGB_565,
		OGL_RGB_555,
		OGL_RGB_888,
		OGL_I_8,
		OGL_AI_88,
		OGL_PVRTC2,
		OGL_PVRTC4,
		OGL_BGRA_8888,
		OGL_A_8,
		OGL_PVRTCII4,	///< Not in use
		OGL_PVRTCII2,	///< Not in use

		// S3TC Encoding
		D3D_DXT1 = 0x20,
		D3D_DXT2,
		D3D_DXT3,
		D3D_DXT4,
		D3D_DXT5,

		//RGB Formats
		D3D_RGB_332,
		D3D_AL_44,
		D3D_LVU_655,
		D3D_XLVU_8888,
		D3D_QWVU_8888,
		
		//10 bit integer - 2 bit alpha
		D3D_ABGR_2101010,
		D3D_ARGB_2101010,
		D3D_AWVU_2101010,

		//16 bit integers
		D3D_GR_1616,
		D3D_VU_1616,
		D3D_ABGR_16161616,

		//Float Formats
		D3D_R16F,
		D3D_GR_1616F,
		D3D_ABGR_16161616F,

		//32 bits per channel
		D3D_R32F,
		D3D_GR_3232F,
		D3D_ABGR_32323232F,
		
		// Ericsson
		ETC_RGB_4BPP,
		ETC_RGBA_EXPLICIT,				///< Unimplemented
		ETC_RGBA_INTERPOLATED,			///< Unimplemented
		
		D3D_A8 = 0x40,
		D3D_V8U8,
		D3D_L16,
				
		D3D_L8,
		D3D_AL_88,

		//Y'UV Colourspace
		D3D_UYVY,
		D3D_YUY2,
		
		// DX10
		DX10_R32G32B32A32_FLOAT= 0x50,
		DX10_R32G32B32A32_UINT , 
		DX10_R32G32B32A32_SINT,

		DX10_R32G32B32_FLOAT,
		DX10_R32G32B32_UINT,
		DX10_R32G32B32_SINT,

		DX10_R16G16B16A16_FLOAT ,
		DX10_R16G16B16A16_UNORM,
		DX10_R16G16B16A16_UINT ,
		DX10_R16G16B16A16_SNORM ,
		DX10_R16G16B16A16_SINT ,

		DX10_R32G32_FLOAT ,
		DX10_R32G32_UINT ,
		DX10_R32G32_SINT ,

		DX10_R10G10B10A2_UNORM ,
		DX10_R10G10B10A2_UINT ,

		DX10_R11G11B10_FLOAT ,				///< Unimplemented

		DX10_R8G8B8A8_UNORM , 
		DX10_R8G8B8A8_UNORM_SRGB ,
		DX10_R8G8B8A8_UINT ,
		DX10_R8G8B8A8_SNORM ,
		DX10_R8G8B8A8_SINT ,

		DX10_R16G16_FLOAT , 
		DX10_R16G16_UNORM , 
		DX10_R16G16_UINT , 
		DX10_R16G16_SNORM ,
		DX10_R16G16_SINT ,

		DX10_R32_FLOAT ,
		DX10_R32_UINT ,
		DX10_R32_SINT ,

		DX10_R8G8_UNORM ,
		DX10_R8G8_UINT ,
		DX10_R8G8_SNORM , 
		DX10_R8G8_SINT ,

		DX10_R16_FLOAT ,
		DX10_R16_UNORM ,
		DX10_R16_UINT ,
		DX10_R16_SNORM ,
		DX10_R16_SINT ,

		DX10_R8_UNORM, 
		DX10_R8_UINT,
		DX10_R8_SNORM,
		DX10_R8_SINT,

		DX10_A8_UNORM, 
		DX10_R1_UNORM, 
		DX10_R9G9B9E5_SHAREDEXP,	///< Unimplemented
		DX10_R8G8_B8G8_UNORM,		///< Unimplemented
		DX10_G8R8_G8B8_UNORM,		///< Unimplemented

		DX10_BC1_UNORM,	
		DX10_BC1_UNORM_SRGB,

		DX10_BC2_UNORM,	
		DX10_BC2_UNORM_SRGB,

		DX10_BC3_UNORM,	
		DX10_BC3_UNORM_SRGB,

		DX10_BC4_UNORM,				///< Unimplemented
		DX10_BC4_SNORM,				///< Unimplemented

		DX10_BC5_UNORM,				///< Unimplemented
		DX10_BC5_SNORM,				///< Unimplemented

		// OpenVG

		/* RGB{A,X} channel ordering */
		ePT_VG_sRGBX_8888  = 0x90,
		ePT_VG_sRGBA_8888,
		ePT_VG_sRGBA_8888_PRE,
		ePT_VG_sRGB_565,
		ePT_VG_sRGBA_5551,
		ePT_VG_sRGBA_4444,
		ePT_VG_sL_8,
		ePT_VG_lRGBX_8888,
		ePT_VG_lRGBA_8888,
		ePT_VG_lRGBA_8888_PRE,
		ePT_VG_lL_8,
		ePT_VG_A_8,
		ePT_VG_BW_1,

		/* {A,X}RGB channel ordering */
		ePT_VG_sXRGB_8888,
		ePT_VG_sARGB_8888,
		ePT_VG_sARGB_8888_PRE,
		ePT_VG_sARGB_1555,
		ePT_VG_sARGB_4444,
		ePT_VG_lXRGB_8888,
		ePT_VG_lARGB_8888,
		ePT_VG_lARGB_8888_PRE,

		/* BGR{A,X} channel ordering */
		ePT_VG_sBGRX_8888,
		ePT_VG_sBGRA_8888,
		ePT_VG_sBGRA_8888_PRE,
		ePT_VG_sBGR_565,
		ePT_VG_sBGRA_5551,
		ePT_VG_sBGRA_4444,
		ePT_VG_lBGRX_8888,
		ePT_VG_lBGRA_8888,
		ePT_VG_lBGRA_8888_PRE,

		/* {A,X}BGR channel ordering */
		ePT_VG_sXBGR_8888,
		ePT_VG_sABGR_8888 ,
		ePT_VG_sABGR_8888_PRE,
		ePT_VG_sABGR_1555,
		ePT_VG_sABGR_4444,
		ePT_VG_lXBGR_8888,
		ePT_VG_lABGR_8888,
		ePT_VG_lABGR_8888_PRE,

		// max cap for iterating
		END_OF_PIXEL_TYPES,

		MGLPT_NOTYPE = 0xffffffff

	};

/*****************************************************************************
* Legacy constants (V1/V2)
*****************************************************************************/

const PVRTuint32 PVRTEX_MIPMAP			= (1<<8);		///< Has mip map levels. DEPRECATED.
const PVRTuint32 PVRTEX_TWIDDLE			= (1<<9);		///< Is twiddled. DEPRECATED.
const PVRTuint32 PVRTEX_BUMPMAP			= (1<<10);		///< Has normals encoded for a bump map. DEPRECATED.
const PVRTuint32 PVRTEX_TILING			= (1<<11);		///< Is bordered for tiled pvr. DEPRECATED.
const PVRTuint32 PVRTEX_CUBEMAP			= (1<<12);		///< Is a cubemap/skybox. DEPRECATED.
const PVRTuint32 PVRTEX_FALSEMIPCOL		= (1<<13);		///< Are there false coloured MIP levels. DEPRECATED.
const PVRTuint32 PVRTEX_VOLUME			= (1<<14);		///< Is this a volume texture. DEPRECATED.
const PVRTuint32 PVRTEX_ALPHA			= (1<<15);		///< v2.1. Is there transparency info in the texture. DEPRECATED.
const PVRTuint32 PVRTEX_VERTICAL_FLIP	= (1<<16);		///< v2.1. Is the texture vertically flipped. DEPRECATED.

const PVRTuint32 PVRTEX_PIXELTYPE		= 0xff;			///< Pixel type is always in the last 16bits of the flags. DEPRECATED.
const PVRTuint32 PVRTEX_IDENTIFIER		= 0x21525650;	///< The pvr identifier is the characters 'P','V','R'. DEPRECATED.

const PVRTuint32 PVRTEX_V1_HEADER_SIZE	= 44;			///< Old header size was 44 for identification purposes. DEPRECATED.

const PVRTuint32 PVRTC2_MIN_TEXWIDTH	= 16;			///< DEPRECATED.
const PVRTuint32 PVRTC2_MIN_TEXHEIGHT	= 8; 			///< DEPRECATED.
const PVRTuint32 PVRTC4_MIN_TEXWIDTH	= 8; 			///< DEPRECATED.
const PVRTuint32 PVRTC4_MIN_TEXHEIGHT	= 8; 			///< DEPRECATED.
const PVRTuint32 ETC_MIN_TEXWIDTH		= 4; 			///< DEPRECATED.
const PVRTuint32 ETC_MIN_TEXHEIGHT		= 4; 			///< DEPRECATED.
const PVRTuint32 DXT_MIN_TEXWIDTH		= 4; 			///< DEPRECATED.
const PVRTuint32 DXT_MIN_TEXHEIGHT		= 4; 			///< DEPRECATED.

/****************************************************************************
** Functions
****************************************************************************/

/*!***************************************************************************
 @fn       		PVRTTextureCreate
 @param[in]		w			Size of the texture
 @param[in]		h			Size of the texture
 @param[in]		wMin		Minimum size of a texture level
 @param[in]		hMin		Minimum size of a texture level
 @param[in]		nBPP		Bits per pixel of the format
 @param[in]		bMIPMap		Create memory for MIP-map levels also?
 @return		Allocated texture memory (must be free()d)
 @brief      	Creates a PVRTextureHeaderV3 structure, including room for
                the specified texture, in memory.
*****************************************************************************/
PVRTextureHeaderV3 *PVRTTextureCreate(
									  unsigned int		w,
									  unsigned int		h,
									  const unsigned int	wMin,
									  const unsigned int	hMin,
									  const unsigned int	nBPP,
									  const bool			bMIPMap);

/*!***************************************************************************
 @fn       		PVRTTextureTile
 @param[in,out]	pOut		The tiled texture in system memory
 @param[in]		pIn			The source texture
 @param[in]		nRepeatCnt	Number of times to repeat the source texture
 @brief      	Allocates and fills, in system memory, a texture large enough
                to repeat the source texture specified number of times.
*****************************************************************************/
void PVRTTextureTile(
					 PVRTextureHeaderV3			**pOut,
					 const PVRTextureHeaderV3	* const pIn,
					 const int					nRepeatCnt);

/****************************************************************************
** Internal Functions
****************************************************************************/
//Preprocessor definitions to generate a pixelID for use when consts are needed. For example - switch statements. These should be evaluated by the compiler rather than at run time - assuming that arguments are all constant.

//Generate a 4 channel PixelID.
#define PVRTGENPIXELID4(C1Name, C2Name, C3Name, C4Name, C1Bits, C2Bits, C3Bits, C4Bits) ( ( (PVRTuint64)C1Name) + ( (PVRTuint64)C2Name<<8) + ( (PVRTuint64)C3Name<<16) + ( (PVRTuint64)C4Name<<24) + ( (PVRTuint64)C1Bits<<32) + ( (PVRTuint64)C2Bits<<40) + ( (PVRTuint64)C3Bits<<48) + ( (PVRTuint64)C4Bits<<56) )

//Generate a 1 channel PixelID.
#define PVRTGENPIXELID3(C1Name, C2Name, C3Name, C1Bits, C2Bits, C3Bits)( PVRTGENPIXELID4(C1Name, C2Name, C3Name, 0, C1Bits, C2Bits, C3Bits, 0) )

//Generate a 2 channel PixelID.
#define PVRTGENPIXELID2(C1Name, C2Name, C1Bits, C2Bits) ( PVRTGENPIXELID4(C1Name, C2Name, 0, 0, C1Bits, C2Bits, 0, 0) )

//Generate a 3 channel PixelID.
#define PVRTGENPIXELID1(C1Name, C1Bits) ( PVRTGENPIXELID4(C1Name, 0, 0, 0, C1Bits, 0, 0, 0))

//Forward declaration of CPVRTMap.
template <typename KeyType, typename DataType>
class CPVRTMap;


/*!***********************************************************************
 @fn       		PVRTGetBitsPerPixel
 @param[in]		u64PixelFormat		A PVR Pixel Format ID.
 @return		const PVRTuint32	Number of bits per pixel.
 @brief      	Returns the number of bits per pixel in a PVR Pixel Format 
				identifier.
*************************************************************************/
PVRTuint32 PVRTGetBitsPerPixel(PVRTuint64 u64PixelFormat);

/*!***********************************************************************
 @fn       		PVRTGetFormatMinDims
 @param[in]		u64PixelFormat	A PVR Pixel Format ID.
 @param[in,out]	minX			Returns the minimum width.
 @param[in,out]	minY			Returns the minimum height.
 @param[in,out]	minZ			Returns the minimum depth.
 @brief      	Gets the minimum dimensions (x,y,z) for a given pixel format.
*************************************************************************/
void PVRTGetFormatMinDims(PVRTuint64 u64PixelFormat, PVRTuint32 &minX, PVRTuint32 &minY, PVRTuint32 &minZ);

/*!***********************************************************************
 @fn       		PVRTConvertOldTextureHeaderToV3
 @param[in]		LegacyHeader	Legacy header for conversion.
 @param[in,out]	NewHeader		New header to output into.
 @param[in,out]	pMetaData		MetaData Map to output into.
 @brief      	Converts a legacy texture header (V1 or V2) to a current 
				generation header (V3).
*************************************************************************/
void PVRTConvertOldTextureHeaderToV3(const PVR_Texture_Header* LegacyHeader, PVRTextureHeaderV3& NewHeader, CPVRTMap<PVRTuint32, CPVRTMap<PVRTuint32,MetaDataBlock> >* pMetaData);

/*!***********************************************************************
 @fn       		PVRTMapLegacyTextureEnumToNewFormat
 @param[in]		OldFormat		Legacy Enumeration Value
 @param[in,out]	newType			New PixelType identifier.
 @param[in,out]	newCSpace		New ColourSpace
 @param[in,out]	newChanType		New Channel Type
 @param[in,out]	isPreMult		Whether format is pre-multiplied
 @brief      	Maps a legacy enumeration value to the new PVR3 style format.
*************************************************************************/
void PVRTMapLegacyTextureEnumToNewFormat(PVRTPixelType OldFormat, PVRTuint64& newType, EPVRTColourSpace& newCSpace, EPVRTVariableType& newChanType, bool& isPreMult);

/*!***************************************************************************
 @fn       		PVRTTextureLoadTiled
 @param[in,out]	pDst			Texture to place the tiled data
 @param[in]		nWidthDst		Width of destination texture
 @param[in]		nHeightDst		Height of destination texture
 @param[in]		pSrc			Texture to tile
 @param[in]		nWidthSrc		Width of source texture
 @param[in]		nHeightSrc		Height of source texture
 @param[in] 	nElementSize	Bytes per pixel
 @param[in]		bTwiddled		True if the data is twiddled
 @brief      	Needed by PVRTTextureTile() in the various PVRTTextureAPIs.
*****************************************************************************/
void PVRTTextureLoadTiled(
						  PVRTuint8		* const pDst,
						  const unsigned int	nWidthDst,
						  const unsigned int	nHeightDst,
						  const PVRTuint8	* const pSrc,
						  const unsigned int	nWidthSrc,
						  const unsigned int	nHeightSrc,
						  const unsigned int	nElementSize,
						  const bool			bTwiddled);


/*!***************************************************************************
 @fn       		PVRTTextureTwiddle
 @param[out]	a	Twiddled value
 @param[in]		u	Coordinate axis 0
 @param[in]		v	Coordinate axis 1
 @brief      	Combine a 2D coordinate into a twiddled value.
*****************************************************************************/
void PVRTTextureTwiddle(unsigned int &a, const unsigned int u, const unsigned int v);

/*!***************************************************************************
 @fn       		PVRTTextureDeTwiddle
 @param[out]	u	Coordinate axis 0
 @param[out]	v	Coordinate axis 1
 @param[in]		a	Twiddled value
 @brief      	Extract 2D coordinates from a twiddled value.
*****************************************************************************/
void PVRTTextureDeTwiddle(unsigned int &u, unsigned int &v, const unsigned int a);

/*!***********************************************************************
 @fn       		PVRTGetTextureDataSize
 @param[in]		sTextureHeader	Specifies the texture header. 
 @param[in]		iMipLevel	Specifies a mip level to check, 'PVRTEX_ALLMIPLEVELS'
                            can be passed to get the size of all MIP levels.  
 @param[in]		bAllSurfaces	Size of all surfaces is calculated if true, 
							only a single surface if false.
 @param[in]		bAllFaces	Size of all faces is calculated if true, 
							only a single face if false.
 @return		PVRTuint32		Size in BYTES of the specified texture area.
 @brief      	Gets the size in BYTES of the texture, given various input 
				parameters.	User can retrieve the size of either all 
				surfaces or a single surface, all faces or a single face and
				all MIP-Maps or a single specified MIP level.
*************************************************************************/
PVRTuint32 PVRTGetTextureDataSize(PVRTextureHeaderV3 sTextureHeader, PVRTint32 iMipLevel=PVRTEX_ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true);

#endif /* _PVRTTEXTURE_H_ */

/*****************************************************************************
End of file (PVRTTexture.h)
*****************************************************************************/

