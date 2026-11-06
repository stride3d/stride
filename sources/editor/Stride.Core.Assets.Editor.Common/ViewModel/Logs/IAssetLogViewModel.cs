// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.ViewModel.Logs
{
    /// <summary>
    /// The part of the asset log (<c>AssetLogViewModel</c>) that the session objects use.
    /// </summary>
    public interface IAssetLogViewModel
    {
        /// <summary>
        /// The name of the logs of package consistency, followed by the package name: one log per package (see <see cref="LogKey.Name"/>).
        /// </summary>
        public const string ConsistencyLogName = "Consistency";

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
