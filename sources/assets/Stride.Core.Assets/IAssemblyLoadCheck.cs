// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// A check that an assembly of a package passes before the session loads it (<see cref="PackageLoadParameters.AssemblyLoadCheck"/>).
/// The editor uses it to leave out a plugin built for a newer editor; the session itself checks nothing.
/// </summary>
public interface IAssemblyLoadCheck
{
    /// <summary>
    /// Whether the session may load an assembly of a package. Called before the assembly loads, once per assembly and
    /// session; an assembly the process has loaded already is not checked.
    /// </summary>
    /// <param name="package">The package whose assembly it is.</param>
    /// <param name="assemblyPath">The path of the assembly.</param>
    /// <param name="log">The log of the session's load, where the check says why it refuses an assembly.</param>
    /// <returns>True to load the assembly; false to leave it out.</returns>
    bool CanLoad(Package package, string assemblyPath, ILogger log);
}
