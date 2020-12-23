// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(LightProbeComponent), true)]
    public class LightProbeGizmo : EntityGizmo<LightProbeComponent>
    {
        public const RenderGroup LightProbeGroup = RenderGroup.Group17;
        public const RenderGroupMask LightProbeGroupMask = RenderGroupMask.Group17;

        private Material lightProbeMaterial;

        public LightProbeGizmo(EntityComponent component)
            : base(component)
        {
            RenderGroup = LightProbeGroup;
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }

            set
            {
                base.IsSelected = value;
                GizmoRootEntity.Tags.Set(EditorGameComponentGizmoService.SelectedKey, value);
            }
        }

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

        public override void Update()
        {
            base.Update();

            if (Component.Coefficients != null)
                lightProbeMaterial.Passes[0].Parameters.Set(ComputeSphericalHarmonicsKeys.SphericalColors, Component.Coefficients.Count, ref Component.Coefficients.Items[0]);
        }

        class ComputeSphericalHarmonics : ComputeValueBase<Color4>, IComputeColor
        {
            public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
            {
                return new ShaderClassSource("ComputeSphericalHarmonics", 5);
            }

            public bool HasChanged { get; }
        }
    }
}
