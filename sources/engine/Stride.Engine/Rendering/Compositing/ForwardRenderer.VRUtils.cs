// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Compositing
{
    public partial class ForwardRenderer
    {
        private static unsafe void ComputeCommonViewMatrices(RenderContext context, Matrix* viewMatrices, Matrix* projectionMatrices)
        {
            // there are some limitations to this technique:
            //  both eyes view matrices must be facing the same direction
            //  eyes don't modify input's projection's bottom and top.

            var commonView = context.RenderView;
            commonView.View = viewMatrices[0];
            // near and far could be overriden by the VR device. let's take them as authority (assuming both eyes equal):
            // refer to http://stackoverflow.com/a/12926655/893406
            float nearLeft = projectionMatrices[0].M43 / projectionMatrices[0].M33;
            float nearRight = projectionMatrices[1].M43 / projectionMatrices[1].M33;
            float farLeft = nearLeft * (-projectionMatrices[0].M33 / (-projectionMatrices[0].M33 - 1));
            float farRight = nearRight * (-projectionMatrices[1].M33 / (-projectionMatrices[1].M33 - 1));
            // Compute left and right
            var projectionLeftOfLeftEye = nearLeft * (projectionMatrices[0].M31 - 1.0f) / projectionMatrices[0].M11;
            var projectionRightOfRightEye = nearRight * (projectionMatrices[1].M31 + 1.0f) / projectionMatrices[1].M11;
            // IPD
            float interPupillaryDistance = (viewMatrices[0].TranslationVector - viewMatrices[1].TranslationVector).Length();
            // find the center eye position according to the scheme described here:
            // http://computergraphics.stackexchange.com/questions/1736/vr-and-frustum-culling
            // tangent of theta, where theta is FOV/2
            var tangentThetaLeftEye = Math.Abs(projectionLeftOfLeftEye / nearLeft);
            var tangentThetaRightEye = Math.Abs(projectionRightOfRightEye / nearRight);
            var recession = interPupillaryDistance / (tangentThetaLeftEye + tangentThetaRightEye);
            // left offset (`A` on the diagram of above link):
            var leftOffset = tangentThetaLeftEye * recession;
            // place the view position in between left and right:
            commonView.View.TranslationVector = Vector3.Lerp(viewMatrices[0].TranslationVector, viewMatrices[1].TranslationVector, leftOffset / interPupillaryDistance);
            // and move backward:
            commonView.View.M43 -= recession;

            // set clip planes to most conservative enclosing planes:
            var oldNear = commonView.NearClipPlane;
            commonView.NearClipPlane = Math.Min(nearRight, nearLeft) + recession;
            commonView.FarClipPlane = Math.Max(farLeft, farRight) + recession;

            // Projection: Need to extend size to cover equivalent of both eyes
            var bottom = context.RenderView.NearClipPlane * (context.RenderView.Projection.M32 - 1.0f) / context.RenderView.Projection.M22;
            var top = context.RenderView.NearClipPlane * (context.RenderView.Projection.M32 + 1.0f) / context.RenderView.Projection.M22;
            // adjust proportionally the parameters (l, r, u, b are defined at near, so we use nears ratio):
            var nearsRatio = commonView.NearClipPlane / oldNear;
            // recreation from scratch:
            Matrix.PerspectiveOffCenterRH(projectionLeftOfLeftEye * nearsRatio, projectionRightOfRightEye * nearsRatio, bottom * nearsRatio, top * nearsRatio, commonView.NearClipPlane, commonView.FarClipPlane, out commonView.Projection);

            // update the view projection:
            Matrix.Multiply(ref commonView.View, ref commonView.Projection, out commonView.ViewProjection);
        }
    }
}
