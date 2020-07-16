// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine.Design;
using Stride.Engine.Processors;

namespace Stride.Engine
{
    [DataContract("InstancingComponent")]
    [Display("Instancing", Expand = ExpandRule.Once)]
    [ComponentCategory("Model")]
    [DefaultEntityComponentRenderer(typeof(InstancingProcessor))]
    public sealed class InstancingComponent : ActivableEntityComponent
    {
        private IInstancing type = new InstancingEntityTransform();

        /// <summary>
        /// Gets or sets the type of the instancing.
        /// </summary>
        /// <value>The type of the instancing.</value>
        /// <userdoc>The type of the instancing</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Instancing Type", Expand = ExpandRule.Always)]
        public IInstancing Type
        {
            get => type;
            set
            {
                if (value != type)
                {
                    type = value;
                    InstancingChanged?.Invoke(this, type);
                }
            }
        }

        /// <summary>
        /// Occurs when the instancing changed. Used to notify instances to change their
        /// </summary>
        public event EventHandler<IInstancing> InstancingChanged;
    }
}
