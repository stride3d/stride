// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Stride.Graphics
{
    struct RasterizerBoundState
    {
        public bool ScissorTestEnable;

        public bool DepthClamp;

        public bool NeedCulling;
        public CullFaceMode CullMode;
        public int DepthBias;
        public float SlopeScaleDepthBias;
        public FrontFaceDirection FrontFaceDirection;

#if !STRIDE_GRAPHICS_API_OPENGLES
        public PolygonMode PolygonMode;
#endif
    }

    class RasterizerState
    {
#if STRIDE_GRAPHICS_API_OPENGLES
        private const EnableCap DepthClamp = (EnableCap)0x864F;
#else
        private const EnableCap DepthClamp = (EnableCap)ArbDepthClamp.DepthClamp;
#endif

        RasterizerBoundState State;

        internal RasterizerState(RasterizerStateDescription rasterizerStateDescription)
        {
            State.ScissorTestEnable = rasterizerStateDescription.ScissorTestEnable;

            State.DepthClamp = !rasterizerStateDescription.DepthClipEnable;

            State.NeedCulling = rasterizerStateDescription.CullMode != CullMode.None;
            State.CullMode = GetCullMode(rasterizerStateDescription.CullMode);

            State.FrontFaceDirection =
                rasterizerStateDescription.FrontFaceCounterClockwise
                ? FrontFaceDirection.Cw
                : FrontFaceDirection.Ccw;

            State.DepthBias = rasterizerStateDescription.DepthBias;
            State.SlopeScaleDepthBias = rasterizerStateDescription.SlopeScaleDepthBias;

#if !STRIDE_GRAPHICS_API_OPENGLES
            State.PolygonMode = rasterizerStateDescription.FillMode == FillMode.Solid ? PolygonMode.Fill : PolygonMode.Line;
#endif

            // TODO: DepthBiasClamp and various other properties are not fully supported yet
            if (rasterizerStateDescription.DepthBiasClamp != 0.0f) throw new NotSupportedException();
        }

        public void Apply(CommandList commandList)
        {
#if !STRIDE_GRAPHICS_API_OPENGLES
            if (commandList.RasterizerBoundState.PolygonMode != State.PolygonMode)
            {
                commandList.RasterizerBoundState.PolygonMode = State.PolygonMode;
                GL.PolygonMode(MaterialFace.FrontAndBack, State.PolygonMode);
            }
#endif

            if (commandList.RasterizerBoundState.DepthBias != State.DepthBias || commandList.RasterizerBoundState.SlopeScaleDepthBias != State.SlopeScaleDepthBias)
            {
                commandList.RasterizerBoundState.DepthBias = State.DepthBias;
                commandList.RasterizerBoundState.SlopeScaleDepthBias = State.SlopeScaleDepthBias;
                GL.PolygonOffset(State.DepthBias, State.SlopeScaleDepthBias);
            }

            if (commandList.RasterizerBoundState.FrontFaceDirection != State.FrontFaceDirection)
            {
                commandList.RasterizerBoundState.FrontFaceDirection = State.FrontFaceDirection;
                GL.FrontFace(State.FrontFaceDirection);
            }

            if (commandList.GraphicsDevice.HasDepthClamp)
            {
                if (commandList.RasterizerBoundState.DepthClamp != State.DepthClamp)
                {
                    commandList.RasterizerBoundState.DepthClamp = State.DepthClamp;
                    if (State.DepthClamp)
                        GL.Enable(DepthClamp);
                    else
                        GL.Disable(DepthClamp);
                }
            }

            if (commandList.RasterizerBoundState.NeedCulling != State.NeedCulling)
            {
                commandList.RasterizerBoundState.NeedCulling = State.NeedCulling;
                if (State.NeedCulling)
                {
                    GL.Enable(EnableCap.CullFace);
                }
                else
                {
                    GL.Disable(EnableCap.CullFace);
                }
            }

            if (commandList.RasterizerBoundState.CullMode != State.CullMode)
            {
                commandList.RasterizerBoundState.CullMode = State.CullMode;
                GL.CullFace(State.CullMode);
            }

            if (commandList.RasterizerBoundState.ScissorTestEnable != State.ScissorTestEnable)
            {
                commandList.RasterizerBoundState.ScissorTestEnable = State.ScissorTestEnable;

                if (State.ScissorTestEnable)
                    GL.Enable(EnableCap.ScissorTest);
                else
                    GL.Disable(EnableCap.ScissorTest);
            }
        }

        private static CullFaceMode GetCullMode(CullMode cullMode)
        {
            switch (cullMode)
            {
                case CullMode.Front:
                    return CullFaceMode.Front;
                case CullMode.Back:
                    return CullFaceMode.Back;
                default:
                    return CullFaceMode.Back; // not used if CullMode.None
            }
        }
    }
} 
#endif
