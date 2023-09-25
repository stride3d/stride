// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Stride.TextureConverter.Tests
{
    internal static class Module
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
            RuntimeHelpers.RunClassConstructor(typeof(TextureTool).TypeHandle);
        }
    }
}
