// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;
using Stride.Core.Presentation.Core;
using Stride.Editor.EditorGame.Game;
using Scene =  Stride.Engine.Scene;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// A base class to implement sprite batch rendering previews.
    /// </summary>
    public abstract class PreviewFromSpriteBatch<T> : BuildAssetPreview<T>, ITextureBasePreview where T : Asset
    {
        private readonly Scene spriteScene;

        public float SpriteScale { get { return spriteScale; } set { spriteScale = value; SpriteScaleChanged?.Invoke(this, EventArgs.Empty); } }

        public event EventHandler SpriteScaleChanged;

        protected SpriteBatch SpriteBatch { get; private set; }

        protected Vector2 SpriteOffsets;

        private float spriteScale;

        protected PreviewFromSpriteBatch()
        {
            spriteScene = new Scene();
        }

        protected override async Task Initialize()
        {
            SpriteOffsets = Vector2.Zero;
            SpriteBatch = new SpriteBatch(Game.GraphicsDevice);

            await base.Initialize();

            Game.Script.AddTask(MoveAndScaleSpriteOnUserInput); 
        }

        protected virtual Vector2 SpriteSize
        {
            get { return Vector2.Zero; }
        }

        protected Vector2 WindowSize
        {
            get
            {
                if (Game == null || Game.Window == null)
                    return Vector2.Zero;

                return new Vector2(Game.Window.ClientBounds.Width, Game.Window.ClientBounds.Height);
            }
        }

        protected override Scene CreatePreviewScene()
        {
            return spriteScene;
        }

        protected override GraphicsCompositor GetGraphicsCompositor()
        {
            return new GraphicsCompositor
            {
                Game = new SceneRendererCollection
                {
                    new ClearRenderer { Color = RenderingMode == RenderingMode.HDR ? EditorServiceGame.EditorBackgroundColorHdr : EditorServiceGame.EditorBackgroundColorLdr },
                    new DelegateSceneRenderer(SafeRenderSprite),
                }
            };
        }

        private async Task MoveAndScaleSpriteOnUserInput()
        {
            var previousMousePosition = Vector2.Zero;

            while (IsRunning)
            {
                await Game.Script.NextFrame();

                if (Game.Input.HasPressedMouseButtons)
                    previousMousePosition = Game.Input.MousePosition;

                var deltaPosition = (Game.Input.MousePosition - previousMousePosition);

                if (Game.Input.HasDownMouseButtons)
                    SpriteOffsets += deltaPosition * WindowSize / SpriteScale;

                if (Game.Input.MouseWheelDelta < 0)
                    ZoomOut(Game.Input.MousePosition);

                if (Game.Input.MouseWheelDelta > 0)
                    ZoomIn(Game.Input.MousePosition);
                
                previousMousePosition = Game.Input.MousePosition;
            }
        }

        protected abstract void RenderSprite();

        private void SafeRenderSprite(RenderDrawContext context)
        {
            if (SpriteBatch == null)
                return;

            try
            {
                RenderSprite();
            }
            catch (Exception e)
            {
                Builder.Logger.Error($"RenderSprite crashed for asset item [{AssetItem}].", e);
            }
        }
        
        public override async Task Dispose()
        {
            await base.Dispose();

            SpriteBatch.Dispose();
        }

        public virtual IEnumerable<int> GetAvailableMipMaps()
        {
            // We don't need to provide mipmap preview (at least yet...)
            return new[] { 0 };
        }

        public virtual void DisplayMipMap(int parseMipMapLevel)
        {
            // We don't need to provide mipmap preview (at least yet...)
            // Intentionally does nothing
        }

        public void ZoomIn(Vector2? centerPosition)
        {
            var newValue = (float)Utils.ZoomFactors.FirstOrDefault(x => (float)x > SpriteScale);
            if (newValue < float.Epsilon)
                newValue = (float)Utils.ZoomFactors.Last();

            ChangeScale(newValue, centerPosition);
        }

        public void ZoomOut(Vector2? centerPosition)
        {
            var newValue = (float)Utils.ZoomFactors.LastOrDefault(x => (float)x < SpriteScale);
            if (newValue < float.Epsilon)
                newValue = (float)Utils.ZoomFactors.First();

            ChangeScale(newValue, centerPosition);
        }

        public virtual void FitOnScreen()
        {
            SpriteOffsets = Vector2.Zero;

            if (SpriteSize == Vector2.Zero)
            {
                SpriteScale = 1;
                return;
            }

            // determine the best scale to display the texture
            SpriteScale = Math.Min(WindowSize.X / SpriteSize.X, WindowSize.Y / SpriteSize.Y);
        }

        public virtual void ScaleToRealSize()
        {
            SpriteScale = 1;
        }

        public override void OnViewAttached()
        {
            base.OnViewAttached();

            SpriteOffsets = Vector2.Zero;
            if (SpriteSize == Vector2.Zero)
            {
                SpriteScale = 1;
                return;
            }

            // Choose the best match between realsize (if it fits) or fit-on-screen
            var screenScale = Math.Min(WindowSize.X / SpriteSize.X, WindowSize.Y / SpriteSize.Y);
            SpriteScale = screenScale < 1 ? screenScale : 1;
        }

        private void ChangeScale(float newValue, Vector2? centerPoint)
        {
            if (centerPoint != null)
            {
                SpriteOffsets += (centerPoint.Value - new Vector2(0.5f)) * WindowSize * (1.0f / newValue - 1.0f / SpriteScale);
            }
            SpriteScale = newValue;
        }
    }
}
