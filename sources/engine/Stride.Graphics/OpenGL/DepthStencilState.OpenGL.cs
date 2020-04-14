// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_OPENGL 
using System;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Xenko.Graphics
{
    struct DepthStencilBoundState
    {
        // Depth
        public bool DepthBufferEnable;
        public bool DepthBufferWriteEnable;
        public DepthFunction DepthFunction;

        // Stencil
        public bool StencilEnable;
        public byte StencilWriteMask;
        public byte StencilMask;

        public StencilFaceState Faces;
    }

    struct StencilFaceState
    {
        public StencilFunction FrontFaceStencilFunction;
        public StencilOp FrontFaceDepthFailOp;
        public StencilOp FrontFaceFailOp;
        public StencilOp FrontFacePassOp;

        public StencilFunction BackFaceStencilFunction;
        public StencilOp BackFaceDepthFailOp;
        public StencilOp BackFaceFailOp;
        public StencilOp BackFacePassOp;

        public bool Equals(StencilFaceState other)
        {
            return FrontFaceStencilFunction == other.FrontFaceStencilFunction && FrontFaceDepthFailOp == other.FrontFaceDepthFailOp && FrontFaceFailOp == other.FrontFaceFailOp && FrontFacePassOp == other.FrontFacePassOp && BackFaceStencilFunction == other.BackFaceStencilFunction && BackFaceDepthFailOp == other.BackFaceDepthFailOp && BackFaceFailOp == other.BackFaceFailOp && BackFacePassOp == other.BackFacePassOp;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StencilFaceState && Equals((StencilFaceState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)FrontFaceStencilFunction;
                hashCode = (hashCode * 397) ^ (int)FrontFaceDepthFailOp;
                hashCode = (hashCode * 397) ^ (int)FrontFaceFailOp;
                hashCode = (hashCode * 397) ^ (int)FrontFacePassOp;
                hashCode = (hashCode * 397) ^ (int)BackFaceStencilFunction;
                hashCode = (hashCode * 397) ^ (int)BackFaceDepthFailOp;
                hashCode = (hashCode * 397) ^ (int)BackFaceFailOp;
                hashCode = (hashCode * 397) ^ (int)BackFacePassOp;
                return hashCode;
            }
        }

        public static bool operator ==(StencilFaceState left, StencilFaceState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StencilFaceState left, StencilFaceState right)
        {
            return !left.Equals(right);
        }
    }

    public class DepthStencilState
    {
        DepthStencilBoundState state;

        internal DepthStencilState(DepthStencilStateDescription depthStencilStateDescription, bool hasDepthStencilBuffer)
        {
            state.DepthBufferEnable = depthStencilStateDescription.DepthBufferEnable;
            state.DepthBufferWriteEnable = depthStencilStateDescription.DepthBufferWriteEnable && hasDepthStencilBuffer;

            state.StencilEnable = depthStencilStateDescription.StencilEnable;
            state.StencilMask = depthStencilStateDescription.StencilMask;
            state.StencilWriteMask = depthStencilStateDescription.StencilWriteMask;

            state.DepthFunction = depthStencilStateDescription.DepthBufferFunction.ToOpenGLDepthFunction();

            state.Faces.FrontFaceStencilFunction = depthStencilStateDescription.FrontFace.StencilFunction.ToOpenGLStencilFunction();
            state.Faces.FrontFaceDepthFailOp = depthStencilStateDescription.FrontFace.StencilDepthBufferFail.ToOpenGL();
            state.Faces.FrontFaceFailOp = depthStencilStateDescription.FrontFace.StencilFail.ToOpenGL();
            state.Faces.FrontFacePassOp = depthStencilStateDescription.FrontFace.StencilPass.ToOpenGL();

            state.Faces.BackFaceStencilFunction = depthStencilStateDescription.BackFace.StencilFunction.ToOpenGLStencilFunction();
            state.Faces.BackFaceDepthFailOp = depthStencilStateDescription.BackFace.StencilDepthBufferFail.ToOpenGL();
            state.Faces.BackFaceFailOp = depthStencilStateDescription.BackFace.StencilFail.ToOpenGL();
            state.Faces.BackFacePassOp = depthStencilStateDescription.BackFace.StencilPass.ToOpenGL();
        }

        public void Apply(CommandList commandList)
        {
            if (commandList.DepthStencilBoundState.DepthBufferEnable != state.DepthBufferEnable)
            {
                commandList.DepthStencilBoundState.DepthBufferEnable = state.DepthBufferEnable;

                if (state.DepthBufferEnable)
                    GL.Enable(EnableCap.DepthTest);
                else
                    GL.Disable(EnableCap.DepthTest);
            }

            if (state.DepthBufferEnable && commandList.DepthStencilBoundState.DepthFunction != state.DepthFunction)
            {
                commandList.DepthStencilBoundState.DepthFunction = state.DepthFunction;
                GL.DepthFunc(state.DepthFunction);
            }

            if (commandList.DepthStencilBoundState.DepthBufferWriteEnable != state.DepthBufferWriteEnable)
            {
                commandList.DepthStencilBoundState.DepthBufferWriteEnable = state.DepthBufferWriteEnable;
                GL.DepthMask(state.DepthBufferWriteEnable);
            }

            if (commandList.DepthStencilBoundState.StencilEnable != state.StencilEnable)
            {
                commandList.DepthStencilBoundState.StencilEnable = state.StencilEnable;

                if (state.StencilEnable)
                    GL.Enable(EnableCap.StencilTest);
                else
                    GL.Disable(EnableCap.StencilTest);
            }

            if (state.StencilEnable && commandList.DepthStencilBoundState.StencilWriteMask != state.StencilWriteMask)
            {
                commandList.DepthStencilBoundState.StencilWriteMask = state.StencilWriteMask;
                GL.StencilMask(state.StencilWriteMask);
            }

            // TODO: Properly handle stencil reference
            if (state.StencilEnable && (commandList.DepthStencilBoundState.Faces != state.Faces || commandList.NewStencilReference != commandList.BoundStencilReference))
            {
                commandList.DepthStencilBoundState.Faces = state.Faces;
                commandList.BoundStencilReference = commandList.NewStencilReference;

                GL.StencilFuncSeparate(StencilFace.Front, state.Faces.FrontFaceStencilFunction, commandList.BoundStencilReference, state.StencilWriteMask); // set both faces
                GL.StencilFuncSeparate(StencilFace.Back, state.Faces.BackFaceStencilFunction, commandList.BoundStencilReference, state.StencilWriteMask); // override back face
                GL.StencilOpSeparate(StencilFace.Front, state.Faces.FrontFaceDepthFailOp, state.Faces.FrontFaceFailOp, state.Faces.FrontFacePassOp);
                GL.StencilOpSeparate(StencilFace.Back, state.Faces.BackFaceDepthFailOp, state.Faces.BackFaceFailOp, state.Faces.BackFacePassOp);
            }
        }
    }
} 
#endif 
