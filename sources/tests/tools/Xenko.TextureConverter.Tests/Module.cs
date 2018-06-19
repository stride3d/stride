// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xenko.Core;

namespace Xenko.TextureConverter.Tests
{
    public static class Module
    {
        public static readonly string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string PathToInputImages = Path.Combine(ApplicationPath, "InputImages") + Path.DirectorySeparatorChar;
        public static readonly string PathToOutputImages = Path.Combine(ApplicationPath, "OutputImages") + Path.DirectorySeparatorChar;
        public static readonly string PathToAtlasImages = PathToInputImages + "atlas" + Path.DirectorySeparatorChar;
        
        static Module()
        {
            LoadLibraries();
        }

        public static void LoadLibraries()
        {
            NativeLibrary.PreloadLibrary("AtitcWrapper.dll");
            NativeLibrary.PreloadLibrary("DxtWrapper.dll");
            NativeLibrary.PreloadLibrary("PVRTexLib.dll");
            NativeLibrary.PreloadLibrary("PvrttWrapper.dll");
            NativeLibrary.PreloadLibrary("FreeImage.dll");
            NativeLibrary.PreloadLibrary("FreeImageNET.dll");
        }
    }
}
