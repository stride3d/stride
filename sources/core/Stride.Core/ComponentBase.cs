// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core
{
    /// <summary>
    /// Base class for a framework component.
    /// </summary>
    [DataContract]
    public abstract class ComponentBase : DisposeBase, IComponent, ICollectorHolder
    {
        private string name;
        private ObjectCollector collector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentBase"/> class.
        /// </summary>
        protected ComponentBase()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentBase"/> class.
        /// </summary>
        /// <param name="name">The name attached to this component</param>
        protected ComponentBase(string name)
        {
            collector = new ObjectCollector();
            Tags = new PropertyContainer(this);
            Name = name ?? GetType().Name;
        }

        /// <summary>
        /// Gets the attached properties to this component.
        /// </summary>
        [DataMemberIgnore] // Do not try to recreate object (preserve Tags.Owner)
        public PropertyContainer Tags;

        /// <summary>
        /// Gets or sets the name of this component.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [DataMemberIgnore] // By default don't store it, unless derived class are overriding this member
        public virtual string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value == name) return;

                name = value;

                OnNameChanged();
            }
        }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        protected override void Destroy()
        {
            collector.Dispose();
        }

        ObjectCollector ICollectorHolder.Collector
        {
            get
            {
                collector.EnsureValid();
                return collector;
            }
        }

        /// <summary>
        /// Called when <see cref="Name"/> property was changed.
        /// </summary>
        protected virtual void OnNameChanged()
        {
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {name}";
        }
    }
}
