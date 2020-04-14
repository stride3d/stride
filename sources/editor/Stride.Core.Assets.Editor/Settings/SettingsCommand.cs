// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings
{
    /// <summary>
    /// This class represents a command that can be executed from the settings menu.
    /// </summary>
    public class SettingsCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsCommand"/> class.
        /// </summary>
        /// <param name="name">The name of the settings command.</param>
        public SettingsCommand(UFile name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of this <see cref="SettingsKey"/>.
        /// </summary>
        [DataMemberIgnore]
        public UFile Name { get; private set; }

        /// <summary>
        /// Gets or sets the action name, usually used to display as caption of a button.
        /// </summary>
        [DataMemberIgnore]
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the <see cref="SettingsKey"/>.
        /// </summary>
        /// <remarks>The default value is the name parameter given to the constructor of this class.</remarks>
        [DataMemberIgnore]
        public UFile DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of this <see cref="SettingsKey"/>.
        /// </summary>
        [DataMemberIgnore]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the actual command to execute.
        /// </summary>
        [DataMemberIgnore]
        public ICommandBase Command { get; set; }
    }
}
