// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public abstract class BillboardingGizmo<T> : EntityGizmo<T> where T : EntityComponent
    {
        private readonly string gizmoName;
        private readonly byte[] billboardData;

        protected BillboardingGizmo(EntityComponent component, string gizmoName, byte[] billboardData) : base(component)
        {
            this.gizmoName = gizmoName;
            this.billboardData = billboardData;
        }

        protected override float SizeAdjustmentFactor { get { return 0.5f; } } // reduce the size of billboard gizmos.

        protected override Entity Create()
        {
            var gizmoTexture = GraphicsDevice.GetOrCreateSharedData(gizmoName, d => TextureExtensions.FromFileData(d, billboardData));

            // Create a root that will contains the billboard as a child, so that it is easier to add other elements with only the billboard set as a GizmoScalingEntity
            var root = new Entity(gizmoName);

            var billboard = new Entity(gizmoName + " Billboard")
            {
                new SpriteComponent
                {
                    SpriteType = SpriteType.Billboard,
                    SpriteProvider = new SpriteFromTexture
                    {
                        IsTransparent = true,  
                        Texture = gizmoTexture,
                        PixelsPerUnit = gizmoTexture.Width,
                    },
                    PremultipliedAlpha = false,
                    Color = new Color(255, 255, 255, 192),
                    IsAlphaCutoff = false,
                    RenderGroup = RenderGroup,
                }
            };

            root.AddChild(billboard);

            // Scaling should only affect billboard
            GizmoScalingEntity = billboard;

            return root;
        }
    }
}
