// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_OPENGL
using System.Linq;
using Stride.Graphics.OpenGL;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif
namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters.
    /// </summary>
    public partial class GraphicsAdapter
    {
        private GraphicsProfile supportedGraphicsProfile;

        internal GraphicsAdapter()
        {
            outputs = new [] { new GraphicsOutput() };

            // set default values
            int detectedVersion = 100;

            var renderer = GL.GetString(StringName.Renderer);
            var vendor = GL.GetString(StringName.Vendor);

            // Stay close to D3D: Cut renderer after first / (ex: "GeForce 670/PCIe/SSE2")
            var rendererSlash = renderer.IndexOf('/');
            if (rendererSlash != -1)
                renderer = renderer.Substring(0, rendererSlash);

            // Stay close to D3D: Remove "Corporation" from vendor
            vendor = vendor.Replace(" Corporation", string.Empty);

            // Generate adapter Description
            Description = $"{vendor} {renderer}";

            // get real values
            // using glGetIntegerv(GL_MAJOR_VERSION / GL_MINOR_VERSION) only works on opengl (es) > 3.0
            var version = GL.GetString(StringName.Version);
            if (version != null)
            {
                var splitVersion = version.Split(new char[] { '.', ' ' });
                // find first number occurrence because:
                //   - on OpenGL, "<major>.<minor>"
                //   - on OpenGL ES, "OpenGL ES <profile> <major>.<minor>"
                for (var i = 0; i < splitVersion.Length - 1; ++i)
                {
                    int versionMajor, versionMinor;
                    if (int.TryParse(splitVersion[i], out versionMajor))
                    {
                        // Note: minor version might have stuff concat, take only until not digits
                        var versionMinorString = splitVersion[i + 1];
                        versionMinorString = new string(versionMinorString.TakeWhile(c => char.IsDigit(c)).ToArray());

                        int.TryParse(versionMinorString, out versionMinor);

                        detectedVersion = versionMajor * 100 + versionMinor * 10;
                        break;
                    }
                }
            }

            supportedGraphicsProfile = OpenGLUtils.GetFeatureLevel(detectedVersion);
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
