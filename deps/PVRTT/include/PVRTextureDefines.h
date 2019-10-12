/*!***********************************************************************

 @file         PVRTextureDefines.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        Method, template and type defines for PVRTexture.

*************************************************************************/

#ifndef _PVRTEXTURE_DEFINES_H
#define _PVRTEXTURE_DEFINES_H

//To use the PVRTexLib .dll on Windows, you need to define _WINDLL_IMPORT
#ifndef PVR_DLL
#if defined(_WINDLL_EXPORT)
#define PVR_DLL __declspec(dllexport)
//Forward declaration of various classes/structs used by this library. This exports their interfaces for DLLs.
struct PVR_DLL PVRTextureHeaderV3;
struct PVR_DLL MetaDataBlock;
template <typename KeyType, typename DataType>
class PVR_DLL CPVRTMap;
template<typename T>
class PVR_DLL CPVRTArray;
class PVR_DLL CPVRTString;
#elif defined(_WINDLL_IMPORT)
#define PVR_DLL __declspec(dllimport)
//Forward declaration of various classes/structs used by this library. This imports their interfaces for DLLs.
struct PVR_DLL PVRTextureHeaderV3;
struct PVR_DLL MetaDataBlock;
template <typename KeyType, typename DataType>
class PVR_DLL CPVRTMap;
template<typename T>
class PVR_DLL CPVRTArray;
class PVR_DLL CPVRTString;
#else

/*!***********************************************************************
 @def       PVR_DLL
 @brief     Required to use PVRTexLib.dll on Windows.
*************************************************************************/
#define PVR_DLL
#endif
#endif

#include "PVRTTexture.h"

/*!***********************************************************************
 @namespace pvrtexture
 @brief     PVRTexture namespace. Contains methods and classes for PVRTexLib.
*************************************************************************/
namespace pvrtexture
{
	// Type defines for standard variable sizes.
    
	typedef	signed char			int8;       //!< Signed 8 bit integer
	typedef	signed short		int16;      //!< Signed 16 bit integer
	typedef	signed int			int32;      //!< Signed 32 bit integer
	typedef	signed long long    int64;      //!< Signed 64 bit integer
	typedef unsigned char		uint8;      //!< Unsigned 8 bit integer
	typedef unsigned short		uint16;     //!< Unsigned 16 bit integer
	typedef unsigned int		uint32;     //!< Unsigned 32 bit integer
	typedef	unsigned long long	uint64;     //!< Unsigned 64 bit integer
	
	// Texture related constants and enumerations. 
    
    /*!***********************************************************************
     @enum      ECompressorQuality
     @brief     Quality level to compress the texture with. Currently valid with
                ETC and PVRTC formats.
    *************************************************************************/
	enum ECompressorQuality
	{
		ePVRTCFastest=0,        //!< PVRTC fastest
		ePVRTCFast,             //!< PVRTC fast
		ePVRTCNormal,           //!< PVRTC normal
		ePVRTCHigh,             //!< PVRTC high
		ePVRTCBest,             //!< PVRTC best
		eNumPVRTCModes,         //!< Number of PVRTC modes

		eETCFast=0,             //!< ETC fast
		eETCFastPerceptual,     //!< ETC fast perceptual
		eETCSlow,               //!< ETC slow
		eETCSlowPerceptual,     //!< ETC slow perceptual
		eNumETCModes,           //!< Number of ETC modes

		eASTCVeryFast=0,        //!< ASTC very fast
		eASTCFast,              //!< ASTC fast
		eASTCMedium,            //!< ASTC medium
		eASTCThorough,          //!< ASTC thorough
		eASTCExhaustive,        //!< ASTC exhaustive
		eNumASTCModes           //!< Number of ASTC modes
	};
    
    /*!***********************************************************************
     @enum      EResizeMode
     @brief     Texture resize mode
    *************************************************************************/
	enum EResizeMode
	{
		eResizeNearest,         //!< Nearest filtering
		eResizeLinear,          //!< Linear filtering 
		eResizeCubic,           //!< Cubic filtering, uses Catmull-Rom splines.
		eNumResizeModes         //!< Number of resize modes
	};

	/*!***********************************************************************
     @enum      ELegacyApi
     @brief     Legacy API enum.
    *************************************************************************/
	enum ELegacyApi
	{
		eOGLES=1,               //!< OpenGL ES 1.x
		eOGLES2,                //!< OpenGL ES 2.0
		eD3DM,                  //!< Direct 3D M
		eOGL,                   //!< Open GL
		eDX9,                   //!< DirextX 9
		eDX10,                  //!< DirectX 10
		eOVG,                   //!< Open VG
		eMGL,                   //!< MGL
	};

	// Useful macros.
    /*!***************************************************************************
	 @def           TEXOFFSET2D
     @brief         2D texture offset
	*****************************************************************************/
	#define TEXOFFSET2D(x,y,width) ( ((x)+(y)*(width)) )
    
    /*!***************************************************************************
	 @def          TEXOFFSET3D
     @brief        3D texture offset
	*****************************************************************************/
	#define TEXOFFSET3D(x,y,z,width,height) ( ((x)+(y)*(width)+(z)*(width)*(height)) )

	/*!***************************************************************************
	 @typedef       MetaDataMap
     @brief         Useful typedef for generating maps of MetaData blocks.
	*****************************************************************************/
	typedef CPVRTMap<uint32, CPVRTMap<uint32,MetaDataBlock> > MetaDataMap;
}
#endif //_PVRTEXTURE_DEFINES_H
