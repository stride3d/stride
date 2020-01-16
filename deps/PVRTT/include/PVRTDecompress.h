/*!****************************************************************************

 @file         PVRTDecompress.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        PVRTC and ETC Texture Decompression.

******************************************************************************/

#ifndef _PVRTDECOMPRESS_H_
#define _PVRTDECOMPRESS_H_

/*!***********************************************************************
 @brief      	Decompresses PVRTC to RGBA 8888.
 @param[in]		pCompressedData The PVRTC texture data to decompress
 @param[in]		Do2bitMode      Signifies whether the data is PVRTC2 or PVRTC4
 @param[in]		XDim            X dimension of the texture
 @param[in]		YDim            Y dimension of the texture
 @param[in,out]	pResultImage    The decompressed texture data
 @return		Returns the amount of data that was decompressed.
*************************************************************************/
int PVRTDecompressPVRTC(const void *pCompressedData,
				const int Do2bitMode,
				const int XDim,
				const int YDim,
				unsigned char* pResultImage);

/*!***********************************************************************
 @brief      	Decompresses ETC to RGBA 8888.
 @param[in]		pSrcData        The ETC texture data to decompress
 @param[in]		x               X dimension of the texture
 @param[in]		y               Y dimension of the texture
 @param[in,out]	pDestData       The decompressed texture data
 @param[in]		nMode           The format of the data
 @return		The number of bytes of ETC data decompressed
*************************************************************************/
int PVRTDecompressETC(const void * const pSrcData,
						 const unsigned int &x,
						 const unsigned int &y,
						 void *pDestData,
						 const int &nMode);


#endif /* _PVRTDECOMPRESS_H_ */

/*****************************************************************************
 End of file (PVRTBoneBatch.h)
*****************************************************************************/

