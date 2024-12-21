// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
// Global usings
#if STRIDE_GRAPHICS_API_OPENGLES
global using Silk.NET.OpenGLES;
global using Silk.NET.OpenGLES.Extensions.EXT;
global using PixelFormatGl = Silk.NET.OpenGLES.PixelFormat;
global using PrimitiveTypeGl = Silk.NET.OpenGLES.PrimitiveType;
#else
global using Silk.NET.OpenGL;
global using PixelFormatGl = Silk.NET.OpenGL.PixelFormat;
global using PrimitiveTypeGl = Silk.NET.OpenGL.PrimitiveType;
#endif
global using Texture = Stride.Graphics.Texture;
#endif
