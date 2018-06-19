/*!***********************************************************************

 @file         PVRTextureUtilities.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        This is the main PVRTexLib header file. This header contains 
               a utility function for transcoding textures, as well as 
               pre-processor utilities such as; resizing, rotating, channel
               copying and MIPMap manipulation.
               
*************************************************************************/

#ifndef _PVRTEXTURE_UTILITIES_H
#define _PVRTEXTURE_UTILITIES_H

#include "PVRTextureFormat.h"
#include "PVRTexture.h"

namespace pvrtexture
{
	/*!***********************************************************************
	 @brief      	Resizes the texture to new specified dimensions. Filtering 
					mode is specified with "eResizeMode".
	 @param[in]		sTexture        Texture to resize
	 @param[in]		u32NewWidth     New width
	 @param[in]		u32NewHeight    New height
	 @param[in]		u32NewDepth     New depth
	 @param[in]		eResizeMode     Filtering mode
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL Resize(CPVRTexture& sTexture, const uint32& u32NewWidth, const uint32& u32NewHeight, const uint32& u32NewDepth, const EResizeMode eResizeMode);
		
	/*!***********************************************************************
	 @brief      	Resizes the canvas of a texture to new specified dimensions. Filtering 
					mode is specified with "eResizeMode". 
     @details       Offset area is filled with transparent black colour. 
	 @param[in]		sTexture        Texture
	 @param[in]		u32NewWidth     New width
	 @param[in]		u32NewHeight    New height
	 @param[in]		u32NewDepth     New depth
	 @param[in]		i32XOffset      X Offset value from the top left corner
	 @param[in]		i32YOffset      Y Offset value from the top left corner
	 @param[in]		i32ZOffset      Z Offset value from the top left corner
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL ResizeCanvas(CPVRTexture& sTexture, const uint32& u32NewWidth, const uint32& u32NewHeight, const uint32& u32NewDepth, const int32& i32XOffset, const int32& i32YOffset, const int32& i32ZOffset);

	/*!***********************************************************************
	 @brief      	Rotates a texture by 90 degrees around the given axis. bForward controls direction of rotation.
	 @param[in]		sTexture        Texture to rotate
	 @param[in]		eRotationAxis   Rotation axis
	 @param[in]		bForward        Direction of rotation; 1 = clockwise, 0 = anti-clockwise 
	 @return		True if the method succeeds or not.
	*************************************************************************/
	bool PVR_DLL Rotate90(CPVRTexture& sTexture, const EPVRTAxis eRotationAxis, const bool bForward);

	/*!***********************************************************************
	 @brief      	Flips a texture in a given direction.
	 @param[in]		sTexture        Texture to flip
	 @param[in]	    eFlipDirection  Flip direction
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL Flip(CPVRTexture& sTexture, const EPVRTAxis eFlipDirection);

	/*!***********************************************************************
	 @brief      	Adds a user specified border to the texture.
	 @param[in]		sTexture        Texture 
	 @param[in]		uiBorderX       X border
	 @param[in]		uiBorderY       Y border
	 @param[in]		uiBorderZ       Z border
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL Border(CPVRTexture& sTexture, uint32 uiBorderX, uint32 uiBorderY, uint32 uiBorderZ);

	/*!***********************************************************************
	 @brief      	Pre-multiplies a texture's colours by its alpha values.
	 @param[in]		sTexture        Texture to premultiply
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL PreMultiplyAlpha(CPVRTexture& sTexture);

	/*!***********************************************************************
	 @brief      	Allows a texture's colours to run into any fully transparent areas.
	 @param[in]		sTexture        Texture
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL Bleed(CPVRTexture& sTexture);

	/*!***********************************************************************
	 @brief      	Sets the specified number of channels to values specified in pValues.
	 @param[in]		sTexture            Texture
	 @param[in]		uiNumChannelSets    Number of channels to set
	 @param[in]		eChannels           Channels to set
	 @param[in]		pValues             uint32 values to set channels to
	 @return		True if the method succeeds.
	 *************************************************************************/
	bool PVR_DLL SetChannels(CPVRTexture& sTexture, uint32 uiNumChannelSets, EChannelName *eChannels, uint32 *pValues);
    
    /*!***********************************************************************
	 @brief      	Sets the specified number of channels to values specified in float pValues.
	 @param[in]		sTexture            Texture
	 @param[in]		uiNumChannelSets    Number of channels to set
	 @param[in]		eChannels           Channels to set
	 @param[in]		pValues             float values to set channels to
	 @return		True if the method succeeds.
	 *************************************************************************/
	bool PVR_DLL SetChannelsFloat(CPVRTexture& sTexture, uint32 uiNumChannelSets, EChannelName *eChannels, float *pValues);

	/*!***********************************************************************
	 @brief      	Copies the specified channels from sTextureSource into sTexture. 
	 @details		sTextureSource is not modified so it is possible to use the
					same texture as both input and output. When using the same 
					texture as source and destination, channels are preserved
					between swaps (e.g. copying Red to Green and then Green to Red
					will result in the two channels trading places correctly).
					Channels in eChannels are set to the value of the channels 
					in eChannelSource.
	 @param[in]		sTexture            Destination texture to copy channels to
	 @param[in]		sTextureSource      Source texture to copy channels from
	 @param[in]		uiNumChannelCopies  Number of channels to copy
	 @param[in]		eChannels           Channels to set
	 @param[in]		eChannelsSource     Source channels to copy from
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL CopyChannels(CPVRTexture& sTexture, const CPVRTexture& sTextureSource, uint32 uiNumChannelCopies, EChannelName *eChannels, EChannelName *eChannelsSource);

	/*!***********************************************************************
	 @brief      	Generates a Normal Map from a given height map.
	 @details		Assumes the red channel has the height values.
					By default outputs to red/green/blue = x/y/z,
					this can be overridden by specifying a channel
					order in sChannelOrder. The channels specified
					will output to red/green/blue/alpha in that order.
					So "xyzh" maps x to red, y to green, z to blue
					and h to alpha. 'h' is used to specify that the
					original height map data should be preserved in
					the given channel.
	 @param[in]		sTexture        Texture
	 @param[in]		fScale          Scale factor
	 @param[in]		sChannelOrder   Channel order
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL GenerateNormalMap(CPVRTexture& sTexture, const float fScale, CPVRTString sChannelOrder);

	/*!***********************************************************************
	 @brief      	Generates MIPMaps for a source texture. Default is to
					create a complete MIPMap chain, however this can be
					overridden with uiMIPMapsToDo.
	 @param[in]		sTexture        Texture
	 @param[in]		eFilterMode     Filter mode
	 @param[in]		uiMIPMapsToDo   Level of MIPMap chain to create
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL GenerateMIPMaps(CPVRTexture& sTexture, const EResizeMode eFilterMode, const uint32 uiMIPMapsToDo=PVRTEX_ALLMIPLEVELS);
	
	/*!***********************************************************************
	 @brief      	Colours a texture's MIPMap levels with artificial colours 
					for debugging. MIP levels are coloured in the following order:
					Red, Green, Blue, Cyan, Magenta and Yellow
					in a repeating pattern.
	 @param[in]		sTexture        Texture
	 @return		True if the method succeeds.
	*************************************************************************/
	bool PVR_DLL ColourMIPMaps(CPVRTexture& sTexture);
	
	/*!***********************************************************************
	 @brief      	Transcodes a texture from its original format into a newly specified format.
					Will either quantise or dither to lower precisions based on bDoDither.
					uiQuality specifies the quality for PVRTC and ETC compression.
	 @param[in]		sTexture        Texture
	 @param[in]		ptFormat        Pixel format type
	 @param[in]		eChannelType    Channel type
	 @param[in]		eColourspace    Colour space
	 @param[in]		eQuality        Quality for PVRTC and ETC compression
	 @param[in]		bDoDither       Dither the texture to lower precisions
	 @return		True if the method succeeds.					
	*************************************************************************/
	bool PVR_DLL Transcode(CPVRTexture& sTexture, const PixelType ptFormat, const EPVRTVariableType eChannelType, const EPVRTColourSpace eColourspace, const ECompressorQuality eQuality=ePVRTCNormal, const bool bDoDither=false);
};
#endif //_PVRTEXTURE_UTILTIES_H

