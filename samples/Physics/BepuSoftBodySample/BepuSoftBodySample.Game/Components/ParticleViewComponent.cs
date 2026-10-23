// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.BepuPhysics.Soft;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Pressing B shows the particles of every soft body in the scene as small octahedra, pinned particles with their own material.
/// </summary>
[ComponentCategory("BepuSoftBodyDemo")]
public class ParticleViewComponent : SyncScript
{
    private readonly Dictionary<SoftBodyComponent, View> _views = new();

    private sealed record View(Entity Entity, Buffer VertexBuffer, Buffer IndexBuffer, VertexPositionNormalTexture[] Vertices);

    /// <summary> The material of the particles that move freely </summary>
    public Material? ParticleMaterial { get; set; }

    /// <summary> The material of the particles held by a pin </summary>
    public Material? PinnedMaterial { get; set; }

    /// <summary> Whether the particles are shown </summary>
    public bool Visible { get; set; }

    public override void Update()
    {
        if (Input.IsKeyPressed(Keys.B))
            Visible = !Visible;

        var bodies = Visible
            ? Entity.Scene.Entities.Select(e => e.Get<SoftBodyComponent>()).Where(b => b is { ParticleCount: > 0 }).ToHashSet()
            : new HashSet<SoftBodyComponent?>();

        foreach (var body in _views.Keys.Where(b => bodies.Contains(b) == false || _views[b].Vertices.Length != b.ParticleCount * 6).ToArray())
            Remove(body);

        foreach (var body in bodies)
        {
            if (_views.TryGetValue(body!, out var view) == false)
                _views[body!] = view = Create(body!);
            Refresh(body!, view);
        }
    }

    public override void Cancel()
    {
        foreach (var body in _views.Keys.ToArray())
            Remove(body);
    }

    private void Remove(SoftBodyComponent body)
    {
        var view = _views[body];
        Entity.Scene.Entities.Remove(view.Entity);
        view.VertexBuffer.Dispose();
        view.IndexBuffer.Dispose();
        _views.Remove(body);
    }

    private View Create(SoftBodyComponent body)
    {
        // Six corners of an octahedron per particle, the free particles' triangles first, then the pinned ones'
        ReadOnlySpan<int> faces = [0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0, 4, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3, 5];
        var indices = new List<int>();
        int free = 0;
        foreach (var pinned in new[] { false, true })
        {
            for (int p = 0; p < body.ParticleCount; p++)
            {
                if (body.IsParticlePinned(p) != pinned)
                    continue;
                foreach (var corner in faces)
                    indices.Add(p * 6 + corner);
            }
            if (pinned == false)
                free = indices.Count;
        }

        var vertices = new VertexPositionNormalTexture[body.ParticleCount * 6];
        var vertexBuffer = Buffer.Vertex.New(GraphicsDevice, bufferSize: vertices.Length * VertexPositionNormalTexture.Layout.VertexStride, usage: GraphicsResourceUsage.Dynamic);
        var indexBuffer = Buffer.Index.New(GraphicsDevice, indices.ToArray());
        var vertexBinding = new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertices.Length);
        var indexBinding = new IndexBufferBinding(indexBuffer, true, indices.Count);
        var everywhere = new BoundingBox(new Vector3(-1e6f), new Vector3(1e6f));

        var model = new Model { BoundingBox = everywhere };
        model.Materials.Add(new MaterialInstance(ParticleMaterial));
        model.Materials.Add(new MaterialInstance(PinnedMaterial ?? ParticleMaterial));
        model.Meshes.Add(new Mesh { MaterialIndex = 0, BoundingBox = everywhere, Draw = new MeshDraw { PrimitiveType = PrimitiveType.TriangleList, DrawCount = free, VertexBuffers = [vertexBinding], IndexBuffer = indexBinding } });
        model.Meshes.Add(new Mesh { MaterialIndex = 1, BoundingBox = everywhere, Draw = new MeshDraw { PrimitiveType = PrimitiveType.TriangleList, StartLocation = free, DrawCount = indices.Count - free, VertexBuffers = [vertexBinding], IndexBuffer = indexBinding } });

        var entity = new Entity("Particles") { new ModelComponent(model) { IsShadowCaster = false } };
        Entity.Scene.Entities.Add(entity);
        return new View(entity, vertexBuffer, indexBuffer, vertices);
    }

    private void Refresh(SoftBodyComponent body, View view)
    {
        var radius = body.Collider is Stride.BepuPhysics.Soft.Colliders.ParticleCollider collider ? collider.ActualRadius * 0.4f : 0.02f;
        for (int p = 0; p < body.ParticleCount; p++)
        {
            var position = body.GetParticlePosition(p);
            for (int axis = 0; axis < 6; axis++)
            {
                var direction = Vector3.Zero;
                direction[axis / 2] = axis % 2 == 0 ? 1f : -1f;
                view.Vertices[p * 6 + axis] = new VertexPositionNormalTexture(position + direction * radius, direction, Vector2.Zero);
            }
        }
        view.VertexBuffer.SetData<VertexPositionNormalTexture>(Game.GraphicsContext.CommandList, view.Vertices.AsSpan());
    }
}
