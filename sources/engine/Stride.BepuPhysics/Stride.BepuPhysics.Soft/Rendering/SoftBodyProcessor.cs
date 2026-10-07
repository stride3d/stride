// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// Swaps the model of each soft body for a <see cref="DeformableModel"/> and keeps it in sync with the particles.
/// </summary>
internal sealed class SoftBodyProcessor : EntityProcessor<SoftBodyComponent, SoftBodyProcessor.Data>
{
    internal sealed class Data
    {
        public ModelComponent? ModelComponent;

        /// <summary> The last model the body was created from or failed to be created from, so it is only retried when the model changes </summary>
        public Model? AttemptedModel;
    }

    private readonly List<DeformableModel> _retired = new();
    private readonly List<SoftBodyComponent> _drawn = new();
    private GraphicsDevice? _device;
    private GraphicsContext? _context;

    public SoftBodyProcessor()
    {
        Order = -100; // Draws after the transforms are updated and before the models are collected for rendering
    }

    protected override void OnSystemAdd()
    {
        _device = Services.GetService<IGraphicsDeviceService>()?.GraphicsDevice;
        _context = Services.GetService<GraphicsContext>();
    }

    protected override void OnSystemRemove()
    {
        DisposeRetired();
    }

    protected override Data GenerateComponentData(Entity entity, SoftBodyComponent component) => new();

    protected override void OnEntityComponentRemoved(Entity entity, SoftBodyComponent component, Data data)
    {
        Release(component, data);
    }

    public override void Update(GameTime time)
    {
        // Models retired during the last frame are no longer referenced by the renderer
        DisposeRetired();

        if (_device is null)
            return;

        foreach (var (component, data) in ComponentDatas)
        {
            var modelComponent = data.ModelComponent ??= component.Entity.Get<ModelComponent>();
            if (component.Simulation is null || component.Topology is not { } topology || component.ParticleCount != topology.ParticleCount)
            {
                Release(component, data);
                // The model may only have been set after the body was added
                if (component.Simulation is null && modelComponent?.Model is { } model && model != data.AttemptedModel)
                {
                    data.AttemptedModel = model;
                    component.TryUpdateFeatures();
                }
                continue;
            }

            data.AttemptedModel = component.SourceModel;
            if (modelComponent is null)
                continue;

            var renderModel = component.RenderModel;
            if (renderModel is not null && modelComponent.Model != renderModel.Model)
            {
                // Someone replaced the model, the body has to be rebuilt from the new one
                Release(component, data, restore: false);
                component.InvalidateTopology();
                continue;
            }

            if (renderModel is null || renderModel.Topology != topology)
            {
                Release(component, data);
                renderModel = component.RenderModel = new DeformableModel(_device, component.SourceModel!, topology);
                modelComponent.Model = renderModel.Model;
            }
        }
    }

    public override void Draw(RenderContext context)
    {
        if (_context is null)
            return;

        _drawn.Clear();
        foreach (var (component, _) in ComponentDatas)
        {
            if (component.RenderModel is { } renderModel && component.Simulation is not null && component.ParticleCount == renderModel.Topology.ParticleCount)
                _drawn.Add(component);
        }

        // Bodies deform independently, only the uploads go through the command list
        Dispatcher.For(0, _drawn.Count, i => _drawn[i].RenderModel!.Deform(_drawn[i], _drawn[i].Entity.Transform.WorldMatrix));
        foreach (var component in _drawn)
            component.RenderModel!.Upload(_context.CommandList);
    }

    private void Release(SoftBodyComponent component, Data data, bool restore = true)
    {
        if (component.RenderModel is not { } renderModel)
            return;

        if (restore && data.ModelComponent is { } modelComponent && modelComponent.Model == renderModel.Model)
            modelComponent.Model = component.SourceModel;
        _retired.Add(renderModel);
        component.RenderModel = null;
    }

    private void DisposeRetired()
    {
        foreach (var renderModel in _retired)
            renderModel.Dispose();
        _retired.Clear();
    }
}
