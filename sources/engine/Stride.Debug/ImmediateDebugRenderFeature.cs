// Copyright (c) Stride contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering;

using Buffer = Stride.Graphics.Buffer;

namespace Stride.DebugRendering
{
    public class ImmediateDebugRenderFeature : RootRenderFeature
    {

        internal enum DebugRenderStage
        {
            Opaque,
            Transparent
        }

        public override Type SupportedRenderObjectType => typeof(ImmediateDebugRenderObject);

        internal struct Primitives
        {

            public int Quads;
            public int Circles;
            public int Spheres;
            public int HalfSpheres;
            public int Cubes;
            public int Capsules;
            public int Cylinders;
            public int Cones;
            public int Lines;

            public void Clear()
            {
                Quads = 0;
                Circles = 0;
                Spheres = 0;
                HalfSpheres = 0;
                Cubes = 0;
                Capsules = 0;
                Cylinders = 0;
                Cones = 0;
                Lines = 0;
            }

        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct LineVertex
        {

            public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.Color<Color>());

            public Vector3 Position;
            public Color Color;

        }

        internal enum RenderableType : byte
        {
            Quad,
            Circle,
            Sphere,
            HalfSphere,
            Cube,
            Capsule,
            Cylinder,
            Cone,
            Line
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Renderable
        {

            public Renderable(ref Quad q) : this()
            {
                Type = RenderableType.Quad;
                QuadData = q;
            }

            public Renderable(ref Circle c) : this()
            {
                Type = RenderableType.Circle;
                CircleData = c;
            }

            public Renderable(ref Sphere s) : this()
            {
                Type = RenderableType.Sphere;
                SphereData = s;
            }

            public Renderable(ref HalfSphere h) : this()
            {
                Type = RenderableType.HalfSphere;
                HalfSphereData = h;
            }

            public Renderable(ref Cube c) : this()
            {
                Type = RenderableType.Cube;
                CubeData = c;
            }

            public Renderable(ref Capsule c) : this()
            {
                Type = RenderableType.Capsule;
                CapsuleData = c;
            }

            public Renderable(ref Cylinder c) : this()
            {
                Type = RenderableType.Cylinder;
                CylinderData = c;
            }

            public Renderable(ref Cone c) : this()
            {
                Type = RenderableType.Cone;
                ConeData = c;
            }

            public Renderable(ref Line l) : this()
            {
                Type = RenderableType.Line;
                LineData = l;
            }

            [FieldOffset(0)]
            public RenderableType Type;

            [FieldOffset(1)]
            public Quad QuadData;

            [FieldOffset(1)]
            public Circle CircleData;

            [FieldOffset(1)]
            public Sphere SphereData;

            [FieldOffset(1)]
            public HalfSphere HalfSphereData;

            [FieldOffset(1)]
            public Cube CubeData;

            [FieldOffset(1)]
            public Capsule CapsuleData;

            [FieldOffset(1)]
            public Cylinder CylinderData;

            [FieldOffset(1)]
            public Cone ConeData;

            [FieldOffset(1)]
            public Line LineData;

        }

        internal struct Quad
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector2 Size;
            public Color Color;
        }

        internal struct Circle
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Radius;
            public Color Color;
        }

        internal struct Sphere
        {
            public Vector3 Position;
            public float Radius;
            public Color Color;
        }

        internal struct HalfSphere
        {
            public Vector3 Position;
            public float Radius;
            public Quaternion Rotation;
            public Color Color;
        }

        internal struct Cube
        {
            public Vector3 Start;
            public Vector3 End;
            public Quaternion Rotation;
            public Color Color;
        }

        internal struct Capsule
        {
            public Vector3 Position;
            public float Height;
            public float Radius;
            public Quaternion Rotation;
            public Color Color;
        }

        internal struct Cylinder
        {
            public Vector3 Position;
            public float Height;
            public float Radius;
            public Quaternion Rotation;
            public Color Color;
        }

        internal struct Cone
        {
            public Vector3 Position;
            public float Height;
            public float Radius;
            public Quaternion Rotation;
            public Color Color;
        }

        internal struct Line
        {
            public Vector3 Start;
            public Vector3 End;
            public Color Color;
        }

        internal struct InstanceData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Color Color;
        }

        private const float DefaultCircleRadius = 0.5f;
        private const float DefaultSphereRadius = 0.5f;
        private const float DefaultCubeSize = 1.0f;
        private const float DefaultCapsuleLength = 1.0f;
        private const float DefaultCapsuleRadius = 0.5f;
        private const float DefaultCylinderHeight = 1.0f;
        private const float DefaultCylinderRadius = 0.5f;
        private const float DefaultPlaneSize = 1.0f;
        private const float DefaultConeRadius = 0.5f;
        private const float DefaultConeHeight = 1.0f;

        private const int CircleTesselation = 16;
        private const int SphereTesselation = 8;
        private const int CapsuleTesselation = 8;
        private const int CylinderTesselation = 16;
        private const int ConeTesselation = 16;

        /* mesh data we will use when stuffing things in vertex buffers */
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) circle = ImmediateDebugPrimitives.GenerateCircle(DefaultCircleRadius, CircleTesselation);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) plane = ImmediateDebugPrimitives.GenerateQuad(DefaultPlaneSize, DefaultPlaneSize);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) sphere = ImmediateDebugPrimitives.GenerateSphere(DefaultSphereRadius, SphereTesselation, uvSplitOffsetVertical: 1);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) cube = ImmediateDebugPrimitives.GenerateCube(DefaultCubeSize);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) capsule = ImmediateDebugPrimitives.GenerateCapsule(DefaultCapsuleLength, DefaultCapsuleRadius, CapsuleTesselation);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) cylinder = ImmediateDebugPrimitives.GenerateCylinder(DefaultCylinderHeight, DefaultCylinderRadius, CylinderTesselation);
        private readonly (VertexPositionTexture[] Vertices, int[] Indices) cone = ImmediateDebugPrimitives.GenerateCone(DefaultConeHeight, DefaultConeRadius, ConeTesselation, uvSplits: 8);

        /* vertex and index buffer for our primitive data */
        private Buffer vertexBuffer;
        private Buffer indexBuffer;

        /* vertex buffer for line rendering */
        private Buffer lineVertexBuffer;

        /* offsets into our vertex/index buffer */
        private Primitives primitiveVertexOffsets;
        private Primitives primitiveIndexOffsets;

        /* other gpu related data */
        private MutablePipelineState pipelineState;
        private InputElementDescription[] inputElements;
        private InputElementDescription[] lineInputElements;
        private DynamicEffectInstance primitiveEffect;
        private DynamicEffectInstance lineEffect;
        private Buffer transformBuffer;
        private Buffer colorBuffer;

        /* intermediate message related data, written to in extract */
        private readonly FastList<InstanceData> instances = new FastList<InstanceData>(1);

        /* data written to buffers in prepare */
        private readonly FastList<Matrix> transforms = new FastList<Matrix>(1);
        private readonly FastList<Color> colors = new FastList<Color>(1);

        /* data only for line rendering */
        private readonly FastList<LineVertex> lineVertices = new FastList<LineVertex>(1);

        public ImmediateDebugRenderFeature()
        {
            SortKey = 0xFF; // render last! .. or things without depth testing in the opaque stage will have the background rendered over them
        }

        protected override void InitializeCore()
        {

            var device = Context.GraphicsDevice;

            inputElements = VertexPositionTexture.Layout.CreateInputElements();
            lineInputElements = LineVertex.Layout.CreateInputElements();

            // create our pipeline state object
            pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();

            // TODO: create our associated effect
            primitiveEffect = new DynamicEffectInstance("PrimitiveShader");
            primitiveEffect.Initialize(Context.Services);
            primitiveEffect.UpdateEffect(device);

            lineEffect = new DynamicEffectInstance("LinePrimitiveShader");
            lineEffect.Initialize(Context.Services);
            lineEffect.UpdateEffect(device);

            {

                // create initial vertex and index buffers
                var vertexData = new VertexPositionTexture[
                    circle.Vertices.Length +
                    plane.Vertices.Length +
                    sphere.Vertices.Length +
                    cube.Vertices.Length +
                    capsule.Vertices.Length +
                    cylinder.Vertices.Length +
                    cone.Vertices.Length
                ];

                /* set up vertex buffer data */

                int vertexBufferOffset = 0;

                Array.Copy(circle.Vertices, vertexData, circle.Vertices.Length);
                primitiveVertexOffsets.Circles = vertexBufferOffset;
                vertexBufferOffset += circle.Vertices.Length;

                Array.Copy(plane.Vertices, 0, vertexData, vertexBufferOffset, plane.Vertices.Length);
                primitiveVertexOffsets.Quads = vertexBufferOffset;
                vertexBufferOffset += plane.Vertices.Length;

                Array.Copy(sphere.Vertices, 0, vertexData, vertexBufferOffset, sphere.Vertices.Length);
                primitiveVertexOffsets.Spheres = vertexBufferOffset;
                primitiveVertexOffsets.HalfSpheres = vertexBufferOffset; // same as spheres
                vertexBufferOffset += sphere.Vertices.Length;

                Array.Copy(cube.Vertices, 0, vertexData, vertexBufferOffset, cube.Vertices.Length);
                primitiveVertexOffsets.Cubes = vertexBufferOffset;
                vertexBufferOffset += cube.Vertices.Length;

                Array.Copy(capsule.Vertices, 0, vertexData, vertexBufferOffset, capsule.Vertices.Length);
                primitiveVertexOffsets.Capsules = vertexBufferOffset;
                vertexBufferOffset += capsule.Vertices.Length;

                Array.Copy(cylinder.Vertices, 0, vertexData, vertexBufferOffset, cylinder.Vertices.Length);
                primitiveVertexOffsets.Cylinders = vertexBufferOffset;
                vertexBufferOffset += cylinder.Vertices.Length;

                Array.Copy(cone.Vertices, 0, vertexData, vertexBufferOffset, cone.Vertices.Length);
                primitiveVertexOffsets.Cones = vertexBufferOffset;
                vertexBufferOffset += cone.Vertices.Length;

                var newVertexBuffer = Buffer.Vertex.New<VertexPositionTexture>(device, vertexData);
                vertexBuffer = newVertexBuffer;

            }

            {

                /* set up index buffer data */

                var indexData = new int[
                    circle.Indices.Length +
                    plane.Indices.Length +
                    sphere.Indices.Length +
                    cube.Indices.Length +
                    capsule.Indices.Length +
                    cylinder.Indices.Length +
                    cone.Indices.Length
                ];

                if (indexData.Length >= 0xFFFF && device.Features.CurrentProfile <= GraphicsProfile.Level_9_3)
                {
                    throw new InvalidOperationException("Cannot generate more than 65535 indices on feature level HW <= 9.3");
                }

                // copy all our primitive data into the buffers

                int indexBufferOffset = 0;

                Array.Copy(circle.Indices, indexData, circle.Indices.Length);
                primitiveIndexOffsets.Circles = indexBufferOffset;
                indexBufferOffset += circle.Indices.Length;

                Array.Copy(plane.Indices, 0, indexData, indexBufferOffset, plane.Indices.Length);
                primitiveIndexOffsets.Quads = indexBufferOffset;
                indexBufferOffset += plane.Indices.Length;

                Array.Copy(sphere.Indices, 0, indexData, indexBufferOffset, sphere.Indices.Length);
                primitiveIndexOffsets.Spheres = indexBufferOffset;
                primitiveIndexOffsets.HalfSpheres = indexBufferOffset; // same as spheres
                indexBufferOffset += sphere.Indices.Length;

                Array.Copy(cube.Indices, 0, indexData, indexBufferOffset, cube.Indices.Length);
                primitiveIndexOffsets.Cubes = indexBufferOffset;
                indexBufferOffset += cube.Indices.Length;

                Array.Copy(capsule.Indices, 0, indexData, indexBufferOffset, capsule.Indices.Length);
                primitiveIndexOffsets.Capsules = indexBufferOffset;
                indexBufferOffset += capsule.Indices.Length;

                Array.Copy(cylinder.Indices, 0, indexData, indexBufferOffset, cylinder.Indices.Length);
                primitiveIndexOffsets.Cylinders = indexBufferOffset;
                indexBufferOffset += cylinder.Indices.Length;

                Array.Copy(cone.Indices, 0, indexData, indexBufferOffset, cone.Indices.Length);
                primitiveIndexOffsets.Cones = indexBufferOffset;
                indexBufferOffset += cone.Indices.Length;

                var newIndexBuffer = Buffer.Index.New<int>(device, indexData);
                indexBuffer = newIndexBuffer;

            }

            // allocate our buffers with position/colour etc data
            var newTransformBuffer = Buffer.Structured.New<Matrix>(device, 1);
            transformBuffer = newTransformBuffer;

            var newColourBuffer = Buffer.Structured.New<Color>(device, colors.Items);
            colorBuffer = newColourBuffer;

            var newLineVertexBuffer = Buffer.Vertex.New<LineVertex>(device, lineVertices.Items, GraphicsResourceUsage.Dynamic);
            lineVertexBuffer = newLineVertexBuffer;

        }

        public override void Extract()
        {

            void ProcessRenderables(FastList<Renderable> renderables, ref Primitives offsets)
            {

                for (int i = 0; i < renderables.Count; ++i)
                {
                    ref var cmd = ref renderables.Items[i];
                    switch (cmd.Type)
                    {
                        case RenderableType.Quad:
                            instances.Items[offsets.Quads].Position = cmd.QuadData.Position;
                            instances.Items[offsets.Quads].Rotation = cmd.QuadData.Rotation;
                            instances.Items[offsets.Quads].Scale = new Vector3(cmd.QuadData.Size.X, 1.0f, cmd.QuadData.Size.Y);
                            instances.Items[offsets.Quads].Color = cmd.QuadData.Color;
                            offsets.Quads++;
                            break;
                        case RenderableType.Circle:
                            instances.Items[offsets.Circles].Position = cmd.CircleData.Position;
                            instances.Items[offsets.Circles].Rotation = cmd.CircleData.Rotation;
                            instances.Items[offsets.Circles].Scale = new Vector3(cmd.CircleData.Radius * 2.0f, 0.0f, cmd.CircleData.Radius * 2.0f);
                            instances.Items[offsets.Circles].Color = cmd.CircleData.Color;
                            offsets.Circles++;
                            break;
                        case RenderableType.Sphere:
                            instances.Items[offsets.Spheres].Position = cmd.SphereData.Position;
                            instances.Items[offsets.Spheres].Rotation = Quaternion.Identity;
                            instances.Items[offsets.Spheres].Scale = new Vector3(cmd.SphereData.Radius * 2);
                            instances.Items[offsets.Spheres].Color = cmd.SphereData.Color;
                            offsets.Spheres++;
                            break;
                        case RenderableType.HalfSphere:
                            instances.Items[offsets.HalfSpheres].Position = cmd.HalfSphereData.Position;
                            instances.Items[offsets.HalfSpheres].Rotation = cmd.HalfSphereData.Rotation;
                            instances.Items[offsets.HalfSpheres].Scale = new Vector3(cmd.HalfSphereData.Radius * 2);
                            instances.Items[offsets.HalfSpheres].Color = cmd.HalfSphereData.Color;
                            offsets.HalfSpheres++;
                            break;
                        case RenderableType.Cube:
                            ref var start = ref cmd.CubeData.Start;
                            ref var end = ref cmd.CubeData.End;
                            instances.Items[offsets.Cubes].Position = start;
                            instances.Items[offsets.Cubes].Rotation = cmd.CubeData.Rotation;
                            instances.Items[offsets.Cubes].Scale = end - start;
                            instances.Items[offsets.Cubes].Color = cmd.CubeData.Color;
                            offsets.Cubes++;
                            break;
                        case RenderableType.Capsule:
                            instances.Items[offsets.Capsules].Position = cmd.CapsuleData.Position;
                            instances.Items[offsets.Capsules].Rotation = cmd.CapsuleData.Rotation;
                            instances.Items[offsets.Capsules].Scale = new Vector3(cmd.CapsuleData.Radius * 2.0f, cmd.CapsuleData.Height, cmd.CapsuleData.Radius * 2.0f);
                            instances.Items[offsets.Capsules].Color = cmd.CapsuleData.Color;
                            offsets.Capsules++;
                            break;
                        case RenderableType.Cylinder:
                            instances.Items[offsets.Cylinders].Position = cmd.CylinderData.Position;
                            instances.Items[offsets.Cylinders].Rotation = cmd.CylinderData.Rotation;
                            instances.Items[offsets.Cylinders].Scale = new Vector3(cmd.CylinderData.Radius * 2.0f, cmd.CylinderData.Height, cmd.CylinderData.Radius * 2.0f);
                            instances.Items[offsets.Cylinders].Color = cmd.CylinderData.Color;
                            offsets.Cylinders++;
                            break;
                        case RenderableType.Cone:
                            instances.Items[offsets.Cones].Position = cmd.ConeData.Position;
                            instances.Items[offsets.Cones].Rotation = cmd.ConeData.Rotation;
                            instances.Items[offsets.Cones].Scale = new Vector3(cmd.ConeData.Radius * 2.0f, cmd.ConeData.Height, cmd.ConeData.Radius * 2.0f);
                            instances.Items[offsets.Cones].Color = cmd.ConeData.Color;
                            offsets.Cones++;
                            break;
                        case RenderableType.Line:
                            lineVertices.Items[offsets.Lines].Position = cmd.LineData.Start;
                            lineVertices.Items[offsets.Lines++].Color = cmd.LineData.Color;
                            lineVertices.Items[offsets.Lines].Position = cmd.LineData.End;
                            lineVertices.Items[offsets.Lines++].Color = cmd.LineData.Color;
                            break;
                    }
                }

            }

            int SumBasicPrimitives(ref Primitives primitives)
            {
                return primitives.Quads
                    + primitives.Circles
                    + primitives.Spheres
                    + primitives.HalfSpheres
                    + primitives.Cubes
                    + primitives.Capsules
                    + primitives.Cylinders
                    + primitives.Cones;
            }

            Primitives SetupPrimitiveOffsets(ref Primitives counts, int offset = 0)
            {
                var offsets = new Primitives();
                offsets.Quads = 0 + offset;
                offsets.Circles = offsets.Quads + counts.Quads;
                offsets.Spheres = offsets.Circles + counts.Circles;
                offsets.HalfSpheres = offsets.Spheres + counts.Spheres;
                offsets.Cubes = offsets.HalfSpheres + counts.HalfSpheres;
                offsets.Capsules = offsets.Cubes + counts.Cubes;
                offsets.Cylinders = offsets.Capsules + counts.Capsules;
                offsets.Cones = offsets.Cylinders + counts.Cylinders;
                return offsets;
            }

            int lastOffset = 0;
            int lastLineOffset = 0;
            foreach (RenderObject renderObject in RenderObjects)
            {

                ImmediateDebugRenderObject debugObject = (ImmediateDebugRenderObject)renderObject;

                /* everything except lines is included here, as lines just get accumulated into a buffer directly */
                int primitivesWithDepth = SumBasicPrimitives(ref debugObject.totalPrimitives);
                int primitivesWithoutDepth = SumBasicPrimitives(ref debugObject.totalPrimitivesNoDepth);
                int totalThingsToDraw = primitivesWithDepth + primitivesWithoutDepth;

                instances.Resize(instances.Count + totalThingsToDraw, true);

                lineVertices.Resize(lineVertices.Count + (debugObject.totalPrimitives.Lines * 2) + (debugObject.totalPrimitivesNoDepth.Lines * 2), true);

                var primitiveOffsets = SetupPrimitiveOffsets(ref debugObject.totalPrimitives, lastOffset);
                var primitiveOffsetsNoDepth = SetupPrimitiveOffsets(ref debugObject.totalPrimitivesNoDepth, primitiveOffsets.Cones + debugObject.totalPrimitives.Cones);

                /* line rendering data, separate buffer so offset isnt relative to the other data */
                primitiveOffsets.Lines = 0 + lastLineOffset;
                primitiveOffsetsNoDepth.Lines = debugObject.totalPrimitives.Lines * 2 + lastLineOffset;

                /* save instance offsets before we mutate them as we need them when rendering */
                debugObject.instanceOffsets = primitiveOffsets;
                debugObject.instanceOffsetsNoDepth = primitiveOffsetsNoDepth;

                ProcessRenderables(debugObject.renderablesWithDepth, ref primitiveOffsets);
                ProcessRenderables(debugObject.renderablesNoDepth, ref primitiveOffsetsNoDepth);

                debugObject.primitivesToDraw = debugObject.totalPrimitives;
                debugObject.primitivesToDrawNoDepth = debugObject.totalPrimitivesNoDepth;

                // store the last offsets, so we can start from there next iteration
                lastOffset = debugObject.instanceOffsetsNoDepth.Cones + debugObject.totalPrimitivesNoDepth.Cones;
                lastLineOffset = debugObject.instanceOffsetsNoDepth.Lines + debugObject.totalPrimitivesNoDepth.Lines * 2;

                // only now clear this data...
                debugObject.renderablesWithDepth.Clear(true);
                debugObject.renderablesNoDepth.Clear(true);
                debugObject.totalPrimitives.Clear();
                debugObject.totalPrimitivesNoDepth.Clear();

            }

        }

        private unsafe static void UpdateBufferIfNecessary(GraphicsDevice device, CommandList commandList, ref Buffer buffer, DataPointer dataPtr, int elementSize)
        {
            int neededBufferSize = dataPtr.Size / elementSize;
            if (neededBufferSize > buffer.ElementCount)
            {
                buffer.Dispose();
                var newBuffer = Stride.Graphics.Buffer.New(
                    device,
                    dataPtr,
                    buffer.StructureByteStride,
                    buffer.Flags
                );
                buffer = newBuffer;
            }
            else
            {
                buffer.SetData(commandList, dataPtr);
            }
        }

        private void CheckBuffers(RenderDrawContext context)
        {

            unsafe
            {

                fixed (Matrix* transformsPtr = transforms.Items)
                {
                    UpdateBufferIfNecessary(
                        context.GraphicsDevice, context.CommandList, buffer: ref transformBuffer,
                        dataPtr: new DataPointer(transformsPtr, transforms.Count * Marshal.SizeOf<Matrix>()),
                        elementSize: Marshal.SizeOf<Matrix>()
                    );
                }

                fixed (Color* colorsPtr = colors.Items)
                {
                    UpdateBufferIfNecessary(
                        context.GraphicsDevice, context.CommandList, buffer: ref colorBuffer,
                        dataPtr: new DataPointer(colorsPtr, colors.Count * Marshal.SizeOf<Color>()),
                        elementSize: Marshal.SizeOf<Color>()
                    );
                }

                fixed (LineVertex* lineVertsPtr = lineVertices.Items)
                {
                    UpdateBufferIfNecessary(
                        context.GraphicsDevice, context.CommandList, buffer: ref lineVertexBuffer,
                        dataPtr: new DataPointer(lineVertsPtr, lineVertices.Count * Marshal.SizeOf<LineVertex>()),
                        elementSize: Marshal.SizeOf<LineVertex>()
                    );
                }

            }

        }

        public override void Prepare(RenderDrawContext context)
        {

            transforms.Resize(instances.Count, true);
            colors.Resize(instances.Count, true);

            Dispatcher.For(0, transforms.Count, (int i) =>
            {
                ref var instance = ref instances.Items[i];
                Matrix.Transformation(ref instance.Scale, ref instance.Rotation, ref instance.Position, out transforms.Items[i]);
                colors[i] = instance.Color;
            }
            );

            CheckBuffers(context);
            lineVertices.Clear(true);
            instances.Clear(true);

        }

        private void SetPrimitiveRenderingPipelineState(CommandList commandList, bool depthTest, FillMode selectedFillMode, bool isDoubleSided = false, bool hasTransparency = false)
        {

            pipelineState.State.SetDefaults();
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
            pipelineState.State.RootSignature = primitiveEffect.RootSignature;
            pipelineState.State.EffectBytecode = primitiveEffect.Effect.Bytecode;
            pipelineState.State.DepthStencilState = (depthTest) ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
            pipelineState.State.RasterizerState.FillMode = selectedFillMode;
            pipelineState.State.RasterizerState.CullMode = (selectedFillMode == FillMode.Solid && !isDoubleSided) ? CullMode.Back : CullMode.None;
            pipelineState.State.BlendState = hasTransparency ? BlendStates.NonPremultiplied : BlendStates.Opaque;
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.State.InputElements = inputElements;
            pipelineState.Update();

        }

        private void SetLineRenderingPipelineState(CommandList commandList, bool depthTest, bool hasTransparency = false)
        {
            pipelineState.State.SetDefaults();
            pipelineState.State.PrimitiveType = PrimitiveType.LineList;
            pipelineState.State.RootSignature = lineEffect.RootSignature;
            pipelineState.State.EffectBytecode = lineEffect.Effect.Bytecode;
            pipelineState.State.DepthStencilState = (depthTest) ? (hasTransparency ? DepthStencilStates.DepthRead : DepthStencilStates.Default) : DepthStencilStates.None;
            pipelineState.State.RasterizerState.FillMode = FillMode.Solid;
            pipelineState.State.RasterizerState.CullMode = CullMode.None;
            pipelineState.State.BlendState = (hasTransparency) ? BlendStates.NonPremultiplied : BlendStates.Opaque;
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.State.InputElements = lineInputElements;
            pipelineState.Update();
        }

        private void RenderPrimitives(RenderDrawContext context, RenderView renderView, ref Primitives offsets, ref Primitives counts, bool depthTest, FillMode fillMode, bool hasTransparency)
        {

            var commandList = context.CommandList;

            // set buffers and our current pipeline state
            commandList.SetVertexBuffer(0, vertexBuffer, 0, VertexPositionTexture.Layout.VertexStride);
            commandList.SetIndexBuffer(indexBuffer, 0, is32bits: true);
            commandList.SetPipelineState(pipelineState.CurrentState);

            // we set line width to something absurdly high to avoid having to alter our shader substantially for now
            primitiveEffect.Parameters.Set(PrimitiveShaderKeys.LineWidthMultiplier, (fillMode == FillMode.Solid) ? 10000.0f : 1.0f);
            primitiveEffect.Parameters.Set(PrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
            primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Transforms, transformBuffer);
            primitiveEffect.Parameters.Set(PrimitiveShaderKeys.Colors, colorBuffer);

            primitiveEffect.UpdateEffect(context.GraphicsDevice);
            primitiveEffect.Apply(context.GraphicsContext);

            // draw spheres
            if (counts.Spheres > 0)
            {

                SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency: hasTransparency);
                commandList.SetPipelineState(pipelineState.CurrentState);

                primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Spheres);
                primitiveEffect.Apply(context.GraphicsContext);

                commandList.DrawIndexedInstanced(sphere.Indices.Length, counts.Spheres, primitiveIndexOffsets.Spheres, primitiveVertexOffsets.Spheres);

            }

            if (counts.Quads > 0 || counts.Circles > 0 || counts.HalfSpheres > 0)
            {

                SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: true, hasTransparency: hasTransparency);
                commandList.SetPipelineState(pipelineState.CurrentState);

                // draw quads
                if (counts.Quads > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Quads);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(plane.Indices.Length, counts.Quads, primitiveIndexOffsets.Quads, primitiveVertexOffsets.Quads);

                }

                // draw circles
                if (counts.Circles > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Circles);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(circle.Indices.Length, counts.Circles, primitiveIndexOffsets.Circles, primitiveVertexOffsets.Circles);

                }

                // draw half spheres
                if (counts.HalfSpheres > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.HalfSpheres);
                    primitiveEffect.Apply(context.GraphicsContext);

                    // HACK: we sort of abuse knowledge of the mesh primitive here.. :P
                    commandList.DrawIndexedInstanced(sphere.Indices.Length / 2, counts.HalfSpheres, primitiveIndexOffsets.HalfSpheres, primitiveVertexOffsets.HalfSpheres);

                }

            }

            if (counts.Cubes > 0 || counts.Capsules > 0 || counts.Cylinders > 0 || counts.Cones > 0)
            {

                SetPrimitiveRenderingPipelineState(commandList, depthTest, fillMode, isDoubleSided: false, hasTransparency: hasTransparency);
                commandList.SetPipelineState(pipelineState.CurrentState);

                // draw cubes
                if (counts.Cubes > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cubes);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(cube.Indices.Length, counts.Cubes, primitiveIndexOffsets.Cubes, primitiveVertexOffsets.Cubes);

                }

                // draw capsules
                if (counts.Capsules > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Capsules);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(capsule.Indices.Length, counts.Capsules, primitiveIndexOffsets.Capsules, primitiveVertexOffsets.Capsules);

                }

                // draw cylinders
                if (counts.Cylinders > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cylinders);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(cylinder.Indices.Length, counts.Cylinders, primitiveIndexOffsets.Cylinders, primitiveVertexOffsets.Cylinders);

                }

                // draw cones
                if (counts.Cones > 0)
                {

                    primitiveEffect.Parameters.Set(PrimitiveShaderKeys.InstanceOffset, offsets.Cones);
                    primitiveEffect.Apply(context.GraphicsContext);

                    commandList.DrawIndexedInstanced(cone.Indices.Length, counts.Cones, primitiveIndexOffsets.Cones, primitiveVertexOffsets.Cones);

                }

            }

            // draw lines
            if (counts.Lines > 0)
            {

                SetLineRenderingPipelineState(commandList, depthTest, hasTransparency);
                commandList.SetVertexBuffer(0, lineVertexBuffer, 0, LineVertex.Layout.VertexStride);
                commandList.SetPipelineState(pipelineState.CurrentState);

                lineEffect.Parameters.Set(LinePrimitiveShaderKeys.ViewProjection, renderView.ViewProjection);
                lineEffect.UpdateEffect(context.GraphicsDevice);
                lineEffect.Apply(context.GraphicsContext);

                commandList.Draw(counts.Lines * 2, offsets.Lines);

            }

        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {

            var commandList = context.CommandList;

            for (int index = startIndex; index < endIndex; index++)
            {

                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var debugObject = (ImmediateDebugRenderObject)(GetRenderNode(renderNodeReference).RenderObject);
                bool objectHasTransparency = debugObject.Stage == DebugRenderStage.Transparent;

                // update pipeline state, render with depth test first
                SetPrimitiveRenderingPipelineState(commandList, depthTest: true, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
                RenderPrimitives(context, renderView, ref debugObject.instanceOffsets, ref debugObject.primitivesToDraw, depthTest: true, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);

                // render without depth test second
                SetPrimitiveRenderingPipelineState(commandList, depthTest: false, selectedFillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);
                RenderPrimitives(context, renderView, offsets: ref debugObject.instanceOffsetsNoDepth, counts: ref debugObject.primitivesToDrawNoDepth, depthTest: false, fillMode: debugObject.CurrentFillMode, hasTransparency: objectHasTransparency);

            }

        }

        public override void Flush(RenderDrawContext context)
        {
        }

        /* FIXME: is there a nicer way to handle dispose, some xenko idiom? */

        public override void Unload()
        {
            base.Unload();
            transformBuffer.Dispose();
            colorBuffer.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            lineVertexBuffer.Dispose();
        }

    }

}
