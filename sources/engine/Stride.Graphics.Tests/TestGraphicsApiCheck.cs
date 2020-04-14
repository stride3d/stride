// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
/* THIS CODE IS DISABLED, WE WILL HAVE TO CLEANUP ASSEMBLY DEPENDENCIES
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
using System;
using System.IO;

using Xunit;

using Stride.PublicApiCheck;

namespace Stride.Graphics
{
    // CANNOT WORK INSIDE THE SAME SOLUTION. NEED TO RUN THIS OUTSIDE THE SOLUTION
    [Description("Check public Graphics API consistency between Reference, Direct3D, OpenGL42, OpenGLES")]
    public class TestGraphicsApi
    {
        public const string Platform = "Windows";
        public const string Target = "Debug";

        private const string PathPattern = @"..\..\..\..\..\..\Build\{0}-{1}-{2}\{3}";

        private static readonly string RootPath = Environment.CurrentDirectory;

        private static readonly string ReferencePath = Path.Combine(RootPath, GraphicsPath("Null"));
        private static readonly string GraphicsDirect3DPath = Path.Combine(RootPath, GraphicsPath("Direct3D"));
        private static readonly string OpenGL4Path = Path.Combine(RootPath, GraphicsPath("OpenGL"));
        private static readonly string OpenGLESPath = Path.Combine(RootPath, GraphicsPath("OpenGLES"));

        private static string GraphicsPath(string api)
        {
            return string.Format(PathPattern, Platform, api, Target, "Stride.Graphics.dll");
        }


        [Fact]
        public void TestDirect3D()
        {
            Assert.That(ApiCheck.DiffAssemblyToString(ReferencePath, GraphicsDirect3DPath), Is.Null);
        }

        [Fact]
        public void TestOpenGL42()
        {
            Assert.That(ApiCheck.DiffAssemblyToString(ReferencePath, OpenGL4Path), Is.Null);
        }

        [Fact]
        public void TestOpenGLES()
        {
            Assert.That(ApiCheck.DiffAssemblyToString(ReferencePath, OpenGLESPath), Is.Null);
        }
    }
}
#endif
*/
