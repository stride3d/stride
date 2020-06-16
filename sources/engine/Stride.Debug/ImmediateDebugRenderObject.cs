// Copyright (c) Stride contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

using static Stride.DebugRendering.ImmediateDebugRenderFeature;

namespace Stride.DebugRendering
{
    public class ImmediateDebugRenderObject : RenderObject
    {

        /* messages */
        internal readonly FastList<Renderable> renderablesWithDepth = new FastList<Renderable>();
        internal readonly FastList<Renderable> renderablesNoDepth = new FastList<Renderable>();

        /* accumulators used when data is being pushed to the system */
        internal Primitives totalPrimitives, totalPrimitivesNoDepth;

        /* used to specify offset into instance data buffers when drawing */
        internal Primitives instanceOffsets, instanceOffsetsNoDepth;

        /* used in render stage to know how many of each instance to draw */
        internal Primitives primitivesToDraw, primitivesToDrawNoDepth;

        /* state set from outside */
        internal FillMode CurrentFillMode { get; set; } = FillMode.Wireframe;

        internal DebugRenderStage Stage { get; set; }

        public void DrawQuad(ref Vector3 position, ref Vector2 size, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Quad() { Position = position, Size = size, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Quads++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Quads++;
            }
        }

        public void DrawCircle(ref Vector3 position, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Circle() { Position = position, Radius = radius, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Circles++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Circles++;
            }
        }

        public void DrawSphere(ref Vector3 position, float radius, ref Color color, bool depthTest = true)
        {
            var cmd = new Sphere() { Position = position, Radius = radius, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Spheres++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Spheres++;
            }
        }

        public void DrawHalfSphere(ref Vector3 position, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new HalfSphere() { Position = position, Radius = radius, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.HalfSpheres++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.HalfSpheres++;
            }
        }

        public void DrawCube(ref Vector3 start, ref Vector3 end, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Cube() { Start = start, End = end, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Cubes++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Cubes++;
            }
        }

        public void DrawCapsule(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Capsule() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Capsules++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Capsules++;
            }
        }

        public void DrawCylinder(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Cylinder() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Cylinders++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Cylinders++;
            }
        }

        public void DrawCone(ref Vector3 position, float height, float radius, ref Quaternion rotation, ref Color color, bool depthTest = true)
        {
            var cmd = new Cone() { Position = position, Height = height, Radius = radius, Rotation = rotation, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Cones++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Cones++;
            }
        }

        public void DrawLine(ref Vector3 start, ref Vector3 end, ref Color color, bool depthTest = true)
        {
            var cmd = new Line() { Start = start, End = end, Color = color };
            var msg = new Renderable(ref cmd);
            if (depthTest)
            {
                renderablesWithDepth.Add(msg);
                totalPrimitives.Lines++;
            }
            else
            {
                renderablesNoDepth.Add(msg);
                totalPrimitivesNoDepth.Lines++;
            }
        }

    }

}
