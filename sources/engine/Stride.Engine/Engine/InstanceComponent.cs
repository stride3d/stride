// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine.Design;
using Stride.Engine.Processors;

namespace Stride.Engine
{
    /// <summary>
    /// Represents a single instance of a model instancing component.
    /// </summary>
    /// <seealso cref="Stride.Engine.ActivableEntityComponent" />
    [DataContract("InstanceComponent")]
    [Display("Instance", Expand = ExpandRule.Once)]
    [ComponentCategory("Model")]
    [DefaultEntityComponentProcessor(typeof(InstanceProcessor))]
    public sealed class InstanceComponent : ActivableEntityComponent
    {
        private InstancingComponent master;
        private InstancingEntityTransform connectedInstancing;

        /// <summary>
        /// Gets or sets the referenced <see cref="InstancingComponent"/> to instance.
        /// </summary>
        /// <value>The referenced <see cref="InstancingComponent"/> to instance.</value>
        /// <userdoc>The "Master" <see cref="InstancingComponent"/> to instance. If not set, it tries to find one in the parent entities</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Instancing", Expand = ExpandRule.Always)]
        public InstancingComponent Master
        {
            get => master;
            set
            {
                if (value != master)
                {
                    DisconnectInstancing();

                    // Remove previous event handler
                    if (master != null)
                        master.InstancingChanged -= Master_InstancingChanged;

                    master = value;

                    if (master != null)
                    {
                        master.InstancingChanged += Master_InstancingChanged;
                        ConnectInstancing();
                    }
                }
            }
        }

        private void Master_InstancingChanged(object sender, IInstancing e)
        {
            DisconnectInstancing();
            ConnectInstancing();
        }

        private void ConnectInstancing()
        {
            if (master != null && master.Type is InstancingEntityTransform instancing)
            {
                instancing.AddInstance(this);
                connectedInstancing = instancing;
            }
        }

        public void DisconnectInstancing()
        {
            if (connectedInstancing != null)
            {
                connectedInstancing.RemoveInstance(this);
                connectedInstancing = null;
            }
        }
    }
}
