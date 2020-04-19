// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Sprites;

namespace Stride.Engine
{
    /// <summary>
    /// Add a <see cref="Sprite"/> to an <see cref="Entity"/>. It could be an animated sprite.
    /// </summary>
    [DataContract("SpriteComponent")]
    [Display("Sprite", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(SpriteRenderProcessor))]
    [ComponentOrder(10000)]
    [ComponentCategory("Sprites")]
    public sealed class SpriteComponent : ActivableEntityComponent
    {
        /// <summary>
        /// The group of sprites associated to the component.
        /// </summary>
        /// <userdoc>The source of the sprite data.</userdoc>
        [DataMember(5)]
        [Display("Source")]
        [NotNull]
        public ISpriteProvider SpriteProvider { get; set; }

        /// <summary>
        /// The type of the sprite.
        /// </summary>
        /// <userdoc>The type of the sprite. A 3D sprite in the scene or billboard perpendicular to camera view.</userdoc>
        [DataMember(10)]
        [DefaultValue(SpriteType.Sprite)]
        [Display("Type")]
        public SpriteType SpriteType;

        /// <summary>
        /// The color shade to apply on the sprite.
        /// </summary>
        /// <userdoc>The color to apply to the sprite.</userdoc>
        [DataMember(40)]
        [Display("Color")]
        public Color4 Color { get; set; } = Color4.White;

        /// <summary>
        /// The intensity by which the color is scaled.
        /// </summary>
        /// <userdoc>
        /// The intensity by which the color is scaled. Mainly used for rendering LDR sprites in in HDR scenes.
        /// </userdoc>
        [DataMember(42)]
        [Display("Intensity")]
        [DefaultValue(1f)]
        public float Intensity { get => intensity; set => intensity = Math.Max(value, 0f); }

        private float intensity = 1f;

        /// <summary>
        /// Gets or sets a value indicating whether the sprite is a pre-multiplied alpha (default is true).
        /// </summary>
        /// <value><c>true</c> if the texture is pre-multiplied by alpha; otherwise, <c>false</c>.</value>
        [DataMember(50)]
        [DefaultValue(true)]
        public bool PremultipliedAlpha { get; set; }

        /// <summary>
        /// Ignore the depth of other elements of the scene when rendering the sprite by disabling the depth test.
        /// </summary>
        /// <userdoc>Ignore the depth of other elements of the scene when rendering the sprite. When checked, the sprite is always put on top of previous elements.</userdoc>
        [DataMember(60)]
        [DefaultValue(false)]
        [Display("Ignore Depth")]
        public bool IgnoreDepth;

        /// <summary>
        /// Discard pixels with low alpha value when rendering the sprite by performing alpha cut off test.
        /// </summary>
        /// <userdoc>Ignore pixels with low alpha value when rendering the sprite by performing alpha cut off test.</userdoc>
        [DataMember(65)]
        [DefaultValue(false)]
        [Display("Is Alpha Cutoff")]
        public bool IsAlphaCutoff;

        /// <summary>
        /// Discard pixels with low alpha value when rendering the sprite by performing alpha cut off test.
        /// </summary>
        /// <userdoc>Ignore pixels with low alpha value when rendering the sprite by performing alpha cut off test.</userdoc>
        [DataMember(67)]
        [DefaultValue(SpriteBlend.Auto)]
        [Display("Blend mode")]
        public SpriteBlend BlendMode { get; set; } = SpriteBlend.Auto;

        /// <summary>
        /// Specifies the texture sampling method to be used for this sprite
        /// </summary>
        /// <userdoc>
        /// Specifies the texture sampling method to be used for this sprite
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(SpriteSampler.LinearClamp)]
        [Display("Sampler")]
        public SpriteSampler Sampler { get; set; } = SpriteSampler.LinearClamp;

        /// <summary>
        /// Specifies the swizzle method for sampling (how to access and mix the color channels)
        /// </summary>
        /// <userdoc>
        /// Specifies the swizzle method for sampling (how to access and mix the color channels)
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(SwizzleMode.None)]
        [Display("Swizzle")]
        public SwizzleMode Swizzle { get; set; } = SwizzleMode.None;

        /// <summary>
        /// The render group for this component.
        /// </summary>
        [DataMember(90)]
        [Display("Render group")]
        [DefaultValue(RenderGroup.Group0)]
        public RenderGroup RenderGroup { get; set; }

        [DataMemberIgnore]
        internal double ElapsedTime;

        /// <summary>
        /// Creates a new instance of <see cref="SpriteComponent"/>
        /// </summary>
        public SpriteComponent()
        {
            SpriteProvider = new SpriteFromSheet();
            PremultipliedAlpha = true;
        }

        /// <summary>
        /// Gets the current frame of the animation.
        /// </summary>
        [DataMemberIgnore]
        public int CurrentFrame => (SpriteProvider as SpriteFromSheet)?.CurrentFrame ?? 0;

        /// <summary>
        /// Gets the current sprite.
        /// </summary>
        [DataMemberIgnore]
        public Sprite CurrentSprite => SpriteProvider?.GetSprite();

        private static readonly Queue<List<int>> SpriteIndicesPool = new Queue<List<int>>();

        [DataMemberIgnore]
        internal double AnimationTime;

        [DataMemberIgnore]
        internal int CurrentIndexIndex;

        [DataMemberIgnore]
        internal bool IsPaused;

        internal struct AnimationInfo
        {
            public float FramePerSeconds;

            public bool ShouldLoop;

            public List<int> SpriteIndices;
        }

        internal Queue<AnimationInfo> Animations = new Queue<AnimationInfo>();

        internal static List<int> GetNewSpriteIndicesList()
        {
            lock (SpriteIndicesPool)
            {
                return SpriteIndicesPool.Count > 0 ? SpriteIndicesPool.Dequeue() : new List<int>();
            }
        }

        internal static void RecycleSpriteIndicesList(List<int> indicesList)
        {
            lock (SpriteIndicesPool)
            {
                indicesList.Clear();
                SpriteIndicesPool.Enqueue(indicesList);
            }
        }

        internal void ClearAnimations()
        {
            lock (SpriteIndicesPool)
            {
                while (Animations.Count > 0)
                    RecycleSpriteIndicesList(Animations.Dequeue().SpriteIndices);
            }
        }

        internal void RecycleFirstAnimation()
        {
            CurrentIndexIndex = 0;
            if (Animations.Count > 0)
            {
                var info = Animations.Dequeue();
                RecycleSpriteIndicesList(info.SpriteIndices);
            }
        }
    }
}
