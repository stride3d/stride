// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Where a session can load, by the .NET major its game needs: the editor loads the game's assemblies into its own process.
/// </summary>
public enum SessionHost
{
    /// <summary>This process runs on a major the game loads on.</summary>
    ThisProcess,

    /// <summary>The game needs a newer major, which is installed: the session opens in a new editor process on it.</summary>
    NewProcess,

    /// <summary>The game needs a major that is not installed: the session cannot open.</summary>
    Missing,
}
