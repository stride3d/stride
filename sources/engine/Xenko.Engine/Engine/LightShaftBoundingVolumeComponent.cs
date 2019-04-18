// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Rendering;

namespace Xenko.Engine
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
