// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xenko.Core.Mathematics;
using Xenko.Rendering;

namespace Xenko.Graphics
{
    /// <summary>
    /// A utility class to batch and draw UI images.
    /// </summary>
    public class UIBatch : BatchBase<UIBatch.UIImageDrawInfo>
    {
        private static readonly short[][] PrimitiveTypeToIndices;

        private const int MaxVerticesPerElement = 16;
        private const int MaxIndicesPerElement = 54;
        private const int MaxElementBatchNumber = 2048;
        private const int MaxVerticesCount = MaxVerticesPerElement * MaxElementBatchNumber;
        private const int MaxIndicesCount = MaxIndicesPerElement * MaxElementBatchNumber;

        /// <summary>
        /// The view projection matrix that will be used for the current begin/end draw calls.
        /// </summary>
        private Matrix viewProjectionMatrix;

        // Cached states
        private BlendStateDescription? currentBlendState;
        private SamplerState currentSamplerState;
        private RasterizerStateDescription? currentRasterizerState;
        private DepthStencilStateDescription? currentDepthStencilState;
        private int currentStencilValue;

        private Vector4 vector4LeftTop = new Vector4(-0.5f, -0.5f, -0.5f, 1);

        private readonly Vector4[] shiftVectorX = new Vector4[4];
        private readonly Vector4[] shiftVectorY = new Vector4[4];

        private readonly Texture whiteTexture;

        private readonly EffectInstance signedDistanceFieldFontEffect;

        private readonly EffectInstance sdfSpriteFontEffect;

        public EffectInstance SDFSpriteFontEffect { get { return sdfSpriteFontEffect; } }

        static UIBatch()
        {
            PrimitiveTypeToIndices = new[]
            {
                new short[6],  // rectangle
                new short[54], // border rectangle
                new short[36], // cube
                new short[36], // reverse cube
            };
            // rectangle
            var indices = PrimitiveTypeToIndices[(int)PrimitiveType.Rectangle];
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;

            // border rectangle
            indices = PrimitiveTypeToIndices[(int)PrimitiveType.BorderRectangle];
            var count = 0;
            for (var j = 0; j < 3; ++j)
            {
                for (var l = 0; l < 3; ++l)
                {
                    indices[count++] = (short)((j << 2) + l + 0);
                    indices[count++] = (short)((j << 2) + l + 1);
                    indices[count++] = (short)((j << 2) + l + 4);

                    indices[count++] = (short)((j << 2) + l + 1);
                    indices[count++] = (short)((j << 2) + l + 5);
                    indices[count++] = (short)((j << 2) + l + 4);
                }
            }

            // cube
            count = 0;
            indices = PrimitiveTypeToIndices[(int)PrimitiveType.Cube];
            indices[count++] = 0; //front
            indices[count++] = 1;
            indices[count++] = 2;
            indices[count++] = 1;
            indices[count++] = 3;
            indices[count++] = 2;

            indices[count++] = 1; // right
            indices[count++] = 5;
            indices[count++] = 7;
            indices[count++] = 1;
            indices[count++] = 7;
            indices[count++] = 3;

            indices[count++] = 5; // back
            indices[count++] = 4;
            indices[count++] = 6;
            indices[count++] = 5;
            indices[count++] = 6;
            indices[count++] = 7;

            indices[count++] = 4; // left
            indices[count++] = 0;
            indices[count++] = 2;
            indices[count++] = 4;
            indices[count++] = 2;
            indices[count++] = 6;

            indices[count++] = 1; // top
            indices[count++] = 0;
            indices[count++] = 4;
            indices[count++] = 1;
            indices[count++] = 4;
            indices[count++] = 5;

            indices[count++] = 2; // bottom
            indices[count++] = 3;
            indices[count++] = 6;
            indices[count++] = 3;
            indices[count++] = 7;
            indices[count] = 6;

            // reverse cube
            var cubeIndices = PrimitiveTypeToIndices[(int)PrimitiveType.Cube];
            indices = PrimitiveTypeToIndices[(int)PrimitiveType.ReverseCube];
            for (var i = 0; i < cubeIndices.Length; i += 3)
            {
                indices[i + 0] = cubeIndices[i + 0];
                indices[i + 1] = cubeIndices[i + 2];
                indices[i + 2] = cubeIndices[i + 1];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIBatch"/> class.
        /// </summary>
        /// <param name="device">A valid instance of <see cref="GraphicsDevice"/>.</param>
        public UIBatch(GraphicsDevice device)
            : base(device, UIEffect.Bytecode, UIEffect.BytecodeSRgb,
            ResourceBufferInfo.CreateDynamicIndexBufferInfo("UIBatch.VertexIndexBuffers", MaxIndicesCount, MaxVerticesCount),
            VertexPositionColorTextureSwizzle.Layout)
        {
            // Create a 1x1 pixel white texture
            whiteTexture = graphicsDevice.GetSharedWhiteTexture();

            //  Load custom font rendering effects here

            // For signed distance field font rendering
            signedDistanceFieldFontEffect = new EffectInstance(new Effect(device, SignedDistanceFieldFontShader.Bytecode) { Name = "UIBatchSignedDistanceFieldFontEffect" });

            // For signed distance field thumbnail rendering
            sdfSpriteFontEffect = new EffectInstance(new Effect(device, SpriteSignedDistanceFieldFontShader.Bytecode) { Name = "UIBatchSDFSpriteFontEffect" });
        }

        /// <summary>
        /// Begins a image batch rendering using the specified blend state, depth stencil and a view-projection transformation matrix.
        /// Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.None).
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="viewProjection">The view projection matrix used for this series of draw calls</param>
        /// <param name="blendState">Blending options.</param>
        /// <param name="depthStencilState">Depth and stencil options.</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference</param>
        public void Begin(GraphicsContext graphicsContext, ref Matrix viewProjection, BlendStateDescription? blendState, DepthStencilStateDescription? depthStencilState, int stencilValue)
        {
            Begin(graphicsContext, ref viewProjection, blendState, null, null, depthStencilState, stencilValue);
        }

        /// <summary>
        /// Begins a image batch rendering using the specified blend state, sampler, depth stencil, rasterizer state objects, and the view-projection transformation matrix.
        /// Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, DepthStencilState.None, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp).
        /// </summary>
        /// <param name="graphicsContext">The graphics context to use.</param>
        /// <param name="viewProjection">The view projection matrix used for this series of draw calls</param>
        /// <param name="blendState">Blending options.</param>
        /// <param name="samplerState">Texture sampling options.</param>
        /// <param name="rasterizerState">Rasterization options.</param>
        /// <param name="depthStencilState">Depth and stencil options.</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference</param>
        public void Begin(GraphicsContext graphicsContext, ref Matrix viewProjection, BlendStateDescription? blendState, SamplerState samplerState, RasterizerStateDescription? rasterizerState, DepthStencilStateDescription? depthStencilState, int stencilValue)
        {
            viewProjectionMatrix = viewProjection;

            currentBlendState = blendState;
            currentSamplerState = samplerState;
            currentRasterizerState = rasterizerState;
            currentDepthStencilState = depthStencilState;
            currentStencilValue = stencilValue;

            Begin(graphicsContext, null, SpriteSortMode.BackToFront, blendState, samplerState, depthStencilState, rasterizerState, stencilValue);
        }

        public void BeginCustom(GraphicsContext graphicsContext, int overrideEffect)
        {
            EffectInstance effect = (overrideEffect == 0) ? null : signedDistanceFieldFontEffect;

            Begin(graphicsContext, effect, SpriteSortMode.BackToFront,
                currentBlendState, currentSamplerState, currentDepthStencilState, currentRasterizerState, currentStencilValue);
        }

        /// <summary>
        /// Draw a rectangle of the provided size at the position specified by the world matrix having the provided color.
        /// </summary>
        /// <param name="worldMatrix">The world matrix specifying the position of the rectangle in the world</param>
        /// <param name="elementSize">The size of the rectangle</param>
        /// <param name="color">The color of the rectangle</param>
        /// <param name="depthBias">The depth bias to use when drawing the element</param>
        public void DrawRectangle(ref Matrix worldMatrix, ref Vector3 elementSize, ref Color color, int depthBias)
        {
            // Skip items with null size
            if (elementSize.Length() < MathUtil.ZeroTolerance)
                return;

            // Calculate the information needed to draw.
            var drawInfo = new UIImageDrawInfo
            {
                DepthBias = depthBias,
                ColorScale = color,
                ColorAdd = Color.Zero,
                Primitive = PrimitiveType.Rectangle,
            };

            var matrix = worldMatrix;
            matrix.M11 *= elementSize.X;
            matrix.M12 *= elementSize.X;
            matrix.M13 *= elementSize.X;
            matrix.M21 *= elementSize.Y;
            matrix.M22 *= elementSize.Y;
            matrix.M23 *= elementSize.Y;
            matrix.M31 *= elementSize.Z;
            matrix.M32 *= elementSize.Z;
            matrix.M33 *= elementSize.Z;

            Matrix worldViewProjection;
            Matrix.Multiply(ref matrix, ref viewProjectionMatrix, out worldViewProjection);
            drawInfo.UnitXWorld = worldViewProjection.Row1;
            drawInfo.UnitYWorld = worldViewProjection.Row2;
            drawInfo.UnitZWorld = worldViewProjection.Row3;
            Vector4.Transform(ref vector4LeftTop, ref worldViewProjection, out drawInfo.LeftTopCornerWorld);

            var elementInfo = new ElementInfo(4, 6, in drawInfo, depthBias);

            Draw(whiteTexture, in elementInfo);
        }

        /// <summary>
        /// Draw a cube of the provided size at the position specified by the world matrix having the provided color.
        /// </summary>
        /// <param name="worldMatrix">The world matrix specifying the position of the cube in the world</param>
        /// <param name="elementSize">The size of the cube</param>
        /// <param name="color">The color of the cube</param>
        /// <param name="depthBias">The depth bias to use when drawing the element</param>
        public void DrawCube(ref Matrix worldMatrix, ref Vector3 elementSize, ref Color color, int depthBias)
        {
            DrawCube(ref worldMatrix, ref elementSize, ref color, depthBias, false);
        }

        /// <summary>
        /// Draw a colored background having provided size at the position specified by the world matrix.
        /// </summary>
        /// <param name="worldMatrix">The world matrix specifying the position of the element in the world</param>
        /// <param name="elementSize">The size of the element</param>
        /// <param name="color">The color of the element</param>
        /// <param name="depthBias">The depth bias to use when drawing the element</param>
        public void DrawBackground(ref Matrix worldMatrix, ref Vector3 elementSize, ref Color color, int depthBias)
        {
            DrawCube(ref worldMatrix, ref elementSize, ref color, depthBias, true);
        }

        private void DrawCube(ref Matrix worldMatrix, ref Vector3 elementSize, ref Color color, int depthBias, bool isReverse)
        {
            // Skip items with null size
            if (elementSize.Length() < MathUtil.ZeroTolerance)
                return;

            // Calculate the information needed to draw.
            var drawInfo = new UIImageDrawInfo
            {
                DepthBias = depthBias,
                ColorScale = color,
                ColorAdd = Color.Zero,
                Primitive = isReverse ? PrimitiveType.ReverseCube : PrimitiveType.Cube,
            };

            var matrix = worldMatrix;
            matrix.M11 *= elementSize.X;
            matrix.M12 *= elementSize.X;
            matrix.M13 *= elementSize.X;
            matrix.M21 *= elementSize.Y;
            matrix.M22 *= elementSize.Y;
            matrix.M23 *= elementSize.Y;
            matrix.M31 *= elementSize.Z;
            matrix.M32 *= elementSize.Z;
            matrix.M33 *= elementSize.Z;

            Matrix worldViewProjection;
            Matrix.Multiply(ref matrix, ref viewProjectionMatrix, out worldViewProjection);
            drawInfo.UnitXWorld = worldViewProjection.Row1;
            drawInfo.UnitYWorld = worldViewProjection.Row2;
            drawInfo.UnitZWorld = worldViewProjection.Row3;
            Vector4.Transform(ref vector4LeftTop, ref worldViewProjection, out drawInfo.LeftTopCornerWorld);

            var elementInfo = new ElementInfo(8, 6 * 6, in drawInfo, depthBias);

            Draw(whiteTexture, in elementInfo);
        }

        /// <summary>
        /// Batch a new border image draw to the draw list.
        /// </summary>
        /// <param name="texture">The texture to use during the draw</param>
        /// <param name="worldMatrix">The world matrix of the element</param>
        /// <param name="sourceRectangle">The rectangle indicating the source region of the texture to use</param>
        /// <param name="elementSize">The size of the ui element</param>
        /// <param name="borderSize">The size of the borders in the texture in pixels (left/top/right/bottom)</param>
        /// <param name="color">The color to apply to the texture image.</param>
        /// <param name="depthBias">The depth bias of the ui element</param>
        /// <param name="imageOrientation">The rotation to apply on the image uv</param>
        /// <param name="swizzle">Swizzle mode indicating the swizzle use when sampling the texture in the shader</param>
        /// <param name="snapImage">Indicate if the image needs to be snapped or not</param>
        public void DrawImage(Texture texture, ref Matrix worldMatrix, ref RectangleF sourceRectangle, ref Vector3 elementSize, ref Vector4 borderSize,
            ref Color color, int depthBias, ImageOrientation imageOrientation = ImageOrientation.AsIs, SwizzleMode swizzle = SwizzleMode.None, bool snapImage = false)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            // Skip items with null size
            if (elementSize.Length() < MathUtil.ZeroTolerance)
                return;

            // Calculate the information needed to draw.
            var drawInfo = new UIImageDrawInfo
            {
                Source =
                {
                    X = sourceRectangle.X / texture.ViewWidth,
                    Y = sourceRectangle.Y / texture.ViewHeight,
                    Width = sourceRectangle.Width / texture.ViewWidth,
                    Height = sourceRectangle.Height / texture.ViewHeight,
                },
                DepthBias = depthBias,
                ColorScale = color,
                ColorAdd = Color.Zero,
                Swizzle = swizzle,
                SnapImage = snapImage,
                Primitive = borderSize == Vector4.Zero ? PrimitiveType.Rectangle : PrimitiveType.BorderRectangle,
                BorderSize = new Vector4(borderSize.X / sourceRectangle.Width, borderSize.Y / sourceRectangle.Height, borderSize.Z / sourceRectangle.Width, borderSize.W / sourceRectangle.Height),
            };

            var rotatedSize = imageOrientation == ImageOrientation.AsIs ? elementSize : new Vector3(elementSize.Y, elementSize.X, 0);
            drawInfo.VertexShift = new Vector4(borderSize.X / rotatedSize.X, borderSize.Y / rotatedSize.Y, 1f - borderSize.Z / rotatedSize.X, 1f - borderSize.W / rotatedSize.Y);

            var matrix = worldMatrix;
            matrix.M11 *= elementSize.X;
            matrix.M12 *= elementSize.X;
            matrix.M13 *= elementSize.X;
            matrix.M21 *= elementSize.Y;
            matrix.M22 *= elementSize.Y;
            matrix.M23 *= elementSize.Y;
            matrix.M31 *= elementSize.Z;
            matrix.M32 *= elementSize.Z;
            matrix.M33 *= elementSize.Z;

            Matrix worldViewProjection;
            Matrix.Multiply(ref matrix, ref viewProjectionMatrix, out worldViewProjection);
            drawInfo.UnitXWorld = worldViewProjection.Row1;
            drawInfo.UnitYWorld = worldViewProjection.Row2;

            // rotate origin and unit axis if need.
            var leftTopCorner = vector4LeftTop;
            if (imageOrientation == ImageOrientation.Rotated90)
            {
                var unitX = drawInfo.UnitXWorld;
                drawInfo.UnitXWorld = -drawInfo.UnitYWorld;
                drawInfo.UnitYWorld = unitX;
                leftTopCorner = new Vector4(-0.5f, 0.5f, 0, 1);
            }
            Vector4.Transform(ref leftTopCorner, ref worldViewProjection, out drawInfo.LeftTopCornerWorld);

            var verticesPerElement = 4;
            var indicesPerElement = 6;
            if (drawInfo.Primitive == PrimitiveType.BorderRectangle)
            {
                verticesPerElement = 16;
                indicesPerElement = 54;
            }

            var elementInfo = new ElementInfo(verticesPerElement, indicesPerElement, in drawInfo, depthBias);

            Draw(texture, in elementInfo);
        }

        internal void DrawCharacter(Texture texture, in Matrix worldViewProjectionMatrix, in RectangleF sourceRectangle, in Color color, int depthBias, SwizzleMode swizzle)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            // Calculate the information needed to draw.
            var drawInfo = new UIImageDrawInfo
            {
                Source =
                {
                    X = sourceRectangle.X / texture.ViewWidth,
                    Y = sourceRectangle.Y / texture.ViewHeight,
                    Width = sourceRectangle.Width / texture.ViewWidth,
                    Height = sourceRectangle.Height / texture.ViewHeight,
                },
                DepthBias = depthBias,
                ColorScale = color,
                ColorAdd = Color.Zero,
                Swizzle = swizzle,
                Primitive = PrimitiveType.Rectangle,
                VertexShift = Vector4.Zero,
                UnitXWorld = worldViewProjectionMatrix.Row1,
                UnitYWorld = worldViewProjectionMatrix.Row2,
                LeftTopCornerWorld = worldViewProjectionMatrix.Row4,
            };

            var elementInfo = new ElementInfo(4, 6, in drawInfo, depthBias);

            Draw(texture, in elementInfo);
        }

        internal void DrawString(SpriteFont font, string text, ref SpriteFont.InternalUIDrawCommand drawCommand)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (text == null) throw new ArgumentNullException(nameof(text));

            var proxy = new SpriteFont.StringProxy(text);

            // shift the string position so that it is written from the left/top corner of the element
            var leftTopCornerOffset = drawCommand.TextBoxSize / 2;
            var worldMatrix = drawCommand.Matrix;
            worldMatrix.M41 -= worldMatrix.M11 * leftTopCornerOffset.X + worldMatrix.M21 * leftTopCornerOffset.Y;
            worldMatrix.M42 -= worldMatrix.M12 * leftTopCornerOffset.X + worldMatrix.M22 * leftTopCornerOffset.Y;
            worldMatrix.M43 -= worldMatrix.M13 * leftTopCornerOffset.X + worldMatrix.M23 * leftTopCornerOffset.Y;

            // transform the world matrix into the world view project matrix
            Matrix.MultiplyTo(ref worldMatrix, ref viewProjectionMatrix, out drawCommand.Matrix);

            if (font.FontType == SpriteFontType.SDF)
            {
                drawCommand.SnapText = false;
                float scaling = drawCommand.RequestedFontSize / font.Size;
                drawCommand.RealVirtualResolutionRatio = 1 / new Vector2(scaling, scaling);
            }
            if (font.FontType == SpriteFontType.Static)
            {
                drawCommand.RealVirtualResolutionRatio = Vector2.One; // ensure that static font are not scaled internally
            }
            if (font.FontType == SpriteFontType.Dynamic || font.FontType == SpriteFontType.Static)
            {
                // do not snap static fonts when real/virtual resolution does not match.
                if (drawCommand.RealVirtualResolutionRatio.X != 1 || drawCommand.RealVirtualResolutionRatio.Y != 1)
                    drawCommand.SnapText = false;   // we don't want snapping of the resolution of the screen does not match virtual resolution. (character alignment problems)
            }
            if (font.FontType == SpriteFontType.Dynamic)
            {
                // Dynamic: use virtual resolution (otherwise requested size might change on every camera move, esp. if UI is in 3D)
                // TODO: some step function to have LOD without regenerating on every small change?
                drawCommand.RealVirtualResolutionRatio = Vector2.One;
            }

            // snap draw start position to prevent characters to be drawn in between two pixels
            if (drawCommand.SnapText)
            {
                var invW = 1.0f / drawCommand.Matrix.M44;
                var backBufferHalfWidth = GraphicsContext.CommandList.RenderTarget.ViewWidth / 2;
                var backBufferHalfHeight = GraphicsContext.CommandList.RenderTarget.ViewHeight / 2;

                drawCommand.Matrix.M41 *= invW;
                drawCommand.Matrix.M42 *= invW;
                drawCommand.Matrix.M41 = (float)(Math.Round(drawCommand.Matrix.M41 * backBufferHalfWidth) / backBufferHalfWidth);
                drawCommand.Matrix.M42 = (float)(Math.Round(drawCommand.Matrix.M42 * backBufferHalfHeight) / backBufferHalfHeight);
                drawCommand.Matrix.M41 /= invW;
                drawCommand.Matrix.M42 /= invW;
            }

            font.InternalUIDraw(GraphicsContext.CommandList, ref proxy, ref drawCommand);
        }

        protected override unsafe void UpdateBufferValuesFromElementInfo(ref ElementInfo elementInfo, IntPtr vertexPtr, IntPtr indexPtr, int vertexOffset)
        {
            // the vertex buffer
            var vertex = (VertexPositionColorTextureSwizzle*)vertexPtr;
            fixed (UIImageDrawInfo* drawInfo = &elementInfo.DrawInfo)
            {
                switch (drawInfo->Primitive)
                {
                    case PrimitiveType.Rectangle:
                        CalculateRectangleVertices(drawInfo, vertex);
                        break;
                    case PrimitiveType.BorderRectangle:
                        CalculateBorderRectangleVertices(drawInfo, vertex);
                        break;
                    case PrimitiveType.Cube:
                    case PrimitiveType.ReverseCube:
                        CalculateCubeVertices(drawInfo, vertex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // the index buffer
                var index = (short*)indexPtr;
                var indices = PrimitiveTypeToIndices[(int)drawInfo->Primitive];
                for (var i = 0; i < indices.Length; ++i)
                    index[i] = (short)(indices[i] + vertexOffset);
            }
        }

        private static unsafe void CalculateCubeVertices(UIImageDrawInfo* drawInfo, VertexPositionColorTextureSwizzle* vertex)
        {
            const int VertexCountPerAxis = 2;

            var depthBiasMultiplier = drawInfo->DepthBias * DepthBiasShiftOneUnit;
            var colorScale = drawInfo->ColorScale.ToColor4();
            var colorAdd = drawInfo->ColorAdd.ToColor4();
            var swizzle = (float)drawInfo->Swizzle;

            // The vertices are arranged from the top-left-back, going left to right first, then top to bottom, then back to front
            var currentPosition = drawInfo->LeftTopCornerWorld;
            for (int l = 0; l < VertexCountPerAxis; l++)
            {
                for (int r = 0; r < VertexCountPerAxis; r++)
                {
                    for (var c = 0; c < VertexCountPerAxis; c++)
                    {
                        vertex->ColorScale = colorScale;
                        vertex->ColorAdd = colorAdd;
                        vertex->Swizzle = swizzle;
                        vertex->TextureCoordinate = Vector2.Zero;   // cubes are used only for color

                        vertex->Position.X = currentPosition.X;
                        vertex->Position.Y = currentPosition.Y;
                        vertex->Position.Z = currentPosition.Z - currentPosition.W * depthBiasMultiplier;
                        vertex->Position.W = currentPosition.W;

                        vertex++;     // Move vertex pointer position

                        if (c == 0)
                            Vector4.Add(ref currentPosition, ref drawInfo->UnitXWorld, out currentPosition);
                        else
                            Vector4.Subtract(ref currentPosition, ref drawInfo->UnitXWorld, out currentPosition);
                    }

                    if (r == 0)
                        Vector4.Add(ref currentPosition, ref drawInfo->UnitYWorld, out currentPosition);
                    else
                        Vector4.Subtract(ref currentPosition, ref drawInfo->UnitYWorld, out currentPosition);
                }

                Vector4.Add(ref currentPosition, ref drawInfo->UnitZWorld, out currentPosition);
            }
        }

        private unsafe void CalculateBorderRectangleVertices(UIImageDrawInfo* drawInfo, VertexPositionColorTextureSwizzle* vertex)
        {
            // The vertices are arranged from the top-left, going left to right first, then top to bottom

            // Set the texture uv vectors
            var texCoordsX = stackalloc float[]
            {
                drawInfo->Source.Left,                                                          // Outer Left
                drawInfo->Source.Left + drawInfo->Source.Width * drawInfo->BorderSize.X,        // Inner Left
                drawInfo->Source.Right - drawInfo->Source.Width * drawInfo->BorderSize.Z,       // Inner Right
                drawInfo->Source.Right,                                                         // Outer Right
            };
            var texCoordsY = stackalloc float[]
            {
                drawInfo->Source.Top,                                                           // Outer Top
                drawInfo->Source.Top + drawInfo->Source.Height * drawInfo->BorderSize.Y,        // Inner Top
                drawInfo->Source.Bottom - drawInfo->Source.Height * drawInfo->BorderSize.W,     // Inner Bottom
                drawInfo->Source.Bottom,                                                        // Outer Bottom
            };

            // Calculate the position offsets

            // posOffsetOuterLeft is just Vector4.Zero
            Vector4.Multiply(ref drawInfo->UnitXWorld, drawInfo->VertexShift.X, out var posOffsetLeftInner);
            Vector4.Multiply(ref drawInfo->UnitXWorld, drawInfo->VertexShift.Z, out var posOffsetRightInner);
            // posOffsetRightOuter is drawInfo->UnitXWorld

            // posOffsetOuterTop is just Vector4.Zero
            Vector4.Multiply(ref drawInfo->UnitYWorld, drawInfo->VertexShift.Y, out var posOffsetTopInner);
            Vector4.Multiply(ref drawInfo->UnitYWorld, drawInfo->VertexShift.W, out var posOffsetBottomInner);
            // posOffsetBottomOuter is drawInfo->UnitYWorld

            var posOffsetX = stackalloc Vector4[]
            {
                Vector4.Zero,               // Outer Left
                posOffsetLeftInner,         // Inner Left
                posOffsetRightInner,        // Inner Right
                drawInfo->UnitXWorld,       // Outer Right
            };
            var posOffsetY = stackalloc Vector4[]
            {
                Vector4.Zero,               // Outer Top
                posOffsetTopInner,          // Inner Top
                posOffsetBottomInner,       // Inner Bottom
                drawInfo->UnitYWorld,       // Outer Bottom
            };

            const int VertexCountPerAxis = 4;

            var depthBiasMultiplier = drawInfo->DepthBias * DepthBiasShiftOneUnit;
            var colorScale = drawInfo->ColorScale.ToColor4();
            var colorAdd = drawInfo->ColorAdd.ToColor4();
            var swizzle = (float)drawInfo->Swizzle;
            for (int r = 0; r < VertexCountPerAxis; r++)
            {
                for (var c = 0; c < VertexCountPerAxis; c++)
                {
                    vertex->ColorScale = colorScale;
                    vertex->ColorAdd = colorAdd;
                    vertex->Swizzle = swizzle;
                    vertex->TextureCoordinate.X = texCoordsX[c];
                    vertex->TextureCoordinate.Y = texCoordsY[r];

                    // drawInfo->LeftTopCornerWorld is the top-left most vertex
                    var vertPos = Vector4Add(ref drawInfo->LeftTopCornerWorld, ref posOffsetX[c], ref posOffsetY[r]);
                    vertex->Position.X = vertPos.X;
                    vertex->Position.Y = vertPos.Y;
                    vertex->Position.Z = vertPos.Z - vertPos.W * depthBiasMultiplier;
                    vertex->Position.W = vertPos.W;

                    vertex++;     // Move vertex pointer position
                }
            }
        }

        private unsafe void CalculateRectangleVertices(UIImageDrawInfo* drawInfo, VertexPositionColorTextureSwizzle* vertex)
        {
            var startPos = drawInfo->LeftTopCornerWorld;

            // Snap first pixel to prevent possible problems when left/top is in the middle of a pixel
            if (drawInfo->SnapImage)
            {
                var invW = 1.0f / startPos.W;
                var backBufferHalfWidth = GraphicsContext.CommandList.RenderTarget.ViewWidth / 2;
                var backBufferHalfHeight = GraphicsContext.CommandList.RenderTarget.ViewHeight / 2;

                startPos.X *= invW;
                startPos.Y *= invW;
                startPos.X = (float)(Math.Round(startPos.X * backBufferHalfWidth) / backBufferHalfWidth);
                startPos.Y = (float)(Math.Round(startPos.Y * backBufferHalfHeight) / backBufferHalfHeight);
                startPos.X *= startPos.W;
                startPos.Y *= startPos.W;
            }

            const int VertexCount = 4;
            var texCoords = stackalloc Vector2[]
            {
                new Vector2(drawInfo->Source.Left,  drawInfo->Source.Top),      // Top Left
                new Vector2(drawInfo->Source.Right, drawInfo->Source.Top),      // Top Right
                new Vector2(drawInfo->Source.Left,  drawInfo->Source.Bottom),   // Bottom Left
                new Vector2(drawInfo->Source.Right, drawInfo->Source.Bottom),   // Bottom Right
            };
            var vertexPositions = stackalloc Vector4[]
            {
                startPos,                                                                        // Top Left
                Vector4Add(ref startPos, ref drawInfo->UnitXWorld),                              // Top Right
                Vector4Add(ref startPos, ref drawInfo->UnitYWorld),                              // Bottom Left (Y axis points up, but Y value will be negative value to orientate correctly)
                Vector4Add(ref startPos, ref drawInfo->UnitXWorld, ref drawInfo->UnitYWorld),    // Bottom Right
            };

            if (drawInfo->SnapImage)
            {
                var backBufferHalfWidth = GraphicsContext.CommandList.RenderTarget.ViewWidth / 2;
                var backBufferHalfHeight = GraphicsContext.CommandList.RenderTarget.ViewHeight / 2;

                // First vertex position was already snapped so don't need to do it again
                for (int i = 1; i < VertexCount; i++)
                {
                    ref var vertPos = ref vertexPositions[i];
                    vertPos.X = (float)(Math.Round(vertPos.X * backBufferHalfWidth) / backBufferHalfWidth);
                    vertPos.Y = (float)(Math.Round(vertPos.Y * backBufferHalfHeight) / backBufferHalfHeight);
                }
            }

            var depthBiasMultiplier = drawInfo->DepthBias * DepthBiasShiftOneUnit;
            var colorScale = drawInfo->ColorScale.ToColor4();
            var colorAdd = drawInfo->ColorAdd.ToColor4();
            var swizzle = (float)drawInfo->Swizzle;
            for (int i = 0; i < VertexCount; i++, vertex++)
            {
                vertex->ColorScale = colorScale;
                vertex->ColorAdd = colorAdd;
                vertex->Swizzle = swizzle;
                vertex->TextureCoordinate = texCoords[i];

                ref var vertPos = ref vertexPositions[i];
                vertex->Position.X = vertPos.X;
                vertex->Position.Y = vertPos.Y;
                vertex->Position.Z = vertPos.Z - vertPos.W * depthBiasMultiplier;
                vertex->Position.W = vertPos.W;
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

        /// <summary>
        /// The primitive type to draw for an element.
        /// </summary>
        public enum PrimitiveType
        {
            /// <summary>
            /// A simple rectangle composed of 2 triangles
            /// </summary>
            Rectangle,

            /// <summary>
            /// A rectangle with borders tessellated as 3x3 rectangles
            /// </summary>
            BorderRectangle,

            /// <summary>
            /// A simple cube (not necessary square faces)
            /// </summary>
            Cube,

            /// <summary>
            /// A cube with back and front faces inversed.
            /// </summary>
            ReverseCube,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UIImageDrawInfo
        {
            public Vector4 LeftTopCornerWorld;
            public Vector4 UnitXWorld;
            public Vector4 UnitYWorld;
            public Vector4 UnitZWorld;
            public RectangleF Source;
            /// <summary>
            /// X = Left, Y = Top, Z = Right, W = Bottom
            /// </summary>
            public Vector4 BorderSize;
            /// <summary>
            /// X = Left, Y = Top, Z = Right, W = Bottom
            /// </summary>
            public Vector4 VertexShift;
            public Color ColorScale;
            public Color ColorAdd;
            public int DepthBias;
            public SwizzleMode Swizzle;
            public bool SnapImage;
            public PrimitiveType Primitive;

            //public float CalculateDepthOrigin()
            //{
            //    return LeftTopCornerWorld.Z / LeftTopCornerWorld.W - DepthBias * DepthBiasShiftOneUnit;
            //}
        }
    }
}
