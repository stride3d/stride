/*!***********************************************************************

 @file         PVRTexture.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        Contains methods concerning basic CPVRTexture creation, saving
               and data duplication. This is the main class for PVRTexLib. 

*************************************************************************/

/*****************************************************************************/
/*! @mainpage PVRTexLib
******************************************************************************

 @section overview Overview
*****************************

PVRTexLib is a library for the management of textures. It occupies the @ref pvrtexture 
namespace and allows users to access the PVRTexTool functionality in a library, 
for easy integration with existing tool chains. 

PVRTexLib contains the facility to:
 \li Load and save PVR files.
 \li Transcode to and from many different texture formats.
 \li Perform a variety of pre-process techniques on decompressed pixel data.
 \li Provide information about a texture file loaded by the library.

 @section pvrtools PVRTools
*****************************
A number of header files from the PowerVR SDK Tools libraries are present in PVRTexLib:
 \li PVRTGlobal.h
 \li PVRTError.h
 \li PVRTArray.h
 \li PVRTMap.h
 \li PVRTString.h
 \li PVRTTexture.h
 
These header files are included in the PVRTexLib package so that separate installation 
of the tools libraries is not required.  If PowerVR Graphics SDK Tools are installed, 
documentation for the these files can be found in the ‘Tools’ folder 
in the PowerVR Insider SDK directory.

*/

#ifndef _PVRTEXTURE_H
#define _PVRTEXTURE_H

#include <iostream>

#include "PVRTextureHeader.h"
#include "PVRTString.h"

namespace pvrtexture
{
    /*!***********************************************************************
     @class         CPVRTexture
     @brief         A full public texture container format, with support for custom 
                    meta-data, and complete, optimised, resource loading code into PVRTools.                    
    *************************************************************************/
	class PVR_DLL CPVRTexture : public CPVRTextureHeader
	{		
	public:
	// Construction methods for a texture.
    
		/*!***********************************************************************
		 @brief      	Creates a new empty texture
		 @return		A new CPVRTexture.
		*************************************************************************/
		CPVRTexture();
        
		/*!***********************************************************************
		 @brief      	Creates a new texture based on a texture header, 
						pre-allocating the correct amount of memory. If data is
						supplied, it will be copied into memory.
		 @param[in]		sHeader     Texture header
		 @param[in]		pData       Texture data
		 @return		A new CPVRTexture.
		*************************************************************************/
		CPVRTexture(const CPVRTextureHeader& sHeader, const void* pData=NULL);

		/*!***********************************************************************
		 @brief      	Creates a new texture from a filepath.
		 @param[in]		szFilePath  File path to existing texture
		 @return		A new CPVRTexture.
		*************************************************************************/
		CPVRTexture(const char* szFilePath);

		/*!***********************************************************************
		 @brief      	Creates a new texture from a pointer that includes a header
						structure, meta data and texture data as laid out in a file.
						This functionality is primarily for user-defined file loading.
						Header may be any version of pvr.
		 @param[in]		pTexture    Pointer to texture data
		 @return		A new CPVRTexture.
		*************************************************************************/
		CPVRTexture( const void* pTexture );

		/*!***********************************************************************
		 @brief      	Creates a new texture as a copy of another.
		 @param[in]		texture     Texture to copy
		 @return		A new CPVRTexture
		*************************************************************************/
		CPVRTexture(const CPVRTexture& texture);

		/*!***********************************************************************
		 @brief      	Deconstructor for CPVRTextures.
		*************************************************************************/
		~CPVRTexture();

		/*!***********************************************************************
		 @brief      	Will copy the contents and information of another texture into this one.
		 @param[in]		rhs         Texture to copy
		 @return		This texture.
		*************************************************************************/
		CPVRTexture& operator=(const CPVRTexture& rhs);
		
	// Texture accessor functions - others are inherited from CPVRTextureHeader.
    
		/*!***********************************************************************
		 @brief      	Returns a pointer into the texture's data. 
		 @details		It is possible to specify an offset to specific array members, 
						faces and MIP Map levels.
		 @param[in]		uiMIPLevel      Offset to MIP Map levels
		 @param[in]		uiArrayMember   Offset to array members
		 @param[in]		uiFaceNumber    Offset to face numbers
		 @return		Pointer to a location in the texture.
		*************************************************************************/
		void* getDataPtr(uint32 uiMIPLevel = 0, uint32 uiArrayMember = 0, uint32 uiFaceNumber = 0) const;

		/*!***********************************************************************
		 @brief      	Gets the header for this texture, allowing you to create a new
						texture based on this one with some changes. Useful for passing
						information about a texture without passing all of its data.
		 @return		Returns the header only for this texture.
		*************************************************************************/
		const CPVRTextureHeader& getHeader() const;

	// File io.

		/*!***********************************************************************
		 @brief      	When writing the texture out to a PVR file, it is often
						desirable to pad the meta data so that the start of the
						texture data aligns to a given boundary.
		 @details		This function pads to a boundary value equal to "uiPadding".
						For example setting uiPadding=8 will align the start of the
						texture data to an 8 byte boundary.
						Note - this should be called immediately before saving as
						the value is worked out based on the current meta data size.
		 @param[in]		uiPadding       Padding boundary value
		*************************************************************************/
		void addPaddingMetaData( uint32 uiPadding );

		/*!***********************************************************************
		 @brief      	Writes out to a file, given a filename and path. 
		 @details		File type will be determined by the extension present in the string. 
						If no extension is present, PVR format will be selected. 
						Unsupported formats will result in failure.
		 @param[in]		filepath        File path to write to
		 @return		True if the method succeeds.
		*************************************************************************/
		bool saveFile(const CPVRTString& filepath) const;
	
		/*!***********************************************************************
		 @brief      	Writes out to a file, stripping any extensions specified
						and appending .pvr. This function is for legacy support only
						and saves out to PVR Version 2 file. The target api must be
						specified in order to save to this format.
		 @param[in]	    filepath        File path to write to
		 @param[in]		eApi            Target API
		 @return		True if the method succeeds.
		*************************************************************************/
		bool saveFileLegacyPVR(const CPVRTString& filepath, ELegacyApi eApi) const;

		/*!***********************************************************************
		@brief      	Saves an ASTC File.
		@param[in]	    filepath        File path to write to
		@return			True if the method succeeds.
		*************************************************************************/
		bool saveASTCFile(const CPVRTString& filepath) const;

		/*!***********************************************************************
		@brief      	Convert texture to KTX
		@param[in]	    out        Output stream for KTX data
		*************************************************************************/
		void toKTX(std::ostream & out) const;

		/*!***********************************************************************
		@brief      	Convert texture to ASTC
		@param[in]	    out        Output stream for ASTC data
		*************************************************************************/
		void toASTC(std::ostream & out) const;

		/*!***********************************************************************
		@brief      	Makes a PVRTexture from a KTX file stream
		@param[in]	    in        The KTX file stream
		@return			The texture
		*************************************************************************/
		static CPVRTexture fromKTX(std::istream & in);

		/*!***********************************************************************
		@brief      	Makes a PVRTexture from a pointer to an ASTC file in memory
		@param[in]	    in        The ASTC file stream
		@return			The texture
		*************************************************************************/
		static CPVRTexture fromASTC(std::istream & in);

	private:
		size_t	m_stDataSize;		//!< Size of the texture data.
		uint8*	m_pTextureData;		//!< Pointer to texture data.

	// Private IO functions
    
		/*!***********************************************************************
		 @brief      	Loads a PVR file.
		 @param[in]		pTextureFile    PVR texture file
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateLoadPVRFile(FILE* pTextureFile);

		/*!***********************************************************************
		 @brief      	Saves a PVR File.
		 @param[in]		pTextureFile    PVR texture file
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateSavePVRFile(FILE* pTextureFile) const;

		/*!***********************************************************************
		 @brief      	Loads a KTX file.
		 @param[in]		pTextureFile    KTX texture file
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateLoadKTXFile(FILE* pTextureFile);
		bool privateLoadKTXFile(std::istream & in);

		/*!***********************************************************************
		 @brief      	Saves a KTX File.
		 @param[in]		pTextureFile    KTX texture file
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateSaveKTXFile(FILE* pTextureFile) const;

		/*!***********************************************************************
		 @brief      	Loads a DDS file.
		 @param[in]		pTextureFile    DDS texture file
		 @return		True if the method succeeds.
		 *************************************************************************/
		bool privateLoadDDSFile(FILE* pTextureFile);

		/*!***********************************************************************
		 @brief      	Saves a DDS File.
		 @param[in]		pTextureFile    DDS texture file
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateSaveDDSFile(FILE* pTextureFile) const;

		/*!***********************************************************************
		@brief      	Loads an ASTC file.
		@param[in]		pTextureFile    ASTC texture file
		@return			True if the method succeeds.
		*************************************************************************/
		bool privateLoadASTCFile(FILE* pTextureFile);
		bool privateLoadASTCFile(std::istream & in);

		/*!***********************************************************************
		@brief      	Saves an ASTC file.
		@param[in]		pTextureFile    ASTC texture file
		@return			True if the method succeeds.
		*************************************************************************/
		bool privateSaveASTCFile(FILE* pTextureFile) const;

	//Legacy IO
    
		/*!***********************************************************************
		 @brief      	Saves a .h File. Legacy operator
		 @param[in]		pTextureFile    PVR texture file
		 @param[in]		filename        File path to write to
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateSaveCHeaderFile(FILE* pTextureFile, CPVRTString filename) const;

		/*!***********************************************************************
		 @brief      	Saves a legacy PVR File - Uses version 2 file format.
		 @param[in]		pTextureFile    PVR texture file
		 @param[in]		eApi            Target API
		 @return		True if the method succeeds.
		*************************************************************************/
		bool privateSaveLegacyPVRFile(FILE* pTextureFile, ELegacyApi eApi) const;
	};
}

#endif //_PVRTEXTURE_H

