// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Xenko.Core.Mathematics;
using Xenko.Rendering;

namespace Xenko.Graphics
{
    /// <summary>
    /// A batcher for sprite in the 3D world.
    /// </summary>
    public class Sprite3DBatch : BatchBase<Sprite3DBatch.Sprite3DDrawInfo>
    {
        private Matrix transformationMatrix;
        private Vector4 vector4UnitX = Vector4.UnitX;
        private Vector4 vector4UnitY = -Vector4.UnitY;

        /// <summary>
        /// Creates a new instance of <see cref="Sprite3DBatch"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferElementCount">The maximum number element that can be batched in one time.</param>
        /// <param name="batchCapacity">The batch capacity default to 64.</param>
        public Sprite3DBatch(GraphicsDevice device, int bufferElementCount = 1024, int batchCapacity = 64)
            : base(device, SpriteBatch.Bytecode, SpriteBatch.BytecodeSRgb, StaticQuadBufferInfo.CreateQuadBufferInfo("Sprite3DBatch.VertexIndexBuffer", false, bufferElementCount, batchCapacity), VertexPositionColorTextureSwizzle.Layout)
        {
        }

        /// <summary>
        /// Begins a 3D sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil, rasterizer state objects, plus a custom effect and a view-projection matrix.
        /// Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp).
        /// Passing a null effect selects the default SpriteBatch Class shader.
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="sortMode">The sprite drawing order to use for the batch session</param>
        /// <param name="effect">The effect to use for the batch session</param>
        /// <param name="blendState">The blending state to use for the batch session</param>
        /// <param name="samplerState">The sampling state to use for the batch session</param>
        /// <param name="depthStencilState">The depth stencil state to use for the batch session</param>
        /// <param name="rasterizerState">The rasterizer state to use for the batch session</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference for the batch session</param>
        /// <param name="viewProjection">The view-projection matrix to use for the batch session</param>
        public void Begin(GraphicsContext graphicsContext, Matrix viewProjection, SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendStateDescription? blendState = null, SamplerState samplerState = null, DepthStencilStateDescription? depthStencilState = null, RasterizerStateDescription? rasterizerState = null, EffectInstance effect = null, int stencilValue = 0)
        {
            CheckEndHasBeenCalled("begin");

            transformationMatrix = viewProjection;

            base.Begin(graphicsContext, effect, sortMode, blendState, samplerState, depthStencilState, rasterizerState, stencilValue);
        }

        /// <summary>
        /// Draw a 3D sprite (or add it to the draw list depending on the sortMode).
        /// </summary>
        /// <param name="texture">The texture to use during the draw</param>
        /// <param name="worldMatrix">The world matrix of the element</param>
        /// <param name="sourceRectangle">The rectangle indicating the source region of the texture to use</param>
        /// <param name="elementSize">The size of the sprite in the object space</param>
        /// <param name="color">The color to apply to the texture image.</param>
        /// <param name="imageOrientation">The rotation to apply on the image uv</param>
        /// <param name="swizzle">Swizzle mode indicating the swizzle use when sampling the texture in the shader</param>
        /// <param name="depth">The depth of the element. If null, it is calculated using world and view-projection matrix.</param>
        public void Draw(Texture texture, ref Matrix worldMatrix, ref RectangleF sourceRectangle, ref Vector2 elementSize, ref Color4 color,
                         ImageOrientation imageOrientation = ImageOrientation.AsIs, SwizzleMode swizzle = SwizzleMode.None, float? depth = null)
        {
            // Check that texture is not null
            if (texture == null)
                throw new ArgumentNullException("texture");

            // Skip items with null size
            if (elementSize.Length() < MathUtil.ZeroTolerance)
                return;

            // Calculate the information needed to draw.
            var drawInfo = new Sprite3DDrawInfo
            {
                Source =
                {
                    X = sourceRectangle.X / texture.ViewWidth,
                    Y = sourceRectangle.Y / texture.ViewHeight,
                    Width = sourceRectangle.Width / texture.ViewWidth,
                    Height = sourceRectangle.Height / texture.ViewHeight,
                },
                ColorScale = color,
                ColorAdd = new Color4(0, 0, 0, 0),
                Swizzle = swizzle,
            };

            var matrix = worldMatrix;
            matrix.M11 *= elementSize.X;
            matrix.M12 *= elementSize.X;
            matrix.M13 *= elementSize.X;
            matrix.M21 *= elementSize.Y;
            matrix.M22 *= elementSize.Y;
            matrix.M23 *= elementSize.Y;

            Vector4.Transform(ref vector4UnitX, ref matrix, out drawInfo.UnitXWorld);
            Vector4.Transform(ref vector4UnitY, ref matrix, out drawInfo.UnitYWorld);

            // rotate origin and unit axis if need.
            var leftTopCorner = new Vector4(-0.5f, 0.5f, 0, 1);
            if (imageOrientation == ImageOrientation.Rotated90)
            {
                var unitX = drawInfo.UnitXWorld;
                drawInfo.UnitXWorld = -drawInfo.UnitYWorld;
                drawInfo.UnitYWorld = unitX;
                leftTopCorner = new Vector4(-0.5f, -0.5f, 0, 1);
            }
            Vector4.Transform(ref leftTopCorner, ref matrix, out drawInfo.LeftTopCornerWorld);

            float depthSprite;
            if (depth.HasValue)
            {
                depthSprite = depth.Value;
            }
            else
            {
                Vector4 projectedPosition;
                var worldPosition = new Vector4(worldMatrix.TranslationVector, 1.0f);
                Vector4.Transform(ref worldPosition, ref transformationMatrix, out projectedPosition);
                depthSprite = projectedPosition.Z / projectedPosition.W;
            }

            var elementInfo = new ElementInfo(StaticQuadBufferInfo.VertexByElement, StaticQuadBufferInfo.IndicesByElement, in drawInfo, depthSprite);

            Draw(texture, in elementInfo);
        }

        protected override void PrepareForRendering()
        {
            // Setup the Transformation matrix of the shader
            Parameters.Set(SpriteBaseKeys.MatrixTransform, ref transformationMatrix);

            base.PrepareForRendering();
        }

        protected override unsafe void UpdateBufferValuesFromElementInfo(ref ElementInfo elementInfo, IntPtr vertexPointer, IntPtr indexPointer, int vexterStartOffset)
        {
            var vertex = (VertexPositionColorTextureSwizzle*)vertexPointer;
            fixed (Sprite3DDrawInfo* drawInfo = &elementInfo.DrawInfo)
            {
                const int VertexCount = 4;
                var texCoords = stackalloc Vector2[]
                {
                    drawInfo->Source.TopLeft,
                    drawInfo->Source.TopRight,
                    drawInfo->Source.BottomLeft,
                    drawInfo->Source.BottomRight,
                };

                ref var startPos = ref drawInfo->LeftTopCornerWorld;
                var vertexPositions = stackalloc Vector4[]
                {
                    startPos,                                                                       // Top Left
                    Vector4Add(ref startPos, ref drawInfo->UnitXWorld),                             // Top Right
                    Vector4Add(ref startPos, ref drawInfo->UnitYWorld),                             // Bottom Left (Y axis points up, but Y value will be negative value to orientate correctly)
                    Vector4Add(ref startPos, ref drawInfo->UnitXWorld, ref drawInfo->UnitYWorld),   // Bottom Right
                };

                var swizzle = (float)drawInfo->Swizzle;
                for (int i = 0; i < VertexCount; i++, vertex++)
                {
                    vertex->ColorScale = drawInfo->ColorScale;
                    vertex->ColorAdd = drawInfo->ColorAdd;
                    vertex->Swizzle = swizzle;
                    vertex->TextureCoordinate = texCoords[i];
                    vertex->Position = vertexPositions[i];
                }
            }
        }

        private static Vector4 Vector4Add(ref Vector4 v1, ref Vector4 v2)
        {
            Vector4 result;
            result.X = v1.X + v2.X;
            result.Y = v1.Y + v2.Y;
            result.Z = v1.Z + v2.Z;
            result.W = v1.W + v2.W;
            return result;
        }

        private static Vector4 Vector4Add(ref Vector4 v1, ref Vector4 v2, ref Vector4 v3)
        {
            Vector4 result;
            result.X = v1.X + v2.X + v3.X;
            result.Y = v1.Y + v2.Y + v3.Y;
            result.Z = v1.Z + v2.Z + v3.Z;
            result.W = v1.W + v2.W + v3.W;
            return result;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Sprite3DDrawInfo
        {
            public Vector4 LeftTopCornerWorld;
            public Vector4 UnitXWorld;
            public Vector4 UnitYWorld;
            public RectangleF Source;
            public Color4 ColorScale;
            public Color4 ColorAdd;
            public SwizzleMode Swizzle;
        }
    }
}
