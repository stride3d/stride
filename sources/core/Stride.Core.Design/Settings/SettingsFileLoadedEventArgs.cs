// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.IO;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Arguments of the <see cref="SettingsContainer.SettingsFileLoaded"/> event.
    /// </summary>
    public class SettingsFileLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsFileLoadedEventArgs"/> class.
        /// </summary>
        /// <param name="path"></param>
        public SettingsFileLoadedEventArgs(UFile path)
        {
            FilePath = path;
        }

        /// <summary>
        /// Gets the path of the file that has been loaded.
        /// </summary>
        public UFile FilePath { get; private set; }
    }
}
