// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A key to identify a specific profile.
    /// </summary>
    public class ProfilingKey
    {
        internal static readonly HashSet<ProfilingKey> AllKeys = new HashSet<ProfilingKey>();
        internal bool Enabled;
        internal ProfilingKeyFlags Flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingKey" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ProfilingKey([NotNull] string name, ProfilingKeyFlags flags = ProfilingKeyFlags.None)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Children = new List<ProfilingKey>();
            Name = name;
            Flags = flags;

            lock (AllKeys)
            {
                AllKeys.Add(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingKey" /> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">parent</exception>
        public ProfilingKey([NotNull] ProfilingKey parent, [NotNull] string name, ProfilingKeyFlags flags = ProfilingKeyFlags.None)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (name == null) throw new ArgumentNullException(nameof(name));
            Children = new List<ProfilingKey>();
            Parent = parent;
            Name = $"{Parent}.{name}";
            Flags = flags;

            lock (AllKeys)
            {
                // Register ourself in parent's children.
                parent.Children?.Add(this);

                AllKeys.Add(this);
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <value>The group.</value>
        public ProfilingKey Parent { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public List<ProfilingKey> Children { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
