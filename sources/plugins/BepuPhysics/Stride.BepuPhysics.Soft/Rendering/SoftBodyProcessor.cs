// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// Swaps the model of each soft body for a <see cref="DeformableModel"/> and keeps it in sync with the particles.
/// </summary>
/// <remarks> Adds a <see cref="SoftBodyDeformationRenderFeature"/> to the mesh render features of the compositor for bodies deformed on the GPU. </remarks>
internal sealed class SoftBodyProcessor : EntityProcessor<SoftBodyComponent, SoftBodyProcessor.Data>
{
    internal sealed class Data
    {
        public ModelComponent? ModelComponent;

        /// <summary> The last model the body was created from or failed to be created from, so it is only retried when the model changes </summary>
        public Model? AttemptedModel;

        /// <summary> Whether the lack of a model was already reported </summary>
        public bool ReportedMissingModel;
    }

    private static readonly Logger Logger = GlobalLogger.GetLogger(nameof(SoftBodyProcessor));

    /// <summary> The deformation and upload of every soft body, each frame </summary>
    internal static readonly ProfilingKey DrawKey = new("SoftBodyProcessor.Draw");

    private readonly List<IDisposable> _retired = new(); // Models and buffers retired during the last frame
    private readonly List<SoftBodyComponent> _drawn = new();
    private GraphicsDevice? _device;
    private GraphicsContext? _context;
    private SceneSystem? _sceneSystem;
    private GraphicsCompositor? _compositor; // The last compositor the render feature was added to
    private readonly List<SoftBodyDeformationRenderFeature> _renderFeatures = new(); // The ones of that compositor
    private SoftBodyConfiguration _configuration = new();
    private bool _gpuSupported;
    private bool _warnedGpuUnsupported;
    private bool _warnedNoCompositor;

    public SoftBodyProcessor()
    {
        Order = -100; // Draws after the transforms are updated and before the models are collected for rendering
    }

    protected override void OnSystemAdd()
    {
        var deviceService = Services.GetService<IGraphicsDeviceService>();
        _device = deviceService?.GraphicsDevice;
        _context = Services.GetService<GraphicsContext>();
        _sceneSystem = Services.GetService<SceneSystem>();
        _configuration = Services.GetOrCreate<SoftBodyConfiguration>();
        // The vertex shader reads structured buffers, which devices have along with compute shaders; effects compile for
        // the shader profile, which may be lower than what the device supports
        var shaderProfile = (deviceService as GraphicsDeviceManager)?.ShaderProfile ?? _device?.Features.RequestedProfile;
        _gpuSupported = _device is not null && _device.Features.HasComputeShaders && shaderProfile >= GraphicsProfile.Level_11_0;
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
                else if (modelComponent?.Model is null && data.ReportedMissingModel == false)
                {
                    data.ReportedMissingModel = true;
                    Logger.Warning($"{component.GetType().Name} on '{component.Entity.Name}' has no shape to take: it needs a {nameof(ModelComponent)} with a model on the same entity");
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

            var mode = DeformationMode();
            var quality = _configuration.DeformationQuality;
            // A tessellated draw keeps the vertex buffers it was made from: its model has to be made again for it to see the new ones
            if (renderModel is null || renderModel.Topology != topology || (renderModel.IsIn(mode, quality) == false && IsTessellated(modelComponent)))
            {
                Release(component, data);
                try
                {
                    renderModel = component.RenderModel = new DeformableModel(_device, Services, component.SourceModel!, topology, mode, quality);
                }
                catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
                {
                    // A body whose model cannot follow it would be simulated without being seen
                    Logger.Error($"{component.GetType().Name} on '{component.Entity.Name}' cannot deform its model, it is left out of the simulation: {e.Message}");
                    component.Detach();
                    continue;
                }
                modelComponent.Model = renderModel.Model;
            }
            else
            {
                // Switching in place keeps Model identity, so what follows the model (node links, skeleton bodies, animations) is left alone
                renderModel.SetMode(_device, mode, quality, _retired);
            }
        }
    }

    public override void Draw(RenderContext context)
    {
        if (_context is null)
            return;

        using var _ = Profiler.Begin(DrawKey);
        _drawn.Clear();
        bool anyOnGpu = false;
        foreach (var (component, _) in ComponentDatas)
        {
            if (component.RenderModel is { } renderModel && component.Simulation is not null && component.ParticleCount == renderModel.Topology.ParticleCount)
            {
                _drawn.Add(component);
                anyOnGpu |= renderModel.Mode == SoftBodyDeformationMode.Gpu;
            }
        }
        if (anyOnGpu)
        {
            foreach (var feature in _renderFeatures)
                feature.Request();
        }

        // Bodies deform independently, only the uploads go through the command list
        Dispatcher.For(0, _drawn.Count, _drawn, static (drawn, i) => drawn[i].RenderModel!.Deform(drawn[i], drawn[i].Entity.Transform.WorldMatrix));
        foreach (var component in _drawn)
            component.RenderModel!.Upload(_context.CommandList);
    }

    /// <summary> CPU or GPU, as the settings say and as far as the device and the compositor can do it </summary>
    private SoftBodyDeformationMode DeformationMode()
    {
        if (_configuration.DeformationMode == SoftBodyDeformationMode.Cpu)
            return SoftBodyDeformationMode.Cpu;

        if (_gpuSupported == false)
        {
            if (_warnedGpuUnsupported == false)
                Logger.Warning($"Soft bodies are deformed on the CPU: deforming them on the GPU needs a device with compute shader support and shaders of the {GraphicsProfile.Level_11_0} profile or above");
            _warnedGpuUnsupported = true;
            return SoftBodyDeformationMode.Cpu;
        }

        // Without the render feature, the meshes would be drawn as they are at rest
        if (AddRenderFeature() == false)
        {
            if (_warnedNoCompositor == false)
                Logger.Warning("Soft bodies are deformed on the CPU: deforming them on the GPU needs a graphics compositor with a mesh render feature");
            _warnedNoCompositor = true;
            return SoftBodyDeformationMode.Cpu;
        }
        return SoftBodyDeformationMode.Gpu;
    }

    /// <summary> Makes the mesh render features of the current compositor draw the meshes deformed on the GPU, false when it has none </summary>
    private bool AddRenderFeature()
    {
        var compositor = _sceneSystem?.GraphicsCompositor;
        if (compositor != _compositor)
        {
            _compositor = compositor;
            _renderFeatures.Clear();
            if (compositor is not null)
            {
                foreach (var feature in compositor.RenderFeatures)
                {
                    if (feature is not MeshRenderFeature meshFeature)
                        continue;

                    var deformation = meshFeature.RenderFeatures.OfType<SoftBodyDeformationRenderFeature>().FirstOrDefault();
                    if (deformation is null)
                        meshFeature.RenderFeatures.Add(deformation = new SoftBodyDeformationRenderFeature());
                    _renderFeatures.Add(deformation);
                }
            }
        }
        return _renderFeatures.Count > 0;
    }

    /// <summary> Whether a material of the model, its own or one the component replaces it with, tessellates the meshes </summary>
    private static bool IsTessellated(ModelComponent modelComponent)
    {
        foreach (var material in modelComponent.Materials.Values)
        {
            if (IsTessellated(material))
                return true;
        }
        if (modelComponent.Model is { } model)
        {
            foreach (var instance in model.Materials)
            {
                if (IsTessellated(instance.Material))
                    return true;
            }
        }
        return false;

        static bool IsTessellated(Material? material)
        {
            if (material is null)
                return false;

            foreach (var pass in material.Passes)
            {
                if (pass.TessellationMethod != StrideTessellationMethod.None)
                    return true;
            }
            return false;
        }
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
        foreach (var retired in _retired)
            retired.Dispose();
        _retired.Clear();
    }
}
