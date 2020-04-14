// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// The following code is a port of DirectXTex http://directxtex.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.
#pragma warning disable SA1310 // Field names should not contain underscore
using System;
using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.Graphics
{
    internal class DDS
    {
        /// <summary>
        /// Magic code to identify DDS header
        /// </summary>
        public const uint MagicHeader = 0x20534444; // "DDS "

        /// <summary>
        /// Internal structure used to describe a DDS pixel format.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DDSPixelFormat
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DDSPixelFormat" /> struct.
            /// </summary>
            /// <param name="flags">The flags.</param>
            /// <param name="fourCC">The four CC.</param>
            /// <param name="rgbBitCount">The RGB bit count.</param>
            /// <param name="rBitMask">The r bit mask.</param>
            /// <param name="gBitMask">The g bit mask.</param>
            /// <param name="bBitMask">The b bit mask.</param>
            /// <param name="aBitMask">A bit mask.</param>
            public DDSPixelFormat(PixelFormatFlags flags, int fourCC, int rgbBitCount, uint rBitMask, uint gBitMask, uint bBitMask, uint aBitMask)
            {
                Size = Utilities.SizeOf<DDSPixelFormat>();
                Flags = flags;
                FourCC = fourCC;
                RGBBitCount = rgbBitCount;
                RBitMask = rBitMask;
                GBitMask = gBitMask;
                BBitMask = bBitMask;
                ABitMask = aBitMask;
            }

            public int Size;
            public PixelFormatFlags Flags;
            public int FourCC;
            public int RGBBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;

            public static readonly DDSPixelFormat DXT1 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', 'T', '1'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat DXT2 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', 'T', '2'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat DXT3 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', 'T', '3'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat DXT4 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', 'T', '4'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat DXT5 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', 'T', '5'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat BC4_UNorm = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('B', 'C', '4', 'U'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat BC4_SNorm = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('B', 'C', '4', 'S'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat BC5_UNorm = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('B', 'C', '5', 'U'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat BC5_SNorm = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('B', 'C', '5', 'S'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat R8G8_B8G8 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('R', 'G', 'B', 'G'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat G8R8_G8B8 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('G', 'R', 'G', 'B'), 0, 0, 0, 0, 0);

            public static readonly DDSPixelFormat A8R8G8B8 = new DDSPixelFormat(PixelFormatFlags.Rgba, 0, 32, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000);

            public static readonly DDSPixelFormat X8R8G8B8 = new DDSPixelFormat(PixelFormatFlags.Rgb, 0, 32, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000);

            public static readonly DDSPixelFormat A8B8G8R8 = new DDSPixelFormat(PixelFormatFlags.Rgba, 0, 32, 0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);

            public static readonly DDSPixelFormat X8B8G8R8 = new DDSPixelFormat(PixelFormatFlags.Rgb, 0, 32, 0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000);

            public static readonly DDSPixelFormat G16R16 = new DDSPixelFormat(PixelFormatFlags.Rgb, 0, 32, 0x0000ffff, 0xffff0000, 0x00000000, 0x00000000);

            public static readonly DDSPixelFormat R5G6B5 = new DDSPixelFormat(PixelFormatFlags.Rgb, 0, 16, 0x0000f800, 0x000007e0, 0x0000001f, 0x00000000);

            public static readonly DDSPixelFormat A1R5G5B5 = new DDSPixelFormat(PixelFormatFlags.Rgba, 0, 16, 0x00007c00, 0x000003e0, 0x0000001f, 0x00008000);

            public static readonly DDSPixelFormat A4R4G4B4 = new DDSPixelFormat(PixelFormatFlags.Rgba, 0, 16, 0x00000f00, 0x000000f0, 0x0000000f, 0x0000f000);

            public static readonly DDSPixelFormat R8G8B8 = new DDSPixelFormat(PixelFormatFlags.Rgb, 0, 24, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000);

            public static readonly DDSPixelFormat L8 = new DDSPixelFormat(PixelFormatFlags.Luminance, 0, 8, 0xff, 0x00, 0x00, 0x00);

            public static readonly DDSPixelFormat L16 = new DDSPixelFormat(PixelFormatFlags.Luminance, 0, 16, 0xffff, 0x0000, 0x0000, 0x0000);

            public static readonly DDSPixelFormat A8L8 = new DDSPixelFormat(PixelFormatFlags.LuminanceAlpha, 0, 16, 0x00ff, 0x0000, 0x0000, 0xff00);

            public static readonly DDSPixelFormat A8 = new DDSPixelFormat(PixelFormatFlags.Alpha, 0, 8, 0x00, 0x00, 0x00, 0xff);

            public static readonly DDSPixelFormat DX10 = new DDSPixelFormat(PixelFormatFlags.FourCC, new FourCC('D', 'X', '1', '0'), 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// PixelFormat flags.
        /// </summary>
        [Flags]
        public enum PixelFormatFlags
        {
            FourCC = 0x00000004, // DDPF_FOURCC
            Rgb = 0x00000040, // DDPF_RGB
            Rgba = 0x00000041, // DDPF_RGB | DDPF_ALPHAPIXELS
            Luminance = 0x00020000, // DDPF_LUMINANCE
            LuminanceAlpha = 0x00020001, // DDPF_LUMINANCE | DDPF_ALPHAPIXELS
            Alpha = 0x00000002, // DDPF_ALPHA
            Pal8 = 0x00000020, // DDPF_PALETTEINDEXED8            
        }

        /// <summary>
        /// DDS Header flags.
        /// </summary>
        [Flags]
        public enum HeaderFlags
        {
            Texture = 0x00001007, // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT 
            Mipmap = 0x00020000, // DDSD_MIPMAPCOUNT
            Volume = 0x00800000, // DDSD_DEPTH
            Pitch = 0x00000008, // DDSD_PITCH
            LinearSize = 0x00080000, // DDSD_LINEARSIZE
            Height = 0x00000002, // DDSD_HEIGHT
            Width = 0x00000004, // DDSD_WIDTH
        }

        /// <summary>
        /// DDS Surface flags.
        /// </summary>
        [Flags]
        public enum SurfaceFlags
        {
            Texture = 0x00001000, // DDSCAPS_TEXTURE
            Mipmap = 0x00400008,  // DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
            Cubemap = 0x00000008, // DDSCAPS_COMPLEX
        }

#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
        /// <summary>
        /// DDS Cubemap flags.
        /// </summary>
        [Flags]
        public enum CubemapFlags
        {
            CubeMap   = 0x00000200, // DDSCAPS2_CUBEMAP
            Volume    = 0x00200000, // DDSCAPS2_VOLUME
            PositiveX = 0x00000600, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
            NegativeX = 0x00000a00, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
            PositiveY = 0x00001200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
            NegativeY = 0x00002200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
            PositiveZ = 0x00004200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
            NegativeZ = 0x00008200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

            AllFaces = PositiveX | NegativeX | PositiveY | NegativeY | PositiveZ | NegativeZ,
        }
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public int Size;
            public HeaderFlags Flags;
            public int Height;
            public int Width;
            public int PitchOrLinearSize;
            public int Depth; // only if DDS_HEADER_FLAGS_VOLUME is set in dwFlags
            public int MipMapCount;

            private readonly uint unused1;
            private readonly uint unused2;
            private readonly uint unused3;
            private readonly uint unused4;
            private readonly uint unused5;
            private readonly uint unused6;
            private readonly uint unused7;
            private readonly uint unused8;
            private readonly uint unused9;
            private readonly uint unused10;
            private readonly uint unused11;

            public DDSPixelFormat PixelFormat;
            public SurfaceFlags SurfaceFlags;
            public CubemapFlags CubemapFlags;

            private readonly uint unused12;
            private readonly uint unused13;

            private readonly uint unused14;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HeaderDXT10
        {
            public PixelFormat DXGIFormat;
            public ResourceDimension ResourceDimension;
            public ResourceOptionFlags MiscFlags; // see DDS_RESOURCE_MISC_FLAG
            public int ArraySize;

            private readonly uint unused;
        }

        /// <summary>
        /// <p>Identifies the type of resource being used.</p>
        /// </summary>
        /// <remarks>
        /// <p>This enumeration is used in <strong><see cref="SharpDX.Direct3D11.Resource.GetDimension"/></strong>. </p>
        /// </remarks>
        /// <include file='.\..\Documentation\CodeComments.xml' path="/comments/comment[@id='D3D11_RESOURCE_DIMENSION']/*"/>
        public enum ResourceDimension : int
        {
            /// <summary>
            /// <dd> <p>Resource is of unknown type.</p> </dd>
            /// </summary>
            /// <include file='.\..\Documentation\CodeComments.xml' path="/comments/comment[@id='D3D11_RESOURCE_DIMENSION_UNKNOWN']/*"/>
            Unknown = unchecked((int)0),

            /// <summary>
            /// <dd> <p>Resource is a buffer.</p> </dd>
            /// </summary>
            Buffer = unchecked((int)1),

            /// <summary>
            /// <dd> <p>Resource is a 1D texture.</p> </dd>
            /// </summary>
            Texture1D = unchecked((int)2),

            /// <summary>
            /// <dd> <p>Resource is a 2D texture.</p> </dd>
            /// </summary>
            Texture2D = unchecked((int)3),

            /// <summary>
            /// <dd> <p>Resource is a 3D texture.</p> </dd>
            /// </summary>
            Texture3D = unchecked((int)4),
        }

        /// <summary>
        /// <p>Identifies options for resources.</p>
        /// </summary>
        /// <remarks>
        /// <p>This enumeration is used in <strong><see cref="SharpDX.Direct3D11.BufferDescription"/></strong>, <strong><see cref="SharpDX.Direct3D11.Texture1DDescription"/></strong>, <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong>, <strong><see cref="SharpDX.Direct3D11.Texture3DDescription"/></strong>. </p><p>These flags can be combined by bitwise OR.</p>
        /// </remarks>
        [Flags]
        public enum ResourceOptionFlags : int
        {
            /// <summary>
            /// <dd> <p>Enables MIP map generation by using <strong><see cref="SharpDX.Direct3D11.DeviceContext.GenerateMips"/></strong> on a texture resource. The resource must be created with the <strong>bind flags</strong> that specify that the resource is a render target and a shader resource.</p> </dd>
            /// </summary>
            GenerateMipMaps = unchecked((int)1),

            /// <summary>
            /// <dd> <p>Enables resource data sharing between two or more Direct3D devices. The only resources that can be shared are 2D non-mipmapped textures.</p> <p><strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.Shared"/></strong> and <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex"/></strong> are mutually exclusive.</p> <p><strong>WARP</strong> and <strong>REF</strong> devices do not support shared resources. If you try to create a resource with this flag on either a <strong>WARP</strong> or <strong>REF</strong> device,  the create method will return an <strong>E_OUTOFMEMORY</strong> error code.</p> </dd>
            /// </summary>
            Shared = unchecked((int)2),

            /// <summary>
            /// <dd> <p>Sets a resource to be a cube texture created from a Texture2DArray that contains 6 textures.</p> </dd>
            /// </summary>
            TextureCube = unchecked((int)4),

            /// <summary>
            /// <dd> <p>Enables instancing of GPU-generated content.</p> </dd>
            /// </summary>
            DrawindirectArgs = unchecked((int)16),

            /// <summary>
            /// <dd> <p>Enables a resource as a byte address buffer.</p> </dd>
            /// </summary>
            BufferAllowRawViews = unchecked((int)32),

            /// <summary>
            /// <dd> <p>Enables a resource as a structured buffer.</p> </dd>
            /// </summary>
            BufferStructured = unchecked((int)64),

            /// <summary>
            /// <dd> <p>Enables a resource with MIP map clamping for use with <strong><see cref="SharpDX.Direct3D11.DeviceContext.SetMinimumLod"/></strong>.</p> </dd>
            /// </summary>
            ResourceClamp = unchecked((int)128),

            /// <summary>
            /// <dd> <p>Enables the resource  to be synchronized by using the <strong><see cref="SharpDX.DXGI.KeyedMutex.Acquire"/></strong> and  <strong><see cref="SharpDX.DXGI.KeyedMutex.Release"/></strong> APIs.  The following Direct3D?11 resource creation  APIs, that take <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags"/></strong> parameters, have been extended to support the new flag.</p> <ul> <li> <strong><see cref="SharpDX.Direct3D11.Device.CreateTexture1D"/></strong> </li> <li> <strong><see cref="SharpDX.Direct3D11.Device.CreateTexture2D"/></strong> </li> <li> <strong><see cref="SharpDX.Direct3D11.Device.CreateTexture3D"/></strong> </li> <li> <strong><see cref="SharpDX.Direct3D11.Device.CreateBuffer"/></strong> </li> </ul> <p>If you call any of these  methods with the <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex"/></strong> flag set, the interface returned will support the <strong><see cref="SharpDX.DXGI.KeyedMutex"/></strong> interface.  You can retrieve a reference to the <strong><see cref="SharpDX.DXGI.KeyedMutex"/></strong> interface from the resource by using <strong>IUnknown::QueryInterface</strong>.  The <strong><see cref="SharpDX.DXGI.KeyedMutex"/></strong> interface implements the <strong><see cref="SharpDX.DXGI.KeyedMutex.Acquire"/></strong> and <strong><see cref="SharpDX.DXGI.KeyedMutex.Release"/></strong> APIs to synchronize access to the surface. The device that creates the surface, and any other device that opens the surface  by using <strong>OpenSharedResource</strong>, must call <strong><see cref="SharpDX.DXGI.KeyedMutex.Acquire"/></strong> before they issue any rendering commands to the surface. When those devices finish rendering, they must call <strong><see cref="SharpDX.DXGI.KeyedMutex.Release"/></strong>.</p> <p><strong> <see cref="SharpDX.Direct3D11.ResourceOptionFlags.Shared"/></strong> and <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex"/></strong> are mutually exclusive.</p> <p><strong>WARP</strong> and <strong>REF</strong> devices do not support shared resources. If you try to create a resource with this flag on either a <strong>WARP</strong> or <strong>REF</strong> device,  the create method will return an <strong>E_OUTOFMEMORY</strong> error code.</p> </dd>
            /// </summary>
            SharedKeyedmutex = unchecked((int)256),

            /// <summary>
            /// <dd> <p>Enables a resource compatible with GDI. You must set the <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/></strong> flag  on surfaces that you use with GDI. Setting the <strong><see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/></strong> flag allows GDI rendering on the surface via <strong><see cref="SharpDX.DXGI.Surface1.GetDC"/></strong>.
            /// </p> <p>Consider the following programming tips for using <see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/> when you create a texture or use that texture in a swap chain:</p> <ul> <li><see cref="SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex"/> and <see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/> are mutually exclusive. Therefore, do not use them together.</li> <li><see cref="SharpDX.Direct3D11.ResourceOptionFlags.ResourceClamp"/> and <see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/> are mutually exclusive. Therefore, do not use them together.</li> <li>You must bind the texture as a render target for the output-merger stage. For example, set the <see cref="SharpDX.Direct3D11.BindFlags.RenderTarget"/> flag in the <strong>BindFlags</strong> member of the <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong> structure.</li> <li>You must set the maximum number of MIP map levels to 1. For example, set the <strong>MipLevels</strong> member of the <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong> structure to 1.</li> <li>You must specify that the texture requires read and write access by the GPU. For example, set the <strong>Usage</strong> member of the <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong> structure to <see cref="SharpDX.Direct3D11.ResourceUsage.Default"/>.</li> <li> <p>You must set the texture format to one of the following types. </p> <ul> <li><see cref="SharpDX.DXGI.Format.B8G8R8A8_UNorm"/>
            /// </li> <li><see cref="SharpDX.DXGI.Format.B8G8R8A8_Typeless"/></li> <li><see cref="SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb"/>
            /// </li> </ul>For example, set the <strong>Format</strong> member of the <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong> structure to one of these  types.</li> <li>You cannot use <see cref="SharpDX.Direct3D11.ResourceOptionFlags.GdiCompatible"/> with multisampling. Therefore, set the <strong>Count</strong> member of the <strong><see cref="SharpDX.DXGI.SampleDescription"/></strong> structure to 1. Then, set the <strong>SampleDesc</strong> member of the <strong><see cref="SharpDX.Direct3D11.Texture2DDescription"/></strong> structure to this <strong><see cref="SharpDX.DXGI.SampleDescription"/></strong> structure.</li> </ul> </dd>
            /// </summary>
            GdiCompatible = unchecked((int)512),

            /// <summary>
            /// None.
            /// </summary>
            None = unchecked((int)0),
        }
    }
}
