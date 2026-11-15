// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Tebjan Halm
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
        private bool isInProcessor;
        private bool isMasterEventSubscribed;

        /// <summary>
        /// Gets or sets the referenced <see cref="InstancingComponent"/> to instance.
        /// </summary>
        /// <value>The referenced <see cref="InstancingComponent"/> to instance.</value>
        /// <userdoc>The "Master" InstancingComponent to instance. If not set, it tries to find one in the parent entities</userdoc>
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
                    var shouldReconnect = isInProcessor;
                    DisconnectFromMaster();
                    master = value;

                    if (shouldReconnect)
                    {
                        ConnectToMaster();
                    }
                }
            }
        }

        private void Master_InstancingChanged(object sender, IInstancing e)
        {
            DisconnectInstancing();
            ConnectInstancing();
        }

        internal void ConnectToMaster()
        {
            isInProcessor = true;

            if (master != null && !isMasterEventSubscribed)
            {
                master.InstancingChanged += Master_InstancingChanged;
                isMasterEventSubscribed = true;
            }

            ConnectInstancing();
        }

        internal void DisconnectFromMaster()
        {
            isInProcessor = false;
            DisconnectInstancing();

            if (master != null && isMasterEventSubscribed)
            {
                master.InstancingChanged -= Master_InstancingChanged;
                isMasterEventSubscribed = false;
            }
        }

        private void ConnectInstancing()
        {
            if (connectedInstancing == null && master != null && master.Type is InstancingEntityTransform instancing)
            {
                instancing.AddInstance(this);
                connectedInstancing = instancing;
            }
        }

        private void DisconnectInstancing()
        {
            if (connectedInstancing != null)
            {
                connectedInstancing.RemoveInstance(this);
                connectedInstancing = null;
            }
        }
    }
}
