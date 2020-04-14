// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Rendering;

namespace Stride.Engine
{
    /// <summary>
    /// A bounding volume for light shafts to be rendered in, can take any <see cref="Model"/> as a volume
    /// </summary>
    [Display("Light shaft bounding volume", Expand = ExpandRule.Always)]
    [DataContract("LightShaftBoundingVolumeComponent")]
    [DefaultEntityComponentProcessor(typeof(LightShaftBoundingVolumeProcessor))]
    [ComponentCategory("Lights")]
    public class LightShaftBoundingVolumeComponent : ActivableEntityComponent
    {
        private Model model;
        private LightShaftComponent lightShaft;
        private bool enabled = true;

        public override bool Enabled
        {
            get { return enabled; }
            set { enabled = value; EnabledChanged?.Invoke(this, null); }
        }

        /// <summary>
        /// The model used to define the bounding volume
        /// </summary>
        public Model Model
        {
            get { return model; }
            set { model = value; ModelChanged?.Invoke(this, null); }
        }

        /// <summary>
        /// The light shaft to which the bounding volume applies
        /// </summary>
        public LightShaftComponent LightShaft
        {
            get { return lightShaft; }
            set { lightShaft = value; LightShaftChanged?.Invoke(this, null); }
        }

        public event EventHandler LightShaftChanged;
        public event EventHandler ModelChanged;
        public event EventHandler EnabledChanged;
    }
}
