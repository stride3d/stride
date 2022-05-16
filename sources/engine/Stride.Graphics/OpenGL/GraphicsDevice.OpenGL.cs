// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Silk.NET.Core.Contexts;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Shaders;
using Stride.Graphics.OpenGL;
using Color4 = Stride.Core.Mathematics.Color4;
#if STRIDE_PLATFORM_ANDROID
using Monitor = System.Threading.Monitor;
#endif

#if STRIDE_UI_SDL
using Silk.NET.SDL;
using WindowState = Stride.Graphics.SDL.FormWindowState;
#endif

namespace Stride.Graphics
{
    /// <summary>
    /// Performs primitive-based rendering, creates resources, handles system-level variables, adjusts gamma ramp levels, and creates shaders.
    /// </summary>
    public partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = 16;

        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        internal int FrameCounter;

        // Used when locking asyncCreationLockObject
        private bool asyncCreationLockTaken;

        internal bool ApplicationPaused = false;
        internal bool ProfileEnabled = false;

        internal object asyncCreationLockObject = new object();
        internal IGLContext deviceCreationContext;

        internal uint defaultVAO;

        internal uint CopyColorSourceFBO, CopyDepthSourceFBO;

        DebugProc debugCallbackInstance;

        private const GraphicsPlatform GraphicPlatform =
#if STRIDE_GRAPHICS_API_OPENGLES
                                                            GraphicsPlatform.OpenGLES;
#else
                                                            GraphicsPlatform.OpenGL;
#endif

        internal SamplerState DefaultSamplerState;
        internal DepthStencilState defaultDepthStencilState;
        internal BlendState defaultBlendState;
        internal GraphicsProfile requestedGraphicsProfile;
        internal int version; // queried version
        internal int currentVersion; // glGetVersion
        internal Texture WindowProvidedRenderTexture;
        internal uint WindowProvidedFrameBuffer;

        internal bool HasDXT;

        internal bool HasDepthClamp;

        internal bool HasAnisotropicFiltering;

        internal bool HasTextureBuffers;
        internal bool HasKhronosDebug;
        internal bool HasTimerQueries;

        internal bool HasExtTextureFormatBGRA8888;

        private bool isFramebufferSRGB;

        private int contextBeginCounter = 0;

        // TODO: Use some LRU scheme to clean up FBOs if not used frequently anymore.
        internal Dictionary<FBOKey, uint> existingFBOs = new Dictionary<FBOKey, uint>(); 

        private static GraphicsDevice _currentGraphicsDevice = null;

        [ThreadStatic] private static List<GraphicsDevice> _graphicsDevicesInUse;

        public static GraphicsDevice Current
        {
            get
            {
                if (_graphicsDevicesInUse != null && _graphicsDevicesInUse.Count > 0)
                    return _graphicsDevicesInUse[_graphicsDevicesInUse.Count - 1];

                return _currentGraphicsDevice;
            }

            set
            {
                _currentGraphicsDevice = value;
            }
        }

#if STRIDE_UI_SDL
        private Stride.Graphics.SDL.Window gameWindow;
        internal IGLContext MainGraphicsContext;

        internal unsafe IntPtr CurrentGraphicsContext => (IntPtr)Graphics.SDL.Window.SDL.GLGetCurrentContext();
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
        // Need to change sampler state depending on if texture has mipmap or not during PreDraw
        private bool[] hasMipmaps = new bool[64];
#endif
        public GL GL { get; internal set; }
#if STRIDE_GRAPHICS_API_OPENGLES
        public ExtDisjointTimerQuery GLExtDisjointTimerQuery { get; internal set; }
#endif

        private uint copyProgram = 0;
        private int copyProgramOffsetLocation = -1;
        private int copyProgramScaleLocation = -1;

        private uint copyProgramSRgb = 0;
        private int copyProgramSRgbOffsetLocation = -1;
        private int copyProgramSRgbScaleLocation = -1;

        internal float[] SquareVertices = {
            0.0f, 0.0f,
            1.0f, 0.0f,
            0.0f, 1.0f, 
            1.0f, 1.0f,
        };

        internal Buffer SquareBuffer;

        /// <summary>
        /// The tick frquency of timestamp queries in Hertz.
        /// </summary>
        public long TimestampFrequency { get; } = 1000_000_000L;

        /// <summary>
        /// Gets the status of this device.
        /// </summary>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                // TODO implement GraphicsDeviceStatus for OpenGL
                return GraphicsDeviceStatus.Normal;
            }
        }

        public void Use()
        {
            if (_graphicsDevicesInUse == null)
                _graphicsDevicesInUse = new List<GraphicsDevice>();

            if (!_graphicsDevicesInUse.Contains(this))
                _graphicsDevicesInUse.Add(this);
        }

        public void Unuse()
        {
            if (_graphicsDevicesInUse == null)
                return;

            _graphicsDevicesInUse.Remove(this);

            if (_graphicsDevicesInUse.Count == 0)
                _graphicsDevicesInUse = null;
        }

        internal UseOpenGLCreationContext UseOpenGLCreationContext()
        {
            return new UseOpenGLCreationContext(this);
        }

        /// <summary>
        /// Marks context as active on the current thread.
        /// </summary>
        public void Begin()
        {
            ++contextBeginCounter;

            if (contextBeginCounter == 1)
            {
                FrameCounter++;

                MainGraphicsContext.MakeCurrent();
            }
        }

        /// <summary>
        /// Unmarks context as active on the current thread.
        /// </summary>
        public void End()
        {
#if DEBUG
            EnsureContextActive();
#endif

            --contextBeginCounter;
            if (contextBeginCounter == 0)
            {
                UnbindGraphicsContext(MainGraphicsContext);
            }
            else if (contextBeginCounter < 0)
            {
                throw new Exception("End context was called more than Begin");
            }
        }

        internal Buffer GetSquareBuffer()
        {
            if (SquareBuffer == null)
            {
                SquareBuffer = Buffer.New(this, SquareVertices, BufferFlags.VertexBuffer);
            }

            return SquareBuffer;
        }

        internal uint GetCopyProgram(bool srgb, out int offsetLocation, out int scaleLocation)
        {
            if (srgb)
            {
                if (copyProgramSRgb == 0)
                {
                    copyProgramSRgb = CreateCopyProgram(true, out copyProgramSRgbOffsetLocation, out copyProgramSRgbScaleLocation);
                }
                offsetLocation = copyProgramSRgbOffsetLocation;
                scaleLocation = copyProgramSRgbScaleLocation;
                return copyProgramSRgb;
            }
            else
            {
                if (copyProgram == 0)
                {
                    copyProgram = CreateCopyProgram(false, out copyProgramOffsetLocation, out copyProgramScaleLocation);
                }
                offsetLocation = copyProgramOffsetLocation;
                scaleLocation = copyProgramScaleLocation;
                return copyProgram;
            }
        }

        private uint CreateCopyProgram(bool srgb, out int offsetLocation, out int scaleLocation)
        {
#if STRIDE_GRAPHICS_API_OPENGLES
            // We aim at OpenGLES 3.0 or greater.
            var shaderVersion = "#version 300 es";
#else
            var shaderVersion = "#version 410";
#endif

            string copyVertexShaderSource =
                shaderVersion + "\n" +
                "in vec2 aPosition;   \n" +
                "out vec2 vTexCoord;  \n" +
                "uniform vec4 uScale;     \n" +
                "uniform vec4 uOffset;     \n" +
                "void main()                 \n" +
                "{                           \n" +
                "   vec4 transformedPosition = aPosition.xyxy * uScale + uOffset;" +
                "   gl_Position = vec4(transformedPosition.zw * 2.0 - 1.0, 0.0, 1.0); \n" +
                "   vTexCoord = transformedPosition.xy;   \n" +
                "}                           \n";

            string copyFragmentShaderSource =
                shaderVersion + "\n" +
                "precision mediump float;                            \n" +
                "in vec2 vTexCoord;                  \n" +
                "out vec4 gFragColor;\n" +
                "uniform sampler2D s_texture;                        \n" +
                "void main()                                         \n" +
                "{                                                   \n" +
                "    gFragColor = texture(s_texture, vTexCoord); \n" +
                "}                                                   \n";

            string copyFragmentShaderSourceSRgb =
                shaderVersion + "\n" +
                "precision mediump float;                            \n" +
                "in vec2 vTexCoord;                  \n" +
                "out vec4 gFragColor;\n" +
                "uniform sampler2D s_texture;                        \n" +
                "void main()                                         \n" +
                "{                                                   \n" +
                "    vec4 color = texture(s_texture, vTexCoord);   \n" +
                "    gFragColor = vec4(sqrt(color.rgb), color.a); \n" +  // approximation of linear to SRgb
                "}                                                   \n";

            // First initialization of shader program
            var vertexShader = TryCompileShader(ShaderType.VertexShader, copyVertexShaderSource);
            var fragmentShader = TryCompileShader(ShaderType.FragmentShader, srgb ? copyFragmentShaderSourceSRgb : copyFragmentShaderSource);

            var program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.BindAttribLocation(program, 0, "aPosition");
            GL.LinkProgram(program);

            int linkStatus;
            GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out linkStatus);

            if (linkStatus != 1)
                throw new InvalidOperationException("Error while linking GLSL shaders.");

            GL.UseProgram(program);
            var textureLocation = GL.GetUniformLocation(program, "s_texture");
            offsetLocation = GL.GetUniformLocation(program, "uOffset");
            scaleLocation = GL.GetUniformLocation(program, "uScale");
            GL.Uniform1(textureLocation, 0);

            return program;
        }

        public void EnableProfile(bool enabledFlag)
        {
            ProfileEnabled = true;
        }

        internal void EnsureContextActive()
        {
            // TODO: Better checks (is active context the expected one?)
            var context = CurrentGraphicsContext;
            if (context == IntPtr.Zero)
                throw new InvalidOperationException("No OpenGL context bound.");
        }

        public void ExecuteCommandList(CompiledCommandList commandList)
        {
#if DEBUG
            EnsureContextActive();
#endif

            throw new NotImplementedException();
        }

        public void ExecuteCommandLists(int count, CompiledCommandList[] commandList)
        {
#if DEBUG
            EnsureContextActive();
#endif

            throw new NotImplementedException();
        }

        internal uint FindOrCreateFBO(GraphicsResourceBase graphicsResource, int subresource)
        {
            if (graphicsResource == WindowProvidedRenderTexture)
                return WindowProvidedFrameBuffer;

            var texture = graphicsResource as Texture;
            if (texture != null)
            {
                return FindOrCreateFBO(new FBOTexture(texture, subresource / texture.MipLevels, subresource % texture.MipLevels));
            }

            throw new NotSupportedException();
        }

        internal uint FindOrCreateFBO(FBOTexture texture)
        {
            var isDepthBuffer = ((texture.Texture.Flags & TextureFlags.DepthStencil) != 0);
            lock (existingFBOs)
            {
                foreach (var key in existingFBOs)
                {
                    if ((isDepthBuffer && key.Key.DepthStencilBuffer == texture)
                        || !isDepthBuffer && key.Key.RenderTargetCount == 1 && key.Key.RenderTargets[0] == texture)
                        return key.Value;
                }
            }

            if (isDepthBuffer)
                return FindOrCreateFBO(texture, null, 0);
            return FindOrCreateFBO(null, new FBOTexture[] { texture }, 1);
        }

        // TODO: I think having a class for FBOs would simplify some stuff. We could implement methods like "Bind()" for it.
        uint GenerateFBO(FBOTexture depthStencilBuffer, FBOTexture[] renderTargets, int renderTargetCount)
        {
            GL.GenFramebuffers(1, out uint fboID);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboID);
            UpdateFBO(FramebufferTarget.Framebuffer, depthStencilBuffer, renderTargets, renderTargetCount);

            var framebufferStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (framebufferStatus != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException(string.Format("FBO is incomplete: {0} RTs: [RT0: {1}]; Depth {2} (error: {3})",
                                                                  renderTargetCount,
                                                                  renderTargets != null && renderTargets.Length > 0 && renderTargets[0].Texture != null ? renderTargets[0].Texture.TextureId : 0,
                                                                  depthStencilBuffer.Texture != null ? depthStencilBuffer.Texture.TextureId : 0,
                                                                  framebufferStatus));
            }

            FBOTexture[] newFBOTextures = null;
            if (renderTargets != null)
            {
                newFBOTextures = (FBOTexture[])renderTargets.Clone();
            }

            FBOKey newFBOKey = new FBOKey(depthStencilBuffer, newFBOTextures, renderTargetCount);
            existingFBOs.Add(newFBOKey, fboID);

            return fboID;
        }

        internal uint FindOrCreateFBO(FBOTexture depthStencilBuffer, FBOTexture[] renderTargets, int renderTargetCount)  // TODO: What's the point of passing an array that has reduntant elements? This could probably be reduced to only the "renderTargets" parameter.
        {
            // Check for existing FBO matching this configuration
            lock (existingFBOs) // TODO: PERFORMANCE: Why is this lock here? Do we ever run this from multiple threads? If so, why?
            {
                // Check if the default-provided render target was requested:
                // TODO: Need to disable some part of rendering if either is null
                var isProvidedRenderTarget = (renderTargetCount == 1 && renderTargets[0] == WindowProvidedRenderTexture);
                if (isProvidedRenderTarget && depthStencilBuffer.Texture != null)
                {
                    throw new InvalidOperationException("It is impossible to bind device provided and user created buffers with OpenGL");
                }
                if (depthStencilBuffer.Texture == null && (isProvidedRenderTarget || renderTargetCount == 0)) // device provided framebuffer
                {
                    return WindowProvidedFrameBuffer;
                }

                // Check if there is an already existing FBO:
                var fboKey = new FBOKey(depthStencilBuffer, renderTargets, renderTargetCount);

                if (existingFBOs.TryGetValue(fboKey, out var fboID))
                    return fboID;

                // Since the desired FBO doesn't already exist, we generate it:
                return GenerateFBO(depthStencilBuffer, renderTargets, renderTargetCount);
            }
        }

        internal FramebufferAttachment UpdateFBO(FramebufferTarget framebufferTarget, FBOTexture renderTarget)
        {
            var texture = renderTarget.Texture;
            var isDepthBuffer = Texture.InternalIsDepthStencilFormat(texture.Format);
            if (isDepthBuffer)
            {
                return UpdateFBODepthStencilAttachment(framebufferTarget, renderTarget);
            }
            else
            {
                UpdateFBOColorAttachment(framebufferTarget, 0, renderTarget);
                return FramebufferAttachment.ColorAttachment0;
            }
        }

        internal void UpdateFBO(FramebufferTarget framebufferTarget, FBOTexture depthStencilBuffer, FBOTexture[] renderTargets, int renderTargetCount)  // TODO: What's the point of passing an array that has reduntant elements? This could probably be reduced to only the "renderTargets" parameter.
        {
            for (int i = 0; i < renderTargetCount; ++i)
            {
                UpdateFBOColorAttachment(framebufferTarget, i, renderTargets[i]);
            }

#if !STRIDE_GRAPHICS_API_OPENGLES
            if (renderTargetCount <= 1)
            {
                GL.DrawBuffer(renderTargetCount != 0 ? DrawBufferMode.ColorAttachment0 : DrawBufferMode.None);
            }
            else
#endif
            {
                // Specify which attachments to render to (all of them in our case):
                var drawBuffers = new DrawBufferMode[renderTargetCount];
                for (var i = 0; i < renderTargetCount; ++i)
                    drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
                GL.DrawBuffers((uint)renderTargetCount, drawBuffers);
            }

            if (depthStencilBuffer.Texture != null)
            {
                UpdateFBODepthStencilAttachment(framebufferTarget, depthStencilBuffer);
            }
        }

        void BindColorAttachment(FramebufferTarget framebufferTarget, int i, FBOTexture renderTarget)
        {
            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0 + i;

            if (renderTarget.Texture.IsMultisample)
            {
                if (renderTarget.Texture.IsRenderbuffer)
                {
                    GL.FramebufferRenderbuffer(framebufferTarget, attachment, RenderbufferTarget.Renderbuffer, renderTarget.Texture.TextureId);
                }
                else
                {
#if STRIDE_GRAPHICS_API_OPENGLES
                    throw new NotSupportedException("Multisample textures are not supported on OpenGL ES.");
#else
                    GL.FramebufferTexture2D(framebufferTarget, attachment, renderTarget.Texture.TextureTarget, renderTarget.Texture.TextureId, renderTarget.MipLevel);
#endif
                }
            }
            else
            {
                var textureTarget = Texture.GetTextureTargetForDataSet2D(renderTarget.Texture.TextureTarget, renderTarget.ArraySlice % 6);
                GL.FramebufferTexture2D(framebufferTarget, attachment, textureTarget, renderTarget.Texture.TextureId, renderTarget.MipLevel);
            }
        }

        internal void UpdateFBOColorAttachment(FramebufferTarget framebufferTarget, int i, FBOTexture renderTarget)
        {
            switch (renderTarget.Texture.TextureTarget)
            {
#if !STRIDE_GRAPHICS_API_OPENGLES
                case TextureTarget.Texture1D:
                    GL.FramebufferTexture1D(framebufferTarget, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture1D, renderTarget.Texture.TextureId, renderTarget.MipLevel);
                    break;
#endif
                case TextureTarget.Texture2D:
                case TextureTarget.TextureCubeMap:
                    // We don't make use of the "TextureTarget.Texture2DMultisample" enum value on purpose, because it
                    // allows for better code sharing between OpenGL ES and OpenGL. We simply use "TextureTarget.Texture2D"
                    // and check the value of "IsMultisample" instead. This is because OpenGL ES doesn't support
                    // multisample textures, but only multisample renderbuffers.
                    BindColorAttachment(framebufferTarget, i, renderTarget);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    GL.FramebufferTextureLayer(framebufferTarget, FramebufferAttachment.ColorAttachment0 + i, renderTarget.Texture.TextureId, renderTarget.MipLevel, renderTarget.ArraySlice);
                    break;
                default:
                    throw new NotImplementedException($"Can't bind FBO with target [{renderTarget.Texture.TextureTarget}]");
            }
        }

        internal FramebufferAttachment UpdateFBODepthStencilAttachment(FramebufferTarget framebufferTarget, FBOTexture depthStencilBuffer)
        {
            bool useSharedAttachment = depthStencilBuffer.Texture.StencilId == depthStencilBuffer.Texture.TextureId;
            var attachmentType = useSharedAttachment ? (FramebufferAttachment)GLEnum.DepthStencilAttachment : FramebufferAttachment.DepthAttachment;

            if (depthStencilBuffer.Texture.IsRenderbuffer)
            {
                // Bind depth-only or packed depth-stencil buffer
                GL.FramebufferRenderbuffer(framebufferTarget, attachmentType, RenderbufferTarget.Renderbuffer, depthStencilBuffer.Texture.TextureId);

                // If stencil buffer is separate, it's resource id might be stored in depthStencilBuffer.Texture.StencilId
                if (depthStencilBuffer.Texture.HasStencil && !useSharedAttachment)
                {
                    GL.FramebufferRenderbuffer(framebufferTarget, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, depthStencilBuffer.Texture.StencilId);
                }
            }
            else
            {
                var textureTarget2d = TextureTarget.Texture2D;
                if (depthStencilBuffer.Texture.IsMultisample)
                {
#if STRIDE_GRAPHICS_API_OPENGLES
                    throw new NotSupportedException("Multisample textures are not supported on OpenGL ES.");
#else
                    textureTarget2d = TextureTarget.Texture2DMultisample;
#endif
                }

                // Bind depth-only or packed depth-stencil buffer   // TODO: What about separate depth and stencil?
                GL.FramebufferTexture2D(framebufferTarget, attachmentType, textureTarget2d, depthStencilBuffer.Texture.TextureId, depthStencilBuffer.MipLevel);
            }

            return attachmentType;
        }

        internal uint TryCompileShader(ShaderType shaderType, string sourceCode)
        {
            var shaderGL = GL.CreateShader(shaderType);
            GL.ShaderSource(shaderGL, sourceCode);
            GL.CompileShader(shaderGL);

            var log = GL.GetShaderInfoLog(shaderGL);

            GL.GetShader(shaderGL, ShaderParameterName.CompileStatus, out var compileStatus);

            if (compileStatus != 1)
                throw new InvalidOperationException("Error while compiling GLSL shader: \n" + log);

            return shaderGL;
        }

        internal static void UnbindGraphicsContext(IGLContext graphicsContext)
        {
            graphicsContext.Clear();
        }

        private void OnApplicationPaused(object sender, EventArgs e)
        {
            // Block async resource creation
            Monitor.Enter(asyncCreationLockObject, ref asyncCreationLockTaken);

            ApplicationPaused = true;

            using (UseOpenGLCreationContext())
            {
                GL.Finish();
            }

            // Unset graphics context
            UnbindGraphicsContext(MainGraphicsContext);
        }

        private void OnApplicationResumed(object sender, EventArgs e)
        {
            // Reenable graphics context
            MainGraphicsContext.MakeCurrent();

            ApplicationPaused = false;

            // Reenable async resource creation
            if (asyncCreationLockTaken)
            {
                Monitor.Exit(asyncCreationLockObject);
                asyncCreationLockTaken = false;
            }
        }

        private string renderer;

        private string GetRendererName()
        {
            return renderer;
        }

        protected unsafe void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, WindowHandle windowHandle)
        {
            // set default values
            version = 100;

            requestedGraphicsProfile = GraphicsProfile.Level_9_1;

            // Find the first profile that is compatible with current GL version
            foreach (var graphicsProfile in graphicsProfiles)
            {
                if (Adapter.IsProfileSupported(graphicsProfile))
                {
                    requestedGraphicsProfile = graphicsProfile;
                    break;
                }
            }

            // Find back OpenGL version from requested version
            OpenGLUtils.GetGLVersion(requestedGraphicsProfile, out version);

            // check what is actually created
            currentVersion = Adapter.OpenGLVersion;
            renderer = Adapter.OpenGLRenderer;

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
            //gameWindow.Load += OnApplicationResumed;
            //gameWindow.Unload += OnApplicationPaused;
#endif

#if STRIDE_UI_SDL
            gameWindow = (Stride.Graphics.SDL.Window)windowHandle.NativeWindow;

            var SDL = Graphics.SDL.Window.SDL;

#if STRIDE_GRAPHICS_API_OPENGLES
            SDL.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileES);
#else
            SDL.GLSetAttribute(GLattr.GLContextProfileMask, (int)GLprofile.GLContextProfileCore);
#endif


            if (IsDebugMode)
                SDL.GLSetAttribute(GLattr.GLContextFlags, (int)GLcontextFlag.GLContextDebugFlag);

            // Setup version
            SDL.GLSetAttribute(GLattr.GLContextMajorVersion, currentVersion / 100);
            SDL.GLSetAttribute(GLattr.GLContextMinorVersion, (currentVersion / 10) % 10);

            MainGraphicsContext = new SdlContext(SDL, (Silk.NET.SDL.Window*)gameWindow.SdlHandle);
            ((SdlContext)MainGraphicsContext).Create();
            if (MainGraphicsContext.Handle == IntPtr.Zero)
            {
                throw new Exception("Cannot create OpenGL context: " + SDL.GetErrorS());
            }

            // The context must be made current to initialize OpenGL
            MainGraphicsContext.MakeCurrent();
#else
#error Creating context is only implemented for SDL
#endif

            // Create shared context for creating graphics resources from other threads
            SDL.GLSetAttribute(GLattr.GLShareWithCurrentContext, 1);
            deviceCreationContext = new SdlContext(SDL, (Silk.NET.SDL.Window*)gameWindow.SdlHandle);
            ((SdlContext)deviceCreationContext).Create();

            MainGraphicsContext.MakeCurrent();

            GL = GL.GetApi(MainGraphicsContext);
#if STRIDE_GRAPHICS_API_OPENGLES
            GLExtDisjointTimerQuery = new ExtDisjointTimerQuery(MainGraphicsContext);
#endif

            // Create default OpenGL State objects
            DefaultSamplerState = SamplerState.New(this, new SamplerStateDescription(TextureFilter.MinPointMagMipLinear, TextureAddressMode.Wrap) { MaxAnisotropy = 1 }).DisposeBy(this);
        }

        private unsafe void InitializePostFeatures()
        {
            // Create and bind default VAO
            GL.GenVertexArrays(1, out defaultVAO);
            GL.BindVertexArray(defaultVAO);

            // Save current FBO aside
            GL.GetInteger(GetPName.DrawFramebufferBinding, out var boundFBO);

            // Create FBO that will be used for copy operations
            CopyColorSourceFBO = GL.GenFramebuffer();
            CopyDepthSourceFBO = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, CopyDepthSourceFBO);
            GL.ReadBuffer(ReadBufferMode.None);

            // Restore FBO
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)boundFBO);

            if (IsDebugMode && HasKhronosDebug)
            {
                GL.DebugMessageCallback(debugCallbackInstance ?? (debugCallbackInstance = DebugCallback), null);
                GL.Enable(EnableCap.DebugOutputSynchronous);
                ProfileEnabled = true;

                // Also do it on async creation context
                deviceCreationContext.MakeCurrent();
                GL.DebugMessageCallback(debugCallbackInstance, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutputSynchronous);
                MainGraphicsContext.MakeCurrent();
            }

            // Create the main command list
            InternalMainCommandList = CommandList.New(this);
        }

        private void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
        }

        private static void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr userparam)
        {
            if ((DebugSeverity)severity == DebugSeverity.DebugSeverityHigh)
            {
                string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message);
                Log.Error($"[GL] {source}; {type}; {id}; {severity}; {msg}");
            }
        }

        protected void DestroyPlatformDevice()
        {
            // Hack: Reset the lock so that UseOpenGLCreationContext works (even if locked by previously called OnApplicationPaused, which might have been done in an unaccessible event thread)
            // TODO: Does it work with Tegra3?
            if (ApplicationPaused)
            {
                asyncCreationLockObject = new object();
            }

#if STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_IOS
            //gameWindow.Load -= OnApplicationResumed;
            //gameWindow.Unload -= OnApplicationPaused;
#endif
        }

        internal void OnDestroyed()
        {
            // Clear existing FBOs
            lock (existingFBOs)
            {
                existingFBOs.Clear();
                existingFBOs[new FBOKey(null, new FBOTexture[] { WindowProvidedRenderTexture }, 1)] = WindowProvidedFrameBuffer;
            }

            //// Clear bound states
            //for (int i = 0; i < boundTextures.Length; ++i)
            //boundTextures[i] = null;

            //boundFrontFace = FrontFaceDirection.Ccw;

            //boundVertexArrayObject = null;
            //enabledVertexAttribArrays = 0;
            //boundDepthStencilState = null;
            //boundStencilReference = 0;
            //boundBlendState = null;
            //boundRasterizerState = null;
            //boundDepthStencilBuffer = null;

            //for (int i = 0; i < boundRenderTargets.Length; ++i)
            //boundRenderTargets[i] = null;

            //boundFBO = 0;
            //boundFBOHeight = 0;
            //boundProgram = 0;
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            if (resourceLink.Resource is GraphicsResource resource)
                resource.DiscardNextMap = true;
        }

        internal void InitDefaultRenderTarget(PresentationParameters presentationParameters)
        {
#if DEBUG
            EnsureContextActive();
#endif

            // TODO: Provide unified ClientSize from GameWindow
#if STRIDE_GRAPHICS_API_OPENGLCORE
            var width = gameWindow.ClientSize.Width;
            var height = gameWindow.ClientSize.Height;
#else
            var width = gameWindow.Size.Width;
            var height = gameWindow.Size.Height;
#endif
            WindowProvidedFrameBuffer = 0;

            // TODO OPENGL detect if created framebuffer is sRGB or not (note: improperly reported by FramebufferParameterName.FramebufferAttachmentColorEncoding)
            isFramebufferSRGB = true;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, WindowProvidedFrameBuffer);

            // TODO: iOS (and possibly other platforms): get real render buffer ID for color/depth?
            WindowProvidedRenderTexture = Texture.New2D(this, width, height, 1,
                // TODO: As a workaround, because OpenTK(+OpenGLES) doesn't support to create SRgb backbuffer, we fake it by creating a non-SRgb here and CopyScaler2D is responsible to transform it to non SRgb
                isFramebufferSRGB ? presentationParameters.BackBufferFormat : presentationParameters.BackBufferFormat.ToNonSRgb(), TextureFlags.RenderTarget | Texture.TextureFlagsCustomResourceId);
            WindowProvidedRenderTexture.Reload = (graphicsResource, services) => { };

            // Extract FBO render target
            if (WindowProvidedFrameBuffer != 0)
            {
                int framebufferAttachmentType;
                GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferAttachmentParameterName.FramebufferAttachmentObjectType, out framebufferAttachmentType);
                if (framebufferAttachmentType == (int)GLEnum.Texture)
                {
                    int renderTargetTextureId;
                    GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, FramebufferAttachmentParameterName.FramebufferAttachmentObjectName, out renderTargetTextureId);
                    WindowProvidedRenderTexture.TextureId = (uint)renderTargetTextureId;
                }
            }

            existingFBOs[new FBOKey(null, new FBOTexture[] { WindowProvidedRenderTexture }, 1)] = WindowProvidedFrameBuffer;
        }

        private class SwapChainBackend
        {
            /// <summary>
            /// Default constructor to initialize fields that are not explicitly set to avoid warnings at compile time.
            /// </summary>
            internal SwapChainBackend()
            {
                PresentationParameters = null;
                PresentCount = 0;
            }

            public PresentationParameters PresentationParameters;
            public int PresentCount;
        }

        /// <summary>
        /// Creates a swap chain from presentation parameters.
        /// </summary>
        /// <param name="presentationParameters">The presentation parameters.</param>
        /// <returns></returns>
        private SwapChainBackend CreateSwapChainBackend(PresentationParameters presentationParameters)
        {
            var swapChainBackend = new SwapChainBackend();
            return swapChainBackend;
        }

        /// <summary>
        /// Gets the default presentation parameters associated with this graphics device.
        /// </summary>
        public PresentationParameters PresentationParameters
        {
            get { throw new InvalidOperationException(FrameworkResources.NoDefaultRenterTarget); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this GraphicsDevice is in fullscreen.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this GraphicsDevice is fullscreen; otherwise, <c>false</c>.
        /// </value>
        public bool IsFullScreen
        {
            get
            {
#if STRIDE_PLATFORM_DESKTOP
                return gameWindow.WindowState == WindowState.Fullscreen;
#else
                throw new NotImplementedException();
#endif
            }

            set
            {
#if STRIDE_PLATFORM_DESKTOP
                if (value ^ (gameWindow.WindowState == WindowState.Fullscreen))
                    gameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
#else
                throw new NotImplementedException();
#endif
            }
        }

        internal struct FBOTexture : IEquatable<FBOTexture>
        {
            public readonly Texture Texture;
            public readonly short ArraySlice;
            public readonly short MipLevel;

            public FBOTexture(Texture texture, int arraySlice = 0, int mipLevel = 0)
            {
                Texture = texture;
                ArraySlice = (short)arraySlice;
                MipLevel = (short)mipLevel;
            }

            public static implicit operator FBOTexture(Texture texture)
            {
                int arraySlice = 0;
                int mipLevel = 0;
                if (texture != null)
                {
                    mipLevel = texture.MipLevel;
                    arraySlice = texture.ArraySlice;
                }

                return new FBOTexture(texture, arraySlice, mipLevel);
            }
            
            public bool Equals(FBOTexture other)
            {
                return Texture == other.Texture && ArraySlice == other.ArraySlice && MipLevel == other.MipLevel;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is FBOTexture && Equals((FBOTexture)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Texture != null ? Texture.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ ArraySlice.GetHashCode();
                    hashCode = (hashCode * 397) ^ MipLevel.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(FBOTexture left, FBOTexture right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(FBOTexture left, FBOTexture right)
            {
                return !left.Equals(right);
            }
        }

        internal struct FBOKey : IEquatable<FBOKey>
        {
            public readonly FBOTexture DepthStencilBuffer;
            public readonly FBOTexture[] RenderTargets;
            public readonly int RenderTargetCount;

            public FBOKey(FBOTexture depthStencilBuffer, FBOTexture[] renderTargets, int renderTargetCount)
            {
                DepthStencilBuffer = depthStencilBuffer;
                RenderTargetCount = renderTargetCount;
                RenderTargets = RenderTargetCount != 0 ? renderTargets : null;
            }

            public bool Equals(FBOKey obj2)
            {
                if (obj2.DepthStencilBuffer != DepthStencilBuffer) return false;

                // Should have same number of render targets
                if (RenderTargetCount != obj2.RenderTargetCount)
                    return false;

                // Since both object have same LastRenderTarget, array is valid at least until this spot.
                for (int i = 0; i < RenderTargetCount; ++i)
                    if (obj2.RenderTargets[i] != RenderTargets[i])
                        return false;

                return true;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is FBOKey)) return false;

                var obj2 = (FBOKey)obj;

                return Equals(obj2);
            }

            public override int GetHashCode()
            {
                var result = DepthStencilBuffer != null ? DepthStencilBuffer.GetHashCode() : 0;
                if (RenderTargets != null)
                {
                    for (int index = 0; index < RenderTargetCount; index++)
                    {
                        var renderTarget = RenderTargets[index];
                        result ^= renderTarget != null ? renderTarget.GetHashCode() : 0;
                    }
                }
                return result;
            }
        }
    }
}

#endif
