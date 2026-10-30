// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Assets.Editor.Gizmos;

/// <summary>
/// Shows a light probe as a small sphere lit by its coefficients.
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>LightProbeGizmo</c>. <see cref="EditorGameLightProbeGizmoService"/> draws it.
/// </remarks>
[GizmoComponent(typeof(LightProbeComponent), true)]
public sealed class LightProbeGizmo : EntityGizmo<LightProbeComponent>
{
    public const RenderGroup LightProbeGroup = RenderGroup.Group17;
    public const RenderGroupMask LightProbeGroupMask = RenderGroupMask.Group17;

    private Material? lightProbeMaterial;

    public LightProbeGizmo(EntityComponent component)
        : base(component)
    {
        RenderGroup = LightProbeGroup;
    }

    /// <inheritdoc/>
    public override bool IsSelected
    {
        get => base.IsSelected;
        set
        {
            base.IsSelected = value;
            // The light probe service draws the selected probes in wireframe
            GizmoRootEntity.Tags.Set(EditorGameComponentGizmoService.SelectedKey, value);
        }
    }

    /// <inheritdoc/>
    protected override Entity Create()
    {
        var gizmoGeometricPrimitive = GraphicsDevice.GetOrCreateSharedData("LightProbe GeometricData", d => GeometricPrimitive.Sphere.New(GraphicsDevice, 0.2f, 8));
        lightProbeMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Emissive = new MaterialEmissiveMapFeature(new ComputeSphericalHarmonics()),
            }
        });

        return new Entity
        {
            new ModelComponent
            {
                Model = new Model
                {
                    lightProbeMaterial,
                    new Mesh
                    {
                        Draw = gizmoGeometricPrimitive.ToMeshDraw(),
                    },
                },
                RenderGroup = RenderGroup,
            }
        };
    }

    /// <inheritdoc/>
    public override void Update()
    {
        base.Update();

        if (Component.Coefficients is { Count: > 0 } coefficients && lightProbeMaterial is not null)
        {
            var coefficientsSpan = CollectionsMarshal.AsSpan(coefficients);
            lightProbeMaterial.Passes[0].Parameters.Set(ComputeSphericalHarmonicsKeys.SphericalColors, coefficients.Count, ref coefficientsSpan[0]);
        }
    }

    private sealed class ComputeSphericalHarmonics : ComputeValueBase<Color4>, IComputeColor
    {
        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            return new ShaderClassSource("ComputeSphericalHarmonics", 5);
        }

        public bool HasChanged => false;
    }
}
