// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Wrapper for <see cref="ICommandBase"/> with additional information, best fitted for menu command (gesture, tooltip...).
    /// </summary>
    public sealed class MenuCommandInfo : DispatcherViewModel
    {
        private string gesture;
        private string displayName;
        private object icon;
        private string tooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuCommandInfo"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use with this view model.</param>
        /// <param name="command">The command to wrap.</param>
        public MenuCommandInfo([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] ICommandBase command)
            : base(serviceProvider)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        public ICommandBase Command { get; }

        /// <summary>
        /// Gets or sets the name that will be displayed in the UI.
        /// </summary>
        public string DisplayName
        {
            get => displayName;
            set => SetValue(ref displayName, value);
        }

        /// <summary>
        /// Gets or sets the gesture text associated with this command.
        /// </summary>
        public string Gesture
        {
            get => gesture;
            set => SetValue(ref gesture, value);
        }

        /// <summary>
        /// Gets or sets the icon that appears in a <see cref="System.Windows.Controls.MenuItem"/>.
        /// </summary>
        public object Icon
        {
            get => icon;
            set => SetValue(ref icon, value);
        }

        /// <summary>
        /// Gets or sets the tooltip text that is shown when the governing UI control is hovered.
        /// </summary>
        public string Tooltip
        {
            get => tooltip;
            set => SetValue(ref tooltip, value);
        }
    }
}
