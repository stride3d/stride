// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_OPENGL
using System.Linq;
using Silk.NET.SDL;
using Stride.Graphics.OpenGL;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters.
    /// </summary>
    public unsafe partial class GraphicsAdapter
    {
        private GraphicsProfile supportedGraphicsProfile;
        internal int OpenGLVersion;
        internal string OpenGLRenderer;

        internal static Silk.NET.SDL.Window* DefaultWindow;

        internal GraphicsAdapter()
        {
            outputs = new [] { new GraphicsOutput() };

            // set default values
            int detectedVersion = 100;

            string renderer, vendor, version;
            int versionMajor, versionMinor;

            var SDL = Stride.Graphics.SDL.Window.SDL;
            // Some platforms (i.e. Android) can only have a single window
            var sdlWindow = DefaultWindow;
            if (sdlWindow == null)
                sdlWindow = SDL.CreateWindow("Stride Hidden OpenGL", 50, 50, 1280, 720, (uint)(WindowFlags.WindowHidden | WindowFlags.WindowOpengl));

            using (var sdlContext = new SdlContext(SDL, sdlWindow))
            using (var gl = GL.GetApi(sdlContext))
            {
#if STRIDE_GRAPHICS_API_OPENGLES
                SDL.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileES);
#else
                SDL.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileCore);
#endif
                sdlContext.Create();
                renderer = gl.GetStringS(StringName.Renderer);
                vendor = gl.GetStringS(StringName.Vendor);
                version = gl.GetStringS(StringName.Version);
                gl.GetInteger(GetPName.MajorVersion, out versionMajor);
                gl.GetInteger(GetPName.MinorVersion, out versionMinor);
            }
            if (sdlWindow != DefaultWindow)
                SDL.DestroyWindow(sdlWindow);

            // Stay close to D3D: Cut renderer after first / (ex: "GeForce 670/PCIe/SSE2")
            var rendererSlash = renderer.IndexOf('/');
            if (rendererSlash != -1)
                renderer = renderer.Substring(0, rendererSlash);

            // Stay close to D3D: Remove "Corporation" from vendor
            vendor = vendor.Replace(" Corporation", string.Empty);

            // Generate adapter Description
            Description = $"{vendor} {renderer}";

            // get real values
            // Note: using glGetIntegerv(GL_MAJOR_VERSION / GL_MINOR_VERSION) only works on opengl (es) >= 3.0
            detectedVersion = versionMajor * 100 + versionMinor * 10;
            supportedGraphicsProfile = OpenGLUtils.GetFeatureLevel(detectedVersion);

            OpenGLVersion = detectedVersion;
            OpenGLRenderer = renderer;
        }

        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
            // TODO: Check OpenGL version?
            // TODO: ES specific code?
            return graphicsProfile <= supportedGraphicsProfile;
        }

        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; }

        /// <summary>
        /// Determines if this instance of GraphicsAdapter is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the vendor identifier.
        /// </summary>
        /// <value>
        /// The vendor identifier.
        /// </value>
        public int VendorId
        {
            get { return 0; }
        }
    }
}

#endif
