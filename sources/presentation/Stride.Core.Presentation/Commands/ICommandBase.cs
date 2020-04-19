// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Input;

namespace Stride.Core.Presentation.Commands
{
    /// <summary>
    /// An interface representing an implementation of <see cref="ICommand"/> with additional properties.
    /// </summary>
    public interface ICommandBase : ICommand
    {
        /// <summary>
        /// Indicates whether the command can be executed or not.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Executes the command with a <c>null</c> parameter.
        /// </summary>
        void Execute();
    }
}
