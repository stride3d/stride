// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Games
{
    /// <summary>
    /// Interface for a game platform (OS, machine dependent).
    /// </summary>
    public interface IGamePlatform
    {
        /// <summary>
        /// Gets the default app directory.
        /// </summary>
        /// <value>The default app directory.</value>
        string DefaultAppDirectory { get; }
    }
}
