// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.ViewModel.Logs
{
    /// <summary>
    /// The part of the asset log (<see cref="AssetLogViewModel"/>) that the session objects use.
    /// </summary>
    public interface IAssetLogViewModel
    {
        /// <summary>
        /// Gets the logger of the given log, creating it if needed and <paramref name="create"/> is true.
        /// </summary>
        Logger GetLogger(LogKey key, bool create = true);

        /// <summary>
        /// Clears the messages of the given log.
        /// </summary>
        void ClearMessages(LogKey key);
    }
}
