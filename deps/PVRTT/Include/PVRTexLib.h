#pragma once

#ifndef PVR_DLL
#if defined(PVRTEXLIB_EXPORT)
#if defined(_MSC_VER)
#define PVR_DLL __declspec(dllexport)
#else
#define PVR_DLL __attribute__((visibility("default")))
#endif
#elif defined(_MSC_VER) && defined(PVRTEXLIB_IMPORT) // To use the PVRTexLib .dll on Windows, define PVRTEXLIB_IMPORT
#define PVR_DLL __declspec(dllimport)
#else
#define PVR_DLL
#endif
#endif

#ifdef __cplusplus
extern "C" {
#endif
#include "PVRTexLibDefines.h"
#include "PVRTextureVersion.h"

typedef void* PVRTexLib_PVRTextureHeader;
typedef const void* PVRTexLib_CPVRTextureHeader;
typedef void* PVRTexLib_PVRTexture;
typedef const void* PVRTexLib_CPVRTexture;

/*!***********************************************************************
 @struct	PVRHeader_CreateParams
 @brief		Structure containing various texture header parameters for
			PVRTexLib_CreateTextureHeader().
*************************************************************************/
struct PVRHeader_CreateParams
{
	PVRTuint64				pixelFormat;     ///< pixel format
	PVRTuint32				width;           ///< texture width
	PVRTuint32				height;          ///< texture height
	PVRTuint32				depth;           ///< texture depth
	PVRTuint32				numMipMaps;      ///< number of MIP maps
	PVRTuint32				numArrayMembers; ///< number of array members
	PVRTuint32				numFaces;        ///< number of faces
	PVRTexLibColourSpace	colourSpace;     ///< colour space
	PVRTexLibVariableType	channelType;     ///< channel type
	bool					preMultiplied;   ///< has the RGB been pre-multiplied by the alpha?
};

/*!***********************************************************************
 @struct	PVRTexLib_Orientation
 @brief		Structure containing a textures orientation in each axis.
*************************************************************************/
struct PVRTexLib_Orientation
{
	PVRTexLibOrientation x; ///< X axis orientation
	PVRTexLibOrientation y; ///< Y axis orientation
	PVRTexLibOrientation z; ///< Z axis orientation
};

/*!***********************************************************************
 @struct	PVRTexLib_OpenGLFormat
 @brief		Structure containing a OpenGL[ES] format.
*************************************************************************/
struct PVRTexLib_OpenGLFormat
{
	PVRTuint32 internalFormat;	///< GL internal format
	PVRTuint32 format;			///< GL format
	PVRTuint32 type;			///< GL type
};

typedef PVRTexLib_OpenGLFormat PVRTexLib_OpenGLESFormat;

/*!***********************************************************************
 @struct	PVRTexLib_MetaDataBlock
 @brief		Structure containing a block of meta data for a texture.
*************************************************************************/
struct PVRTexLib_MetaDataBlock
{
	PVRTuint32	DevFOURCC;		///< A 4cc descriptor of the data type's creator. Values starting with 'PVR' are reserved for PVRTexLib.
	PVRTuint32	u32Key;			///< A unique value identifying the data type, and thus how to read it. For example PVRTexLibMetaData.
	PVRTuint32	u32DataSize;	///< Size of 'Data' in bytes.
	PVRTuint8*	Data;			///< Meta data bytes
};

#define PVRTEXLIB_SIZEOF_METADATABLOCK(BLOCK) offsetof(PVRTexLib_MetaDataBlock, Data) + BLOCK.u32DataSize;

/*!***********************************************************************
 @struct	PVRTexLib_TranscoderOptions
 @brief		Structure containing the transcoder options for
			PVRTexLib_TranscodeTexture().
*************************************************************************/
#pragma pack(push,4)
struct PVRTexLib_TranscoderOptions
{
	PVRTuint32 sizeofStruct;				///< For versioning - sizeof(PVRTexLib_TranscoderOptions)
	PVRTuint64 pixelFormat;					///< Pixel format type
	PVRTexLibVariableType channelType[4U];	///< Per-channel variable type.
	PVRTexLibColourSpace colourspace;		///< Colour space
	PVRTexLibCompressorQuality quality;		///< Compression quality for PVRTC, ASTC, ETC, BASISU and IMGIC, higher quality usually requires more processing time.
	bool doDither;							///< Apply dithering to lower precision formats.
	float maxRange;							///< Max range value for RGB[M|D] encoding
	PVRTuint32 maxThreads;					///< Max number of threads to use for transcoding, if set to 0 PVRTexLib will use all available cores.
};
#pragma pack(pop)

/*!***********************************************************************
 @struct	PVRTexLib_ErrorMetrics
 @brief		Structure containing the resulting error metrics computed by:
			PVRTexLib_MaxDifference(),
			PVRTexLib_MeanError(),
			PVRTexLib_MeanSquaredError(),
			PVRTexLib_RootMeanSquaredError(),
			PVRTexLib_StandardDeviation(),
			PVRTexLib_PeakSignalToNoiseRatio().
*************************************************************************/
struct PVRTexLib_ErrorMetrics
{
	struct
	{
		PVRTexLibChannelName name; ///< Channel name. PVRTLCN_NoChannel indicates invalid entry.
		double value; ///< Value for this channel.
	} channels[4U]; ///< Per-channel metrics, not all entries have to be valid.

	double allChannels; ///< Value for all channels.
	double rgbChannels; ///< Value for RGB channels.
};

/*!***********************************************************************
 @brief			Sets up default texture header parameters.
 @param[in,out]	result Default header attributes.
*************************************************************************/
PVR_DLL void PVRTexLib_SetDefaultTextureHeaderParams(PVRHeader_CreateParams* result);

/*!***********************************************************************
 @brief     Creates a new texture header using the supplied
			header parameters.
 @param[in]	attribs The header attributes
 @return	A handle to a new texture header.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTextureHeader PVRTexLib_CreateTextureHeader(const PVRHeader_CreateParams* attribs);

/*!***********************************************************************
 @brief     Creates a new texture header from a PVRV3 structure.
			Optionally supply meta data.
 @param[in]	header PVRTextureHeaderV3 structure to create from.
 @param[in]	metaDataCount Number of items in metaData, can be 0.
 @param[in]	metaData Array of meta data blocks, can be null.
 @return	A handle to a new texture header.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTextureHeader PVRTexLib_CreateTextureHeaderFromHeader(const PVRTextureHeaderV3* header, PVRTuint32 metaDataCount, PVRTexLib_MetaDataBlock* metaData);

/*!***********************************************************************
 @brief		Creates a new texture header by copying values from a
			previously allocated texture header.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	A handle to a new texture header.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTextureHeader PVRTexLib_CopyTextureHeader(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief      	Free a previously allocated texture header.
 @param[in]		header Handle to a PVRTexLib_PVRTextureHeader.
*************************************************************************/
PVR_DLL void PVRTexLib_DestroyTextureHeader(PVRTexLib_PVRTextureHeader header);

/*!************************************************************************
 @brief     Low level texture creation function.
			Creates a PVRTextureHeaderV3 structure,
			including room for the specified texture, in memory.
 @param[in]	width Width of the texture in pixels
 @param[in]	height Height of the texture in pixels
 @param[in] depth Number of Z layers
 @param[in]	wMin Minimum width of a texture level
 @param[in]	hMin Minimum height of a texture level
 @param[in] dMin Minimum depth of a texture level
 @param[in]	nBPP Bits per pixel
 @param[in]	bMIPMap	Create memory for MIP-map levels also?
 @param[in] pfnAllocCallback Memory allocation callback function.
 @return	Allocated texture memory. free()d by caller.
**************************************************************************/
PVR_DLL PVRTextureHeaderV3* PVRTexLib_TextureCreateRaw(
	PVRTuint32 width,
	PVRTuint32 height,
	PVRTuint32 depth,
	PVRTuint32 wMin,
	PVRTuint32 hMin,
	PVRTuint32 dMin,
	PVRTuint32 nBPP,
	bool		 bMIPMap,
	void* (pfnAllocCallback)(PVRTuint64 allocSize));

/*!***************************************************************************
 @brief		Low level texture creation function.
			Load blocks of data from pSrc into pDst.
 @param[in]	pDst Texture to place the tiled data
 @param[in]	widthDst Width of destination texture
 @param[in]	heightDst Height of destination texture
 @param[in]	pSrc Texture to tile
 @param[in]	widthSrc Width of source texture
 @param[in]	heightSrc Height of source texture
 @param[in] elementSize Bytes per pixel
 @param[in]	twiddled True if the data is twiddled
*****************************************************************************/
PVR_DLL void PVRTexLib_TextureLoadTiled(
	PVRTuint8*	pDst,
	PVRTuint32	widthDst,
	PVRTuint32	heightDst,
	PVRTuint8*	pSrc,
	PVRTuint32	widthSrc,
	PVRTuint32	heightSrc,
	PVRTuint32	elementSize,
	bool		twiddled);

/*!***********************************************************************
 @brief		Gets the number of bits per pixel for the specified texture header.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Number of bits per pixel.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureBitsPerPixel(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Gets the number of bits per pixel for the specified pixel format.
 @param		u64PixelFormat A PVR pixel format ID.
 @return	Number of bits per pixel.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetFormatBitsPerPixel(PVRTuint64 u64PixelFormat);

/*!***********************************************************************
 @brief		Gets the number of channels for the specified texture header.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	For uncompressed formats the number of channels between 1 and 4. 
			For compressed formats 0
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureChannelCount(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Gets the channel type for the specified texture header.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	PVRTexLibVariableType enum.
*************************************************************************/
PVR_DLL PVRTexLibVariableType PVRTexLib_GetTextureChannelType(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Gets the colour space for the specified texture header.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	PVRTexLibColourSpace enum.
*************************************************************************/
PVR_DLL PVRTexLibColourSpace PVRTexLib_GetTextureColourSpace(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the width of the user specified MIP-Map level for the
            texture
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	uiMipLevel MIP level that user is interested in.
 @return    Width of the specified MIP-Map level.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureWidth(PVRTexLib_CPVRTextureHeader header, PVRTuint32 mipLevel);

/*!***********************************************************************
 @brief     Gets the height of the user specified MIP-Map 
 			level for the texture
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	uiMipLevel MIP level that user is interested in.
 @return	Height of the specified MIP-Map level.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureHeight(PVRTexLib_CPVRTextureHeader header, PVRTuint32 mipLevel);

/*!***********************************************************************
 @brief     Gets the depth of the user specified MIP-Map 
			level for the texture
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	uiMipLevel MIP level that user is interested in.
 @return	Depth of the specified MIP-Map level.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureDepth(PVRTexLib_CPVRTextureHeader header, PVRTuint32 mipLevel);

/*!***********************************************************************
 @brief     Gets the size in PIXELS of the texture, given various input 
			parameters.	User can retrieve the total size of either all 
			surfaces or a single surface, all faces or a single face and
			all MIP-Maps or a single specified MIP level. All of these
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	iMipLevel		Specifies a MIP level to check, 
							'PVRTEX_ALLMIPLEVELS' can be passed to get 
							the size of all MIP levels. 
 @param[in]	bAllSurfaces	Size of all surfaces is calculated if true, 
							only a single surface if false.
 @param[in]	bAllFaces		Size of all faces is calculated if true, 
							only a single face if false.
 @return	Size in PIXELS of the specified texture area.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureSize(PVRTexLib_CPVRTextureHeader header, PVRTint32 mipLevel, bool allSurfaces, bool allFaces);

/*!***********************************************************************
 @brief		Gets the size in BYTES of the texture, given various input 
			parameters.	User can retrieve the size of either all 
			surfaces or a single surface, all faces or a single face 
			and all MIP-Maps or a single specified MIP level.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	iMipLevel		Specifies a mip level to check, 
							'PVRTEX_ALLMIPLEVELS' can be passed to get 
							the size of all MIP levels. 
 @param[in]	bAllSurfaces	Size of all surfaces is calculated if true, 
							only a single surface if false.
 @param[in]	bAllFaces		Size of all faces is calculated if true, 
							only a single face if false.
 @return	Size in BYTES of the specified texture area.
*************************************************************************/
PVR_DLL PVRTuint64 PVRTexLib_GetTextureDataSize(PVRTexLib_CPVRTextureHeader header, PVRTint32 mipLevel, bool allSurfaces, bool allFaces);

/*!***********************************************************************
 @brief      	Gets the data orientation for this texture.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	result Pointer to a PVRTexLib_Orientation structure.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureOrientation(PVRTexLib_CPVRTextureHeader header, PVRTexLib_Orientation* result);

/*!***********************************************************************
 @brief      	Gets the OpenGL equivalent format for this texture.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	result Pointer to a PVRTexLib_OpenGLFormat structure.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureOpenGLFormat(PVRTexLib_CPVRTextureHeader header, PVRTexLib_OpenGLFormat* result);

/*!***********************************************************************
 @brief      	Gets the OpenGLES equivalent format for this texture.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	result Pointer to a PVRTexLib_OpenGLESFormat structure.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureOpenGLESFormat(PVRTexLib_CPVRTextureHeader header, PVRTexLib_OpenGLESFormat* result);

/*!***********************************************************************
 @brief     Gets the Vulkan equivalent format for this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	A VkFormat enum value.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureVulkanFormat(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the Direct3D equivalent format for this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	A D3DFORMAT enum value.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureD3DFormat(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the DXGI equivalent format for this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	A DXGI_FORMAT enum value.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureDXGIFormat(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief 				Gets the minimum dimensions (x,y,z)
						for the textures pixel format.
 @param[in] header		A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	minX	Returns the minimum width.
 @param[in,out]	minY	Returns the minimum height.
 @param[in,out]	minZ	Returns the minimum depth.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureFormatMinDims(PVRTexLib_CPVRTextureHeader header, PVRTuint32* minX, PVRTuint32* minY, PVRTuint32* minZ);

/*!***********************************************************************
 @brief 		Gets the minimum dimensions (x,y,z)
				for the textures pixel format.
 @param[in]		u64PixelFormat A PVR Pixel Format ID.
 @param[in,out]	minX Returns the minimum width.
 @param[in,out]	minY Returns the minimum height.
 @param[in,out]	minZ Returns the minimum depth.
*************************************************************************/
PVR_DLL void PVRTexLib_GetPixelFormatMinDims(PVRTuint64 ui64Format, PVRTuint32* minX, PVRTuint32* minY, PVRTuint32* minZ);

/*!***********************************************************************
 @brief		Returns the total size of the meta data stored in the header.
			This includes the size of all information stored in all MetaDataBlocks.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Size, in bytes, of the meta data stored in the header.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureMetaDataSize(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Returns whether or not the texture's colour has been
			pre-multiplied by the alpha values.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	True if texture is premultiplied.
*************************************************************************/
PVR_DLL bool PVRTexLib_GetTextureIsPreMultiplied(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Returns whether or not the texture is compressed using
			PVRTexLib's FILE compression - this is independent of
			any texture compression.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	True if it is file compressed.
*************************************************************************/
PVR_DLL bool PVRTexLib_GetTextureIsFileCompressed(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Returns whether or not the texture is a bump map.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	True if it is a bump map.
*************************************************************************/
PVR_DLL bool PVRTexLib_GetTextureIsBumpMap(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the bump map scaling value for this texture.
			If the texture is not a bump map, 0.0f is returned. If the
			texture is a bump map but no meta data is stored to
			specify its scale, then 1.0f is returned.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Returns the bump map scale value as a float.
*************************************************************************/
PVR_DLL float PVRTexLib_GetTextureBumpMapScale(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Works out the number of possible texture atlas members in
			the texture based on the width, height, depth and data size.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	The number of sub textures defined by meta data.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetNumTextureAtlasMembers(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief			Returns a pointer to the texture atlas data.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	count Number of floats in the returned data set.
 @return		A pointer directly to the texture atlas data. NULL if
				the texture does not have atlas data.
*************************************************************************/
PVR_DLL const float* PVRTexLib_GetTextureAtlasData(PVRTexLib_CPVRTextureHeader header, PVRTuint32* count);

/*!***********************************************************************
 @brief     Gets the number of MIP-Map levels stored in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Number of MIP-Map levels in this texture.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureNumMipMapLevels(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the number of faces stored in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Number of faces in this texture.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureNumFaces(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Gets the number of array members stored in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	Number of array members in this texture.
*************************************************************************/
PVR_DLL PVRTuint32 PVRTexLib_GetTextureNumArrayMembers(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief			Gets the cube map face order.
				cubeOrder string will be in the form "ZzXxYy" with capitals
				representing positive and lower case letters representing
				negative. I.e. Z=Z-Positive, z=Z-Negative.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out] cubeOrder Null terminated cube map order string.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureCubeMapOrder(PVRTexLib_CPVRTextureHeader header, char cubeOrder[7]);

/*!***********************************************************************
 @brief      	Gets the bump map channel order relative to rgba.
				For	example, an RGB texture with bumps mapped to XYZ returns
				'xyz'. A BGR texture with bumps in the order ZYX will also
				return 'xyz' as the mapping is the same: R=X, G=Y, B=Z.
				If the letter 'h' is present in the string, it means that
				the height map has been stored here.
				Other characters are possible if the bump map was created
				manually, but PVRTexLib will ignore these characters. They
				are returned simply for completeness.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	Null terminated bump map order string relative to rgba.
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureBumpMapOrder(PVRTexLib_CPVRTextureHeader header, char bumpOrder[5]);

/*!***********************************************************************
 @brief     Gets the 64-bit pixel type ID of the texture.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	64-bit pixel type ID.
*************************************************************************/
PVR_DLL PVRTuint64 PVRTexLib_GetTexturePixelFormat(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief     Checks whether the pixel format of the texture is packed.
			E.g. R5G6B5, R11G11B10, R4G4B4A4 etc.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @return	True if the texture format is packed, false otherwise.
*************************************************************************/
PVR_DLL bool PVRTexLib_TextureHasPackedChannelData(PVRTexLib_CPVRTextureHeader header);

/*!***********************************************************************
 @brief		Sets the variable type for the channels in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	type A PVRTexLibVariableType enum.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureChannelType(PVRTexLib_PVRTextureHeader header, PVRTexLibVariableType type);

/*!***********************************************************************
 @brief     Sets the colour space for this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	colourSpace	A PVRTexLibColourSpace enum.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureColourSpace(PVRTexLib_PVRTextureHeader header, PVRTexLibColourSpace colourSpace);

/*!***********************************************************************
 @brief     Sets the format of the texture to PVRTexLib's internal
			representation of the D3D format.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in] d3dFormat A D3DFORMAT enum.
 @return	True if successful.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureD3DFormat(PVRTexLib_PVRTextureHeader header, PVRTuint32 d3dFormat);

/*!***********************************************************************
 @brief     Sets the format of the texture to PVRTexLib's internal
			representation of the DXGI format.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	dxgiFormat A DXGI_FORMAT enum.
 @return	True if successful.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureDXGIFormat(PVRTexLib_PVRTextureHeader header, PVRTuint32 dxgiFormat);

/*!***********************************************************************
 @brief		Sets the format of the texture to PVRTexLib's internal
			representation of the OpenGL format.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	oglFormat The OpenGL format.
 @return	True if successful.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureOGLFormat(PVRTexLib_PVRTextureHeader header, const PVRTexLib_OpenGLFormat* oglFormat);

/*!***********************************************************************
 @brief		Sets the format of the texture to PVRTexLib's internal
			representation of the OpenGLES format.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	oglesFormat The OpenGLES format.
 @return	True if successful.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureOGLESFormat(PVRTexLib_PVRTextureHeader header, const PVRTexLib_OpenGLESFormat* oglesFormat);

/*!***********************************************************************
 @brief		Sets the format of the texture to PVRTexLib's internal
			representation of the OpenGLES format.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	vulkanFormat A VkFormat enum.
 @return	True if successful.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureVulkanFormat(PVRTexLib_PVRTextureHeader header, PVRTuint32 vulkanFormat);

/*!***********************************************************************
 @brief     Sets the pixel format for this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	format	The format of the pixel.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTexturePixelFormat(PVRTexLib_PVRTextureHeader header, PVRTuint64 format);

/*!***********************************************************************
 @brief		Sets the texture width.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	width The new width.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureWidth(PVRTexLib_PVRTextureHeader header, PVRTuint32 width);

/*!***********************************************************************
 @brief		Sets the texture height.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	height The new height.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureHeight(PVRTexLib_PVRTextureHeader header, PVRTuint32 height);

/*!***********************************************************************
 @brief		Sets the texture depth.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	depth The new depth.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureDepth(PVRTexLib_PVRTextureHeader header, PVRTuint32 depth);

/*!***********************************************************************
 @brief		Sets the number of array members in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	newNumMembers The new number of array members.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureNumArrayMembers(PVRTexLib_PVRTextureHeader header, PVRTuint32 numMembers);

/*!***********************************************************************
 @brief		Sets the number of MIP-Map levels in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	numMIPLevels New number of MIP-Map levels.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureNumMIPLevels(PVRTexLib_PVRTextureHeader header, PVRTuint32 numMIPLevels);

/*!***********************************************************************
 @brief		Sets the number of faces stored in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	numFaces New number of faces for this texture.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureNumFaces(PVRTexLib_PVRTextureHeader header, PVRTuint32 numFaces);

/*!***********************************************************************
 @brief     Sets the data orientation for a given axis in this texture.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	orientation Pointer to a PVRTexLib_Orientation struct.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureOrientation(PVRTexLib_PVRTextureHeader header, const PVRTexLib_Orientation* orientation);

/*!***********************************************************************
 @brief		Sets whether or not the texture is compressed using
			PVRTexLib's FILE compression - this is independent of 
			any texture compression. Currently unsupported.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	isFileCompressed Sets the file compression to true or false.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureIsFileCompressed(PVRTexLib_PVRTextureHeader header, bool isFileCompressed);

/*!***********************************************************************
 @brief     Sets whether or not the texture's colour has been
			pre-multiplied by the alpha values.
 @param[in] header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in] isPreMultiplied	Sets if texture is premultiplied.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureIsPreMultiplied(PVRTexLib_PVRTextureHeader header, bool isPreMultiplied);

/*!***********************************************************************
 @brief			Obtains the border size in each dimension for this texture.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in,out]	borderWidth   Border width
 @param[in,out]	borderHeight  Border height
 @param[in,out]	borderDepth   Border depth
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureBorder(PVRTexLib_CPVRTextureHeader header,
	PVRTuint32* borderWidth,
	PVRTuint32* borderHeight,
	PVRTuint32* borderDepth);

/*!***********************************************************************
 @brief			Returns a copy of a block of meta data from the texture.
				If the meta data doesn't exist, a block with a data size
				of 0 will be returned.
 @param[in]		header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]		DevFOURCC Four character descriptor representing the 
				creator of the meta data
 @param[in]		u32Key Key value representing the type of meta data stored
 @param[in,out] dataBlock returned meta block data
 @param[in]		pfnAllocCallback Memory allocation callback function.
 @return		True if the meta data block was found.
*************************************************************************/
PVR_DLL bool PVRTexLib_GetMetaDataBlock(
	PVRTexLib_CPVRTextureHeader header,
	PVRTuint32				 devFOURCC,
	PVRTuint32				 key,
	PVRTexLib_MetaDataBlock* dataBlock,
	void* (pfnAllocCallback)(PVRTuint32 allocSize));

/*!***********************************************************************
 @brief     Returns whether or not the specified meta data exists as 
			part of this texture header.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	DevFOURCC Four character descriptor representing the 
					  creator of the meta data
 @param[in]	u32Key Key value representing the type of meta data stored
 @return	True if the specified meta data bock exists
*************************************************************************/
PVR_DLL bool PVRTexLib_TextureHasMetaData(PVRTexLib_CPVRTextureHeader header, PVRTuint32 devFOURCC, PVRTuint32 key);

/*!***********************************************************************
 @brief     Sets a texture's bump map data.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	bumpScale Floating point "height" value to scale the bump map.
 @param[in]	bumpOrder Up to 4 character string, with values x,y,z,h in 
					  some combination. Not all values need to be present.
					  Denotes channel order; x,y,z refer to the 
					  corresponding axes, h indicates presence of the
					  original height map. It is possible to have only some
					  of these values rather than all. For example if 'h'
					  is present alone it will be considered a height map.
					  The values should be presented in RGBA order, regardless
					  of the texture format, so a zyxh order in a bgra texture
					  should still be passed as 'xyzh'. Capitals are allowed.
					  Any character stored here that is not one of x,y,z,h
					  or a NULL character	will be ignored when PVRTexLib 
					  reads the data,	but will be preserved. This is useful
					  if you wish to define a custom data channel for instance.
					  In these instances PVRTexLib will assume it is simply
					  colour data.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureBumpMap(PVRTexLib_PVRTextureHeader header, float bumpScale, const char* bumpOrder);

/*!***********************************************************************
 @brief		Sets the texture atlas coordinate meta data for later display.
			It is up to the user to make sure that this texture atlas
			data actually makes sense in the context of the header.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	atlasData Pointer to an array of atlas data.
 @param[in]	dataSize Number of floats in atlasData.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureAtlas(PVRTexLib_PVRTextureHeader header, const float* atlasData, PVRTuint32 dataSize);

/*!***********************************************************************
 @brief     Sets the texture's face ordering.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	cubeMapOrder	Up to 6 character string, with values 
							x,X,y,Y,z,Z in some combination. Not all 
							values need to be present. Denotes face 
							order; Capitals refer to positive axis 
							positions and small letters refer to 
							negative axis positions. E.g. x=X-Negative,
							X=X-Positive. It is possible to have only 
							some of these values rather than all, as 
							long as they are NULL terminated.
							NB: Values past the 6th character are not read.
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureCubeMapOrder(PVRTexLib_PVRTextureHeader header, const char* cubeMapOrder);

/*!***********************************************************************
 @brief     Sets a texture's border size data. This value is subtracted 
			from the current texture height/width/depth to get the valid 
			texture data.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	borderWidth   Border width
 @param[in]	borderHeight  Border height
 @param[in]	borderDepth   Border depth
*************************************************************************/
PVR_DLL void PVRTexLib_SetTextureBorder(PVRTexLib_PVRTextureHeader header, PVRTuint32 borderWidth, PVRTuint32 borderHeight, PVRTuint32 borderDepth);

/*!***********************************************************************
 @brief     Adds an arbitrary piece of meta data.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	dataBlock Meta data block to be added.
*************************************************************************/
PVR_DLL void PVRTexLib_AddMetaData(PVRTexLib_PVRTextureHeader header, const PVRTexLib_MetaDataBlock* dataBlock);

/*!***********************************************************************
 @brief     Removes a specified piece of meta data, if it exists.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	DevFOURCC Four character descriptor representing the 
					  creator of the meta data
 @param[in] u32Key Key value representing the type of meta data stored.
*************************************************************************/
PVR_DLL void PVRTexLib_RemoveMetaData(PVRTexLib_PVRTextureHeader header, PVRTuint32 devFOURCC, PVRTuint32 key);

/*!***********************************************************************
 @brief     Creates a new texture based on a texture header, 
			and optionally copies the supplied texture data.
 @param[in]	header A handle to a previously allocated PVRTexLib_PVRTextureHeader.
 @param[in]	data Texture data (may be NULL)
 @return	A new texture handle.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTexture PVRTexLib_CreateTexture(PVRTexLib_CPVRTextureHeader header, const void* data);

/*!***********************************************************************
 @brief     Creates a copy of the supplied texture.
 @param[in]	texture A handle to a PVRTexLib_PVRTexture to copy from.
 @return	A new texture handle.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTexture PVRTexLib_CopyTexture(PVRTexLib_CPVRTexture texture);

/*!***********************************************************************
 @brief     Creates a new texture, moving the contents of the
			supplied texture into the new texture.
 @param[in]	texture A handle to a PVRTexLib_PVRTexture to move from.
 @return	A new texture handle.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTexture PVRTexLib_MoveTexture(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief     Free a texture.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
*************************************************************************/
PVR_DLL void PVRTexLib_DestroyTexture(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief     Creates a new texture from a file.
			Accepted file formats are: PVR, KTX, KTX2, ASTC, DDS, BASIS,
			PNG, JPEG, BMP, TGA, GIF, HDR, EXR, PSD, PPM, PGM and PIC
 @param[in] filePath  File path to the texture to load from.
 @return	A new texture handle OR NULL on failure.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTexture PVRTexLib_CreateTextureFromFile(const char* filePath);

/*!***********************************************************************
 @brief     Creates a new texture from a pointer that includes a header
			structure, meta data and texture data as laid out in a file.
			This functionality is primarily for user-defined file loading.
			Header may be any version of pvr.
 @param[in]	data Pointer to texture data
 @return	A new texture handle OR NULL on failure.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTexture PVRTexLib_CreateTextureFromData(const void* data);

/*!***********************************************************************
 @brief     Returns a pointer to the texture's data.
			The data offset is calculated using the parameters below.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	MIPLevel Offset to MIP Map levels
 @param[in]	arrayMember Offset to array members
 @param[in]	faceNumber Offset to face numbers
 @param[in]	ZSlice Offset to Z slice (3D textures only)
 @return	Pointer into the texture data OR NULL on failure.
*************************************************************************/
PVR_DLL void* PVRTexLib_GetTextureDataPtr(PVRTexLib_PVRTexture texture,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 faceNumber,
	PVRTuint32 ZSlice);

/*!***********************************************************************
 @brief     Returns a const pointer to the texture's data.
			The data offset is calculated using the parameters below.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	MIPLevel Offset to MIP Map levels
 @param[in]	arrayMember Offset to array members
 @param[in]	faceNumber Offset to face numbers
 @param[in]	ZSlice Offset to Z slice (3D textures only)
 @return	Pointer into the texture data OR NULL on failure.
*************************************************************************/
PVR_DLL const void* PVRTexLib_GetTextureDataConstPtr(
	PVRTexLib_CPVRTexture texture,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 faceNumber,
	PVRTuint32 ZSlice);

/*!***********************************************************************
 @brief     Returns a read only texture header.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	PVRTexLib_PVRTextureHeader handle.
*************************************************************************/
PVR_DLL PVRTexLib_CPVRTextureHeader PVRTexLib_GetTextureHeader(PVRTexLib_CPVRTexture texture);

/*!***********************************************************************
 @brief     Gets a write-able handle to the texture header.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	PVRTexLib_PVRTextureHeader handle.
*************************************************************************/
PVR_DLL PVRTexLib_PVRTextureHeader PVRTexLib_GetTextureHeaderW(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief 	Pads the texture data to a boundary value equal to "padding".
			For example setting padding=8 will align the start of the
			texture data to an 8 byte boundary.     
			NB: This should be called immediately before saving as
			the value is worked out based on the current meta data size.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	padding Padding boundary value
*************************************************************************/
PVR_DLL void PVRTexLib_AddPaddingMetaData(PVRTexLib_PVRTexture texture, PVRTuint32 padding);

/*!***********************************************************************
 @brief		Saves the texture to a given file path. 
			File type will be determined by the extension present in the string.
			Valid extensions are: PVR, KTX, KTX2, ASTC, DDS, BASIS and h
			If no extension is present the PVR format will be selected. 
			Unsupported formats will result in failure.
			ASTC files only support ASTC texture formats.
			BASIS files only support Basis Universal texture formats.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	filepath File path to write to
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_SaveTextureToFile(PVRTexLib_CPVRTexture texture, const char* filePath);

/*!***********************************************************************
 @brief			Similar to PVRTexLib_SaveTextureToFile, but redirects
				the data to a memory buffer instead of a file.
				Caller is responsible for de-allocating memory.
 @param[in]		texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]		fileType File container type to wrap the texture data with.
 @param[in]		privateData Pointer to a user supplied allocation context.
				PVRTexLib will pass this into pfnRealloc when a [re]allocation
				is required.
 @param[out]	outSize Size, in bytes, of the resulting 'file'/data.
 @param[in]		pfnRealloc Callback function to reallocate memory on-demand.
				Return NULL to indicate allocation failure.
 @return		True if the method succeeds.
				N.B This function may allocate even if it fails.
*************************************************************************/
PVR_DLL bool PVRTexLib_SaveTextureToMemory(
	PVRTexLib_CPVRTexture texture,
	PVRTexLibFileContainerType fileType,
	void* privateData,
	PVRTuint64* outSize,
	PVRTuint8*(pfnRealloc)(void* privateData, PVRTuint64 allocSize));

/*!***********************************************************************
 @brief		Writes out a single surface to a given image file.
 @details	File type is determined by the extension present in the filepath string.
			Supported file types are PNG, JPG, BMP, TGA and HDR.
			If no extension is present then the PNG format will be selected.
			Unsupported formats will result in failure.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	filepath Path to write the image file.
 @param[in]	MIPLevel Mip level.
 @param[in]	arrayMember Array index.
 @param[in]	face Face index.
 @param[in]	ZSlice Z index.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_SaveSurfaceToImageFile(
	PVRTexLib_CPVRTexture texture,
	const char* filePath,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 ZSlice);

/*!***********************************************************************
 @brief     Saves the texture to a file, stripping any
			extensions specified and appending .pvr. This function is
			for legacy support only and saves out to PVR Version 2 file.
			The target api must be specified in order to save to this format.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	filepath File path to write to
 @param[in]	api Target API
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_SaveTextureToLegacyPVRFile(PVRTexLib_PVRTexture texture, const char* filePath, PVRTexLibLegacyApi api);

/*!***********************************************************************
 @brief		Queries the texture object to determine if there are multiple
			texture objects associated with the handle. This may be the
			case after loading certain file types such as EXR, since EXR
			files may contain several images/layers with unique pixel formats.
			In these cases PVRTexLib will group all images with the same
			pixel format into a single PVRTexLib_PVRTexture object, where
			each PVRTexLib_PVRTexture can contain multiple array surfaces.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	True if texture contains more than one PVRTexLib_PVRTexture.
*************************************************************************/
PVR_DLL bool PVRTexLib_IsTextureMultiPart(PVRTexLib_CPVRTexture texture);

/*!***********************************************************************
 @brief		 Retrieves (and moves ownership of) the PVRTexLib_PVRTexture handles
			 stored within a multi-part texture and stores them in 'outTextures'.
			 Call this function with 'outTextures' == NULL to populate count
			 and then allocate an appropriately sized array. Handles returned
			 in 'outTextures' should be de-allocated as normal via
			 PVRTexLib_DestroyTexture. After calling this function, subsequent
			 calls to PVRTexLib_IsTextureMultiPart on the same handle will
			 return false.
 @param[in]	 texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 outTextures Array of PVRTexLib_PVRTexture handles to be populated.
			 All returned handles are independent of each other and 'inTexture'.
 @param[out] count The number of parts held by inTexture
*************************************************************************/
PVR_DLL void PVRTexLib_GetTextureParts(PVRTexLib_PVRTexture inTexture, void** outTextures, PVRTuint32 *count);

/*!***********************************************************************
 @brief     Resizes the texture to new specified dimensions.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	newWidth New width
 @param[in]	newHeight New height
 @param[in]	newDepth New depth
 @param[in]	resizeMode Filtering mode
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_ResizeTexture(
	PVRTexLib_PVRTexture texture,
	PVRTuint32 newWidth,
	PVRTuint32 newHeight,
	PVRTuint32 newDepth,
	PVRTexLibResizeMode resizeMode);

/*!***********************************************************************
 @brief		Resizes the canvas of a texture to new specified dimensions.
 			Offset area is filled with transparent black colour.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	u32NewWidth		New width
 @param[in]	u32NewHeight	New height
 @param[in]	u32NewDepth     New depth
 @param[in]	i32XOffset      X Offset value from the top left corner
 @param[in]	i32YOffset      Y Offset value from the top left corner
 @param[in]	i32ZOffset      Z Offset value from the top left corner
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_ResizeTextureCanvas(
	PVRTexLib_PVRTexture texture,
	PVRTuint32 newWidth,
	PVRTuint32 newHeight,
	PVRTuint32 newDepth,
	PVRTint32 xOffset,
	PVRTint32 yOffset,
	PVRTint32 zOffset);

/*!***********************************************************************
 @brief     Rotates a texture by 90 degrees around the given axis.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	rotationAxis   Rotation axis
 @param[in]	forward        Direction of rotation; true = clockwise, false = anti-clockwise
 @return	True if the method succeeds or not.
*************************************************************************/
PVR_DLL bool PVRTexLib_RotateTexture(PVRTexLib_PVRTexture texture, PVRTexLibAxis rotationAxis, bool forward);

/*!***********************************************************************
 @brief     Flips a texture on a given axis.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	flipDirection  Flip direction
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_FlipTexture(PVRTexLib_PVRTexture texture, PVRTexLibAxis flipDirection);

/*!***********************************************************************
 @brief     Adds a user specified border to the texture.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	borderX	X border
 @param[in]	borderY	Y border
 @param[in]	borderZ	Z border
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_BorderTexture(PVRTexLib_PVRTexture texture, PVRTuint32 borderX, PVRTuint32 borderY, PVRTuint32 borderZ);

/*!***********************************************************************
 @brief     Pre-multiplies a texture's colours by its alpha values.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_PreMultiplyAlpha(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief     Allows a texture's colours to run into any fully transparent areas.
 @param[in]	texture A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_Bleed(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief     Sets the specified number of channels to values specified in pValues.
 @param[in]	texture			A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	numChannelSets	Number of channels to set
 @param[in]	channels		Channels to set
 @param[in]	pValues			uint32 values to set channels to
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureChannels(
	PVRTexLib_PVRTexture texture,
	PVRTuint32 numChannelSets,
	const PVRTexLibChannelName* channels,
	const PVRTuint32* pValues);

/*!***********************************************************************
 @brief     Sets the specified number of channels to values specified in float pValues.
 @param[in]	texture			A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	numChannelSets	Number of channels to set
 @param[in]	channels		Channels to set
 @param[in]	pValues			float values to set channels to
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_SetTextureChannelsFloat(
	PVRTexLib_PVRTexture texture,
	PVRTuint32 numChannelSets,
	const PVRTexLibChannelName* channels,
	const float* pValues);

/*!***********************************************************************
 @brief     Copies the specified channels from textureSource
 			into textureDestination. textureSource is not modified so it
 			is possible to use the same texture as both input and output.
 			When using the same texture as source and destination, channels
 			are preserved between swaps e.g. copying Red to Green and then
 			Green to Red will result in the two channels trading places
 			correctly. Channels in eChannels are set to the value of the channels
 			in eChannelSource.
 @param[in]	textureDestination	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	textureSource		A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	uiNumChannelCopies	Number of channels to copy
 @param[in]	destinationChannels	Channels to set
 @param[in]	sourceChannels		Source channels to copy from
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_CopyTextureChannels(
	PVRTexLib_PVRTexture textureDestination,
	PVRTexLib_CPVRTexture textureSource,
	PVRTuint32 numChannelCopies,
	const PVRTexLibChannelName* destinationChannels,
	const PVRTexLibChannelName* sourceChannels);

/*!***********************************************************************
 @brief     Generates a Normal Map from a given height map.
			Assumes the red channel has the height values.
			By default outputs to red/green/blue = x/y/z,
			this can be overridden by specifying a channel
			order in channelOrder. The channels specified
			will output to red/green/blue/alpha in that order.
			So "xyzh" maps x to red, y to green, z to blue
			and h to alpha. 'h' is used to specify that the
			original height map data should be preserved in
			the given channel.
 @param[in]	texture			A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	fScale			Scale factor
 @param[in]	channelOrder	Channel order
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_GenerateNormalMap(PVRTexLib_PVRTexture texture, float fScale, const char* channelOrder);

/*!***********************************************************************
 @brief     Generates MIPMap chain for a texture.
 @param[in]	texture		A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	filterMode	Filter mode
 @param[in]	mipMapsToDo	Number of levels of MIPMap chain to create.
						Use PVRTEX_ALLMIPLEVELS to create a full mip chain.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_GenerateMIPMaps(PVRTexLib_PVRTexture texture, PVRTexLibResizeMode filterMode, PVRTint32 mipMapsToDo);

/*!***********************************************************************
 @brief     Colours a texture's MIPMap levels with different colours
			for debugging purposes. MIP levels are coloured in the
			following repeating pattern: Red, Green, Blue, Cyan,
			Magenta and Yellow
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_ColourMIPMaps(PVRTexLib_PVRTexture texture);

/*!***********************************************************************
 @brief     Transcodes a texture from its original format into the specified format.
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	transcoderOptions struct containing transcoder options.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_TranscodeTexture(PVRTexLib_PVRTexture texture, const PVRTexLib_TranscoderOptions& transcoderOptions);

/*!***********************************************************************
 @brief     A convenience function to decompresses a texture into the most
 			appropriate format based on the textures 'compressed' format,
			for example a PVRTC compressed texture may decompress to RGB888
			or RGBA8888. This function may also be used	to 'decompress'
			packed formats into something easier to manipulate for example
			RGB565 will be decompressed to RGB888.
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	maxThreads The maximum number of threads to use for decompression,
			if set to 0 PVRTexLib will use all available cores.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_Decompress(PVRTexLib_PVRTexture texture, PVRTuint32 maxThreads);

/*!***********************************************************************
 @brief     Creates a cubemap with six faces from an equirectangular
			projected texture. The input must have an aspect ratio of 2:1,
			i.e. the width must be exactly twice the height.
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	filterMode Filtering mode to apply when sampling the source texture.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_EquiRectToCubeMap(PVRTexLib_PVRTexture texture, PVRTexLibResizeMode filter);

/*!***********************************************************************
 @brief     Generates a mipmapped diffuse irradiance texture from a cubemap
			environment map, to be used	primarily with physically based
			rendering (PBR) techniques.
			The input must be a cubemap, the width must equal height,
			and the depth must equal 1.
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	sampleCount The number of samples to use when generating
			the diffuse map.
 @param[in] mapSize Output dimensions, in pixels.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_GenerateDiffuseIrradianceCubeMap(PVRTexLib_PVRTexture texture, PVRTuint32 sampleCount, PVRTuint32 mapSize);

/*!***********************************************************************
 @brief		Generates a prefiltered specular irradiance texture from a
			cubemap environment map, to be used	primarily with physically
			based rendering (PBR) techniques.
			Each Mip level of the specular map is blurred by a roughness
			value between 0 and 1.
			The input must be a cubemap, the width must equal height,
			and the depth must equal 1.
 @param[in]	texture	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	sampleCount The number of samples to use when generating
			the specular map.
 @param[in] mapSize Output dimensions, in pixels.
 @param[in] numMipLevelsToDiscard The number of Mip levels to be discarded
			from the bottom of the Mip chain.
 @param[in] zeroRoughnessIsExternal False to include a roughness of zero
			when generating the prefiltered environment map.
			True to omit a rougness of zero, implying that the user
			will supply roughness zero from the environment texture.
 @return	True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_GeneratePreFilteredSpecularCubeMap(
	PVRTexLib_PVRTexture texture,
	PVRTuint32 sampleCount,
	PVRTuint32 mapSize,
	PVRTuint32 numMipLevelsToDiscard,
	bool zeroRoughnessIsExternal);

/*!***********************************************************************
 @brief		 Computes the maximum difference between two given input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_MaxDifference(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the mean error between two given input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS	A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_MeanError(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the mean squared error (MSE) between two given input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_MeanSquaredError(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the root mean squared error (RMSE) between two given
			 input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_RootMeanSquaredError(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the standard deviation between two given input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_StandardDeviation(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the PSNR between two given input textures.
			 The MIPLevel, arrayMember, face and zSlice values determine which
			 surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
			 should be valid in both input textures. Both textures must have the
			 same dimensions. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 MIPLevel The Mip to compare.
 @param[in]	 arrayMember The array to compare.
 @param[in]	 face The face to compare.
 @param[in]	 zSlice The Z slice to compare.
 @param[out] metrics Structure containing the resulting values.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_PeakSignalToNoiseRatio(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTuint32 MIPLevel,
	PVRTuint32 arrayMember,
	PVRTuint32 face,
	PVRTuint32 zSlice,
	PVRTexLib_ErrorMetrics* metrics);

/*!***********************************************************************
 @brief		 Computes the the [mode] delta per channel between two given
 			 input textures. Both textures must have the same dimensions and
			 may not be compressed. The function will only compare common
			 channels i.e. if 'LHS' has RGB while 'RHS' has RGBA channels,
			 then only the RGB channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[out] textureResult PVRTexLib_PVRTexture that will contain the result on success.
 @param[in]	 multiplier The factor to multiply the deltas to highlight differences,
 			 generally a value between 1 and 10.
 @param[in]	 mode The clamping mode to use, currently supports absolute and signed.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_ColourDiff(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTexLib_PVRTexture* textureResult,
	float multiplier,
	PVRTexLibColourDiffMode mode);

/*!***********************************************************************
 @brief		 Computes the total absolute pixel difference between two given
 			 input textures and modulates the output based on the tolerance
			 value supplied. Deltas of zero will appear black while pixels
			 with deltas greater than or equal to the threshold are set to
			 red and finally deltas less than the tolerance are set to blue.
			 Both textures must have the same dimensions and may not be
			 compressed. The function will only compare common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be compared.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[out] textureResult PVRTexLib_PVRTexture that will contain the result on success.
 @param[in]	 tolerance The cut-off value to compare the pixel delta to.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_ToleranceDiff(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTexLib_PVRTexture* textureResult,
	float tolerance);

/*!***********************************************************************
 @brief		 Blend each channel of the input textures using the blend factor
 			 as a weighting of the first texture against the second.
			 Both textures must have the same dimensions and may not be
			 compressed. The function will only blend common channels i.e.
			 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
			 channels will be blended.
 @param[in]	 textureLHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[in]	 textureRHS A handle to a previously allocated PVRTexLib_PVRTexture.
 @param[out] textureResult PVRTexLib_PVRTexture that will contain the result on success.
 @param[in]	 blendFactor The blend weight to use in the blend equation:
 			 (LHS_delta * BF) + (RHS_delta * (1 - BF)). The value is clamped
			 between 0 and 1.
 @return	 True if the method succeeds.
*************************************************************************/
PVR_DLL bool PVRTexLib_BlendDiff(
	PVRTexLib_CPVRTexture textureLHS,
	PVRTexLib_CPVRTexture textureRHS,
	PVRTexLib_PVRTexture* textureResult,
	float blendFactor);

#ifdef __cplusplus
}
#endif

/*****************************************************************************
End of file (PVRTexLib.h)
*****************************************************************************/