// Copyright (c) Silk.NET
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if STRIDE_GRAPHICS_API_OPENGL 
using Silk.NET.Core.Loader;

namespace Silk.NET.OpenGL
{
    /// <summary>
    /// Contains the library name of OpenGL.
    /// </summary>
    internal class GLCoreLibraryNameContainer : SearchPathContainer
    {
        /// <inheritdoc />
        public override string[] Linux => new[] { "libGL.so.1" };

        /// <inheritdoc />
        public override string[] MacOS => new[] { "/System/Library/Frameworks/OpenGL.framework/OpenGL" };

        /// <inheritdoc />
        public override string[] Android => new[] { "libGL.so.1" };

        /// <inheritdoc />
        public override string[] IOS => new[] { "/System/Library/Frameworks/OpenGL.framework/OpenGL" };

        /// <inheritdoc />
        public override string[] Windows64 => new[] { "opengl32.dll" };

        /// <inheritdoc />
        public override string[] Windows86 => new[] { "opengl32.dll" };
    }
}
#endif
