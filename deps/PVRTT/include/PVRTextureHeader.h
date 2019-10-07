/*!***********************************************************************

 @file         PVRTextureHeader.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        Texture header methods.
 @details      Includes pixel and channel type methods, size retrieval and 
               dimension manipulation. As well as set and get methods for 
               BumpMaps, Meta Data and cube map order.

*************************************************************************/

#ifndef _PVRTEXTURE_HEADER_H
#define _PVRTEXTURE_HEADER_H

#include "PVRTextureDefines.h"
#include "PVRTextureFormat.h"
#include "PVRTString.h"
#include "PVRTMap.h"

namespace pvrtexture
{
	/*!***********************************************************************
     @class         CPVRTextureHeader
     @brief         Wrapper class for PVRTextureHeaderV3, adds 'smart' accessor functions.
    *************************************************************************/
	class PVR_DLL CPVRTextureHeader
	{	
	protected:
		PVRTextureHeaderV3											m_sHeader;		//!< Texture header as laid out in a file.
		CPVRTMap<uint32, CPVRTMap<uint32,MetaDataBlock> >			m_MetaData;		//!< Map of all the meta data stored for a texture.

	public:
	// Construction methods for a texture header.
		/*!***********************************************************************
		 @brief      	Default constructor for a CPVRTextureHeader. Returns an empty header.
		 @return		A new texture header.
		*************************************************************************/
		CPVRTextureHeader();

		/*!***********************************************************************
		 @brief      	Creates a new texture header from a PVRTextureHeaderV3, 
						and appends Meta data if any is supplied.
		 @param[in]		fileHeader          PVRTextureHeaderV3
		 @param[in]		metaDataCount       Number of Meta data blocks to add
		 @param[in]		metaData            Pointer to meta data block
		 @return		A new texture header.
		*************************************************************************/
		CPVRTextureHeader(	PVRTextureHeaderV3	fileHeader,
							uint32				metaDataCount=0,
							MetaDataBlock*		metaData=NULL);

		/*!***********************************************************************
		 @brief      	Creates a new texture header based on individual header
						variables.
		 @param[in]		u64PixelFormat      PixelFormat
		 @param[in]		u32Height           Texture height
		 @param[in]		u32Width            Texture width
		 @param[in]		u32Depth            Texture depth
		 @param[in]		u32NumMipMaps       Number of MIP Maps
		 @param[in]		u32NumArrayMembers  Number of array members
		 @param[in]		u32NumFaces         Number of faces
		 @param[in]		eColourSpace        Colour space
		 @param[in]		eChannelType        Channel type
		 @param[in]		bPreMultiplied      Whether or not the texture's colour has been
                                            pre-multiplied by the alpha values
		 @return		A new texture header.
		*************************************************************************/
		CPVRTextureHeader(	uint64				u64PixelFormat,
							uint32				u32Height=1,
							uint32				u32Width=1,
							uint32				u32Depth=1,
							uint32				u32NumMipMaps=1,
							uint32				u32NumArrayMembers=1,
							uint32				u32NumFaces=1,
							EPVRTColourSpace	eColourSpace=ePVRTCSpacelRGB,
							EPVRTVariableType	eChannelType=ePVRTVarTypeUnsignedByteNorm,
							bool				bPreMultiplied=false);

		/*!***********************************************************************
		@brief      	Deconstructor for CPVRTextureHeader.
		*************************************************************************/
		~CPVRTextureHeader();

		/*!***********************************************************************
		 @brief      	Will copy the contents and information of another header into this one.
		 @param[in]		rhs     Header to copy.
		 @return		This header.
		*************************************************************************/
		CPVRTextureHeader& operator=(const CPVRTextureHeader& rhs);
		
	// Accessor Methods for a texture's properties - getters.

		/*!***********************************************************************
		 @brief      	Gets the file header structure.
		 @return		The file header.
		*************************************************************************/
		PVRTextureHeaderV3 getFileHeader() const;

		/*!***********************************************************************
		 @brief      	Gets the 64-bit pixel type ID of the texture.
		 @return		64-bit pixel type ID.
		*************************************************************************/
		PixelType getPixelType() const;

		/*!***********************************************************************
		 @brief      	Gets the bits per pixel of the texture format.
		 @return		Number of bits per pixel.
		*************************************************************************/
		uint32 getBitsPerPixel() const;

		/*!***********************************************************************
		 @brief      	Returns the colour space of the texture.
		 @return		enum representing colour space.
		*************************************************************************/
		EPVRTColourSpace getColourSpace() const;

		/*!***********************************************************************
		 @brief      	Returns the variable type that the texture's data is stored in.
		 @return		enum representing the type of the texture.
		*************************************************************************/
		EPVRTVariableType getChannelType() const;

		/*!***********************************************************************
		 @brief      	Gets the width of the user specified MIP-Map 
						level for the texture
		 @param[in]		uiMipLevel	    MIP level that user is interested in.
		 @return		Width of the specified MIP-Map level.
		*************************************************************************/
		uint32 getWidth(uint32 uiMipLevel=PVRTEX_TOPMIPLEVEL) const;

		/*!***********************************************************************
		 @brief      	Gets the height of the user specified MIP-Map 
						level for the texture
		 @param[in]		uiMipLevel	    MIP level that user is interested in.
		 @return		Height of the specified MIP-Map level.
		*************************************************************************/
		uint32 getHeight(uint32 uiMipLevel=PVRTEX_TOPMIPLEVEL) const;

		/*!***********************************************************************
		 @brief      	Gets the depth of the user specified MIP-Map 
						level for the texture
		 @param[in]		uiMipLevel	    MIP level that user is interested in.
		 @return		Depth of the specified MIP-Map level.
		*************************************************************************/
		uint32 getDepth(uint32 uiMipLevel=PVRTEX_TOPMIPLEVEL) const;

		/*!***********************************************************************
		 @brief      	Gets the size in PIXELS of the texture, given various input 
						parameters.	User can retrieve the total size of either all 
						surfaces or a single surface, all faces or a single face and
						all MIP-Maps or a single specified MIP level. All of these
		 @param[in]		iMipLevel		Specifies a MIP level to check, 
										'PVRTEX_ALLMIPLEVELS' can be passed to get 
										the size of all MIP levels. 
		 @param[in]		bAllSurfaces	Size of all surfaces is calculated if true, 
										only a single surface if false.
		 @param[in]		bAllFaces		Size of all faces is calculated if true, 
										only a single face if false.
		 @return		Size in PIXELS of the specified texture area.
		*************************************************************************/
		uint32 getTextureSize(int32 iMipLevel=PVRTEX_ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true) const;

		/*!***********************************************************************
		 @brief      	Gets the size in BYTES of the texture, given various input 
						parameters.	User can retrieve the size of either all 
						surfaces or a single surface, all faces or a single face 
						and all MIP-Maps or a single specified MIP level.
		 @param[in]		iMipLevel		Specifies a mip level to check, 
										'PVRTEX_ALLMIPLEVELS' can be passed to get 
										the size of all MIP levels. 
		 @param[in]		bAllSurfaces	Size of all surfaces is calculated if true, 
										only a single surface if false.
		 @param[in]		bAllFaces		Size of all faces is calculated if true, 
										only a single face if false.
		 @return		Size in BYTES of the specified texture area.
		*************************************************************************/
		uint32 getDataSize(int32 iMipLevel=PVRTEX_ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true) const;

		/*!***********************************************************************
		 @brief      	Gets the number of array members stored in this texture.
		 @return		Number of array members in this texture.
		*************************************************************************/
		uint32 getNumArrayMembers() const;

		/*!***********************************************************************
		 @brief      	Gets the number of MIP-Map levels stored in this texture.
		 @return		Number of MIP-Map levels in this texture.
		*************************************************************************/
		uint32 getNumMIPLevels() const;

		/*!***********************************************************************
		 @brief      	Gets the number of faces stored in this texture.
		 @return		Number of faces in this texture.
		*************************************************************************/
		uint32 getNumFaces() const;

		/*!***********************************************************************
		 @brief      	Gets the data orientation for this texture.
		 @param[in]		axis			EPVRTAxis type specifying the axis to examine.
		 @return		Enum orientation of the axis.
		*************************************************************************/
		EPVRTOrientation getOrientation(EPVRTAxis axis) const;

		/*!***********************************************************************
		 @brief      	Returns whether or not the texture is compressed using
						PVRTexLib's FILE compression - this is independent of 
						any texture compression.
		 @return		True if it is file compressed.
		*************************************************************************/
		bool isFileCompressed() const;
				
		/*!***********************************************************************
		 @brief      	Returns whether or not the texture's colour has been
						pre-multiplied by the alpha values.
		 @return		True if texture is premultiplied.
		*************************************************************************/
		bool isPreMultiplied() const;

		/*!***********************************************************************
		 @brief      	Returns the total size of the meta data stored in the header. 
						This includes the size of all information stored in all MetaDataBlocks.
		 @return		Size, in bytes, of the meta data stored in the header.
		*************************************************************************/
		uint32 getMetaDataSize() const;

		/*!***********************************************************************
		@brief      	Gets the OpenGL equivalent values of internal format, format
						and type for this texture. This will return any supported
						OpenGL texture values, it is up to the user to decide if 
						these are valid for their current platform.
		@param[in,out]	internalformat      Internal format
		@param[in,out]	format              Format
		@param[in,out]	type                Type
		*************************************************************************/
		void getOGLFormat(uint32& internalformat, uint32& format, uint32& type) const;

		/*!***********************************************************************
		 @brief      	Gets the OpenGLES equivalent values of internal format, 
						format and type for this texture. This will return any 
						supported OpenGLES texture values, it is up to the user 
						to decide if these are valid for their current platform.
		@param[in,out]	internalformat      Internal format
		@param[in,out]	format              Format
		@param[in,out]	type                Type
		*************************************************************************/
		void getOGLESFormat(uint32& internalformat, uint32& format, uint32& type) const;

		/*!***********************************************************************
		@brief      	Gets the Vulkan equivalent values for this texture.
						This will return any supported Vulkan texture formats, it is up to
						the user to decide if these are valid for their current platform.
		@return			VkFormat, represented by a uint32.
		*************************************************************************/
		uint32 getVulkanFormat() const;

		/*!***********************************************************************
		 @brief      	Gets the D3DFormat (up to DirectX 9 and Direct 3D Mobile)
						equivalent values for this texture. This will return any 
						supported D3D texture formats, it is up to the user to
						decide if this is valid for their current platform.
		 @return		D3D format, represented by an uint32.
		*************************************************************************/
		uint32 getD3DFormat() const;
		
		/*!***********************************************************************
		 @brief      	Gets the DXGIFormat (DirectX 10 onward) equivalent values 
						for this texture. This will return any supported DX texture
						formats, it is up to the user to decide if this is valid 
						for their current platform.
		 @return		DXGIFormat, represented by a uint32. 
		*************************************************************************/
		uint32 getDXGIFormat() const;

	// Accessor Methods for a texture's properties - setters.

		/*!***********************************************************************
		 @brief      	Sets the pixel format for this texture.
		 @param[in]		uPixelFormat	The format of the pixel.
		*************************************************************************/
		void setPixelFormat(PixelType uPixelFormat);

		/*!***********************************************************************
		 @brief      	Sets the colour space for this texture. Default is lRGB.
		 @param[in]		eColourSpace	A colour space enum.
		*************************************************************************/
		void setColourSpace(EPVRTColourSpace eColourSpace);

		/*!***********************************************************************
		 @brief      	Sets the variable type for the channels in this texture.
		 @param[in]		eVarType	    A variable type enum.
		*************************************************************************/
		void setChannelType(EPVRTVariableType eVarType);

		/*!***********************************************************************
		 @brief      	Sets the format of the texture to PVRTexLib's internal
						representation of the OGL format.
		@param[in,out]	internalformat      Internal format
		@param[in,out]	format              Format
		@param[in,out]	type                Type
		 @return		True if successful.
		*************************************************************************/
		bool setOGLFormat(const uint32& internalformat, const uint32& format, const uint32& type);

		/*!***********************************************************************
		 @brief      	Sets the format of the texture to PVRTexLib's internal
						representation of the OGLES format.
		@param[in,out]	internalformat      Internal format
		@param[in,out]	format              Format
		@param[in,out]	type                Type
		 @return		True if successful.
		*************************************************************************/
		bool setOGLESFormat(const uint32& internalformat, const uint32& format, const uint32& type);

		/*!***********************************************************************
		 @brief      	Sets the format of the texture to PVRTexLib's internal
						representation of the D3D format.
		 @return		True if successful.
		*************************************************************************/
		bool setD3DFormat(const uint32& DWORD_D3D_FORMAT);
		
		/*!***********************************************************************
		 @brief      	Sets the format of the texture to PVRTexLib's internal
						representation of the DXGI format.
		 @return		True if successful.
		*************************************************************************/
		bool setDXGIFormat(const uint32& DWORD_DXGI_FORMAT);

		/*!***********************************************************************
		 @brief      	Sets the width.
		 @param[in]		newWidth	The new width.
		*************************************************************************/
		void setWidth(uint32 newWidth);

		/*!***********************************************************************
		 @brief      	Sets the height.
		 @param[in]		newHeight	The new height.
		*************************************************************************/
		void setHeight(uint32 newHeight);

		/*!***********************************************************************
		 @brief      	Sets the depth.
		 @param[in]		newDepth	The new depth.
		*************************************************************************/
		void setDepth(uint32 newDepth);

		/*!***********************************************************************
		 @brief      	Sets the depth.
		 @param[in]		newNumMembers	The new number of members in this array.
		*************************************************************************/
		void setNumArrayMembers(uint32 newNumMembers);

		/*!***********************************************************************
		 @brief      	Sets the number of MIP-Map levels in this texture.
		 @param[in]		newNumMIPLevels		New number of MIP-Map levels.
		*************************************************************************/
		void setNumMIPLevels(uint32 newNumMIPLevels);

		/*!***********************************************************************
		 @brief      	Sets the number of faces stored in this texture.
		 @param[in]		newNumFaces     New number of faces for this texture.
		*************************************************************************/
		void setNumFaces(uint32 newNumFaces);

		/*!***********************************************************************
		 @brief      	Sets the data orientation for a given axis in this texture.
		 @param[in]		eAxisOrientation    Enum specifying axis and orientation.
		*************************************************************************/
		void setOrientation(EPVRTOrientation eAxisOrientation);

		/*!***********************************************************************
		 @brief      	Sets whether or not the texture is compressed using
						PVRTexLib's FILE compression - this is independent of 
						any texture compression. Currently unsupported.
		 @param[in]		isFileCompressed	Sets file compression to true/false.
		*************************************************************************/
		void setIsFileCompressed(bool isFileCompressed);
		
		/*!***********************************************************************
		 @brief      	Sets whether or not the texture's colour has been
						pre-multiplied by the alpha values.
		 @return		isPreMultiplied	    Sets if texture is premultiplied.
		*************************************************************************/
		void setIsPreMultiplied(bool isPreMultiplied);

	// Meta Data functions - Getters.	

		/*!***********************************************************************
		 @brief      	Returns whether the texture is a bump map or not.
		 @return		True if the texture is a bump map.
		*************************************************************************/
		bool isBumpMap() const;

		/*!***********************************************************************
		 @brief      	Gets the bump map scaling value for this texture. 
         @details       If the texture is not a bump map, 0.0f is returned. If the
						texture is a bump map but no meta data is stored to
						specify its scale, then 1.0f is returned.
		 @return		Returns the bump map scale value as a float.
		*************************************************************************/
		float getBumpMapScale() const;

		/*!***********************************************************************
		 @brief      	Gets the bump map channel order relative to rgba. 
         @details       For	example, an RGB texture with bumps mapped to XYZ returns 
						'xyz'. A BGR texture with bumps in the order ZYX will also 
						return 'xyz' as the mapping is the same: R=X, G=Y, B=Z.
						If the letter 'h' is present in the string, it means that
						the height map has been stored here.
						Other characters are possible if the bump map was created
						manually, but PVRTexLib will ignore these characters. They
						are returned simply for completeness.
		 @return		Bump map order relative to rgba.
		*************************************************************************/
		CPVRTString getBumpMapOrder() const;

		/*!***********************************************************************
		 @brief      	Works out the number of possible texture atlas members in
						the texture based on the w/h/d and the data size.
		 @return		The number of sub textures defined by meta data.
		*************************************************************************/
		int getNumTextureAtlasMembers() const;

		/*!***********************************************************************
		 @brief      	Returns a pointer to the texture atlas data.
		 @return		A pointer directly to the texture atlas data.
		*************************************************************************/
		const float* getTextureAtlasData() const;

		/*!***********************************************************************
		 @brief      	Gets the cube map face order. 
         @details       Returned string will be in the form "ZzXxYy" with capitals 
                        representing positive and small letters representing 
                        negative. I.e. Z=Z-Positive, z=Z-Negative.
		 @return		Cube map order string.
		*************************************************************************/
		CPVRTString getCubeMapOrder() const;

		/*!***********************************************************************
		 @brief      	Obtains the border size in each dimension for this texture.
		 @param[in]		uiBorderWidth   Border width
		 @param[in]		uiBorderHeight  Border height
		 @param[in]		uiBorderDepth   Border depth
		*************************************************************************/
		void getBorder(uint32& uiBorderWidth, uint32& uiBorderHeight, uint32& uiBorderDepth) const;

		/*!***********************************************************************
		 @brief      	Returns a block of meta data from the texture. If the meta 
                        data doesn't exist, a block with data size 0 will be returned.
		 @param[in]		DevFOURCC       Four character descriptor representing the 
                                        creator of the meta data
		 @param[in]		u32Key          Key value representing the type of meta 
                                        data stored
		 @return		A copy of the meta data from the texture.
		*************************************************************************/
		MetaDataBlock getMetaData(uint32 DevFOURCC, uint32 u32Key) const;

		/*!***********************************************************************
		 @brief      	Returns whether or not the specified meta data exists as 
						part of this texture header.
		 @param[in]		DevFOURCC       Four character descriptor representing the 
                                        creator of the meta data
		 @param[in]		u32Key          Key value representing the type of meta 
                                        data stored
		 @return		True if the specified meta data bock exists
		*************************************************************************/
		bool hasMetaData(uint32 DevFOURCC, uint32 u32Key) const;

		/*!***********************************************************************
		 @brief      	A pointer directly to the Meta Data Map, to allow users to read out data.
		 @return		A direct pointer to the MetaData map.
		*************************************************************************/
		const MetaDataMap* getMetaDataMap() const;

	// Meta Data functions - Setters.
		
		/*!***********************************************************************
		 @brief      	Sets a texture's bump map data.
		 @param[in]		bumpScale	Floating point "height" value to scale the bump map.
		 @param[in]		bumpOrder	Up to 4 character string, with values x,y,z,h in 
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
		void setBumpMap(float bumpScale, CPVRTString bumpOrder="xyz");

		/*!***********************************************************************
		 @brief      	Sets the texture atlas coordinate meta data for later display.
						It is up to the user to make sure that this texture atlas
						data actually makes sense in the context of the header. It is
						suggested that the "generateTextureAtlas" method in the tools
						is used to create a texture atlas, manually setting one up is 
						possible but should be done with care.
		 @param[in]		pAtlasData	    Pointer to an array of atlas data.
		 @param[in]		dataSize	    Number of floats that the data pointer contains.
		*************************************************************************/
		void setTextureAtlas(float* pAtlasData, uint32 dataSize);

		/*!***********************************************************************
		 @brief      	Sets a texture's bump map data.
		 @param[in]		cubeMapOrder	Up to 6 character string, with values 
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
		void setCubeMapOrder(CPVRTString cubeMapOrder="XxYyZz");

		/*!***********************************************************************
		 @brief      	Sets a texture's border size data. This value is subtracted 
						from the current texture height/width/depth to get the valid 
						texture data.
		 @param[in]		uiBorderWidth   Border width
		 @param[in]		uiBorderHeight  Border height
		 @param[in]		uiBorderDepth   Border depth
		*************************************************************************/
		void setBorder(uint32 uiBorderWidth, uint32 uiBorderHeight, uint32 uiBorderDepth);

		/*!***********************************************************************
		 @brief      	Adds an arbitrary piece of meta data.
		 @param[in]		MetaBlock	    Meta data block to be added.
		*************************************************************************/
		void addMetaData(const MetaDataBlock& MetaBlock);
				
		/*!***********************************************************************
		 @brief      	Removes a specified piece of meta data, if it exists.
		 @param[in]		DevFOURCC       Four character descriptor representing the 
                                        creator of the meta data
		 @param[in]		u32Key          Key value representing the type of meta 
                                        data stored
		*************************************************************************/
		void removeMetaData(const uint32& DevFOURCC, const uint32& u32Key);
	};
}

#endif
