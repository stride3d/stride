// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// An event occurring when an assembly is registered with <see cref="AssemblyRegistry"/>.
    /// </summary>
    public class AssemblyRegisteredEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRegisteredEventArgs"/> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="categories">The categories.</param>
        public AssemblyRegisteredEventArgs([NotNull] Assembly assembly, [NotNull] HashSet<string> categories)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (categories == null) throw new ArgumentNullException(nameof(categories));
            Assembly = assembly;
            Categories = categories;
        }

        /// <summary>
        /// Gets the assembly that has been registered.
        /// </summary>
        /// <value>The assembly.</value>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// Gets the new categories registered for the specified <see cref="Assembly"/>
        /// </summary>
        /// <value>The categories.</value>
        public HashSet<string> Categories { get; private set; }
    }
}
