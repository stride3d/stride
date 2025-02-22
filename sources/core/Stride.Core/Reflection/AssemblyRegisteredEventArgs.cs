// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Stride.Core.Reflection;

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
    public AssemblyRegisteredEventArgs(Assembly assembly, HashSet<string> categories)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(categories);
        Assembly = assembly;
        Categories = categories;
    }

    /// <summary>
    /// Gets the assembly that has been registered.
    /// </summary>
    /// <value>The assembly.</value>
    public Assembly Assembly { get; }

    /// <summary>
    /// Gets the new categories registered for the specified <see cref="Assembly"/>
    /// </summary>
    /// <value>The categories.</value>
    public HashSet<string> Categories { get; }
}
