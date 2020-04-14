// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Settings
{
    /// <summary>
    /// Arguments of the <see cref="SettingsKey.ChangesValidated"/> event.
    /// </summary>
    public class ChangesValidatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangesValidatedEventArgs"/> class.
        /// </summary>
        /// <param name="profile">The profile in which changes have been validated.</param>
        public ChangesValidatedEventArgs(SettingsProfile profile)
        {
            Profile = profile;
        }

        /// <summary>
        /// Gets the <see cref="SettingsProfile"/> in which changes have been validated.
        /// </summary>
        public SettingsProfile Profile { get; private set; }
    }
}
