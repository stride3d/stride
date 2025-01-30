/*!***********************************************************************
 @file         PVRTexLib.hpp
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        C++ wrapper around PVRTexLib's C interface.
*************************************************************************/
#pragma once

#include "PVRTexLib.h"
#include <string>
#include <memory>
#include <vector>
#include <limits>
#include <cassert>
#include <stdexcept>

namespace pvrtexlib
{
	struct MetaDataBlock
	{
		PVRTuint32	DevFOURCC;				///< A 4cc descriptor of the data type's creator. Values equating to values between 'P' 'V' 'R' 0 and 'P' 'V' 'R' 255 will be used by our headers.
		PVRTuint32	u32Key;					///< A DWORD (enum value) identifying the data type, and thus how to read it.
		PVRTuint32	u32DataSize;			///< Size of the Data member.
		std::unique_ptr<PVRTuint8[]> Data;	///< Meta data bytes

		MetaDataBlock()
			: DevFOURCC(PVRTEX_CURR_IDENT)
			, u32Key()
			, u32DataSize()
			, Data()
		{}
	};

	/*!***********************************************************************
 	@brief     Texture header methods.
 	@details   Setters & getters for pixel type, channel type and colour space.
	           Size retrieval and dimension manipulation. Setters & getters for 
               texture meta data.
	*************************************************************************/
	class PVRTextureHeader
	{
	public:
		/*!***********************************************************************
		 @brief     Creates a new texture header with default parameters.
		 @param[in]	params PVRHeader_CreateParams
		 @return	A new PVRTextureHeader
		*************************************************************************/
		inline PVRTextureHeader();

		/*!***********************************************************************
		 @brief     Creates a new texture header using the supplied parameters.
		 @param[in]	params PVRHeader_CreateParams
		 @return	A new PVRTextureHeader
		*************************************************************************/
		inline PVRTextureHeader(const PVRHeader_CreateParams* params);

		/*!***********************************************************************
		 @brief     Creates a new texture header using the supplied parameters.
		 @param[in]	pixelFormat texture format
		 @param[in]	width texture width in pixels
		 @param[in]	height texture height in pixels
		 @param[in]	depth texture depth
		 @param[in]	numMipMaps number of MIP map levels
		 @param[in]	numArrayMembers number of array members
		 @param[in]	numFaces number of faces
		 @param[in]	colourSpace colour space
		 @param[in]	channelType channel type
		 @param[in]	preMultiplied texture's colour has been pre-multiplied by the alpha values?
		 @return	A new PVRTextureHeader
		*************************************************************************/
		inline PVRTextureHeader(
			PVRTuint64				pixelFormat,
			PVRTuint32				width,
			PVRTuint32				height,
			PVRTuint32				depth = 1U,
			PVRTuint32				numMipMaps = 1U,
			PVRTuint32				numArrayMembers = 1U,
			PVRTuint32				numFaces = 1U,
			PVRTexLibColourSpace	colourSpace = PVRTexLibColourSpace::PVRTLCS_sRGB,
			PVRTexLibVariableType	channelType = PVRTexLibVariableType::PVRTLVT_UnsignedByteNorm,
			bool					preMultiplied = false);

		/*!***********************************************************************
		 @brief		Creates a new texture header from a PVRTextureHeader
		 @param[in]	rhs Texture header to copy
		 @return	A new PVRTextureHeader
		*************************************************************************/
		inline PVRTextureHeader(const PVRTextureHeader& rhs);

		/*!***********************************************************************
		 @brief     Creates a new texture, moving the contents of the
					supplied texture into the new texture.
		 @param[in]	texture A PVRTextureHeader to move from.
		 @return	A new PVRTextureHeader.
		*************************************************************************/
		inline PVRTextureHeader(PVRTextureHeader&& rhs) noexcept;

		/*!***********************************************************************
		 @brief     Copies the contents of another texture header into this one.
		 @param[in]	rhs PVRTextureHeader to copy
		 @return	This texture header.
		*************************************************************************/
		inline PVRTextureHeader& operator=(const PVRTextureHeader& rhs);

		/*!***********************************************************************
		 @brief     Moves ownership of texture header data to this object.
		 @param[in]	rhs PVRTextureHeader to move
		 @return	This texture header.
		*************************************************************************/
		inline PVRTextureHeader& operator=(PVRTextureHeader&& rhs) noexcept;

		/*!***********************************************************************
		 @brief     Deconstructor for PVRTextureHeader.
		*************************************************************************/
		inline virtual ~PVRTextureHeader();

		/*!***********************************************************************
		 @brief		Gets the number of bits per pixel for this texture header.
		 @return	Number of bits per pixel.
		*************************************************************************/
		inline PVRTuint32 GetTextureBitsPerPixel() const;

		/*!***********************************************************************
		 @brief		Gets the number of bits per pixel for the specified pixel format.
		 @param		u64PixelFormat A PVR pixel format ID.
		 @return	Number of bits per pixel.
		*************************************************************************/
		static inline PVRTuint32 GetTextureBitsPerPixel(PVRTuint64 u64PixelFormat);

		/*!***********************************************************************
		 @brief		Gets the number of channels for this texture header.
		 @return	For uncompressed formats the number of channels between 1 and 4.
					For compressed formats 0
		*************************************************************************/
		inline PVRTuint32 GetTextureChannelCount() const;

		/*!***********************************************************************
		 @brief		Gets the channel type for this texture header.
		 @return	PVRTexLibVariableType enum.
		*************************************************************************/
		inline PVRTexLibVariableType GetTextureChannelType() const;

		/*!***********************************************************************
		 @brief		Gets the colour space for this texture header.
		 @return	PVRTexLibColourSpace enum.
		*************************************************************************/
		inline PVRTexLibColourSpace GetColourSpace() const;

		/*!***********************************************************************
		 @brief     Gets the width of the user specified MIP-Map level for the
					texture
		 @param[in]	uiMipLevel MIP level that user is interested in.
		 @return    Width of the specified MIP-Map level.
		*************************************************************************/
		inline PVRTuint32 GetTextureWidth(PVRTuint32 mipLevel = 0U) const;

		/*!***********************************************************************
		 @brief     Gets the height of the user specified MIP-Map
					level for the texture
		 @param[in]	uiMipLevel MIP level that user is interested in.
		 @return	Height of the specified MIP-Map level.
		*************************************************************************/
		inline PVRTuint32 GetTextureHeight(PVRTuint32 mipLevel = 0U) const;

		/*!***********************************************************************
		 @brief     Gets the depth of the user specified MIP-Map
					level for the texture
		 @param[in]	uiMipLevel MIP level that user is interested in.
		 @return	Depth of the specified MIP-Map level.
		*************************************************************************/
		inline PVRTuint32 GetTextureDepth(PVRTuint32 mipLevel = 0U) const;

		/*!***********************************************************************
		 @brief     Gets the size in PIXELS of the texture, given various input
					parameters.	User can retrieve the total size of either all
					surfaces or a single surface, all faces or a single face and
					all MIP-Maps or a single specified MIP level.
		 @param[in]	iMipLevel		Specifies a MIP level to check,
									'PVRTEX_ALLMIPLEVELS' can be passed to get
									the size of all MIP levels.
		 @param[in]	bAllSurfaces	Size of all surfaces is calculated if true,
									only a single surface if false.
		 @param[in]	bAllFaces		Size of all faces is calculated if true,
									only a single face if false.
		 @return	Size in PIXELS of the specified texture area.
		*************************************************************************/
		inline PVRTuint32 GetTextureSize(PVRTint32 mipLevel = PVRTEX_ALLMIPLEVELS, bool allSurfaces = true, bool allFaces = true) const;

		/*!***********************************************************************
		 @brief		Gets the size in BYTES of the texture, given various input
					parameters.	User can retrieve the size of either all
					surfaces or a single surface, all faces or a single face
					and all MIP-Maps or a single specified MIP level.
		 @param[in]	iMipLevel		Specifies a mip level to check,
									'PVRTEX_ALLMIPLEVELS' can be passed to get
									the size of all MIP levels.
		 @param[in]	bAllSurfaces	Size of all surfaces is calculated if true,
									only a single surface if false.
		 @param[in]	bAllFaces		Size of all faces is calculated if true,
									only a single face if false.
		 @return	Size in BYTES of the specified texture area.
		*************************************************************************/
		inline PVRTuint64 GetTextureDataSize(PVRTint32 mipLevel = PVRTEX_ALLMIPLEVELS, bool allSurfaces = true, bool allFaces = true) const;

		/*!***********************************************************************
		 @brief      	Gets the data orientation for this texture.
		 @param[in,out]	result Pointer to a PVRTexLib_Orientation structure.
		*************************************************************************/
		inline void GetTextureOrientation(PVRTexLib_Orientation& result) const;

		/*!***********************************************************************
		 @brief      	Gets the OpenGL equivalent format for this texture.
		 @param[in,out]	result Pointer to a PVRTexLib_OpenGLFormat structure.
		*************************************************************************/
		inline void GetTextureOpenGLFormat(PVRTexLib_OpenGLFormat& result) const;

		/*!***********************************************************************
		 @brief      	Gets the OpenGLES equivalent format for this texture.
		 @param[in,out]	result Pointer to a PVRTexLib_OpenGLESFormat structure.
		*************************************************************************/
		inline void GetTextureOpenGLESFormat(PVRTexLib_OpenGLESFormat& result) const;

		/*!***********************************************************************
		 @brief     Gets the Vulkan equivalent format for this texture.
		 @return	A VkFormat enum value.
		*************************************************************************/
		inline PVRTuint32 GetTextureVulkanFormat() const;

		/*!***********************************************************************
		 @brief     Gets the Direct3D equivalent format for this texture.
		 @return	A D3DFORMAT enum value.
		*************************************************************************/
		inline PVRTuint32 GetTextureD3DFormat() const;

		/*!***********************************************************************
		 @brief     Gets the DXGI equivalent format for this texture.
		 @return	A DXGI_FORMAT enum value.
		*************************************************************************/
		inline PVRTuint32 GetTextureDXGIFormat() const;

		/*!***********************************************************************
		 @brief 				Gets the minimum dimensions (x,y,z)
								for the textures pixel format.
		 @param[in,out]	minX	Returns the minimum width.
		 @param[in,out]	minY	Returns the minimum height.
		 @param[in,out]	minZ	Returns the minimum depth.
		*************************************************************************/
		inline void GetTextureFormatMinDims(PVRTuint32& minX, PVRTuint32& minY, PVRTuint32& minZ) const;

		/*!***********************************************************************
		 @brief 		Gets the minimum dimensions (x,y,z)
						for a given pixel format.
		 @param[in]		u64PixelFormat A PVR Pixel Format ID.
		 @param[in,out]	minX Returns the minimum width.
		 @param[in,out]	minY Returns the minimum height.
		 @param[in,out]	minZ Returns the minimum depth.
		*************************************************************************/
		static inline void GetPixelFormatMinDims(PVRTuint64 ui64Format, PVRTuint32& minX, PVRTuint32& minY, PVRTuint32& minZ);

		/*!***********************************************************************
		 @brief		Returns the total size of the meta data stored in the header.
					This includes the size of all information stored in all MetaDataBlocks.
		 @return	Size, in bytes, of the meta data stored in the header.
		*************************************************************************/
		inline PVRTuint32 GetTextureMetaDataSize() const;

		/*!***********************************************************************
		 @brief		Returns whether or not the texture's colour has been
					pre-multiplied by the alpha values.
		 @return	True if texture is premultiplied.
		*************************************************************************/
		inline bool GetTextureIsPreMultiplied() const;

		/*!***********************************************************************
		 @brief		Returns whether or not the texture is compressed using
					PVRTexLib's FILE compression - this is independent of
					any texture compression.
		 @return	True if it is file compressed.
		*************************************************************************/
		inline bool GetTextureIsFileCompressed() const;

		/*!***********************************************************************
		 @brief		Returns whether or not the texture is a bump map.
		 @return	True if it is a bump map.
		*************************************************************************/
		inline bool GetTextureIsBumpMap() const;

		/*!***********************************************************************
		 @brief     Gets the bump map scaling value for this texture.
					If the texture is not a bump map, 0.0f is returned. If the
					texture is a bump map but no meta data is stored to
					specify its scale, then 1.0f is returned.
		 @return	Returns the bump map scale value as a float.
		*************************************************************************/
		inline float GetTextureBumpMapScale() const;

		/*!***********************************************************************
		 @brief     Works out the number of possible texture atlas members in
					the texture based on the width, height, depth and data size.
		 @return	The number of sub textures defined by meta data.
		*************************************************************************/
		inline PVRTuint32 GetNumTextureAtlasMembers() const;

		/*!***********************************************************************
		 @brief			Returns a pointer to the texture atlas data.
		 @param[in,out]	count Number of floats in the returned data set.
		 @return		A pointer directly to the texture atlas data. NULL if
						the texture does not have atlas data.
		*************************************************************************/
		inline const float* GetTextureAtlasData(PVRTuint32& count) const;

		/*!***********************************************************************
		 @brief     Gets the number of MIP-Map levels stored in this texture.
		 @return	Number of MIP-Map levels in this texture.
		*************************************************************************/
		inline PVRTuint32 GetTextureNumMipMapLevels() const;

		/*!***********************************************************************
		 @brief     Gets the number of faces stored in this texture.
		 @return	Number of faces in this texture.
		*************************************************************************/
		inline PVRTuint32 GetTextureNumFaces() const;

		/*!***********************************************************************
		 @brief     Gets the number of array members stored in this texture.
		 @return	Number of array members in this texture.
		*************************************************************************/
		inline PVRTuint32 GetTextureNumArrayMembers() const;

		/*!***********************************************************************
		 @brief		Gets the cube map face order.
					cubeOrder string will be in the form "ZzXxYy" with capitals
					representing positive and lower case letters representing
					negative. I.e. Z=Z-Positive, z=Z-Negative.
		 @return	Null terminated cube map order string.
		*************************************************************************/
		inline std::string GetTextureCubeMapOrder() const;

		/*!***********************************************************************
		 @brief     Gets the bump map channel order relative to RGBA.
					For	example, an RGB texture with bumps mapped to XYZ returns
					'xyz'. A BGR texture with bumps in the order ZYX will also
					return 'xyz' as the mapping is the same: R=X, G=Y, B=Z.
					If the letter 'h' is present in the string, it means that
					the height map has been stored here.
					Other characters are possible if the bump map was created
					manually, but PVRTexLib will ignore these characters. They
					are returned simply for completeness.
		 @return	Null terminated bump map order string relative to RGBA.
		*************************************************************************/
		inline std::string GetTextureBumpMapOrder() const;

		/*!***********************************************************************
		 @brief     Gets the 64-bit pixel type ID of the texture.
		 @return	64-bit pixel type ID.
		*************************************************************************/
		inline PVRTuint64 GetTexturePixelFormat() const;

		/*!***********************************************************************
		 @brief     Checks whether this textures pixel format is packed.
					E.g. R5G6B5, R11G11B10, R4G4B4A4 etc.
		 @return	True if the texture format is packed, false otherwise.
		*************************************************************************/
		inline bool TextureHasPackedChannelData() const;

		/*!***********************************************************************
		 @brief     Checks whether this textures pixel format is compressed.
					E.g. PVRTC, ETC, ASTC etc.
		 @return	True if the texture format is compressed, false otherwise.
		*************************************************************************/
		inline bool IsPixelFormatCompressed() const;

		/*!***********************************************************************
		 @brief		Sets the variable type for the channels in this texture.
		 @param[in]	type A PVRTexLibVariableType enum.
		*************************************************************************/
		inline void SetTextureChannelType(PVRTexLibVariableType type);

		/*!***********************************************************************
		 @brief     Sets the colour space for this texture.
		 @param[in]	colourSpace	A PVRTexLibColourSpace enum.
		*************************************************************************/
		inline void SetTextureColourSpace(PVRTexLibColourSpace colourSpace);

		/*!***********************************************************************
		 @brief     Sets the format of the texture to PVRTexLib's internal
					representation of the D3D format.
		 @param[in] d3dFormat A D3DFORMAT enum.
		 @return	True if successful.
		*************************************************************************/
		inline bool SetTextureD3DFormat(PVRTuint32 d3dFormat);

		/*!***********************************************************************
		 @brief     Sets the format of the texture to PVRTexLib's internal
					representation of the DXGI format.
		 @param[in]	dxgiFormat A DXGI_FORMAT enum.
		 @return	True if successful.
		*************************************************************************/
		inline bool SetTextureDXGIFormat(PVRTuint32 dxgiFormat);

		/*!***********************************************************************
		 @brief		Sets the format of the texture to PVRTexLib's internal
					representation of the OpenGL format.
		 @param[in]	oglFormat The OpenGL format.
		 @return	True if successful.
		*************************************************************************/
		inline bool SetTextureOGLFormat(const PVRTexLib_OpenGLFormat& oglFormat);

		/*!***********************************************************************
		 @brief		Sets the format of the texture to PVRTexLib's internal
					representation of the OpenGLES format.
		 @param[in]	oglesFormat The OpenGLES format.
		 @return	True if successful.
		*************************************************************************/
		inline bool SetTextureOGLESFormat(const PVRTexLib_OpenGLESFormat& oglesFormat);

		/*!***********************************************************************
		 @brief		Sets the format of the texture to PVRTexLib's internal
					representation of the Vulkan format.
		 @param[in]	vulkanFormat A VkFormat enum.
		 @return	True if successful.
		*************************************************************************/
		inline bool SetTextureVulkanFormat(PVRTuint32 vulkanFormat);

		/*!***********************************************************************
		 @brief     Sets the pixel format for this texture.
		 @param[in]	format	The format of the pixel.
		*************************************************************************/
		inline void SetTexturePixelFormat(PVRTuint64 format);

		/*!***********************************************************************
		 @brief		Sets the texture width.
		 @param[in]	width The new width.
		*************************************************************************/
		inline void SetTextureWidth(PVRTuint32 width);

		/*!***********************************************************************
		 @brief		Sets the texture height.
		 @param[in]	height The new height.
		*************************************************************************/
		inline void SetTextureHeight(PVRTuint32 height);

		/*!***********************************************************************
		 @brief		Sets the texture depth.
		 @param[in]	depth The new depth.
		*************************************************************************/
		inline void SetTextureDepth(PVRTuint32 depth);

		/*!***********************************************************************
		 @brief		Sets the number of array members in this texture.
		 @param[in]	newNumMembers The new number of array members.
		*************************************************************************/
		inline void SetTextureNumArrayMembers(PVRTuint32 numMembers);

		/*!***********************************************************************
		 @brief		Sets the number of MIP-Map levels in this texture.
		 @param[in]	numMIPLevels New number of MIP-Map levels.
		*************************************************************************/
		inline void SetTextureNumMIPLevels(PVRTuint32 numMIPLevels);

		/*!***********************************************************************
		 @brief		Sets the number of faces stored in this texture.
		 @param[in]	numFaces New number of faces for this texture.
		*************************************************************************/
		inline void SetTextureNumFaces(PVRTuint32 numFaces);

		/*!***********************************************************************
		 @brief     Sets the data orientation for a given axis in this texture.
		 @param[in]	orientation Pointer to a PVRTexLib_Orientation structure.
		*************************************************************************/
		inline void SetTextureOrientation(const PVRTexLib_Orientation& orientation);

		/*!***********************************************************************
		 @brief		Sets whether or not the texture is compressed using
					PVRTexLib's FILE compression - this is independent of
					any texture compression. Currently unsupported.
		 @param[in]	isFileCompressed Sets the file compression to true or false.
		*************************************************************************/
		inline void SetTextureIsFileCompressed(bool isFileCompressed);

		/*!***********************************************************************
		 @brief     Sets whether or not the texture's colour has been
					pre-multiplied by the alpha values.
		 @param[in] isPreMultiplied	Sets if texture is premultiplied.
		*************************************************************************/
		inline void SetTextureIsPreMultiplied(bool isPreMultiplied);

		/*!***********************************************************************
		 @brief			Obtains the border size in each dimension for this texture.
		 @param[in,out]	borderWidth   Border width
		 @param[in,out]	borderHeight  Border height
		 @param[in,out]	borderDepth   Border depth
		*************************************************************************/
		inline void GetTextureBorder(PVRTuint32& borderWidth, PVRTuint32& borderHeight, PVRTuint32& borderDepth) const;

		/*!***********************************************************************
		 @brief			Returns a copy of a block of meta data from the texture.
						If the meta data doesn't exist, a block with a data size
						of 0 will be returned.
		 @param[in]		key Value representing the type of meta data stored
		 @param[in,out] dataBlock returned meta block data
		 @param[in]		devFOURCC Four character descriptor representing the
						creator of the meta data
		 @return		True if the meta data block was found. False otherwise.
		*************************************************************************/
		inline bool GetMetaDataBlock(PVRTuint32 key, MetaDataBlock& dataBlock, PVRTuint32 devFOURCC = PVRTEX_CURR_IDENT) const;

		/*!***********************************************************************
		 @brief     Returns whether or not the specified meta data exists as
					part of this texture header.
		 @param[in]	u32Key Key value representing the type of meta data stored
		 @param[in]	DevFOURCC Four character descriptor representing the
							  creator of the meta data
		 @return	True if the specified meta data bock exists
		*************************************************************************/
		inline bool TextureHasMetaData(PVRTuint32 key, PVRTuint32 devFOURCC = PVRTEX_CURR_IDENT) const;

		/*!***********************************************************************
		 @brief     Sets a texture's bump map data.
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
		inline void SetTextureBumpMap(float bumpScale, const std::string& bumpOrder);

		/*!***********************************************************************
		 @brief		Sets the texture atlas coordinate meta data for later display.
					It is up to the user to make sure that this texture atlas
					data actually makes sense in the context of the header.
		 @param[in]	atlasData Pointer to an array of atlas data.
		 @param[in]	dataSize Number of floats in atlasData.
		*************************************************************************/
		inline void SetTextureAtlas(const float* atlasData, PVRTuint32 dataSize);

		/*!***********************************************************************
		 @brief     Sets the texture's face ordering.
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
		inline void SetTextureCubeMapOrder(const std::string& cubeMapOrder);

		/*!***********************************************************************
		 @brief     Sets a texture's border size data. This value is subtracted
					from the current texture height/width/depth to get the valid
					texture data.
		 @param[in]	borderWidth   Border width
		 @param[in]	borderHeight  Border height
		 @param[in]	borderDepth   Border depth
		*************************************************************************/
		inline void SetTextureBorder(PVRTuint32 borderWidth, PVRTuint32 borderHeight, PVRTuint32 borderDepth);

		/*!***********************************************************************
		 @brief     Adds an arbitrary piece of meta data.
		 @param[in]	dataBlock Meta data block to be added.
		*************************************************************************/
		inline void AddMetaData(const MetaDataBlock& dataBlock);

		/*!***********************************************************************
		 @brief     Adds an arbitrary piece of meta data.
		 @param[in]	dataBlock Meta data block to be added.
		*************************************************************************/
		inline void AddMetaData(const PVRTexLib_MetaDataBlock& dataBlock);

		/*!***********************************************************************
		 @brief     Removes a specified piece of meta data, if it exists.
		 @param[in] u32Key Key value representing the type of meta data stored.
		 @param[in]	DevFOURCC Four character descriptor representing the
							  creator of the meta data
		*************************************************************************/
		inline void RemoveMetaData(PVRTuint32 key, PVRTuint32 devFOURCC = PVRTEX_CURR_IDENT);

	protected:
		inline PVRTextureHeader(bool);
		static inline PVRTexLib_CPVRTextureHeader GetHeader(const PVRTextureHeader& header);
		PVRTexLib_PVRTextureHeader m_hTextureHeader;
	};

	/*!***********************************************************************
 	@brief     Texture loading, saving and manipulation.
 	@details   Methods to load and save texture data to various container
               and image file formats such as; PVR, KTX, Basis, PNG, JPG and HDR.
			   Methods for image manipulation operations such as; compressing,
			   transcoding, resizing and generating MIP chains & normal maps etc.
			   Also allows for direct data access.
	*************************************************************************/
	class PVRTexture : public PVRTextureHeader
	{
	public:
		/*!***********************************************************************
		 @brief     Default constructor.
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture();

		/*!***********************************************************************
		 @brief     Creates a new texture based on a texture header,
					and optionally copies the supplied texture data.
		 @param[in]	header A PVRTextureHeader.
		 @param[in]	data Texture data (may be NULL)
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture(const PVRTextureHeader& header, const void *textureData);

		/*!***********************************************************************
		 @brief     Creates a new texture from a file.
					Accepted file formats are: PVR, KTX, KTX2, ASTC, DDS, BASIS,
					PNG, JPEG, BMP, TGA, GIF, HDR, EXR, PSD, PPM, PGM and PIC
		 @param[in] filePath  File path to a texture to load from.
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture(const std::string& filePath);

		/*!***********************************************************************
		 @brief     Creates a new texture from a file.
					Accepted file formats are: PVR, KTX, KTX2, ASTC, DDS, BASIS,
					PNG, JPEG, BMP, TGA, GIF, HDR, EXR, PSD, PPM, PGM and PIC
		 @param[in] filePath  File path to a texture to load from.
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture(const char* filePath);

		/*!***********************************************************************
		 @brief     Creates a new texture from a pointer that includes a header
					structure, meta data and texture data as laid out in a file.
					This functionality is primarily for user-defined file loading.
					Header may be any version of PVR.
		 @param[in]	data Pointer to texture data
		 @return	A new texture object.
		*************************************************************************/
		inline explicit PVRTexture(const void* data);

		/*!***********************************************************************
		 @brief		Creates a copy of the supplied texture.
		 @param[in]	texture A PVRTexture to copy from.
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture(const PVRTexture& rhs);

		/*!***********************************************************************
		 @brief		Creates a new texture, moving the contents of the
					supplied texture into the new texture.
		 @param[in]	texture A PVRTexture to move from.
		 @return	A new texture object.
		*************************************************************************/
		inline PVRTexture(PVRTexture&& rhs) noexcept;

		/*!***********************************************************************
		 @brief		Copies the contents of another texture into this one.
		 @param[in]	rhs Texture to copy
		 @return	This texture.
		*************************************************************************/
		inline PVRTexture& operator=(const PVRTexture& rhs);

		/*!***********************************************************************
		 @brief		Moves ownership of texture data to this object.
		 @param[in]	rhs Texture to move
		 @return	This texture.
		*************************************************************************/
		inline PVRTexture& operator=(PVRTexture&& rhs) noexcept;

		/*!***********************************************************************
		 @brief     Deconstructor for PVRTexture.
		*************************************************************************/
		inline ~PVRTexture();

		/*!***********************************************************************
		 @brief     Returns a pointer to the texture's data.
					The data offset is calculated using the parameters below.
		 @param[in]	MIPLevel Offset to MIP Map levels
		 @param[in]	arrayMember Offset to array members
		 @param[in]	faceNumber Offset to face numbers
		 @param[in]	ZSlice Offset to Z slice (3D textures only)
		 @return	Pointer into the texture data OR NULL on failure.
		*************************************************************************/
		inline void* GetTextureDataPointer(
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 faceNumber = 0U,
			PVRTuint32 ZSlice = 0U);

		/*!***********************************************************************
		 @brief     Returns a constant pointer to the texture's data.
					The data offset is calculated using the parameters below.
		 @param[in]	MIPLevel Offset to MIP Map levels
		 @param[in]	arrayMember Offset to array members
		 @param[in]	faceNumber Offset to face numbers
		 @param[in]	ZSlice Offset to Z slice (3D textures only)
		 @return	Pointer into the texture data OR NULL on failure.
		*************************************************************************/
		inline const void* GetTextureDataPointer(
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 faceNumber = 0U,
			PVRTuint32 ZSlice = 0U) const;

		/*!***********************************************************************
		 @brief 	Pads the texture data to a boundary value equal to "padding".
					For example setting padding=8 will align the start of the
					texture data to an 8 byte boundary.
					NB: This should be called immediately before saving as
					the value is worked out based on the current meta data size.
		 @param[in]	padding Padding boundary value
		*************************************************************************/
		inline void AddPaddingMetaData(PVRTuint32 padding);

		/*!***********************************************************************
		 @brief     Saves the texture to a given file path.
					File type will be determined by the extension present in the string.
					Valid extensions are: PVR, KTX, KTX2, ASTC, DDS, BASIS and h
					If no extension is present the PVR format will be selected.
					Unsupported formats will result in failure.
					ASTC files only support ASTC texture formats.
					BASIS files only support Basis Universal texture formats.
		 @param[in]	filepath File path to write to
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool SaveToFile(const std::string& filePath) const;

		/*!***********************************************************************
		 @brief     Saves the texture to a file, stripping any
					extensions specified and appending .pvr. This function is
					for legacy support only and saves out to PVR Version 2 file.
					The target api must be specified in order to save to this format.
		 @param[in]	filepath File path to write to
		 @param[in]	api Target API
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool SaveToFile(const std::string& filePath, PVRTexLibLegacyApi api) const;

		/*!***********************************************************************
		@brief		Similar to SaveToFile, but redirects the data to a memory
					buffer instead of a file.
					The caller is responsible for de-allocating memory.
		@param[in]	fileType File container type to wrap the texture data with.
		@param[in]	privateData Pointer to a user supplied allocation context.
					PVRTexLib will pass this into pfnRealloc when a [re]allocation
					is required.
		@param[out]	outSize Size, in bytes, of the resulting 'file'/data.
		@param[in]	pfnRealloc Callback function to reallocate memory on-demand.
					Return NULL to indicate allocation failure.
		@return		True if the method succeeds.
					N.B This function may allocate even if it fails.
		*************************************************************************/
		inline bool SaveTextureToMemory(
			PVRTexLibFileContainerType fileType,
			void* privateData,
			PVRTuint64& outSize,
			PVRTuint8*(pfnRealloc)(void* privateData, PVRTuint64 allocSize)) const;

		/*!***********************************************************************
		@brief			Similar to SaveToFile, but redirects
						the data to a memory buffer instead of a file.
		@param[in]		fileType File container type to wrap the texture data with.
		@param[in,out]	outData std::vector containing the resulting data.
		@return			True if the method succeeds.
		*************************************************************************/
		inline bool SaveTextureToMemory(
			PVRTexLibFileContainerType fileType,
			std::vector<PVRTuint8>& outData) const;

		/*!***********************************************************************
		@brief		Similar to SaveToFile, but redirects the data to a memory
					buffer instead of a file.
		@param[in]	fileType File container type to wrap the texture data with.
		@param[out]	outSize Size, in bytes, of the resulting 'file'/data.
		@return		Managed pointer to the resulting data or nullptr on failure.
		*************************************************************************/
		inline std::unique_ptr<PVRTuint8, void(*)(PVRTuint8*)> SaveTextureToMemory(
			PVRTexLibFileContainerType fileType,
			PVRTuint64& outSize) const;

		/*!***********************************************************************
		@brief		Writes out a single surface to a given image file.
		@details	File type is determined by the extension present in the filepath string.
					Supported file types are PNG, JPG, BMP, TGA and HDR.
					If no extension is present then the PNG format will be selected.
					Unsupported formats will result in failure.
		@param[in]	filepath Path to write the image file.
		@param[in]	MIPLevel Mip level.
		@param[in]	arrayMember Array index.
		@param[in]	face Face index.
		@param[in]	ZSlice Z index.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool SaveSurfaceToImageFile(
			const std::string& filePath,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 ZSlice = 0U) const;

		/*!***********************************************************************
		 @brief		Queries the texture object to determine if there are multiple
		 			texture objects associated with it. This may be the case after
					loading certain file types such as EXR, since EXR files may
					contain several images/layers with unique pixel formats.
		 			In these cases PVRTexLib will group all images with the same
		 			pixel format into a single PVRTexLib_PVRTexture object, where
		 			each PVRTexLib_PVRTexture can contain multiple array surfaces.
		 @return	True if this texture contains more than one PVRTexLib_PVRTexture.
		*************************************************************************/
		inline bool IsTextureMultiPart() const;

		/*!***********************************************************************
		@brief		Retrieves (and moves ownership of) all PVRTexLib_PVRTexture handles
					associated with this texture object. After calling this function
					any subsequent calls on this texture to IsTextureMultiPart
					will return false.
		@return		Vector of PVRTexture objects. All returned textures are
					independent of each other and this texture.
		*************************************************************************/
		inline std::vector<PVRTexture> GetTextureParts();

		/*!***********************************************************************
		 @brief		Resizes the texture to new specified dimensions.
		 @param[in]	newWidth New width
		 @param[in]	newHeight New height
		 @param[in]	newDepth New depth
		 @param[in]	resizeMode Filtering mode
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Resize(
			PVRTuint32 newWidth,
			PVRTuint32 newHeight,
			PVRTuint32 newDepth,
			PVRTexLibResizeMode resizeMode);

		/*!***********************************************************************
		 @brief		Resizes the canvas of a texture to new specified dimensions.
		 			Offset area is filled with transparent black colour.
		 @param[in]	u32NewWidth		New width
		 @param[in]	u32NewHeight	New height
		 @param[in]	u32NewDepth     New depth
		 @param[in]	i32XOffset      X Offset value from the top left corner
		 @param[in]	i32YOffset      Y Offset value from the top left corner
		 @param[in]	i32ZOffset      Z Offset value from the top left corner
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool ResizeCanvas(
			PVRTuint32 newWidth,
			PVRTuint32 newHeight,
			PVRTuint32 newDepth,
			PVRTint32 xOffset,
			PVRTint32 yOffset,
			PVRTint32 zOffset);

		/*!***********************************************************************
		 @brief		Rotates a texture by 90 degrees around the given axis.
		 @param[in]	rotationAxis   Rotation axis
		 @param[in]	forward        Direction of rotation; true = clockwise, false = anti-clockwise
		 @return	True if the method succeeds or not.
		*************************************************************************/
		inline bool Rotate(PVRTexLibAxis rotationAxis, bool forward);

		/*!***********************************************************************
		 @brief		Flips a texture on a given axis.
		 @param[in]	flipDirection  Flip direction
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Flip(PVRTexLibAxis flipDirection);

		/*!***********************************************************************
		 @brief		Adds a user specified border to the texture.
		 @param[in]	borderX	X border
		 @param[in]	borderY	Y border
		 @param[in]	borderZ	Z border
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Border(PVRTuint32 borderX, PVRTuint32 borderY, PVRTuint32 borderZ);

		/*!***********************************************************************
		 @brief		Pre-multiplies a texture's colours by its alpha values.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool PreMultiplyAlpha();

		/*!***********************************************************************
		 @brief		Allows a texture's colours to run into any fully transparent areas.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Bleed();

		/*!***********************************************************************
		 @brief		Sets the specified number of channels to values specified in pValues.
		 @param[in]	numChannelSets	Number of channels to set
		 @param[in]	channels		Channels to set
		 @param[in]	pValues			uint32 values to set channels to
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool SetChannels(
			PVRTuint32 numChannelSets,
			const PVRTexLibChannelName* channels,
			const PVRTuint32* pValues);

		/*!***********************************************************************
		 @brief		Sets the specified number of channels to values specified in float pValues.
		 @param[in]	numChannelSets	Number of channels to set
		 @param[in]	channels		Channels to set
		 @param[in]	pValues			float values to set channels to
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool SetChannels(
			PVRTuint32 numChannelSets,
			const PVRTexLibChannelName* channels,
			const float* pValues);

		/*!***********************************************************************
		 @brief		Copies the specified channels from textureSource
		 			into textureDestination. textureSource is not modified so it
		 			is possible to use the same texture as both input and output.
		 			When using the same texture as source and destination, channels
		 			are preserved between swaps e.g. copying Red to Green and then
		 			Green to Red will result in the two channels trading places
		 			correctly. Channels in eChannels are set to the value of the channels
		 			in eChannelSource.
		 @param[in]	sourceTexture		A PVRTexture to copy channels from.
		 @param[in]	uiNumChannelCopies	Number of channels to copy
		 @param[in]	destinationChannels	Channels to set
		 @param[in]	sourceChannels		Source channels to copy from
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool CopyChannels(
			const PVRTexture& sourceTexture,
			PVRTuint32 numChannelCopies,
			const PVRTexLibChannelName* destinationChannels,
			const PVRTexLibChannelName* sourceChannels);

		/*!***********************************************************************
		 @brief		Generates a Normal Map from a given height map.
					Assumes the red channel has the height values.
					By default outputs to red/green/blue = x/y/z,
					this can be overridden by specifying a channel
					order in channelOrder. The channels specified
					will output to red/green/blue/alpha in that order.
					So "xyzh" maps x to red, y to green, z to blue
					and h to alpha. 'h' is used to specify that the
					original height map data should be preserved in
					the given channel.
		 @param[in]	fScale			Scale factor
		 @param[in]	channelOrder	Channel order
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool GenerateNormalMap(float fScale, const std::string& channelOrder);

		/*!***********************************************************************
		 @brief		Generates MIPMap chain for a texture.
		 @param[in]	filterMode	Filter mode
		 @param[in]	mipMapsToDo	Number of levels of MIPMap chain to create.
								Use PVRTEX_ALLMIPLEVELS to create a full mip chain.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool GenerateMIPMaps(PVRTexLibResizeMode filterMode, PVRTint32 mipMapsToDo = PVRTEX_ALLMIPLEVELS);

		/*!***********************************************************************
		 @brief		Colours a texture's MIPMap levels with different colours
					for debugging purposes. MIP levels are coloured in the
					following repeating pattern: Red, Green, Blue, Cyan,
					Magenta and Yellow
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool ColourMIPMaps();

		/*!***********************************************************************
		 @brief		Transcodes a texture from its original format into the specified format.
					Will either quantise or dither to lower precisions based on "bDoDither".
					"quality" specifies the quality for compressed formats:	PVRTC, ETC,
					ASTC, and BASISU. Higher quality generally means a longer computation time.
		 @param[in]	pixelFormat	Pixel format type
		 @param[in]	channelType	Channel type
		 @param[in]	colourspace	Colour space
		 @param[in]	quality		Quality level for compressed formats, higher quality generally
								requires more processing time.
		 @param[in]	doDither	Dither the texture to lower precisions
		 @param[in] maxRange	Maximum range value for RGB{M/D} encoding
		 @param[in] maxThreads	Maximum number of threads to use for transcoding, if set to
								0 then PVRTexLib will use all available logical cores.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Transcode(
			PVRTuint64 pixelFormat,
			PVRTexLibVariableType channelType,
			PVRTexLibColourSpace colourspace,
			PVRTexLibCompressorQuality quality = PVRTexLibCompressorQuality::PVRTLCQ_PVRTCNormal,
			bool doDither = false,
			float maxRange = 1.0f,
			PVRTuint32 maxThreads = 0U);

		/*!***********************************************************************
		 @brief		Transcodes a texture from its original format into the specified format.
		 @param[in]	options	structure containing transcoder options.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Transcode(const PVRTexLib_TranscoderOptions& options);

		/*!***********************************************************************
		 @brief     A convenience function to decompresses a texture into the most
		 			appropriate format based on the textures 'compressed' format,
					for example a PVRTC compressed texture may decompress to RGB888
					or RGBA8888. This function may also be used	to 'decompress'
					packed formats into something easier to manipulate for example
					RGB565 will be decompressed to RGB888.
		 @param[in]	maxThreads The maximum number of threads to use for decompression,
					if set to 0 PVRTexLib will use all available cores.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool Decompress(PVRTuint32 maxThreads = 0U);

		/*!***********************************************************************
		 @brief		Creates a cube-map with six faces from an equi-rectangular
					projected texture. The input must have an aspect ratio of 2:1,
					i.e. the width must be exactly twice the height.
		 @param[in]	filterMode Filtering mode to apply when sampling the source texture.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool EquiRectToCubeMap(PVRTexLibResizeMode filter);

		/*!***********************************************************************
		 @brief		Generates a mip-mapped diffuse irradiance texture from a cube-map
					environment map, to be used	primarily with physically based
					rendering (PBR) techniques.
					The texture must be a cube-map, the width must equal height,
					and the depth must equal 1.
		 @param[in]	sampleCount The number of samples to use when generating
					the diffuse map.
		 @param[in] mapSize Output dimensions, in pixels.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool GenerateDiffuseIrradianceCubeMap(PVRTuint32 sampleCount, PVRTuint32 mapSize);

		/*!***********************************************************************
		 @brief		Generates a pre-filtered specular irradiance texture from a
					cube-map environment map, to be used primarily with physically
					based rendering (PBR) techniques.
					Each Mip level of the specular map is blurred by a roughness
					value between 0 and 1.
					The texture must be a cube-map, the width must equal height,
					and the depth must equal 1.
		 @param[in]	sampleCount The number of samples to use when generating
					the specular map.
		 @param[in] mapSize Output dimensions, in pixels.
		 @param[in] numMipLevelsToDiscard The number of Mip levels to be discarded
					from the bottom of the Mip chain.
		 @param[in] zeroRoughnessIsExternal False to include a roughness of zero
					when generating the pre-filtered environment map.
					True to omit a roughness of zero, implying that the user
					will supply roughness zero from the environment texture.
		 @return	True if the method succeeds.
		*************************************************************************/
		inline bool GeneratePreFilteredSpecularCubeMap(
			PVRTuint32 sampleCount,
			PVRTuint32 mapSize,
			PVRTuint32 numMipLevelsToDiscard,
			bool zeroRoughnessIsExternal);

		/*!***********************************************************************
		@brief		Computes the maximum difference between two textures; this and
					'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool MaxDifference(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;

		/*!***********************************************************************
		@brief		Computes the mean error between two textures; this and 'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool MeanError(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;

		/*!***********************************************************************
		@brief		Computes the mean squared error (MSE) between two textures;
					this and 'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool MeanSquaredError(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;

		/*!***********************************************************************
		@brief		Computes the root mean squared error (RMSE) between two textures;
					this and 'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool RootMeanSquaredError(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;

		/*!***********************************************************************
		@brief		Computes the standard deviation between two textures; this and
					'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool StandardDeviation(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;

		/*!***********************************************************************
		@brief		Computes the PSNR between two textures; this and 'texture'.
					The MIPLevel, arrayMember, face and zSlice values determine which
					surfaces are compared. NB: MIPLevel, arrayMember, face and zSlice
					should be valid in both textures. Both textures must have the
					same dimensions. The function will only compare common channels
					i.e. if 'this' has RGB while 'texture' has RGBA channels, then
					only the RGB channels will be compared.
		@param[in]	texture The PVRTexture to compare with this.
		@param[out] metrics Structure containing the resulting values.
		@param[in]	MIPLevel The Mip to compare.
		@param[in]	arrayMember The array to compare.
		@param[in]	face The face to compare.
		@param[in]	zSlice The Z slice to compare.
		@return		True if the method succeeds.
		*************************************************************************/
		inline bool PeakSignalToNoiseRatio(
			const PVRTexture& texture,
			PVRTexLib_ErrorMetrics& metrics,
			PVRTuint32 MIPLevel = 0U,
			PVRTuint32 arrayMember = 0U,
			PVRTuint32 face = 0U,
			PVRTuint32 zSlice = 0U) const;
		
		/*!***********************************************************************
		 @brief		 Computes the the [mode] delta per channel between this
		 			 and 'texture'. Both textures must have the same dimensions and
					 may not be compressed. The function will only compare common
					 channels i.e. if 'LHS' has RGB while 'RHS' has RGBA channels,
					 then only the RGB channels will be compared.
		 @param[in]	 texture The PVRTexture to compare with this.
		 @param[out] textureResult A PVRTexture that will contain the result on success.
		 @param[in]	 multiplier The factor to multiply the deltas to highlight differences,
		 			 generally a value between 1 and 10.
		 @param[in]	 mode The clamping mode to use, currently supports absolute and signed.
		 @return	 True if the method succeeds.
		*************************************************************************/
		inline bool ColourDiff(
			const PVRTexture& texture,
			PVRTexture& textureResult,
			float multiplier = 1.0f,
			PVRTexLibColourDiffMode mode = PVRTexLibColourDiffMode::PVRTLCDM_Abs) const;

		/*!***********************************************************************
		 @brief		 Computes the total absolute pixel difference between this
		 			 and 'texture', modulating the output based on the tolerance
					 value supplied. Deltas of zero will appear black while pixels
					 with deltas greater than or equal to the threshold are set to
					 red and finally deltas less than the tolerance are set to blue.
					 Both textures must have the same dimensions and may not be
					 compressed. The function will only compare common channels i.e.
					 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
					 channels will be compared.
		 @param[in]	 texture The PVRTexture to compare with this.
		 @param[out] textureResult A PVRTexture that will contain the result on success.
		 @param[in]	 tolerance The cut-off value to compare the pixel delta to.
		 @return	 True if the method succeeds.
		*************************************************************************/
		inline bool ToleranceDiff(
			const PVRTexture& texture,
			PVRTexture& textureResult,
			float tolerance = 0.1f) const;

		/*!***********************************************************************
		 @brief		 Blend each channel of this texture with 'texture' using the
		 			 blend factor as a weighting of the first texture against the second.
					 Both textures must have the same dimensions and may not be
					 compressed. The function will only blend common channels i.e.
					 if 'LHS' has RGB while 'RHS' has RGBA channels, then only the RGB
					 channels will be blended.
		 @param[in]	 texture The PVRTexture to compare with this.
		 @param[out] textureResult A PVRTexture that will contain the result on success.
		 @param[in]	 blendFactor The blend weight to use in the blend equation:
		 			 (LHS_delta * BF) + (RHS_delta * (1 - BF)). The value is clamped
					 between 0 and 1.
		 @return	 True if the method succeeds.
		*************************************************************************/
		inline bool BlendDiff(
			const PVRTexture& texture,
			PVRTexture& textureResult,
			float blendFactor = 0.5f) const;

	protected:
		inline void Destroy();
		inline PVRTexture& operator=(PVRTexLib_PVRTexture rhs) noexcept;
		PVRTexLib_PVRTexture m_hTexture;
	};

	/*!***********************************************************************
	 Begin implementation for PVRTextureHeader
	*************************************************************************/
	PVRTextureHeader::PVRTextureHeader()
		: m_hTextureHeader()
	{
		PVRHeader_CreateParams params;
		PVRTexLib_SetDefaultTextureHeaderParams(&params);
		m_hTextureHeader = PVRTexLib_CreateTextureHeader(&params);
	}

	PVRTextureHeader::PVRTextureHeader(const PVRHeader_CreateParams* params)
		: m_hTextureHeader(PVRTexLib_CreateTextureHeader(params)) {}

	PVRTextureHeader::PVRTextureHeader(
		PVRTuint64				pixelFormat,
		PVRTuint32				width,
		PVRTuint32				height,
		PVRTuint32				depth,
		PVRTuint32				numMipMaps,
		PVRTuint32				numArrayMembers,
		PVRTuint32				numFaces,
		PVRTexLibColourSpace	colourSpace,
		PVRTexLibVariableType	channelType,
		bool					preMultiplied)
		: m_hTextureHeader()
	{
		PVRHeader_CreateParams params;
		params.pixelFormat = pixelFormat;
		params.width = width;
		params.height = height;
		params.depth = depth;
		params.numMipMaps = numMipMaps;
		params.numArrayMembers = numArrayMembers;
		params.numFaces = numFaces;
		params.colourSpace = colourSpace;
		params.channelType = channelType;
		params.preMultiplied = preMultiplied;
		m_hTextureHeader = PVRTexLib_CreateTextureHeader(&params);
	}

	PVRTextureHeader::PVRTextureHeader(bool)
		: m_hTextureHeader() {}

	PVRTextureHeader::PVRTextureHeader(const PVRTextureHeader& rhs)
		: m_hTextureHeader(PVRTexLib_CopyTextureHeader(rhs.m_hTextureHeader)) {}

	PVRTextureHeader::PVRTextureHeader(PVRTextureHeader&& rhs) noexcept
		: m_hTextureHeader(rhs.m_hTextureHeader)
	{
		rhs.m_hTextureHeader = nullptr;
	}

	PVRTextureHeader& PVRTextureHeader::operator=(const PVRTextureHeader& rhs)
	{
		if (&rhs == this)
			return *this;

		if (m_hTextureHeader)
		{
			PVRTexLib_DestroyTextureHeader(m_hTextureHeader);
			m_hTextureHeader = nullptr;
		}

		m_hTextureHeader = PVRTexLib_CopyTextureHeader(rhs.m_hTextureHeader);
		return *this;
	}

	PVRTextureHeader& PVRTextureHeader::operator=(PVRTextureHeader&& rhs) noexcept
	{
		if (&rhs == this)
			return *this;

		if (m_hTextureHeader)
		{
			PVRTexLib_DestroyTextureHeader(m_hTextureHeader);
			m_hTextureHeader = nullptr;
		}

		m_hTextureHeader = rhs.m_hTextureHeader;
		rhs.m_hTextureHeader = nullptr;
		return *this;
	}

	PVRTextureHeader::~PVRTextureHeader()
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_DestroyTextureHeader(m_hTextureHeader);
			m_hTextureHeader = nullptr;
		}			
	}

	PVRTexLib_CPVRTextureHeader PVRTextureHeader::GetHeader(const PVRTextureHeader& header)
	{
		return header.m_hTextureHeader;
	}

	PVRTuint32 PVRTextureHeader::GetTextureBitsPerPixel() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureBitsPerPixel(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureBitsPerPixel(PVRTuint64 u64PixelFormat)
	{
		return PVRTexLib_GetFormatBitsPerPixel(u64PixelFormat);
	}

	PVRTuint32 PVRTextureHeader::GetTextureChannelCount() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureChannelCount(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTexLibVariableType PVRTextureHeader::GetTextureChannelType() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureChannelType(m_hTextureHeader);
		}

		return PVRTexLibVariableType::PVRTLVT_Invalid;
	}

	PVRTexLibColourSpace PVRTextureHeader::GetColourSpace() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureColourSpace(m_hTextureHeader);
		}

		return PVRTexLibColourSpace::PVRTLCS_NumSpaces;
	}

	PVRTuint32 PVRTextureHeader::GetTextureWidth(PVRTuint32 mipLevel) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureWidth(m_hTextureHeader, mipLevel);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureHeight(PVRTuint32 mipLevel) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureHeight(m_hTextureHeader, mipLevel);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureDepth(PVRTuint32 mipLevel) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureDepth(m_hTextureHeader, mipLevel);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureSize(PVRTint32 mipLevel, bool allSurfaces, bool allFaces) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureSize(m_hTextureHeader, mipLevel, allSurfaces, allFaces);
		}

		return 0U;
	}

	PVRTuint64 PVRTextureHeader::GetTextureDataSize(PVRTint32 mipLevel, bool allSurfaces, bool allFaces) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureDataSize(m_hTextureHeader, mipLevel, allSurfaces, allFaces);
		}

		return 0ULL;
	}

	void PVRTextureHeader::GetTextureOrientation(PVRTexLib_Orientation& result) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureOrientation(m_hTextureHeader, &result);
		}
		else
		{
			result.x = (PVRTexLibOrientation)0U;
			result.y = (PVRTexLibOrientation)0U;
			result.z = (PVRTexLibOrientation)0U;
		}
	}

	void PVRTextureHeader::GetTextureOpenGLFormat(PVRTexLib_OpenGLFormat& result) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureOpenGLFormat(m_hTextureHeader, &result);
		}
		else
		{
			result.internalFormat = 0U;
			result.format = 0U;
			result.type = 0U;
		}
	}

	void PVRTextureHeader::GetTextureOpenGLESFormat(PVRTexLib_OpenGLESFormat& result) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureOpenGLESFormat(m_hTextureHeader, &result);
		}
		else
		{
			result.internalFormat = 0U; 
			result.format = 0U;
			result.type = 0U;
		}
	}

	PVRTuint32 PVRTextureHeader::GetTextureVulkanFormat() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureVulkanFormat(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureD3DFormat() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureD3DFormat(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureDXGIFormat() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureDXGIFormat(m_hTextureHeader);
		}

		return 0U;
	}

	void PVRTextureHeader::GetTextureFormatMinDims(PVRTuint32& minX, PVRTuint32& minY, PVRTuint32& minZ) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureFormatMinDims(m_hTextureHeader, &minX, &minY, &minZ);
		}
		else
		{
			minX = 1U;
			minY = 1U;
			minZ = 1U;
		}
	}

	void PVRTextureHeader::GetPixelFormatMinDims(PVRTuint64 ui64Format, PVRTuint32& minX, PVRTuint32& minY, PVRTuint32& minZ)
	{
		PVRTexLib_GetPixelFormatMinDims(ui64Format, &minX, &minY, &minZ);
	}

	PVRTuint32 PVRTextureHeader::GetTextureMetaDataSize() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureMetaDataSize(m_hTextureHeader);
		}

		return 0U;
	}

	bool PVRTextureHeader::GetTextureIsPreMultiplied() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureIsPreMultiplied(m_hTextureHeader);
		}

		return false;
	}

	bool PVRTextureHeader::GetTextureIsFileCompressed() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureIsFileCompressed(m_hTextureHeader);
		}

		return false;
	}

	bool PVRTextureHeader::GetTextureIsBumpMap() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureIsBumpMap(m_hTextureHeader);
		}

		return false;
	}

	float PVRTextureHeader::GetTextureBumpMapScale() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureBumpMapScale(m_hTextureHeader);
		}

		return 0.0f;
	}

	PVRTuint32 PVRTextureHeader::GetNumTextureAtlasMembers() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetNumTextureAtlasMembers(m_hTextureHeader);
		}

		return 0U;
	}

	const float* PVRTextureHeader::GetTextureAtlasData(PVRTuint32& count) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureAtlasData(m_hTextureHeader, &count);
		}

		count = 0U;
		return nullptr;
	}

	PVRTuint32 PVRTextureHeader::GetTextureNumMipMapLevels() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureNumMipMapLevels(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureNumFaces() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureNumFaces(m_hTextureHeader);
		}

		return 0U;
	}

	PVRTuint32 PVRTextureHeader::GetTextureNumArrayMembers() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTextureNumArrayMembers(m_hTextureHeader);
		}

		return 0U;
	}

	std::string PVRTextureHeader::GetTextureCubeMapOrder() const
	{
		std::string cubeOrder(7, '\0');
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureCubeMapOrder(m_hTextureHeader, &cubeOrder[0]);
		}

		return cubeOrder;
	}

	std::string PVRTextureHeader::GetTextureBumpMapOrder() const
	{
		std::string bumpOrder(5, '\0');
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureBumpMapOrder(m_hTextureHeader, &bumpOrder[0]);
		}

		return bumpOrder;
	}

	PVRTuint64 PVRTextureHeader::GetTexturePixelFormat() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_GetTexturePixelFormat(m_hTextureHeader);
		}

		return static_cast<PVRTuint64>(PVRTexLibPixelFormat::PVRTLPF_NumCompressedPFs);
	}

	bool PVRTextureHeader::TextureHasPackedChannelData() const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_TextureHasPackedChannelData(m_hTextureHeader);
		}

		return false;
	}

	bool PVRTextureHeader::IsPixelFormatCompressed() const
	{
		return !(GetTexturePixelFormat() & PVRTEX_PFHIGHMASK);
	}

	void PVRTextureHeader::SetTextureChannelType(PVRTexLibVariableType type)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureChannelType(m_hTextureHeader, type);
		}
	}

	void PVRTextureHeader::SetTextureColourSpace(PVRTexLibColourSpace colourSpace)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureColourSpace(m_hTextureHeader, colourSpace);
		}
	}

	bool PVRTextureHeader::SetTextureD3DFormat(PVRTuint32 d3dFormat)
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_SetTextureD3DFormat(m_hTextureHeader, d3dFormat);
		}

		return false;
	}

	bool PVRTextureHeader::SetTextureDXGIFormat(PVRTuint32 dxgiFormat)
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_SetTextureDXGIFormat(m_hTextureHeader, dxgiFormat);
		}

		return false;
	}

	bool PVRTextureHeader::SetTextureOGLFormat(const PVRTexLib_OpenGLFormat& oglFormat)
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_SetTextureOGLFormat(m_hTextureHeader, &oglFormat);
		}

		return false;
	}

	bool PVRTextureHeader::SetTextureOGLESFormat(const PVRTexLib_OpenGLESFormat& oglesFormat)
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_SetTextureOGLESFormat(m_hTextureHeader, &oglesFormat);
		}

		return false;
	}

	bool PVRTextureHeader::SetTextureVulkanFormat(PVRTuint32 vulkanFormat)
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_SetTextureVulkanFormat(m_hTextureHeader, vulkanFormat);
		}

		return false;
	}

	void PVRTextureHeader::SetTexturePixelFormat(PVRTuint64 format)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTexturePixelFormat(m_hTextureHeader, format);
		}
	}

	void PVRTextureHeader::SetTextureWidth(PVRTuint32 width)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureWidth(m_hTextureHeader, width);
		}
	}

	void PVRTextureHeader::SetTextureHeight(PVRTuint32 height)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureHeight(m_hTextureHeader, height);
		}
	}

	void PVRTextureHeader::SetTextureDepth(PVRTuint32 depth)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureDepth(m_hTextureHeader, depth);
		}
	}

	void PVRTextureHeader::SetTextureNumArrayMembers(PVRTuint32 numMembers)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureNumArrayMembers(m_hTextureHeader, numMembers);
		}
	}

	void PVRTextureHeader::SetTextureNumMIPLevels(PVRTuint32 numMIPLevels)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureNumMIPLevels(m_hTextureHeader, numMIPLevels);
		}
	}

	void PVRTextureHeader::SetTextureNumFaces(PVRTuint32 numFaces)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureNumFaces(m_hTextureHeader, numFaces);
		}
	}

	void PVRTextureHeader::SetTextureOrientation(const PVRTexLib_Orientation& orientation)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureOrientation(m_hTextureHeader, &orientation);
		}
	}

	void PVRTextureHeader::SetTextureIsFileCompressed(bool isFileCompressed)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureIsFileCompressed(m_hTextureHeader, isFileCompressed);
		}
	}

	void PVRTextureHeader::SetTextureIsPreMultiplied(bool isPreMultiplied)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureIsPreMultiplied(m_hTextureHeader, isPreMultiplied);
		}
	}

	void PVRTextureHeader::GetTextureBorder(PVRTuint32& borderWidth, PVRTuint32& borderHeight, PVRTuint32& borderDepth) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_GetTextureBorder(m_hTextureHeader, &borderWidth, &borderHeight, &borderDepth);
		}
		else
		{
			borderWidth = 0U;
			borderHeight = 0U;
			borderDepth = 0U;
		}
	}

	bool PVRTextureHeader::GetMetaDataBlock(PVRTuint32 key, MetaDataBlock& dataBlock, PVRTuint32 devFOURCC) const
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_MetaDataBlock tmp;
			if (PVRTexLib_GetMetaDataBlock(m_hTextureHeader, devFOURCC, key,
				&tmp, [](PVRTuint32 bytes) { return (void*)new PVRTuint8[bytes]; }))
			{
				dataBlock.DevFOURCC = tmp.DevFOURCC;
				dataBlock.u32Key = tmp.u32Key;
				dataBlock.u32DataSize = tmp.u32DataSize;
				dataBlock.Data.reset(tmp.Data);
				return true;
			}
		}

		dataBlock = MetaDataBlock();
		return false;
	}

	bool PVRTextureHeader::TextureHasMetaData(PVRTuint32 key, PVRTuint32 devFOURCC) const
	{
		if (m_hTextureHeader)
		{
			return PVRTexLib_TextureHasMetaData(m_hTextureHeader, devFOURCC, key);
		}

		return false;
	}

	void PVRTextureHeader::SetTextureBumpMap(float bumpScale, const std::string& bumpOrder)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureBumpMap(m_hTextureHeader, bumpScale, bumpOrder.c_str());
		}
	}

	void PVRTextureHeader::SetTextureAtlas(const float* atlasData, PVRTuint32 dataSize)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureAtlas(m_hTextureHeader, atlasData, dataSize);
		}
	}

	void PVRTextureHeader::SetTextureCubeMapOrder(const std::string& cubeMapOrder)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureCubeMapOrder(m_hTextureHeader, cubeMapOrder.c_str());
		}
	}

	void PVRTextureHeader::SetTextureBorder(PVRTuint32 borderWidth, PVRTuint32 borderHeight, PVRTuint32 borderDepth)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_SetTextureBorder(m_hTextureHeader, borderWidth, borderHeight, borderDepth);
		}
	}

	void PVRTextureHeader::AddMetaData(const MetaDataBlock& dataBlock)
	{
		if (m_hTextureHeader && dataBlock.u32DataSize)
		{
			PVRTexLib_MetaDataBlock tmp;
			tmp.DevFOURCC	= dataBlock.DevFOURCC;
			tmp.u32Key		= dataBlock.u32Key;
			tmp.u32DataSize	= dataBlock.u32DataSize;
			tmp.Data		= dataBlock.Data.get();
			PVRTexLib_AddMetaData(m_hTextureHeader, &tmp);
		}
	}

	void PVRTextureHeader::AddMetaData(const PVRTexLib_MetaDataBlock& dataBlock)
	{
		if (m_hTextureHeader && dataBlock.u32DataSize)
		{
			PVRTexLib_AddMetaData(m_hTextureHeader, &dataBlock);
		}
	}

	void PVRTextureHeader::RemoveMetaData(PVRTuint32 key, PVRTuint32 devFOURCC)
	{
		if (m_hTextureHeader)
		{
			PVRTexLib_RemoveMetaData(m_hTextureHeader, devFOURCC, key);
		}
	}

	/*!***********************************************************************
	 Begin implementation for PVRTexture
	*************************************************************************/
	PVRTexture::PVRTexture()
		: PVRTextureHeader(false)
		, m_hTexture() {}

	PVRTexture::PVRTexture(const PVRTextureHeader& header, const void *textureData)
		: PVRTextureHeader(false)
		, m_hTexture(PVRTexLib_CreateTexture(GetHeader(header), textureData))
	{
		m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
	}

	PVRTexture::PVRTexture(const std::string& filePath)
		: PVRTextureHeader(false)
		, m_hTexture(PVRTexLib_CreateTextureFromFile(filePath.c_str()))
	{
		if (m_hTexture)
		{
			m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
		}
		else
		{
			throw std::runtime_error("Couldn't load texture: " + filePath);
		}
	}

	PVRTexture::PVRTexture(const char* filePath)
		: PVRTextureHeader(false)
		, m_hTexture(PVRTexLib_CreateTextureFromFile(filePath))
	{
		if (m_hTexture)
		{
			m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
		}
		else
		{
			throw std::runtime_error("Couldn't load texture: " + std::string(filePath));
		}
	}

	PVRTexture::PVRTexture(const void* data)
		: PVRTextureHeader(false)
		, m_hTexture(PVRTexLib_CreateTextureFromData(data))
	{
		if (m_hTexture)
		{
			m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
		}
		else
		{
			throw std::runtime_error("Provided pointer to texture data is invalid");
		}
	}

	PVRTexture::PVRTexture(const PVRTexture& rhs)
		: PVRTextureHeader(false)
		, m_hTexture(PVRTexLib_CopyTexture(rhs.m_hTexture))
	{
		m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
	}

	PVRTexture::PVRTexture(PVRTexture&& rhs) noexcept
		: PVRTextureHeader(false)
		, m_hTexture(rhs.m_hTexture)
	{
		m_hTextureHeader = rhs.m_hTextureHeader;
		rhs.m_hTextureHeader = nullptr;
		rhs.m_hTexture = nullptr;
	}

	PVRTexture& PVRTexture::operator=(const PVRTexture& rhs)
	{
		if (&rhs == this)
			return *this;

		Destroy();
		m_hTexture = PVRTexLib_CopyTexture(rhs.m_hTexture);
		m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
		return *this;
	}

	PVRTexture& PVRTexture::operator=(PVRTexture&& rhs) noexcept
	{
		if (&rhs == this)
			return *this;

		Destroy();
		m_hTexture = rhs.m_hTexture;
		m_hTextureHeader = rhs.m_hTextureHeader;

		rhs.m_hTextureHeader = nullptr;
		rhs.m_hTexture = nullptr;
		return *this;
	}

	PVRTexture& PVRTexture::operator=(PVRTexLib_PVRTexture rhs) noexcept
	{
		if (m_hTexture == rhs || rhs == nullptr)
			return *this;

		Destroy();
		m_hTexture = rhs;
		m_hTextureHeader = PVRTexLib_GetTextureHeaderW(m_hTexture);
		return *this;
	}

	PVRTexture::~PVRTexture()
	{
		Destroy();
	}

	void PVRTexture::Destroy()
	{
		if (m_hTexture)
		{
			PVRTexLib_DestroyTexture(m_hTexture);
			m_hTexture = nullptr;
			m_hTextureHeader = nullptr;
		}
	}

	void* PVRTexture::GetTextureDataPointer(
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 faceNumber,
		PVRTuint32 ZSlice)
	{
		if (m_hTexture)
		{
			return PVRTexLib_GetTextureDataPtr(m_hTexture, MIPLevel, arrayMember, faceNumber, ZSlice);
		}

		return nullptr;
	}

	const void* PVRTexture::GetTextureDataPointer(
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 faceNumber,
		PVRTuint32 ZSlice) const
	{
		if (m_hTexture)
		{
			return PVRTexLib_GetTextureDataConstPtr(m_hTexture, MIPLevel, arrayMember, faceNumber, ZSlice);
		}

		return nullptr;
	}

	void PVRTexture::AddPaddingMetaData(PVRTuint32 padding)
	{
		if (m_hTexture)
		{
			PVRTexLib_AddPaddingMetaData(m_hTexture, padding);
		}
	}

	bool PVRTexture::SaveToFile(const std::string& filePath) const
	{
		if (m_hTexture)
		{
			return PVRTexLib_SaveTextureToFile(m_hTexture, filePath.c_str());
		}

		return false;
	}

	bool PVRTexture::SaveTextureToMemory(
		PVRTexLibFileContainerType fileType,
		void* privateData,
		PVRTuint64& outSize,
		PVRTuint8*(pfnRealloc)(void* privateData, PVRTuint64 allocSize)) const
	{
		if (m_hTexture)
		{
			return PVRTexLib_SaveTextureToMemory(m_hTexture, fileType, privateData, &outSize, pfnRealloc);
		}

		return false;
	}

	bool PVRTexture::SaveTextureToMemory(
		PVRTexLibFileContainerType fileType,
		std::vector<PVRTuint8>& outData) const
	{
		if (m_hTexture)
		{
			PVRTuint64 outSize;

			if (PVRTexLib_SaveTextureToMemory(m_hTexture, fileType,
				static_cast<void*>(&outData), &outSize,
				[](void* privateData, PVRTuint64 allocSize) {
					auto buffer = static_cast<std::vector<PVRTuint8>*>(privateData);
					assert(allocSize <= std::numeric_limits<size_t>::max());
					buffer->resize(static_cast<size_t>(allocSize));
					return buffer->data();
				}))
			{
				outData.resize(static_cast<size_t>(outSize));
				return true;
			}
		}

		return false;
	}

	std::unique_ptr<PVRTuint8, void(*)(PVRTuint8*)> PVRTexture::SaveTextureToMemory(
		PVRTexLibFileContainerType fileType,
		PVRTuint64& outSize) const
	{
		std::unique_ptr<PVRTuint8, void(*)(PVRTuint8*)> result(nullptr, [](PVRTuint8* data) { free(data); });

		if (m_hTexture)
		{
			if (!PVRTexLib_SaveTextureToMemory(m_hTexture, fileType,
				static_cast<void*>(&result), &outSize,
				[](void* privateData, PVRTuint64 allocSize) {
					auto buffer = static_cast<std::unique_ptr<PVRTuint8, void(*)(PVRTuint8*)>*>(privateData);
					auto currentPtr = buffer->release();
					assert(allocSize <= std::numeric_limits<size_t>::max());
					auto newPtr = std::realloc(currentPtr, static_cast<size_t>(allocSize));

					if (!newPtr)
					{
						free(currentPtr);
					}

					buffer->reset(static_cast<PVRTuint8*>(newPtr));
					return buffer->get();
				}))
			{
				result.reset();
			}
		}

		return result;
	}

	bool PVRTexture::SaveToFile(const std::string& filePath, PVRTexLibLegacyApi api) const
	{
		if (m_hTexture)
		{
			return PVRTexLib_SaveTextureToLegacyPVRFile(m_hTexture, filePath.c_str(), api);
		}

		return false;
	}

	bool PVRTexture::SaveSurfaceToImageFile(
		const std::string& filePath,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 ZSlice) const
	{
		if (m_hTexture)
		{
			return PVRTexLib_SaveSurfaceToImageFile(m_hTexture, filePath.c_str(), MIPLevel, arrayMember, face, ZSlice);
		}

		return false;
	}

	bool PVRTexture::IsTextureMultiPart() const
	{
		if (m_hTexture)
		{
			return PVRTexLib_IsTextureMultiPart(m_hTexture);
		}
		
		return false;
	}

	std::vector<PVRTexture> PVRTexture::GetTextureParts()
	{
		std::vector<PVRTexture> textures;

		if (m_hTexture)
		{
			PVRTuint32 count;
			std::vector<void*> handles;

			PVRTexLib_GetTextureParts(m_hTexture, nullptr, &count);
			handles.resize(count);
			PVRTexLib_GetTextureParts(m_hTexture, handles.data(), &count);

			for (auto handle : handles)
			{
				textures.emplace_back();
				textures.back().m_hTexture = handle;
				textures.back().m_hTextureHeader = PVRTexLib_GetTextureHeaderW(handle);
			}
		}

		return textures;
	}

	bool PVRTexture::Resize(
		PVRTuint32 newWidth,
		PVRTuint32 newHeight,
		PVRTuint32 newDepth,
		PVRTexLibResizeMode resizeMode)
	{
		if (m_hTexture)
		{
			return PVRTexLib_ResizeTexture(m_hTexture, newWidth, newHeight, newDepth, resizeMode);
		}

		return false;
	}

	bool PVRTexture::ResizeCanvas(
		PVRTuint32 newWidth,
		PVRTuint32 newHeight,
		PVRTuint32 newDepth,
		PVRTint32 xOffset,
		PVRTint32 yOffset,
		PVRTint32 zOffset)
	{
		if (m_hTexture)
		{
			return PVRTexLib_ResizeTextureCanvas(m_hTexture, newWidth, newHeight, newDepth, xOffset, yOffset, zOffset);
		}

		return false;
	}

	bool PVRTexture::Rotate(PVRTexLibAxis rotationAxis, bool forward)
	{
		if (m_hTexture)
		{
			return PVRTexLib_RotateTexture(m_hTexture, rotationAxis, forward);
		}

		return false;
	}

	bool PVRTexture::Flip(PVRTexLibAxis flipDirection)
	{
		if (m_hTexture)
		{
			return PVRTexLib_FlipTexture(m_hTexture, flipDirection);
		}

		return false;
	}

	bool PVRTexture::Border(PVRTuint32 borderX, PVRTuint32 borderY, PVRTuint32 borderZ)
	{
		if (m_hTexture)
		{
			return PVRTexLib_BorderTexture(m_hTexture, borderX, borderY, borderZ);
		}

		return false;
	}

	bool PVRTexture::PreMultiplyAlpha()
	{
		if (m_hTexture)
		{
			return PVRTexLib_PreMultiplyAlpha(m_hTexture);
		}

		return false;
	}

	bool PVRTexture::Bleed()
	{
		if (m_hTexture)
		{
			return PVRTexLib_Bleed(m_hTexture);
		}

		return false;
	}

	bool PVRTexture::SetChannels(
		PVRTuint32 numChannelSets,
		const PVRTexLibChannelName* channels,
		const PVRTuint32* pValues)
	{
		if (m_hTexture)
		{
			return PVRTexLib_SetTextureChannels(m_hTexture, numChannelSets, channels, pValues);
		}

		return false;
	}

	bool PVRTexture::SetChannels(
		PVRTuint32 numChannelSets,
		const PVRTexLibChannelName* channels,
		const float* pValues)
	{
		if (m_hTexture)
		{
			return PVRTexLib_SetTextureChannelsFloat(m_hTexture, numChannelSets, channels, pValues);
		}

		return false;
	}

	bool PVRTexture::CopyChannels(
		const PVRTexture& sourceTexture,
		PVRTuint32 numChannelCopies,
		const PVRTexLibChannelName* destinationChannels,
		const PVRTexLibChannelName* sourceChannels)
	{
		if (m_hTexture && sourceTexture.m_hTexture)
		{
			return PVRTexLib_CopyTextureChannels(m_hTexture, sourceTexture.m_hTexture, numChannelCopies, destinationChannels, sourceChannels);
		}

		return false;
	}

	bool PVRTexture::GenerateNormalMap(float fScale, const std::string& channelOrder)
	{
		if (m_hTexture)
		{
			return PVRTexLib_GenerateNormalMap(m_hTexture, fScale, channelOrder.c_str());
		}

		return false;
	}

	bool PVRTexture::GenerateMIPMaps(PVRTexLibResizeMode filterMode, PVRTint32 mipMapsToDo)
	{
		if (m_hTexture)
		{
			return PVRTexLib_GenerateMIPMaps(m_hTexture, filterMode, mipMapsToDo);
		}

		return false;
	}

	bool PVRTexture::ColourMIPMaps()
	{
		if (m_hTexture)
		{
			return PVRTexLib_ColourMIPMaps(m_hTexture);
		}

		return false;
	}

	bool PVRTexture::Transcode(
		PVRTuint64 pixelFormat,
		PVRTexLibVariableType channelType,
		PVRTexLibColourSpace colourspace,
		PVRTexLibCompressorQuality quality,
		bool doDither,
		float maxRange,
		PVRTuint32 maxThreads)
	{
		if (m_hTexture)
		{
			
			PVRTexLib_TranscoderOptions options;
			options.sizeofStruct = sizeof(PVRTexLib_TranscoderOptions);
			options.pixelFormat = pixelFormat;
			options.channelType[0] = options.channelType[1] = options.channelType[2] = options.channelType[3] = channelType;
			options.colourspace = colourspace;
			options.quality = quality;
			options.doDither = doDither;
			options.maxRange = maxRange;
			options.maxThreads = maxThreads;
			return PVRTexLib_TranscodeTexture(m_hTexture, options);
		}

		return false;
	}

	bool PVRTexture::Transcode(const PVRTexLib_TranscoderOptions& options)
	{
		if (m_hTexture)
		{
			return PVRTexLib_TranscodeTexture(m_hTexture, options);
		}

		return false;
	}

	bool PVRTexture::Decompress(PVRTuint32 maxThreads)
	{
		if (m_hTexture)
		{
			return PVRTexLib_Decompress(m_hTexture, maxThreads);
		}

		return false;
	}

	bool PVRTexture::EquiRectToCubeMap(PVRTexLibResizeMode filter)
	{
		if (m_hTexture)
		{
			return PVRTexLib_EquiRectToCubeMap(m_hTexture, filter);
		}

		return false;
	}

	bool PVRTexture::GenerateDiffuseIrradianceCubeMap(PVRTuint32 sampleCount, PVRTuint32 mapSize)
	{
		if (m_hTexture)
		{
			return PVRTexLib_GenerateDiffuseIrradianceCubeMap(m_hTexture, sampleCount, mapSize);
		}

		return false;
	}

	bool PVRTexture::GeneratePreFilteredSpecularCubeMap(
		PVRTuint32 sampleCount,
		PVRTuint32 mapSize,
		PVRTuint32 numMipLevelsToDiscard,
		bool zeroRoughnessIsExternal)
	{
		if (m_hTexture)
		{
			return PVRTexLib_GeneratePreFilteredSpecularCubeMap(m_hTexture, sampleCount, mapSize, numMipLevelsToDiscard, zeroRoughnessIsExternal);
		}

		return false;
	}

	bool PVRTexture::MaxDifference(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_MaxDifference(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::MeanError(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_MeanError(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::MeanSquaredError(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_MeanSquaredError(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::RootMeanSquaredError(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_RootMeanSquaredError(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::StandardDeviation(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_StandardDeviation(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::PeakSignalToNoiseRatio(
		const PVRTexture& texture,
		PVRTexLib_ErrorMetrics& metrics,
		PVRTuint32 MIPLevel,
		PVRTuint32 arrayMember,
		PVRTuint32 face,
		PVRTuint32 zSlice) const
	{
		if (m_hTexture && texture.m_hTexture)
		{
			return PVRTexLib_PeakSignalToNoiseRatio(m_hTexture, texture.m_hTexture, MIPLevel, arrayMember, face, zSlice, &metrics);
		}

		return false;
	}

	bool PVRTexture::ColourDiff(
		const PVRTexture& texture,
		PVRTexture& textureResult,
		float multiplier,
		PVRTexLibColourDiffMode mode) const
	{
		if (m_hTexture &&
			texture.m_hTexture)
		{
			PVRTexLib_PVRTexture result = nullptr;
			if (PVRTexLib_ColourDiff(m_hTexture, texture.m_hTexture, &result, multiplier, mode))
			{
				textureResult = result;
				return true;
			}
		}

		return false;
	}

	bool PVRTexture::ToleranceDiff(
		const PVRTexture& texture,
		PVRTexture& textureResult,
		float tolerance) const
	{
		if (m_hTexture &&
			texture.m_hTexture)
		{
			PVRTexLib_PVRTexture result = nullptr;
			if (PVRTexLib_ToleranceDiff(m_hTexture, texture.m_hTexture, &result, tolerance))
			{
				textureResult = result;
				return true;
			}
		}

		return false;
	}

	bool PVRTexture::BlendDiff(
		const PVRTexture& texture,
		PVRTexture& textureResult,
		float blendFactor) const
	{
		if (m_hTexture &&
			texture.m_hTexture)
		{
			PVRTexLib_PVRTexture result = nullptr;
			if (PVRTexLib_BlendDiff(m_hTexture, texture.m_hTexture, &result, blendFactor))
			{
				textureResult = result;
				return true;
			}
		}

		return false;
	}
}
/*****************************************************************************
End of file (PVRTexLib.hpp)
*****************************************************************************/
