// Copyright (c) Silk.NET
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if STRIDE_GRAPHICS_API_OPENGL 
using Silk.NET.Core.Loader;

namespace Silk.NET.OpenGLES
{
    /// <summary>
    /// Contains the library name of OpenGLES.
    /// </summary>
    internal class OpenGLESLibraryNameContainer : SearchPathContainer
    {
        /// <inheritdoc />
        public override string[] Linux => new[] { "libGLESv2.so" };

        /// <inheritdoc />
        public override string[] MacOS => new[] { "/System/Library/Frameworks/OpenGLES.framework/OpenGLES" };

        /// <inheritdoc />
        public override string[] Android => new[] { "libGLESv2.so" };

        /// <inheritdoc />
        public override string[] IOS => new[] { "/System/Library/Frameworks/OpenGLES.framework/OpenGLES" };

        /// <inheritdoc />
        public override string[] Windows64 => new[] { "libGLESv2.dll" };

        /// <inheritdoc />
        public override string[] Windows86 => new[] { "libGLESv2.dll" };
    }
}
#endif
