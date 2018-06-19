// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Data;

namespace Xenko.Graphics
{
    //Workaround needed for now, since we don't support orientation changes during game play
    public enum RequiredDisplayOrientation
    {
        /// <summary>
        /// The default value for the orientation.
        /// </summary>
        Default = DisplayOrientation.Default,

        /// <summary>
        /// Displays in landscape mode to the left.
        /// </summary>
        [Display("Landscape Left")]
        LandscapeLeft = DisplayOrientation.LandscapeLeft,

        /// <summary>
        /// Displays in landscape mode to the right.
        /// </summary>
        [Display("Landscape Right")]
        LandscapeRight = DisplayOrientation.LandscapeRight,

        /// <summary>
        /// Displays in portrait mode.
        /// </summary>
        Portrait = DisplayOrientation.Portrait
    }

    public enum PreferredGraphicsPlatform
    {
        Default,

        /// <summary>
        /// Direct3D11.
        /// </summary>
        Direct3D11,

        /// <summary>
        /// Direct3D12.
        /// </summary>
        Direct3D12,

        /// <summary>
        /// OpenGL.
        /// </summary>
        OpenGL,

        /// <summary>
        /// OpenGL ES.
        /// </summary>
        [Display("OpenGL ES")]
        OpenGLES,

        /// <summary>
        /// Vulkan
        /// </summary>
        Vulkan
    }

    [DataContract]
    [Display("Rendering")]
    public class RenderingSettings : Configuration
    {
        /// <summary>
        /// Gets or sets the width of the back buffer.
        /// </summary>
        /// <userdoc>
        /// The desired back buffer width.
        /// Might be overriden depending on actual device resolution and/or ratio.
        /// On Windows, it will be the window size. On Android/iOS, it will be the off-screen target resolution.
        /// </userdoc>
        [DataMember(0)]
        public int DefaultBackBufferWidth = 1280;

        /// <summary>
        /// Gets or sets the height of the back buffer.
        /// </summary>
        /// <userdoc>
        /// The desired back buffer height.
        /// Might be overriden depending on actual device resolution and/or ratio.
        /// On Windows, it will be the window size. On Android/iOS, it will be the off-screen target resolution.
        /// </userdoc>
        [DataMember(10)]
        public int DefaultBackBufferHeight = 720;

        /// <summary>
        /// Gets or sets a value that if true will make sure that the aspect ratio of screen is kept.
        /// </summary>
        /// <userdoc>
        /// If true, adapt the ratio of the back buffer so that it fits the screen ratio.
        /// </userdoc>
        [DataMember(15)]
        public bool AdaptBackBufferToScreen = false;

        /// <summary>
        /// Gets or sets the default graphics profile.
        /// </summary>
        /// <userdoc>The graphics feature level this game require.</userdoc>
        [DataMember(20)]
        public GraphicsProfile DefaultGraphicsProfile = GraphicsProfile.Level_10_0;

        /// <summary>
        /// Gets or sets the colorspace.
        /// </summary>
        /// <value>The colorspace.</value>
        /// <userdoc>The colorspace (Gamma or Linear) used for rendering. This value affects both the runtime and editor.</userdoc>
        [DataMember(30)]
        public ColorSpace ColorSpace = ColorSpace.Linear;

        /// <summary>
        /// Gets or sets the display orientation.
        /// </summary>
        /// <userdoc>The display orientations this game support.</userdoc>
        [DataMember(40)]
        public RequiredDisplayOrientation DisplayOrientation = RequiredDisplayOrientation.Default;

        /// <summary>
        /// Gets or sets the target graphics platform.
        /// </summary>
        /// <userdoc>
        /// The project will be built for the specified graphics platform.
        /// Leave as Default to let the engine choose the most appropriate platform depending on the hardware.
        /// </userdoc>
        [DataMember(50)]
        [Display("Target graphics platform")]
        public PreferredGraphicsPlatform PreferredGraphicsPlatform = PreferredGraphicsPlatform.Default;

        private static GraphicsPlatform GetDefaultGraphicsPlatform(PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Windows:
                case PlatformType.UWP:
                    return GraphicsPlatform.Direct3D11;
                case PlatformType.Android:
                case PlatformType.iOS:
                    return GraphicsPlatform.OpenGLES;
                case PlatformType.Linux:
                case PlatformType.macOS:
                    return GraphicsPlatform.OpenGL;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool CheckGraphicsPlatform(PlatformType platform, PreferredGraphicsPlatform preferredGraphicsPlatform)
        {
            switch (platform)
            {
                case PlatformType.Shared:
                    return false;
                case PlatformType.UWP:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.Direct3D11;
                case PlatformType.Windows:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.Direct3D11
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.Direct3D12
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGL
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGLES
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.Vulkan;
                case PlatformType.Android:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGLES
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.Vulkan;
                case PlatformType.iOS:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGLES;
                case PlatformType.macOS:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGL;
                case PlatformType.Linux:
                    return preferredGraphicsPlatform == PreferredGraphicsPlatform.OpenGL
                        || preferredGraphicsPlatform == PreferredGraphicsPlatform.Vulkan;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }

        public static GraphicsPlatform GetGraphicsPlatform(PlatformType platform, PreferredGraphicsPlatform preferredGraphicsPlatform)
        {
            //revert to default if platforms are not compatible
            if (!CheckGraphicsPlatform(platform, preferredGraphicsPlatform))
            {
                return GetDefaultGraphicsPlatform(platform);
            }

            GraphicsPlatform graphicsPlatform;
            switch (preferredGraphicsPlatform)
            {
                case PreferredGraphicsPlatform.Default:
                    graphicsPlatform = GetDefaultGraphicsPlatform(platform);
                    break;
                case PreferredGraphicsPlatform.Direct3D11:
                    graphicsPlatform = GraphicsPlatform.Direct3D11;
                    break;
                case PreferredGraphicsPlatform.Direct3D12:
                    graphicsPlatform = GraphicsPlatform.Direct3D12;
                    break;
                case PreferredGraphicsPlatform.OpenGL:
                    graphicsPlatform = GraphicsPlatform.OpenGL;
                    break;
                case PreferredGraphicsPlatform.OpenGLES:
                    graphicsPlatform = GraphicsPlatform.OpenGLES;
                    break;
                case PreferredGraphicsPlatform.Vulkan:
                    graphicsPlatform = GraphicsPlatform.Vulkan;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return graphicsPlatform;
        }
    }
}
