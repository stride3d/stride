// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Presentation.Commands
{
    /// <summary>
    /// This class represents a command that is always disabled and does nothing when executed.
    /// </summary>
    /// <remarks>This class is a singleton, you can access to the instance via the static property <see cref="Instance"/>.</remarks>
    public sealed class DisabledCommand : ICommandBase
    {
        static DisabledCommand()
        {
            Instance = new DisabledCommand();
        }

        /// <summary>
        /// Gets a singleton instance of the <see cref="DisabledCommand"/>
        /// </summary>
        public static DisabledCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisabledCommand"/> class.
        /// </summary>
        private DisabledCommand()
        {
        }

        /// <inheritdoc/>
        public bool IsEnabled
        {
            get { return false; }
            set { if (value == false) throw new InvalidOperationException($"The {nameof(IsEnabled)} property of the {nameof(DisabledCommand)} cannot be modified"); }
        }

        /// <inheritdoc/>
        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }
        
        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            return false;
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            throw new InvalidOperationException($"The {nameof(DisabledCommand)} cannot be executed.");
        }

        /// <inheritdoc/>
        public void Execute()
        {
            throw new InvalidOperationException($"The {nameof(DisabledCommand)} cannot be executed.");
        }
    }
}
