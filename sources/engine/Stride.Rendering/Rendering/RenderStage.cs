// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Shaders;

namespace Stride.Rendering
{
    /// <summary>
    /// Represents a way to instantiate a given <see cref="RenderObject"/> for rendering, esp. concerning effect selection and permutation.
    /// </summary>
    /// Note that a stage doesn't imply any order. You can render any combination of <see cref="RenderStage"/> and <see cref="RenderView"/> arbitrarily at any point of the Draw phase.
    [DataContract]
    public sealed class RenderStage : IIdentifiable
    {
        /// <inheritdoc/>
        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <summary>
        /// The name of this <see cref="RenderStage"/>.
        /// </summary>
        public string Name { get; set; } = "RenderStage";

        /// <summary>
        /// The effect permutation slot name. If similar to another <see cref="RenderStage"/>, effect permutations will be shared.
        /// </summary>
        public string EffectSlotName { get; set; } = "Main";

        /// <summary>
        /// Defines how <see cref="RenderNode"/> sorting should be performed.
        /// </summary>
        [DefaultValue(null)]
        public SortMode SortMode { get; set; }

        [DefaultValue(null)]
        public RenderStageFilter Filter { get; set; }

        public RenderStage()
        {
            Id = Guid.NewGuid();
            OutputValidator = new RenderOutputValidator(this);
        }

        public RenderStage(string name, string effectSlotName) : this()
        {
            Name = name;
            EffectSlotName = effectSlotName;
        }

        /// <summary>
        /// Defines render targets this stage outputs to.
        /// </summary>
        [DataMemberIgnore]
        public RenderOutputDescription Output;

        [DataMemberIgnore]
        public RenderOutputValidator OutputValidator { get; }

        /// <summary>
        /// Index in <see cref="RenderSystem.RenderStages"/>.
        /// </summary>
        [DataMemberIgnore]
        public int Index = -1;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
