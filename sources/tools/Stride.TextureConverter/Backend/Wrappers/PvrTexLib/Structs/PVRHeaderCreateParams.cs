namespace Stride.TextureConverter.PvrttWrapper;

internal struct PVRHeaderCreateParams
{
	///<summary>Pixel format</summary>
    internal ulong pixelFormat;
	
    ///<summary>Texture width</summary>
    internal uint width; 

	///<summary>Texture height</summary>
    internal uint height;

	///<summary>Texture depth</summary>
    internal uint depth;

	///<summary>Number of MIP maps</summary>
    internal uint numMipMaps;     

	///<summary>Number of array members</summary>
    internal uint numArrayMembers;

	///<summary>Number of faces</summary>
    internal uint numFaces;
	
    ///<summary>Colour space</summary>
    internal EPVRTColourSpace colourSpace;

    ///<summary>Channel type</summary>
    internal EPVRTVariableType channelType;

    ///<summary>Has the RGB been pre-multiplied by the alpha?</summary>
	internal bool preMultiplied;
    
}
