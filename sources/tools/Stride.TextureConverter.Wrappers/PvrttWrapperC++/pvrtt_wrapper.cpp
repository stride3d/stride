// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "pvrtt_wrapper.h"
#include <PVRTString.h>
#include <PVRTextureUtilities.h>
#include <PVRTDecompress.h>

//#include <stdio.h>
// intern conversion class
pvrtexture::PixelType convertPixelType(PvrttPixelType pixelFormat)
{
	switch(pixelFormat)
	{
	default:
	case PVRTT_Standard8PixelType:
		return pvrtexture::PixelType('r','g','b','a',8,8,8,8);
		break;
	case PVRTT_tandard16PixelType:
		return pvrtexture::PixelType('r','g','b','a',16,16,16,16);
		break;
	case PVRTT_Standard32PixelType:
		return pvrtexture::PixelType('r','g','b','a',32,32,32,32);
		break;
	}
}

pvrtexture::uint64 pvrttConvertPixelType(PvrttPixelType pixelFormat)
{
	switch(pixelFormat)
	{
	default:
	case PVRTT_Standard8PixelType:
		return pvrtexture::PixelType('r','g','b','a',8,8,8,8).PixelTypeID;
		break;
	case PVRTT_tandard16PixelType:
		return pvrtexture::PixelType('r','g','b','a',16,16,16,16).PixelTypeID;
		break;
	case PVRTT_Standard32PixelType:
		return pvrtexture::PixelType('r','g','b','a',32,32,32,32).PixelTypeID;
		break;
	}
}


// CPVRTextureHeader class
PvrttTextureHeader * pvrttCreateTextureHeaderEmpty()
{
	return new pvrtexture::CPVRTextureHeader();
}

PvrttTextureHeader * pvrttCreateTextureHeader(PvrttPixelType pixelFormat, int height=1, int width=1, int depth=1, int numMipMaps=1, int numArrayMembers=1, int numFaces=1, EPVRTColourSpace eColourSpace=ePVRTCSpacelRGB, EPVRTVariableType eChannelType=ePVRTVarTypeUnsignedByteNorm, bool	bPreMultiplied=false)
{
	return new PvrttTextureHeader(convertPixelType(pixelFormat).PixelTypeID, height, width, depth, numMipMaps, numArrayMembers, numFaces, eColourSpace, eChannelType, bPreMultiplied);
}

PvrttTextureHeader * pvrttCreateTextureHeaderFromCompressedTexture(pvrtexture::uint64 pixelFormat, int height=1, int width=1, int depth=1, int numMipMaps=1, int numArrayMembers=1, int numFaces=1, EPVRTColourSpace eColourSpace=ePVRTCSpacelRGB, EPVRTVariableType eChannelType=ePVRTVarTypeUnsignedByteNorm, bool	bPreMultiplied=false)
{
	return new PvrttTextureHeader(pixelFormat, height, width, depth, numMipMaps, numArrayMembers, numFaces, eColourSpace, eChannelType, bPreMultiplied);
}

PvrttTextureHeader * pvrttCopyTextureHeader(const PvrttTextureHeader * headerIn)
{
	PvrttTextureHeader * headerOut = new PvrttTextureHeader();
	*headerOut = *headerIn;
	return headerOut;
}

pvrtexture::uint32 pvrttGetWidth(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel=PVRTEX_TOPMIPLEVEL)
{
	return header->getWidth(uiMipLevel);
}

pvrtexture::uint32 pvrttGetHeight(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel=PVRTEX_TOPMIPLEVEL)
{
	return header->getHeight(uiMipLevel);
}

void pvrttSetWidth(PvrttTextureHeader * header, pvrtexture::uint32 newWidth)
{
	header->setWidth(newWidth);
}

void pvrttSetHeight(PvrttTextureHeader * header, pvrtexture::uint32 newHeight)
{
	header->setWidth(newHeight);
}

void pvrttSetPixelFormat(PvrttTextureHeader * header, PvrttPixelType pixelFormat)
{
	header->setPixelFormat(convertPixelType(pixelFormat));
}

pvrtexture::uint32 pvrttGetDataSize(PvrttTextureHeader * header, int iMipLevel=PVRTEX_ALLMIPLEVELS, bool bAllSurfaces=true, bool bAllFaces=true)
{
	return header->getDataSize(iMipLevel, bAllSurfaces, bAllFaces);
}

pvrtexture::uint32 pvrttGetTextureSize(PvrttTextureHeader * header, int iMipLevel, bool bAllSurfaces, bool bAllFaces)
{
	return header->getTextureSize();
}

pvrtexture::uint32 pvrttGetNumMIPLevels(PvrttTextureHeader * header)
{
	return header->getNumMIPLevels();
}

void pvrttSetNumMIPLevels(PvrttTextureHeader * header, int newNumMIPLevels)
{
	header->setNumMIPLevels(newNumMIPLevels);
}

pvrtexture::uint32 pvrttGetDepth(PvrttTextureHeader * header, pvrtexture::uint32 uiMipLevel = PVRTEX_TOPMIPLEVEL)
{
	return header->getDepth(uiMipLevel);
}

pvrtexture::uint32 pvrttGetBPP(PvrttTextureHeader * header)
{
	return header->getBitsPerPixel();
}

pvrtexture::uint32 pvrttGetNumArrayMembers(PvrttTextureHeader * header)
{
	return header->getNumArrayMembers();
}

pvrtexture::uint32 pvrttGetNumFaces(PvrttTextureHeader * header)
{
	return header->getNumFaces();
}

bool pvrttIsFileCompressed(PvrttTextureHeader * header)
{
	return header->isFileCompressed();
}

pvrtexture::uint64 pvrttGetPixelType(PvrttTextureHeader * header)
{
	return header->getPixelType().PixelTypeID;
}

pvrtexture::uint32 pvrttGetMetaDataSize(PvrttTextureHeader * header)
{
	return header->getMetaDataSize();
}

EPVRTVariableType pvrttGetChannelType(PvrttTextureHeader * header)
{
	return header->getChannelType();
}

EPVRTColourSpace pvrttGetColourSpace(PvrttTextureHeader * header)
{
	return header->getColourSpace();
}



// CPVRTexture class
PvrttTexture * pvrttCreateTexture()
{
	return new pvrtexture::CPVRTexture();
}

PvrttTexture * pvrttCreateTextureFromHeader(PvrttTextureHeader* sHeader, const void* pData=NULL)
{
	if(pData == 0) pData=NULL;
	return new pvrtexture::CPVRTexture(*sHeader, pData);
}

PvrttTexture * pvrttCreateTextureFromFile(const char* szFilePath)
{
	return new pvrtexture::CPVRTexture(szFilePath);
}

PvrttTexture * pvrttCreateTextureFromMemory(const void* pTexture )
{
	return new pvrtexture::CPVRTexture(pTexture);
}

/*PvrttTexture * pvrttCreateTexture(const PvrttTexture& texture)
{
	return new pvrtexture::CPVRTexture(texture);
}*/

void pvrttDestroyTexture(PvrttTexture * texture)
{
	delete texture;
}

bool pvrttSaveFile(PvrttTexture * texture, const char* filePath)
{
	return texture->saveFile(filePath);
}

const PvrttTextureHeader * pvrttGetHeader(PvrttTexture * texture)
{
	return  &(texture->getHeader());
}

void* pvrttGetDataPtr(PvrttTexture * texture, pvrtexture::uint32 uiMIPLevel = 0, pvrtexture::uint32 uiArrayMember = 0, pvrtexture::uint32 uiFaceNumber = 0)
{
	return texture->getDataPtr(uiMIPLevel, uiArrayMember, uiFaceNumber);
}

// Utilities

bool pvrttGenerateMIPMaps(PvrttTexture& texture, const pvrtexture::EResizeMode eFilterMode, int uiMIPMapsToDo=PVRTEX_ALLMIPLEVELS)
{
	return pvrtexture::GenerateMIPMaps(texture, eFilterMode, uiMIPMapsToDo);
}

bool pvrttTranscodeWithNoConversion(PvrttTexture& texture, const PvrttPixelType ptFormat, const EPVRTVariableType eChannelType, const EPVRTColourSpace eColourspace, const pvrtexture::ECompressorQuality eQuality=pvrtexture::ePVRTCNormal, const bool bDoDither=false)
{
	return pvrtexture::Transcode(texture, convertPixelType(ptFormat), eChannelType, eColourspace, eQuality, bDoDither);
}

bool pvrttTranscode(PvrttTexture& texture, pvrtexture::uint64 ptFormat, const EPVRTVariableType eChannelType, const EPVRTColourSpace eColourspace, const pvrtexture::ECompressorQuality eQuality, const bool bDoDither)
{
	return pvrtexture::Transcode(texture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
}


/*int pvrttDecompressPVRTC(const void *pCompressedData, const int Do2bitMode, const int XDim, const int YDim, unsigned char* pResultImage)
{
	return PVRTDecompressPVRTC(pCompressedData, Do2bitMode, XDim, YDim, pResultImage);
}

int pvrttDecompressETC(const void * const pSrcData, const unsigned int &x, const unsigned int &y, void *pDestData, const int &nMode)
{
	return PVRTDecompressETC(pSrcData, x, y, pDestData, nMode);
}*/

bool pvrttCopyChannels(PvrttTexture& sTexture, const PvrttTexture& sTextureSource, pvrtexture::uint32 uiNumChannelCopies, pvrtexture::EChannelName *eChannels, pvrtexture::EChannelName *eChannelsSource)
{
	return pvrtexture::CopyChannels(sTexture, sTextureSource, uiNumChannelCopies, eChannels, eChannelsSource);
}

bool pvrttResize(PvrttTexture& sTexture, const pvrtexture::uint32& u32NewWidth, const pvrtexture::uint32& u32NewHeight, const pvrtexture::uint32& u32NewDepth, const pvrtexture::EResizeMode eResizeMode)
{
	return pvrtexture::Resize(sTexture, u32NewWidth, u32NewHeight, u32NewDepth, eResizeMode);
}

bool pvrttFlip(PvrttTexture& sTexture, const EPVRTAxis eFlipDirection)
{
	return pvrtexture::Flip(sTexture, eFlipDirection);
}

bool pvrttGenerateNormalMap(PvrttTexture& sTexture, const float fScale, const char* sChannelOrder)
{
	return pvrtexture::GenerateNormalMap(sTexture, fScale, sChannelOrder);
}

bool pvrttPreMultipliedAlpha(PvrttTexture& sTexture)
{
	return pvrtexture::PreMultiplyAlpha(sTexture);
}
