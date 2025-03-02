using System.Runtime.InteropServices;

namespace Stride.TextureConverter.PvrttWrapper;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PVRTTranscoderOptions
{
    ///<summary>For versioning - sizeof(PVRTexLib_TranscoderOptions)`</summary>
    internal uint sizeofStruct;
    internal ulong pixelFormat;
    internal EPVRTVariableTypeArray channelType;
	internal EPVRTColourSpace colourspace;
    internal ECompressorQuality quality;
    internal bool doDither;
    ///<summary>Max range value for RGB[M|D] encoding</summary>
    internal float maxRange;
    ///<summary> Max number of threads to use for transcoding, if set to 0 PVRTexLib will use all available cores.</summary>						
    internal uint maxThreads;

    public PVRTTranscoderOptions(uint sizeofStruct, ulong pixelFormat, 
	EPVRTColourSpace colourSpace, ECompressorQuality quality, 
	bool doDither, float maxRange)
    {
        this.sizeofStruct = sizeofStruct;
        this.pixelFormat = pixelFormat;
        colourspace = colourSpace;
        this.quality = quality;
        this.doDither = doDither;
        this.maxRange = maxRange;
    }
}
[System.Runtime.CompilerServices.InlineArray(4)]
public struct EPVRTVariableTypeArray
{
	private EPVRTVariableType _value;
}