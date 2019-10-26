// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Xenko.Core.Mathematics;
using Xenko.Native;
using Xenko.Rendering;

namespace Xenko.Graphics
{
    /// <summary>
    /// Renders a group of sprites.
    /// </summary>
    public partial class SpriteBatch : BatchBase<SpriteBatch.SpriteDrawInfo>
    {
        private static readonly Vector2[] CornerOffsets = { Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY };
        private static Vector2 vector2Zero = Vector2.Zero;
        private static RectangleF? nullRectangle;
        
        private Matrix userViewMatrix;
        private Matrix userProjectionMatrix;

        private readonly Matrix defaultViewMatrix = Matrix.Identity;
        private Matrix defaultProjectionMatrix;

        private readonly EffectInstance textureSpriteFontEffect;

        public EffectInstance TextureSpriteFontEffect { get { return textureSpriteFontEffect; } }

        /// <summary>
        /// Gets or sets the default depth value used by the <see cref="SpriteBatch"/> when the <see cref="VirtualResolution"/> is not set. 
        /// </summary>
        /// <remarks>More precisely, this value represents the length "farPlane-nearPlane" used by the default projection matrix.</remarks>
        public float DefaultDepth { get; set; }

        /// <summary>
        /// Gets or sets the virtual resolution used for this <see cref="SpriteBatch"/>
        /// </summary>
        public Vector3? VirtualResolution { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteBatch" /> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="bufferElementCount">The maximum number element that can be batched in one time.</param>
        /// <param name="batchCapacity">The batch capacity default to 64.</param>
        public SpriteBatch(GraphicsDevice graphicsDevice, int bufferElementCount = 1024, int batchCapacity = 64)
            : base(graphicsDevice, Bytecode, BytecodeSRgb, StaticQuadBufferInfo.CreateQuadBufferInfo("SpriteBatch.VertexIndexBuffer", true, bufferElementCount, batchCapacity), VertexPositionColorTextureSwizzle.Layout)
        {
            DefaultDepth = 200f;

            // For signed distance field thumbnail rendering
            textureSpriteFontEffect = new EffectInstance(new Effect(graphicsDevice, SpriteSignedDistanceFieldFontShader.Bytecode) { Name = "TextureSpriteFontEffect" });
        }

        /// <summary>
        /// Calculate the default projection matrix for the provided virtual resolution.
        /// </summary>
        /// <returns>The default projection matrix for the provided virtual resolution</returns>
        /// <remarks>The sprite batch default projection is an orthogonal matrix such as (0,0) is the Top/Left corner of the screen and 
        /// (VirtualResolution.X, VirtualResolution.Y) is the Bottom/Right corner of the screen.</remarks>
        public static Matrix CalculateDefaultProjection(Vector3 virtualResolution)
        {
            Matrix matrix;

            CalculateDefaultProjection(ref virtualResolution, out matrix);

            return matrix;
        }

        /// <summary>
        /// Calculate the default projection matrix for the provided virtual resolution.
        /// </summary>
        public static void CalculateDefaultProjection(ref Vector3 virtualResolution, out Matrix projection)
        {
            var xRatio = 1f / virtualResolution.X;
            var yRatio = -1f / virtualResolution.Y;
            var zRatio = -1f / virtualResolution.Z;

            projection = new Matrix { M11 = 2f * xRatio, M22 = 2f * yRatio, M33 = zRatio, M44 = 1f, M41 = -1f, M42 = 1f, M43 = 0.5f };
        }

        private Vector3 GetCurrentResolution(CommandList commandList)
        {
            return VirtualResolution.HasValue ? VirtualResolution.Value : new Vector3(commandList.Viewport.Width, commandList.Viewport.Height, DefaultDepth);
        }

        private void UpdateDefaultProjectionMatrix(CommandList commandList)
        {
            var resolution = GetCurrentResolution(commandList);
            CalculateDefaultProjection(ref resolution, out defaultProjectionMatrix);
        }

        /// <summary>
        /// Begins a sprite batch operation using deferred sort and default state objects (BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise).
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="sortMode">The sprite drawing order to use for the batch session</param>
        /// <param name="effect">The effect to use for the batch session</param>
        public void Begin(GraphicsContext graphicsContext, SpriteSortMode sortMode, EffectInstance effect)
        {
            UpdateDefaultProjectionMatrix(graphicsContext.CommandList);
            Begin(graphicsContext, defaultViewMatrix, defaultProjectionMatrix, sortMode, null, null, null, null, effect);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil and rasterizer state objects, plus a custom effect. Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp). Passing a null effect selects the default SpriteBatch Class shader.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="sortMode">The sprite drawing order to use for the batch session</param>
        /// <param name="blendState">The blending state to use for the batch session</param>
        /// <param name="samplerState">The sampling state to use for the batch session</param>
        /// <param name="depthStencilState">The depth stencil state to use for the batch session</param>
        /// <param name="rasterizerState">The rasterizer state to use for the batch session</param>
        /// <param name="effect">The effect to use for the batch session</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference for the batch session</param>
        public void Begin(GraphicsContext graphicsContext, SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendStateDescription? blendState = null, SamplerState samplerState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, EffectInstance effect = null, int stencilValue = 0)
        {
            UpdateDefaultProjectionMatrix(graphicsContext.CommandList);
            Begin(graphicsContext, defaultViewMatrix, defaultProjectionMatrix, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, stencilValue);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil, rasterizer state objects, plus a custom effect and a 2D transformation matrix. Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp). Passing a null effect selects the default SpriteBatch Class shader.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="viewMatrix">The view matrix to use for the batch session</param>
        /// <param name="sortMode">The sprite drawing order to use for the batch session</param>
        /// <param name="blendState">The blending state to use for the batch session</param>
        /// <param name="samplerState">The sampling state to use for the batch session</param>
        /// <param name="depthStencilState">The depth stencil state to use for the batch session</param>
        /// <param name="rasterizerState">The rasterizer state to use for the batch session</param>
        /// <param name="effect">The effect to use for the batch session</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference for the batch session</param>
        public void Begin(GraphicsContext graphicsContext, Matrix viewMatrix, SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendStateDescription? blendState = null, SamplerState samplerState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, EffectInstance effect = null, int stencilValue = 0)
        {
            UpdateDefaultProjectionMatrix(graphicsContext.CommandList);
            Begin(graphicsContext, viewMatrix, defaultProjectionMatrix, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, stencilValue);
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil, rasterizer state objects, plus a custom effect and a 2D transformation matrix. Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp). Passing a null effect selects the default SpriteBatch Class shader.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="viewMatrix">The view matrix to use for the batch session</param>
        /// <param name="projectionMatrix">The projection matrix to use for the batch session</param>
        /// <param name="sortMode">The sprite drawing order to use for the batch session</param>
        /// <param name="blendState">The blending state to use for the batch session</param>
        /// <param name="samplerState">The sampling state to use for the batch session</param>
        /// <param name="depthStencilState">The depth stencil state to use for the batch session</param>
        /// <param name="rasterizerState">The rasterizer state to use for the batch session</param>
        /// <param name="effect">The effect to use for the batch session</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference for the batch session</param>
        public void Begin(GraphicsContext graphicsContext, Matrix viewMatrix, Matrix projectionMatrix, SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendStateDescription? blendState = null, SamplerState samplerState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, EffectInstance effect = null, int stencilValue = 0)
        {
            CheckEndHasBeenCalled("begin");

            userViewMatrix = viewMatrix;
            userProjectionMatrix = projectionMatrix;

            Begin(graphicsContext, effect, sortMode, blendState, samplerState, depthStencilState, rasterizerState, stencilValue);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, destination rectangle, and color. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="destinationRectangle">A rectangle that specifies (in screen coordinates) the destination for drawing the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <remarks>
        /// Before making any calls to Draw, you must call Begin. Once all calls to Draw are complete, call End. 
        /// </remarks>
        public void Draw(Texture texture, RectangleF destinationRectangle, Color4 color, Color4 colorAdd = default(Color4))
        {
            DrawSprite(texture, ref destinationRectangle, false, ref nullRectangle, color, colorAdd, 0f, ref vector2Zero, SpriteEffects.None, ImageOrientation.AsIs, 0f);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position and color. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        public void Draw(Texture texture, Vector2 position)
        {
            Draw(texture, position, Color.White);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position and color. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        public void Draw(Texture texture, Vector2 position, Color color, Color4 colorAdd = default(Color4))
        {
            var destination = new RectangleF(position.X, position.Y, 1f, 1f);
            DrawSprite(texture, ref destination, true, ref nullRectangle, color, colorAdd, 0f, ref vector2Zero, SpriteEffects.None, ImageOrientation.AsIs, 0f);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, destination rectangle, source rectangle, color, rotation, origin, effects and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="destinationRectangle">A rectangle that specifies (in screen coordinates) the destination for drawing the sprite. If this rectangle is not the same size as the source rectangle, the sprite will be scaled to fit.</param>
        /// <param name="sourceRectangle">A rectangle that specifies (in texels) the source texels from a texture. Use null to draw the entire texture. </param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in the texture in pixels (dependent of image orientation). Default value is (0,0) which represents the upper-left corner.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="orientation">The source image orientation</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, RectangleF destinationRectangle, RectangleF? sourceRectangle, Color4 color, float rotation, Vector2 origin, 
            SpriteEffects effects = SpriteEffects.None, ImageOrientation orientation = ImageOrientation.AsIs, float layerDepth = 0f, Color4 colorAdd = default(Color4), SwizzleMode swizzle = SwizzleMode.None) 
        {
            DrawSprite(texture, ref destinationRectangle, false, ref sourceRectangle, color, colorAdd, rotation, ref origin, effects, orientation, layerDepth, swizzle);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, color, rotation, origin, scale, effects, and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in the texture in pixels (dependent of image orientation). Default value is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="orientation">The source image orientation</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, Vector2 position, Color4 color, float rotation, Vector2 origin, float scale = 1.0f, 
            SpriteEffects effects = SpriteEffects.None, ImageOrientation orientation = ImageOrientation.AsIs, float layerDepth = 0)
        {
            Draw(texture, position, null, color, rotation, origin, scale, effects, orientation, layerDepth);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, color, rotation, origin, scale, effects, and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in the texture in pixels (dependent of image orientation). Default value is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="orientation">The source image orientation</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, Vector2 position, Color4 color, float rotation, Vector2 origin, Vector2 scale, 
            SpriteEffects effects = SpriteEffects.None, ImageOrientation orientation = ImageOrientation.AsIs, float layerDepth = 0)
        {
            Draw(texture, position, null, color, rotation, origin, scale, effects, orientation, layerDepth);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, and color. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="sourceRectangle">A rectangle that specifies (in texels) the source texels from a texture. Use null to draw the entire texture. </param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        public void Draw(Texture texture, Vector2 position, RectangleF? sourceRectangle, Color4 color, Color4 colorAdd = default(Color4))
        {
            var destination = new RectangleF(position.X, position.Y, 1f, 1f);
            DrawSprite(texture, ref destination, true, ref sourceRectangle, color, colorAdd, 0f, ref vector2Zero, SpriteEffects.None, ImageOrientation.AsIs, 0f);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, color, rotation, origin, scale, effects, and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="sourceRectangle">A rectangle that specifies (in texels) the source texels from a texture. Use null to draw the entire texture. </param>
        /// <param name="color">The color to tint a sprite. Use Color4.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in the texture in pixels (dependent of image orientation). Default value is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="orientation">The source image orientation</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, Vector2 position, RectangleF? sourceRectangle, Color4 color, float rotation, 
            Vector2 origin, float scale = 1f, SpriteEffects effects = SpriteEffects.None, ImageOrientation orientation = ImageOrientation.AsIs, float layerDepth = 0, Color4 colorAdd = default(Color4), SwizzleMode swizzle = SwizzleMode.None)
        {
            var destination = new RectangleF(position.X, position.Y, scale, scale);
            DrawSprite(texture, ref destination, true, ref sourceRectangle, color, colorAdd, rotation, ref origin, effects, orientation, layerDepth, swizzle);
        }

        /// <summary>
        /// Adds a sprite to a batch of sprites for rendering using the specified texture, position, source rectangle, color, rotation, origin, scale, effects, and layer. 
        /// </summary>
        /// <param name="texture">A texture.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="sourceRectangle">A rectangle that specifies (in texels) the source texels from a texture. Use null to draw the entire texture. </param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in the texture in pixels (dependent of image orientation). Default value is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="orientation">The source image orientation</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        public void Draw(Texture texture, Vector2 position, RectangleF? sourceRectangle, Color4 color, float rotation, 
            Vector2 origin, Vector2 scale, SpriteEffects effects = SpriteEffects.None, ImageOrientation orientation = ImageOrientation.AsIs, float layerDepth = 0, Color4 colorAdd = default(Color4))
        {
            var destination = new RectangleF(position.X, position.Y, scale.X, scale.Y);
            DrawSprite(texture, ref destination, true, ref sourceRectangle, color, colorAdd, rotation, ref origin, effects, orientation, layerDepth);
        }

        /// <summary>
        /// Measure the size of the given text in virtual pixels depending on the target size.
        /// </summary>
        /// <param name="spriteFont">The font used to draw the text.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="targetSize">The size of the target to render in. If null, the size of the window back buffer is used.</param>
        /// <returns>The size of the text in virtual pixels.</returns>
        /// <exception cref="ArgumentNullException">The provided sprite font is null.</exception>
        public Vector2 MeasureString(SpriteFont spriteFont, string text, Vector2? targetSize = null)
        {
            if (spriteFont == null) throw new ArgumentNullException("spriteFont");

            return MeasureString(spriteFont, text, spriteFont.Size, targetSize);
        }

        /// <summary>
        /// Measure the size of the given text in virtual pixels depending on the target size.
        /// </summary>
        /// <param name="spriteFont">The font used to draw the text.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontSize">The font size (in pixels) used to draw the text.</param>
        /// <param name="targetSize">The size of the target to render in. If null, the size of the window back buffer is used.</param>
        /// <returns>The size of the text in virtual pixels.</returns>
        /// <exception cref="ArgumentNullException">The provided sprite font is null.</exception>
        public Vector2 MeasureString(SpriteFont spriteFont, string text, float fontSize, Vector2? targetSize = null)
        {
            if (spriteFont == null) throw new ArgumentNullException("spriteFont");

            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            var targetSizeValue = targetSize ?? new Vector2(graphicsDevice.Presenter.BackBuffer.Width, graphicsDevice.Presenter.BackBuffer.Height);

            // calculate the size of the text that will be used to draw
            var virtualResolution = VirtualResolution ?? new Vector3(targetSizeValue, DefaultDepth);
            var ratio = new Vector2(targetSizeValue.X / virtualResolution.X, targetSizeValue.Y / virtualResolution.Y);

            var realSize = spriteFont.MeasureString(text, fontSize * ratio);

            // convert pixel size into virtual pixel size (if needed) 
            var virtualSize = realSize;
            virtualSize.X /= ratio.X;
            virtualSize.Y /= ratio.Y;

            return virtualSize;
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, and color.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">A text string.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color4 color, TextAlignment alignment = TextAlignment.Left)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, -1, ref position, ref color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, and color.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">Text string.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color4 color, TextAlignment alignment = TextAlignment.Left)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, -1, ref position, ref color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, and color.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">A text string.</param>
        /// <param name="fontSize">The font size in pixels (ignored in the case of static fonts)</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, string text, float fontSize, Vector2 position, Color4 color, TextAlignment alignment = TextAlignment.Left)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, fontSize, ref position, ref color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, and color.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">Text string.</param>
        /// <param name="fontSize">The font size in pixels (ignored in the case of static fonts)</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, StringBuilder text, float fontSize, Vector2 position, Color4 color, TextAlignment alignment = TextAlignment.Left)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, fontSize, ref position, ref color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, color, rotation, origin, scale, effects and layer.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">A text string.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in virtual pixels; the default is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color4 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, -1, ref position, ref color, rotation, ref origin, ref scale, effects, layerDepth, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, color, rotation, origin, scale, effects and layer.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">Text string.</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in virtual pixels; the default is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color4 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, -1, ref position, ref color, rotation, ref origin, ref scale, effects, layerDepth, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, color, rotation, origin, scale, effects and layer.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">A text string.</param>
        /// <param name="fontSize">The font size in pixels (ignored in the case of static fonts)</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in virtual pixels; the default is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, string text, float fontSize, Vector2 position, Color4 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, fontSize, ref position, ref color, rotation, ref origin, ref scale, effects, layerDepth, alignment);
        }

        /// <summary>Adds a string to a batch of sprites for rendering using the specified font, text, position, color, rotation, origin, scale, effects and layer.</summary>
        /// <param name="spriteFont">A font for displaying text.</param>
        /// <param name="text">Text string.</param>
        /// <param name="fontSize">The font size in pixels (ignored in the case of static fonts)</param>
        /// <param name="position">The location (in screen coordinates) to draw the sprite.</param>
        /// <param name="color">The color to tint a sprite. Use Color.White for full color with no tinting.</param>
        /// <param name="rotation">Specifies the angle (in radians) to rotate the sprite about its center.</param>
        /// <param name="origin">The sprite origin in virtual pixels; the default is (0,0) which represents the upper-left corner.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="effects">Effects to apply.</param>
        /// <param name="layerDepth">The depth of a layer. By default, 0 represents the front layer and 1 represents a back layer. Use SpriteSortMode if you want sprites to be sorted during drawing.</param>
        /// <param name="alignment">Describes how to align the text to draw</param>
        public void DrawString(SpriteFont spriteFont, StringBuilder text, float fontSize, Vector2 position, Color4 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            var proxy = new SpriteFont.StringProxy(text);
            DrawString(spriteFont, ref proxy, fontSize, ref position, ref color, rotation, ref origin, ref scale, effects, layerDepth, alignment);
        }

        private void DrawString(SpriteFont spriteFont, ref SpriteFont.StringProxy text, float fontSize, ref Vector2 position, ref Color4 color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            DrawString(spriteFont, ref text, fontSize, ref position, ref color, rotation, ref origin, ref scale, effects, layerDepth, alignment);
        }

        private void DrawString(SpriteFont spriteFont, ref SpriteFont.StringProxy text, float fontSize, ref Vector2 position, ref Color4 color, float rotation, ref Vector2 origin, ref Vector2 scale, SpriteEffects effects, float layerDepth, TextAlignment alignment)
        {
            if (spriteFont == null)
            {
                throw new ArgumentNullException("spriteFont");
            }
            if (text.IsNull)
            {
                throw new ArgumentNullException("text");
            }
            if (fontSize < 0)
                fontSize = spriteFont.Size;

            // calculate the resolution ratio between the screen real size and the virtual resolution
            var commandList = GraphicsContext.CommandList;
            var viewportSize = commandList.Viewport;
            var virtualResolution = GetCurrentResolution(commandList);
            var resolutionRatio = new Vector2(viewportSize.Width / virtualResolution.X, viewportSize.Height / virtualResolution.Y);
            scale.X = scale.X / resolutionRatio.X;
            scale.Y = scale.Y / resolutionRatio.Y;

            var fontSize2 = fontSize * ((spriteFont.FontType == SpriteFontType.Dynamic) ? resolutionRatio : Vector2.One);
            var drawCommand = new SpriteFont.InternalDrawCommand(this, in fontSize2, in position, in color, rotation, in origin, in scale, effects, layerDepth);

            // snap the position the closest 'real' pixel
            Vector2.Modulate(ref drawCommand.Position, ref resolutionRatio, out drawCommand.Position);
            drawCommand.Position.X = (float)Math.Round(drawCommand.Position.X);
            drawCommand.Position.Y = (float)Math.Round(drawCommand.Position.Y);
            drawCommand.Position.X /= resolutionRatio.X;
            drawCommand.Position.Y /= resolutionRatio.Y;

            spriteFont.InternalDraw(commandList, ref text, ref drawCommand, alignment);
        }
        
        internal unsafe void DrawSprite(Texture texture, ref RectangleF destination, bool scaleDestination, ref RectangleF? sourceRectangle, Color4 color, Color4 colorAdd,
            float rotation, ref Vector2 origin, SpriteEffects effects, ImageOrientation orientation, float depth, SwizzleMode swizzle = SwizzleMode.None, bool realSize = false)
        {
            // Check that texture is not null
            if (texture == null)
            {
                throw new ArgumentNullException("texture");
            }
            
            // Put values in next ElementInfo
            var elementInfo = new ElementInfo();
            var spriteInfo = &elementInfo.DrawInfo;

            float width;
            float height;

            // If the source rectangle has a value, then use it.
            if (sourceRectangle.HasValue)
            {
                var rectangle = sourceRectangle.Value;
                spriteInfo->Source.X = rectangle.X;
                spriteInfo->Source.Y = rectangle.Y;
                width = rectangle.Width;
                height = rectangle.Height;
            }
            else
            {
                // Else, use directly the size of the texture
                spriteInfo->Source.X = 0.0f;
                spriteInfo->Source.Y = 0.0f;
                width = texture.ViewWidth;
                height = texture.ViewHeight;
            }

            // Sets the width and height
            spriteInfo->Source.Width = width;
            spriteInfo->Source.Height = height;

            // Scale the destination box
            if (scaleDestination)
            {
                if (orientation == ImageOrientation.Rotated90)
                {
                    destination.Width *= height;
                    destination.Height *= width;
                }
                else
                {
                    destination.Width *= width;
                    destination.Height *= height;
                }
            }

            // Sets the destination
            spriteInfo->Destination = destination;

            // Copy all other values.
            spriteInfo->Origin.X = origin.X;
            spriteInfo->Origin.Y = origin.Y;
            spriteInfo->Rotation = rotation;
            spriteInfo->Depth = depth;
            spriteInfo->SpriteEffects = effects;
            spriteInfo->ColorScale = color;
            spriteInfo->ColorAdd = colorAdd;
            spriteInfo->Swizzle = swizzle;
            spriteInfo->TextureSize.X = texture.ViewWidth;
            spriteInfo->TextureSize.Y = texture.ViewHeight;
            spriteInfo->Orientation = orientation;

            elementInfo.VertexCount = StaticQuadBufferInfo.VertexByElement;
            elementInfo.IndexCount = StaticQuadBufferInfo.IndicesByElement;
            elementInfo.Depth = depth;

            Draw(texture, in elementInfo);
        }

        protected override unsafe void UpdateBufferValuesFromElementInfo(ref ElementInfo elementInfo, IntPtr vertexPtr, IntPtr indexPtr, int vertexOffset)
        {
            fixed (SpriteDrawInfo* drawInfo = &elementInfo.DrawInfo)
            {
                NativeInvoke.UpdateBufferValuesFromElementInfo(new IntPtr(drawInfo), vertexPtr, indexPtr, vertexOffset);
            }
        }

        protected override void PrepareForRendering()
        {
            Matrix viewProjection;
            Matrix.MultiplyTo(ref userViewMatrix, ref userProjectionMatrix, out viewProjection);

            // Setup effect states and parameters: SamplerState and MatrixTransform
            // Sets the sampler state
            Parameters.Set(SpriteBaseKeys.MatrixTransform, viewProjection);

            base.PrepareForRendering();
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct SpriteDrawInfo
        {
            public RectangleF Source;
            public RectangleF Destination;
            public Vector2 Origin;
            public float Rotation;
            public float Depth;
            public SpriteEffects SpriteEffects;
            public Color4 ColorScale;
            public Color4 ColorAdd;
            public SwizzleMode Swizzle;
            public Vector2 TextureSize;
            public ImageOrientation Orientation;
        }
    }
}
