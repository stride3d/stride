// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Engine
{
    /// <summary>
    /// The scene system handles the scenes of a game.
    /// </summary>
    public class SceneSystem : GameSystemBase
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("SceneSystem");

        private RenderContext renderContext;
        private RenderDrawContext renderDrawContext;

        private int previousWidth;
        private int previousHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameSystemBase" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <remarks>The GameSystem is expecting the following services to be registered: <see cref="IGame" /> and <see cref="IContentManager" />.</remarks>
        public SceneSystem(IServiceRegistry registry)
            : base(registry)
        {
            Enabled = true;
            Visible = true;
            GraphicsCompositor = new GraphicsCompositor();
        }

        /// <summary>
        /// Gets or sets the root scene.
        /// </summary>
        /// <value>The scene</value>
        /// <exception cref="System.ArgumentNullException">Scene cannot be null</exception>
        public SceneInstance SceneInstance { get; set; }

        /// <summary>
        /// URL of the scene loaded at initialization.
        /// </summary>
        public string InitialSceneUrl { get; set; }

        /// <summary>
        /// URL of the graphics compositor loaded at initialization.
        /// </summary>
        public string InitialGraphicsCompositorUrl { get; set; }

        /// <summary>
        /// URL of the splash screen texture loaded at initialization.
        /// </summary>
        public string SplashScreenUrl { get; set; }

        /// <summary>
        /// Splash screen background color.
        /// </summary>
        public Color4 SplashScreenColor { get; set; }

        /// <summary>
        /// Is the splash screen displayed in VR double view.
        /// </summary>
        public bool DoubleViewSplashScreen { get; set; }

        /// <summary>
        /// If splash screen rendering is enabled, true if a splash screen texture is present, and only in release builds
        /// </summary>
        public bool SplashScreenEnabled { get; set; }

        public GraphicsCompositor GraphicsCompositor { get; set; }

        private Task<Scene> sceneTask;
        private Task<GraphicsCompositor> compositorTask;

        private const double MinSplashScreenTime = 4.0f;
        private const float SplashScreenFadeTime = 1.0f;

        private double fadeTime;
        private Texture splashScreenTexture;

        public enum SplashScreenState
        {
            Invalid,
            Intro,
            FadingIn,
            Showing,
            FadingOut,
        }

        private SplashScreenState splashScreenState = SplashScreenState.Invalid;

        protected override void LoadContent()
        {
            var content = Services.GetSafeServiceAs<ContentManager>();
            var graphicsContext = Services.GetSafeServiceAs<GraphicsContext>();

            if (SplashScreenUrl != null && content.Exists(SplashScreenUrl))
            {
                splashScreenTexture = content.Load<Texture>(SplashScreenUrl, ContentManagerLoaderSettings.StreamingDisabled);
                splashScreenState = splashScreenTexture != null ? SplashScreenState.Intro : SplashScreenState.Invalid;
                SplashScreenEnabled = true;
            }

            // Preload the scene if it exists and show splash screen
            if (InitialSceneUrl != null && content.Exists(InitialSceneUrl))
            {
                if (SplashScreenEnabled)
                    sceneTask = content.LoadAsync<Scene>(InitialSceneUrl);
                else
                    SceneInstance = new SceneInstance(Services, content.Load<Scene>(InitialSceneUrl));
            }

            if (InitialGraphicsCompositorUrl != null && content.Exists(InitialGraphicsCompositorUrl))
            {
                if (SplashScreenEnabled)
                    compositorTask = content.LoadAsync<GraphicsCompositor>(InitialGraphicsCompositorUrl);
                else
                    GraphicsCompositor = content.Load<GraphicsCompositor>(InitialGraphicsCompositorUrl);
            }

            // Create the drawing context
            renderContext = RenderContext.GetShared(Services);
            renderDrawContext = new RenderDrawContext(Services, renderContext, graphicsContext);
        }

        protected override void Destroy()
        {
            if (SceneInstance != null)
            {
                ((IReferencable)SceneInstance).Release();
                SceneInstance = null;
            }

            if (GraphicsCompositor != null)
            {
                GraphicsCompositor.Dispose();
                GraphicsCompositor = null;
            }

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            // Execute Update step of SceneInstance
            // This will run entity processors
            SceneInstance?.Update(gameTime);
        }

        private void RenderSplashScreen(Color4 color, BlendStateDescription blendState)
        {
            var renderTarget = Game.GraphicsContext.CommandList.RenderTarget;
            Game.GraphicsContext.CommandList.Clear(renderTarget, SplashScreenColor);
            
            var viewWidth = renderTarget.Width / (DoubleViewSplashScreen ? 2 : 1);
            var viewHeight = renderTarget.Height;
            var viewportSize = Math.Min(viewWidth, viewHeight);

            var initialViewport = Game.GraphicsContext.CommandList.Viewport;

            var x = (viewWidth - viewportSize) / 2;
            var y = (viewHeight - viewportSize) / 2;
            var newViewport = new Viewport(x, y, viewportSize, viewportSize);
            
            Game.GraphicsContext.CommandList.SetViewport(newViewport);
            Game.GraphicsContext.DrawTexture(splashScreenTexture, color, blendState);

            if (DoubleViewSplashScreen)
            {
                x += viewWidth;
                newViewport = new Viewport(x, y, viewportSize, viewportSize);

                Game.GraphicsContext.CommandList.SetViewport(newViewport);
                Game.GraphicsContext.DrawTexture(splashScreenTexture, color, blendState);
            }

            Game.GraphicsContext.CommandList.SetViewport(initialViewport);
        }

        public override void Draw(GameTime gameTime)
        {
            // Reset the context
            renderContext.Reset();

            var renderTarget = renderDrawContext.CommandList.RenderTarget;

            // If the width or height changed, we have to recycle all temporary allocated resources.
            // NOTE: We assume that they are mostly resolution dependent.
            if (previousWidth != renderTarget.ViewWidth || previousHeight != renderTarget.ViewHeight)
            {
                // Force a recycle of all allocated temporary textures
                renderContext.Allocator.Recycle(link => true);
            }

            previousWidth = renderTarget.ViewWidth;
            previousHeight = renderTarget.ViewHeight;

            // Update the entities at draw time.
            renderContext.Time = gameTime;

            // The camera processor needs the graphics compositor
            using (renderContext.PushTagAndRestore(GraphicsCompositor.Current, GraphicsCompositor))
            {
                // Execute Draw step of SceneInstance
                // This will run entity processors
                SceneInstance?.Draw(renderContext);
            }

            // Render phase
            // TODO GRAPHICS REFACTOR
            //context.GraphicsDevice.Parameters.Set(GlobalKeys.Time, (float)gameTime.Total.TotalSeconds);
            //context.GraphicsDevice.Parameters.Set(GlobalKeys.TimeStep, (float)gameTime.Elapsed.TotalSeconds);

            renderDrawContext.ResourceGroupAllocator.Flush();
            renderDrawContext.QueryManager.Flush();

            // Push context (pop after using)
            using (renderDrawContext.RenderContext.PushTagAndRestore(SceneInstance.Current, SceneInstance))
            {
                GraphicsCompositor?.Draw(renderDrawContext);
            }

            //do this here, make sure GCompositor and Scene are updated/rendered the next frame!
            if (sceneTask != null && compositorTask != null)
            {
                switch (splashScreenState)
                {
                    case SplashScreenState.Invalid:
                        {
                            if (sceneTask.IsCompleted && compositorTask.IsCompleted)
                            {
                                SceneInstance = new SceneInstance(Services, sceneTask.Result);
                                GraphicsCompositor = compositorTask.Result;
                                sceneTask = null;
                                compositorTask = null;
                            }
                            break;
                        }
                    case SplashScreenState.Intro:
                        {
                            Game.GraphicsContext.CommandList.Clear(Game.GraphicsContext.CommandList.RenderTarget, SplashScreenColor);

                            if (gameTime.Total.TotalSeconds > SplashScreenFadeTime)
                            {
                                splashScreenState = SplashScreenState.FadingIn;
                                fadeTime = 0.0f;
                            }
                            break;
                        }
                    case SplashScreenState.FadingIn:
                        {
                            var color = Color4.White;
                            var factor = MathUtil.SmoothStep((float)fadeTime / SplashScreenFadeTime);
                            color *= factor;
                            if (factor >= 1.0f)
                            {
                                splashScreenState = SplashScreenState.Showing;
                            }

                            fadeTime += gameTime.Elapsed.TotalSeconds;

                            RenderSplashScreen(color, BlendStates.AlphaBlend);
                            break;
                        }
                    case SplashScreenState.Showing:
                        {
                            RenderSplashScreen(Color4.White, BlendStates.Default);

                            if (gameTime.Total.TotalSeconds > MinSplashScreenTime && sceneTask.IsCompleted && compositorTask.IsCompleted)
                            {
                                splashScreenState = SplashScreenState.FadingOut;
                                fadeTime = 0.0f;
                            }
                            break;
                        }
                    case SplashScreenState.FadingOut:
                        {
                            var color = Color4.White;
                            var factor = (MathUtil.SmoothStep((float)fadeTime / SplashScreenFadeTime) * -1) + 1;
                            color *= factor;
                            if (factor <= 0.0f)
                            {
                                splashScreenState = SplashScreenState.Invalid;
                            }

                            fadeTime += gameTime.Elapsed.TotalSeconds;

                            RenderSplashScreen(color, BlendStates.AlphaBlend);
                            break;
                        }
                }
            }
        }
    }
}
