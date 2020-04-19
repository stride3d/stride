// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Data;

namespace Stride.Editor.Build
{
    /// <summary>
    /// Used to access the game settings in a read-only way
    /// </summary>
    public interface IGameSettingsAccessor
    {
        /// <summary>
        /// Gets a copy of the requested <see cref="Configuration"/>. Can be null.
        /// </summary>
        /// <typeparam name="T">The requestted <see cref="Configuration"/></typeparam>
        /// <param name="profile">If not null, it will filter the results giving priority to the specified profile</param>
        /// <returns>The requested <see cref="Configuration"/> or null if not found</returns>
        T GetConfiguration<T>() where T : Configuration;
    }
}
