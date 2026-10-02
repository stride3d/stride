// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Stride.Heightfield;

/// <summary>
/// Basic rendering component for an <see cref="IHeightfieldSource"/> mesh.
/// Rendered as a uniformly dense plane static, or around the camera (<see cref="VirtualPlaneRatio"/>).
/// Work in conjunction with a material hosting a <see cref="HeightfieldDisplacementFeature"/>.
/// </summary>
[DataContract("HeightfieldModelComponent")]
[Display("Heightfield Model Component")]
[ComponentCategory("Model")]
[DefaultEntityComponentProcessor(typeof(Processor))]
public class HeightfieldModelComponent : EntityComponent
{
    private int _version;

    /// <summary>
    /// The heightfield definition to render
    /// </summary>
    public required IHeightfieldSource Source
    {
        get;
        set
        {
            field = value;
            _version++;
        }
    }

    /// <summary>
    /// The material to use for the mesh. Must have <see cref="HeightfieldDisplacementFeature"/> set as the displacement feature.
    /// </summary>
    public readonly MaterialInstance Material = new();

    /// <summary>
    /// The <see cref="Stride.Rendering.RenderGroup"/> to assign for this mesh
    /// </summary>
    [DefaultValue(RenderGroup.Group0)]
    public RenderGroup RenderGroup { get; set; }

    /// <summary>
    /// Control whether the plane is stretched across the whole heightfield (0), or centered around the camera (>=1), best for very large terrain.
    /// A value greater than one stretches the mesh across a larger amount of heightfield subdivision,
    /// for example: two would have a ratio of two heightfield subdivisions per vertices
    /// </summary>
    public int VirtualPlaneRatio { get; set; } = 0;

    /// <summary>
    /// The amount of subdivisions for the plane, 4 producing a grid of 4^2 vertices
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value must be greater than one</exception>
    public int Tessellation
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
            _version++;
        }
    } = 128;

    public class AssociatedData
    {
        public ModelComponent ModelComponent = new();
        public int Version;
        public Model? Model;
    }

    public class Processor : EntityProcessor<HeightfieldModelComponent, AssociatedData>
    {
        protected override AssociatedData GenerateComponentData(Entity entity, HeightfieldModelComponent component)
        {
            var e = new Entity($"{nameof(HeightfieldModelComponent)} model");
            e.Transform.Parent = entity.Transform;
            var data = new AssociatedData();
            e.Add(data.ModelComponent);
            return data;
        }

        protected override void OnEntityComponentAdding(Entity entity, HeightfieldModelComponent component, AssociatedData data)
        {
            base.OnEntityComponentAdding(entity, component, data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, HeightfieldModelComponent component, AssociatedData data)
        {
            base.OnEntityComponentRemoved(entity, component, data);

            data.ModelComponent.Entity.Transform.Parent = null;
            Dispose(ref data.Model);
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            foreach (var (heightfieldRendering, data) in ComponentDatas)
            {
                if (data.Version != heightfieldRendering._version)
                {
                    data.Version = heightfieldRendering._version;
                    Generate(ref data.Model, data.ModelComponent, heightfieldRendering);
                }

                data.ModelComponent.RenderGroup = heightfieldRendering.RenderGroup;

                var mat = heightfieldRendering.Material.Material;
                if (mat is null)
                    continue;

                data.ModelComponent.Materials[0] = mat;

                if (mat.Passes.Count == 0)
                    continue; // In editor

                if (heightfieldRendering.Source is null)
                    continue;

                var targetCellSize = heightfieldRendering.Source.Size / heightfieldRendering.Source.Subdivision * 2;
                var virtPlaneSize = targetCellSize * heightfieldRendering.Tessellation;
                var planeSize = heightfieldRendering.Source.Size;
                if (heightfieldRendering.VirtualPlaneRatio > 0)
                    planeSize = Math.Min(virtPlaneSize * heightfieldRendering.VirtualPlaneRatio, planeSize);
                var cellSize = planeSize / heightfieldRendering.Tessellation;
                mat.Passes[0].Parameters.Set(HeightfieldDisplacementPropertiesKeys.PlaneCellSize, cellSize);
                mat.Passes[0].Parameters.Set(HeightfieldDisplacementPropertiesKeys.PlaneSize, planeSize);
                mat.Passes[0].Parameters.Set(HeightfieldDisplacementPropertiesKeys.HeightfieldSize, heightfieldRendering.Source.Size);
            }
        }

        private void Generate(ref Model? model, ModelComponent modelComponent, HeightfieldModelComponent component)
        {
            Dispose(ref model);

            if (Services is null || component.Source == null!)
                return;

            var sourceSize = component.Source.Size;

            model = new PlaneProceduralModel
            {
                Tessellation = new Int2(component.Tessellation),
                Size = new Vector2(1f),
                UvScale = new Vector2(1),
                LocalOffset = new Vector3(0.5f, 0, 0.5f),
                NumberOfTextureCoordinates = 1
            }.Generate(Services);

            BoundingBox bb;
            bb.Minimum = default;
            bb.Maximum = new Vector3(sourceSize);
            bb.Minimum.Y = component.Source.MinHeight;
            bb.Maximum.Y = component.Source.MaxHeight;

            BoundingSphere bs;
            bs.Center = bb.Center;
            bs.Radius = bb.Extent.Length();

            model.Meshes[0].BoundingBox = bb;
            model.Meshes[0].BoundingSphere = bs;
            model.BoundingBox = bb;
            model.BoundingSphere = bs;
            modelComponent.Model = model;
        }

        private void Dispose(ref Model? model)
        {
            var previous = Interlocked.Exchange(ref model, null);
            if (previous == null)
                return;

            foreach (var mesh in previous.Meshes)
            {
                mesh.Draw.IndexBuffer.Buffer.Dispose();
                foreach (var vBuffer in mesh.Draw.VertexBuffers)
                    vBuffer.Buffer.Dispose();
            }
        }
    }
}
