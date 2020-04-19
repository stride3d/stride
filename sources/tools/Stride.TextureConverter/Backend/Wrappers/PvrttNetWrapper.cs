// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Stride.TextureConverter.PvrttWrapper
{

    #region Enum
    internal enum PixelType
    {
	    Standard8PixelType,
	    Standard16PixelType,
	    Standard32PixelType,
    };

    internal enum EPVRTColourSpace
    {
        ePVRTCSpacelRGB,
        ePVRTCSpaceSRgb,
        ePVRTCSpaceNumSpaces
    };

    internal enum EPVRTVariableType
    {
        ePVRTVarTypeUnsignedByteNorm,
        ePVRTVarTypeSignedByteNorm,
        ePVRTVarTypeUnsignedByte,
        ePVRTVarTypeSignedByte,
        ePVRTVarTypeUnsignedShortNorm,
        ePVRTVarTypeSignedShortNorm,
        ePVRTVarTypeUnsignedShort,
        ePVRTVarTypeSignedShort,
        ePVRTVarTypeUnsignedIntegerNorm,
        ePVRTVarTypeSignedIntegerNorm,
        ePVRTVarTypeUnsignedInteger,
        ePVRTVarTypeSignedInteger,
        ePVRTVarTypeSignedFloat, ePVRTVarTypeFloat = ePVRTVarTypeSignedFloat, //the name ePVRTVarTypeFloat is now deprecated.
        ePVRTVarTypeUnsignedFloat,
        ePVRTVarTypeNumVarTypes
    };

    internal enum ECompressorQuality
	{
		ePVRTCFast=0,
		ePVRTCNormal,
		ePVRTCHigh,
		ePVRTCBest,
		eNumPVRTCModes,

		eETCFast=0,
		eETCFastPerceptual,
		eETCSlow,
		eETCSlowPerceptual,
		eNumETCModes
	};

    internal enum EResizeMode
    {
        eResizeNearest,
        eResizeLinear,
        eResizeCubic,
        eNumResizeModes
    };

    internal enum EChannelName
    {
        eNoChannel,
        eRed,
        eGreen,
        eBlue,
        eAlpha,
        eLuminance,
        eIntensity,
        eUnspecified,
        eNumChannels
    };

    internal enum EPVRTAxis
    {
	    ePVRTAxisX = 0,
	    ePVRTAxisY = 1,
	    ePVRTAxisZ = 2
    };

    internal enum EPVRTPixelFormat
    {
        ePVRTPF_PVRTCI_2bpp_RGB,
        ePVRTPF_PVRTCI_2bpp_RGBA,
        ePVRTPF_PVRTCI_4bpp_RGB,
        ePVRTPF_PVRTCI_4bpp_RGBA,
        ePVRTPF_PVRTCII_2bpp,
        ePVRTPF_PVRTCII_4bpp,
        ePVRTPF_ETC1,
        ePVRTPF_DXT1,
        ePVRTPF_DXT2,
        ePVRTPF_DXT3,
        ePVRTPF_DXT4,
        ePVRTPF_DXT5,

        //These formats are identical to some DXT formats.
        ePVRTPF_BC1 = ePVRTPF_DXT1,
        ePVRTPF_BC2 = ePVRTPF_DXT3,
        ePVRTPF_BC3 = ePVRTPF_DXT5,

        //These are currently unsupported:
        ePVRTPF_BC4,
        ePVRTPF_BC5,
        ePVRTPF_BC6,
        ePVRTPF_BC7,

        //These are supported
        ePVRTPF_UYVY,
        ePVRTPF_YUY2,
        ePVRTPF_BW1bpp,
        ePVRTPF_SharedExponentR9G9B9E5,
        ePVRTPF_RGBG8888,
        ePVRTPF_GRGB8888,
        ePVRTPF_ETC2_RGB,
        ePVRTPF_ETC2_RGBA,
        ePVRTPF_ETC2_RGB_A1,
        ePVRTPF_EAC_R11,
        ePVRTPF_EAC_RG11,

        //Invalid value
        ePVRTPF_NumCompressedPFs
    };

    #endregion

    #region Constants
    public class Constant
    {
        public const uint TOPMIPLEVEL = 0;
        public const int ALLMIPLEVELS = -1;
        public const uint PVRTEX3_IDENT = 0x03525650;	// 'P''V''R'3
        public const uint PVRTEX3_PREMULTIPLIED = (1 << 1);		//	Texture has been premultiplied by alpha value.	
    }
    #endregion




    #region public class Utilities

    /// <summary>
    /// Provides utilities methods to handle PVR Texture type.
    /// </summary>
    internal class Utilities
    {
        #region Bindings
        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttTranscodeWithNoConversion(IntPtr texture, PixelType ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality, bool bDoDither);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttTranscode(IntPtr texture, UInt64 ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality, bool bDoDither);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttCopyChannels(IntPtr sTexture, IntPtr sTextureSource, uint uiNumChannelCopies, out EChannelName eChannels, out EChannelName eChannelsSource);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttResize(IntPtr sTexture, out uint u32NewWidth, out uint u32NewHeight, out uint u32NewDepth, EResizeMode eResizeMode);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttFlip(IntPtr sTexture, EPVRTAxis eFlipDirection);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttGenerateNormalMap(IntPtr sTexture, float fScale, string sChannelOrder);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt64 pvrttConvertPixelType(PixelType pixelFormat);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static int pvrttDecompressPVRTC(IntPtr pCompressedData, int Do2bitMode, int XDim, int YDim, IntPtr pResultImage);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static int pvrttDecompressETC(IntPtr pSrcData, uint x, uint y, IntPtr pDestData, int nMode);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttPreMultipliedAlpha(IntPtr sTexture);

        #endregion

        /// <summary>
        /// Copies a specified channel from one texture to a specified channel in another texture.
        /// </summary>
        /// <param name="sTexture">The destination texture.</param>
        /// <param name="sTextureSource">The source texture.</param>
        /// <param name="uiNumChannelCopies">The UI num channel copies.</param>
        /// <param name="eChannels">The destination channel.</param>
        /// <param name="eChannelsSource">The source channel.</param>
        /// <returns></returns>
        public static bool CopyChannels(PVRTexture sTexture, PVRTexture sTextureSource, uint uiNumChannelCopies, out EChannelName eChannels, out EChannelName eChannelsSource)
        {
            return pvrttCopyChannels(sTexture.texture, sTextureSource.texture, uiNumChannelCopies, out eChannels, out eChannelsSource);
        }

        public static bool Transcode(PVRTexture sTexture, PixelType ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality = ECompressorQuality.ePVRTCNormal, bool bDoDither = false)
        {
            return pvrttTranscodeWithNoConversion(sTexture.texture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
        }

        public static bool Transcode(PVRTexture sTexture, UInt64 ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality = ECompressorQuality.ePVRTCNormal, bool bDoDither = false)
        {
            return pvrttTranscode(sTexture.texture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
        }

        /// <summary>
        /// Resizes the specified texture.
        /// </summary>
        /// <param name="sTexture">The texture.</param>
        /// <param name="u32NewWidth">The new width.</param>
        /// <param name="u32NewHeight">The new height.</param>
        /// <param name="u32NewDepth">The new depth.</param>
        /// <param name="eResizeMode">The resize mode (Filter).</param>
        /// <returns></returns>
        public static bool Resize(PVRTexture sTexture, uint u32NewWidth, uint u32NewHeight, uint u32NewDepth, EResizeMode eResizeMode)
        {
            return pvrttResize(sTexture.texture, out u32NewWidth, out u32NewHeight, out u32NewDepth, eResizeMode);
        }


        /// <summary>
        /// Flips the specified texture.
        /// </summary>
        /// <param name="sTexture">The texture.</param>
        /// <param name="eFlipDirection">The flip direction.</param>
        /// <returns></returns>
        public static bool Flip(PVRTexture sTexture, EPVRTAxis eFlipDirection)
        {
            return pvrttFlip(sTexture.texture, eFlipDirection);
        }

        public static bool GenerateNormalMap(PVRTexture sTexture, float fScale, string sChannelOrder)
        {
            return pvrttGenerateNormalMap(sTexture.texture, fScale, sChannelOrder);
        }

        public static UInt64 ConvertPixelType(PixelType pixelFormat)
        {
            return pvrttConvertPixelType(pixelFormat);
        }

        public static int DecompressPVRTC(IntPtr pCompressedData, int Do2bitMode, int XDim, int YDim, IntPtr pResultImage)
        {
            return pvrttDecompressPVRTC(pCompressedData, Do2bitMode, XDim, YDim, pResultImage);
        }

        public static int DecompressETC(IntPtr pSrcData, uint x, uint y, IntPtr pDestData, int nMode)
        {
            return pvrttDecompressETC(pSrcData, x, y, pDestData, nMode);
        }

        public static bool PreMultipliedAlpha(PVRTexture sTexture)
        {
            return pvrttPreMultipliedAlpha(sTexture.texture);
        }
    }
    #endregion

    #region public class CPVRTexture
    /// <summary>
    /// Binding class of PVR Texture class PVRTexture.
    /// </summary>
    internal class PVRTexture : IDisposable
    {
        internal IntPtr texture;

        #region Constants
            const uint TOPMIPLEVEL = 0;
            const int ALLMIPLEVELS = -1;
        #endregion

        #region Bindings
        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTexture();

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureFromHeader(IntPtr header, IntPtr data);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureFromMemory(IntPtr pTexture);

        [DllImport("PvrttWrapper.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureFromFile(String filePath);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void pvrttDestroyTexture(IntPtr texture);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttSaveFile(IntPtr texture, String filePath);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttGetHeader(IntPtr texture);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttGetDataPtr(IntPtr texture, uint uiMIPLevel, uint uiArrayMember, uint uiFaceNumber);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttGenerateMIPMaps(IntPtr texture, EResizeMode eFilterMode, int uiMIPMapsToDo);

        #endregion

        public PVRTexture()
        {
            texture = pvrttCreateTexture();
        }

        public PVRTexture(IntPtr pTexture)
        {
            texture = pvrttCreateTextureFromMemory(pTexture);
        }

        public PVRTexture(string filePath)
        {
            texture = pvrttCreateTextureFromFile(filePath);
        }

        public PVRTexture(PVRTextureHeader headerIn, IntPtr data)
        {
            texture = pvrttCreateTextureFromHeader(headerIn.header, data);
        }

        public bool Save(string filePath)
        {
            return pvrttSaveFile(texture, filePath);
        }

        public PVRTextureHeader GetHeader()
        {
            return new PVRTextureHeader(pvrttGetHeader(texture));
        }

        public IntPtr GetDataPtr(uint uiMIPLevel = 0, uint uiArrayMember = 0, uint uiFaceNumber = 0) {
            return pvrttGetDataPtr(texture, uiMIPLevel, uiArrayMember, uiFaceNumber);
        }

        public bool GenerateMIPMaps(EResizeMode eFilterMode, int uiMIPMapsToDo = ALLMIPLEVELS)
        {
            return pvrttGenerateMIPMaps(texture, eFilterMode, uiMIPMapsToDo);
        }


        public void Dispose()
        {
            pvrttDestroyTexture(texture);
        }
    }
    #endregion

    #region public class PvrttTextureHeader
    /// <summary>
    /// Binding class of PVR Texture class PVRTextureHeader.
    /// </summary>
    internal class PVRTextureHeader : IDisposable
    {
        public IntPtr header { internal set; get; }

        #region Bindings
        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureHeaderEmpty();

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureHeader(PixelType pixelFormat, int height, int width, int depth, int numMipMaps, int numArrayMembers, int numFaces, EPVRTColourSpace eColourSpace, EPVRTVariableType eChannelType, bool bPreMultiplied);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCreateTextureHeaderFromCompressedTexture(UInt64 pixelFormat, int height, int width, int depth, int numMipMaps, int numArrayMembers, int numFaces, EPVRTColourSpace eColourSpace, EPVRTVariableType eChannelType, bool bPreMultiplied);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr pvrttCopyTextureHeader(IntPtr headerIn);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt32 pvrttGetWidth(IntPtr header, uint uiMipLevel);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt32 pvrttGetHeight(IntPtr header, uint uiMipLevel);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt32 pvrttGetNumMIPLevels(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void pvrttSetNumMIPLevels(IntPtr header, int newMipsLevels);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void pvrttSetWidth(IntPtr header, uint newWidth);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void pvrttSetHeight(IntPtr header, uint newHeight);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void pvrttSetPixelFormat(IntPtr header, PixelType pixelType);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetDataSize(IntPtr header, int iMipLevel, bool bAllSurfaces, bool bAllFaces);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetTextureSize(IntPtr header, int iMipLevel, bool bAllSurfaces, bool bAllFaces);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetDepth(IntPtr header, uint uiMipLevel);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetBPP(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetNumArrayMembers(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint pvrttGetNumFaces(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool pvrttIsFileCompressed(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt64 pvrttGetPixelType(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static UInt32 pvrttGetMetaDataSize(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static EPVRTVariableType pvrttGetChannelType(IntPtr header);

        [DllImport("PvrttWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static EPVRTColourSpace pvrttGetColourSpace(IntPtr header);

        #endregion


        public PVRTextureHeader()
        {
            header = pvrttCreateTextureHeaderEmpty();
        }

        public PVRTextureHeader(PixelType pixelFormat, int height=1, int width=1, int depth=1, int numMipMaps=1, int numArrayMembers=1, int numFaces=1, EPVRTColourSpace eColourSpace=EPVRTColourSpace.ePVRTCSpacelRGB, EPVRTVariableType eChannelType=EPVRTVariableType.ePVRTVarTypeUnsignedByteNorm, bool bPreMultiplied=false)
        {
            header = pvrttCreateTextureHeader(pixelFormat, height, width, depth, numMipMaps, numArrayMembers, numFaces, eColourSpace, eChannelType, bPreMultiplied);
        }

        public PVRTextureHeader(UInt64 pixelFormat, int height=1, int width=1, int depth=1, int numMipMaps=1, int numArrayMembers=1, int numFaces=1, EPVRTColourSpace eColourSpace=EPVRTColourSpace.ePVRTCSpacelRGB, EPVRTVariableType eChannelType=EPVRTVariableType.ePVRTVarTypeUnsignedByteNorm, bool bPreMultiplied=false)
        {
            header = pvrttCreateTextureHeaderFromCompressedTexture(pixelFormat, height, width, depth, numMipMaps, numArrayMembers, numFaces, eColourSpace, eChannelType, bPreMultiplied);
        }

        public PVRTextureHeader(IntPtr headerPtr)
        {
            header = headerPtr;
        }

        public PVRTextureHeader Copy()
        {
            return new PVRTextureHeader(pvrttCopyTextureHeader(header));
        }

        public uint GetWidth(uint uiMipLevel = Constant.TOPMIPLEVEL)
        {
            return pvrttGetWidth(header, uiMipLevel);
        }

        public uint GetHeight(uint uiMipLevel = Constant.TOPMIPLEVEL)
        {
            return pvrttGetHeight(header, uiMipLevel);
        }

        public uint GetNumMIPLevels()
        {
            return pvrttGetNumMIPLevels(header);
        }

        public void SetNumMIPLevels(int newMipsLevels)
        {
            pvrttSetNumMIPLevels(header, newMipsLevels);
        }

        public void SetPixelFormat(PixelType pixelType)
        {
            pvrttSetPixelFormat(header, pixelType);
        }

        public void SetWidth(uint newWidth)
        {
            pvrttSetWidth(header, newWidth);
        }

        public void SetHeight(uint newHeight)
        {
            pvrttSetHeight(header, newHeight);
        }

        public uint GetDataSize(int iMipLevel = Constant.ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true)
        {
            return pvrttGetDataSize(header, iMipLevel, bAllSurfaces, bAllFaces);
        }

        public uint GetTextureSize(int iMipLevel = Constant.ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true)
        {
            return pvrttGetTextureSize(header, iMipLevel, bAllSurfaces, bAllFaces);
        }

        public uint GetDepth(uint uiMipLevel = Constant.TOPMIPLEVEL)
        {
            return pvrttGetDepth(header, uiMipLevel);
        }

        public uint GetBPP()
        {
            return pvrttGetBPP(header);
        }

        public uint GetNumArrayMembers()
        {
            return pvrttGetNumArrayMembers(header);
        }

        
        public uint GetNumFaces()
        {
            return pvrttGetNumFaces(header);
        }

        public uint GetMetaDataSize()
        {
            return pvrttGetMetaDataSize(header);
        }

        public EPVRTVariableType GetChannelType()
        {
            return pvrttGetChannelType(header);
        }

        public UInt64 GetPixelType()
        {
            return pvrttGetPixelType(header);
        }

        public EPVRTColourSpace GetColourSpace()
        {
            return pvrttGetColourSpace(header);
        }

        public Stride.Graphics.PixelFormat GetFormat()
        {
            UInt64 format = pvrttGetPixelType(header);

            switch (format)
            {
                case 6:
                    return Stride.Graphics.PixelFormat.ETC1;
                case 22:
                    return Stride.Graphics.PixelFormat.ETC2_RGB;
                case 23:
                    return Stride.Graphics.PixelFormat.ETC2_RGBA;
                case 24:
                    return Stride.Graphics.PixelFormat.ETC2_RGB_A1;
                case 25:
                    return Stride.Graphics.PixelFormat.EAC_R11_Unsigned;
                case 26:
                    return Stride.Graphics.PixelFormat.EAC_R11_Signed;
                case 27:
                    return Stride.Graphics.PixelFormat.EAC_RG11_Unsigned;
                case 28:
                    return Stride.Graphics.PixelFormat.EAC_RG11_Signed;
                /*default:
                    throw new TexLibraryException("Unknown format by PowerVC Texture Tool.");*/
            }

            if (format == Utilities.ConvertPixelType(PixelType.Standard8PixelType))
                return Stride.Graphics.PixelFormat.R8G8B8A8_UNorm;
            else if (format == Utilities.ConvertPixelType(PixelType.Standard16PixelType))
                return Stride.Graphics.PixelFormat.R16G16B16A16_UNorm;
            else if (format == Utilities.ConvertPixelType(PixelType.Standard32PixelType))
                return Stride.Graphics.PixelFormat.R32G32B32A32_Float;
            else
                throw new TextureToolsException("Unknown format by PowerVC Texture Tool.");
        }

        public int GetAlphaDepth()
        {
            UInt64 format = pvrttGetPixelType(header);
            if (format <= 0xffffffff)
            {
                switch (format)
                {
                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCI_2bpp_RGB:
                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCI_4bpp_RGB:
                    case (int)EPVRTPixelFormat.ePVRTPF_ETC1:
                    case (int)EPVRTPixelFormat.ePVRTPF_ETC2_RGB:
                    case (int)EPVRTPixelFormat.ePVRTPF_EAC_R11:
                    case (int)EPVRTPixelFormat.ePVRTPF_EAC_RG11:
                        return 0;

                    case (int)EPVRTPixelFormat.ePVRTPF_ETC2_RGB_A1:
                        return 1;

                    case (int)EPVRTPixelFormat.ePVRTPF_ETC2_RGBA:
                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCI_2bpp_RGBA:
                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCI_4bpp_RGBA:
                        return 8;

                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCII_2bpp:
                    case (int)EPVRTPixelFormat.ePVRTPF_PVRTCII_4bpp:
                        return 8;  // or 0
                }
                return 0;
            }
            for (int i = 0 ; i < 4 ; i++)
            {
                if (((format & 255)|0x20) == 'a')
                {
                    return ((int)(format>>32) & 255);
                }
                format >>= 8;
            }
            return 0;
        }

        public void Dispose()
        {
        }
    }
    #endregion

}
