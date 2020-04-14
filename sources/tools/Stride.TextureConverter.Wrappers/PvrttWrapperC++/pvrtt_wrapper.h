// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#ifndef PVRTT_WRAPPER_H
#define PVRTT_WRAPPER_H

#define PVRTT_API __declspec(dllexport)

//#include <PVRTString.h>
#include <PVRTexture.h>

typedef class pvrtexture::CPVRTextureHeader PvrttTextureHeader;
typedef class pvrtexture::CPVRTexture PvrttTexture;

typedef enum
{
	PVRTT_Standard8PixelType,
	PVRTT_tandard16PixelType,
	PVRTT_Standard32PixelType,
} PvrttPixelType;

extern "C" {

	// CPVRTextureHeader class
	PVRTT_API PvrttTextureHeader * pvrttCreateTextureHeaderEmpty();
	PVRTT_API PvrttTextureHeader * pvrttCopyTextureHeader(const PvrttTextureHeader * headerIn);
	PVRTT_API PvrttTextureHeader * pvrttCreateTextureHeader(PvrttPixelType pixelFormat, int height, int width, int depth, int numMipMaps,int numArrayMembers, int numFaces, EPVRTColourSpace eColourSpace, EPVRTVariableType eChannelType, bool	bPreMultiplied);
	PVRTT_API PvrttTextureHeader * pvrttCreateTextureHeaderFromCompressedTexture(pvrtexture::uint64 pixelFormat, int height, int width, int depth, int numMipMaps, int numArrayMembers, int numFaces, EPVRTColourSpace eColourSpace, EPVRTVariableType eChannelType, bool	bPreMultiplied);
	PVRTT_API pvrtexture::uint32 pvrttGetWidth(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel);
	PVRTT_API pvrtexture::uint32 pvrttGetHeight(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel);
	PVRTT_API void pvrttSetWidth(PvrttTextureHeader * header, pvrtexture::uint32 newWidth);
	PVRTT_API void pvrttSetHeight(PvrttTextureHeader * header, pvrtexture::uint32 newHeight);
	PVRTT_API void pvrttSetPixelFormat(PvrttTextureHeader * header, PvrttPixelType pixelFormat);
	PVRTT_API pvrtexture::uint32 pvrttGetDataSize(PvrttTextureHeader * header, int iMipLevel, bool bAllSurfaces, bool bAllFaces);
	PVRTT_API pvrtexture::uint32 pvrttGetTextureSize(PvrttTextureHeader * header, int iMipLevel, bool bAllSurfaces, bool bAllFaces);
	PVRTT_API pvrtexture::uint32 pvrttGetNumMIPLevels(PvrttTextureHeader * header);
	PVRTT_API void pvrttSetNumMIPLevels(PvrttTextureHeader * header, int newNumMIPLevels);
	PVRTT_API pvrtexture::uint32 pvrttGetDepth(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel);
	PVRTT_API pvrtexture::uint32 pvrttGetBPP(PvrttTextureHeader * header);
	PVRTT_API pvrtexture::uint32 pvrttGetNumArrayMembers(PvrttTextureHeader * header);
	PVRTT_API pvrtexture::uint32 pvrttGetNumFaces(PvrttTextureHeader * header);
	PVRTT_API bool pvrttIsFileCompressed(PvrttTextureHeader * header);
	PVRTT_API pvrtexture::uint64 pvrttGetPixelType(PvrttTextureHeader * header);
	PVRTT_API pvrtexture::uint32 pvrttGetMetaDataSize(PvrttTextureHeader * header);
	PVRTT_API EPVRTVariableType pvrttGetChannelType(PvrttTextureHeader * header);
	PVRTT_API EPVRTColourSpace pvrttGetColourSpace(PvrttTextureHeader * header);

	// CPVRTexture class
	PVRTT_API PvrttTexture * pvrttCreateTexture();
	PVRTT_API PvrttTexture * pvrttCreateTextureFromHeader(PvrttTextureHeader* sHeader, const void* pData);
	PVRTT_API PvrttTexture * pvrttCreateTextureFromFile(const char* szFilePath);
	PVRTT_API PvrttTexture * pvrttCreateTextureFromMemory(const void* pTexture );
	PVRTT_API void pvrttDestroyTexture(PvrttTexture * texture);
	PVRTT_API bool pvrttSaveFile(PvrttTexture * texture, const char* filePath);
	PVRTT_API const PvrttTextureHeader * pvrttGetHeader(PvrttTexture * texture);
	PVRTT_API void* pvrttGetDataPtr(PvrttTexture * texture, pvrtexture::uint32 uiMIPLevel, pvrtexture::uint32 uiArrayMember, pvrtexture::uint32 uiFaceNumber);

	// Utilities
	PVRTT_API bool pvrttGenerateMIPMaps(PvrttTexture& texture, const pvrtexture::EResizeMode eFilterMode, int uiMIPMapsToDo);
	PVRTT_API bool pvrttTranscodeWithNoConversion(PvrttTexture& texture, const PvrttPixelType ptFormat, const EPVRTVariableType eChannelType, const EPVRTColourSpace eColourspace, const pvrtexture::ECompressorQuality eQuality, const bool bDoDither);
	PVRTT_API bool pvrttTranscode(PvrttTexture& texture, pvrtexture::uint64 ptFormat, const EPVRTVariableType eChannelType, const EPVRTColourSpace eColourspace, const pvrtexture::ECompressorQuality eQuality, const bool bDoDither);
	//PVRTT_API int pvrttDecompressPVRTC(const void *pCompressedData, const int Do2bitMode, const int XDim, const int YDim, unsigned char* pResultImage);
	//PVRTT_API int pvrttDecompressETC(const void * const pSrcData, const unsigned int &x, const unsigned int &y, void *pDestData, const int &nMode);
	PVRTT_API bool pvrttCopyChannels(PvrttTexture& sTexture, const PvrttTexture& sTextureSource, pvrtexture::uint32 uiNumChannelCopies, pvrtexture::EChannelName *eChannels, pvrtexture::EChannelName *eChannelsSource);
	PVRTT_API bool pvrttResize(PvrttTexture& sTexture, const pvrtexture::uint32& u32NewWidth, const pvrtexture::uint32& u32NewHeight, const pvrtexture::uint32& u32NewDepth, const pvrtexture::EResizeMode eResizeMode);
	PVRTT_API bool pvrttFlip(PvrttTexture& sTexture, const EPVRTAxis eFlipDirection);
	PVRTT_API bool pvrttGenerateNormalMap(PvrttTexture& sTexture, const float fScale, const char* sChannelOrder);
	PVRTT_API pvrtexture::uint64 pvrttConvertPixelType(PvrttPixelType pixelFormat);
	PVRTT_API bool pvrttPreMultipliedAlpha(PvrttTexture& sTexture);
} // extern "C"



#endif // PVRTT_WRAPPER_H
