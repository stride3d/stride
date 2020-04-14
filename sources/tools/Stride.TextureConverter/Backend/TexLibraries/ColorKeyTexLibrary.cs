// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xenko.Core.Diagnostics;
using Xenko.Graphics;
using Xenko.TextureConverter.Requests;

namespace Xenko.TextureConverter.TexLibraries
{
    /// <summary>
    /// Allows the creation and manipulation of texture atlas.
    /// </summary>
    internal class ColorKeyTexLibrary : ITexLibrary
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("ColorKeyTexLibrary");

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorKeyTexLibrary"/> class.
        /// </summary>
        public ColorKeyTexLibrary() { }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request) => request.Type == RequestType.ColorKey;

        public void Execute(TexImage image, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.ColorKey:
                    ApplyColorKey(image, (ColorKeyRequest)request);
                    break;
                default:
                    Log.Error("ColorKeyTexLibrary can't handle this request: " + request.Type);
                    throw new TextureToolsException("ColorKeyTexLibrary can't handle this request: " + request.Type);
            }
        }

        public void Dispose(TexImage image)
        {
            Marshal.FreeHGlobal(image.Data);
        }

        public void Dispose() { }

        public void StartLibrary(TexImage image) { }

        public void EndLibrary(TexImage image) { }

        public bool SupportBGRAOrder()
        {
            return true;
        }

        public unsafe void ApplyColorKey(TexImage image, ColorKeyRequest request)
        {
            Log.Info($"Apply color key [{request.ColorKey}]");

            var colorKey = request.ColorKey;
            var rowPtr = image.Data;
            if (image.Format == PixelFormat.R8G8B8A8_UNorm || image.Format == PixelFormat.R8G8B8A8_UNorm_SRgb)
            {
                for (int i = 0; i < image.Height; i++)
                {
                    var colors = (Core.Mathematics.Color*)rowPtr;
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (colors[x] == colorKey)
                        {
                            colors[x] = Core.Mathematics.Color.Transparent;
                        }
                    }
                    rowPtr = IntPtr.Add(rowPtr, image.RowPitch);
                }
            }
            else if (image.Format == PixelFormat.B8G8R8A8_UNorm || image.Format == PixelFormat.B8G8R8A8_UNorm_SRgb)
            {
                var rgbaColorKey = colorKey.ToRgba();
                for (int i = 0; i < image.Height; i++)
                {
                    var colors = (Core.Mathematics.ColorBGRA*)rowPtr;
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (colors[x].ToRgba() == rgbaColorKey)
                        {
                            colors[x] = Core.Mathematics.Color.Transparent;
                        }
                    }
                    rowPtr = IntPtr.Add(rowPtr, image.RowPitch);
                }
            }
        }
    }
}
