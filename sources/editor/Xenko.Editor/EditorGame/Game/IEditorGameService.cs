// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;

namespace Xenko.Editor.EditorGame.Game
{
    /// <summary>
    /// Base interface for services that handle specific features of a <see cref="Game"/> instantiated for an asset editor.
    /// </summary>
    public interface IEditorGameService : IAsyncDisposable
    {
        /// <summary>
        /// Gets whether this service has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets whether this service is currently active.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets the type of services that are required for this service to work.
        /// </summary>
        [ItemNotNull, NotNull]
        IEnumerable<Type> Dependencies { get; }

        /// <summary>
        /// Initializes this service, allowing it to register scripts and modify the graphics compositor.
        /// </summary>
        /// <param name="game">The game for which this service is initialized.</param>
        /// <returns>This method is invoked after the game is fully initialized/</returns>
        Task<bool> InitializeService([NotNull] EditorServiceGame game);

        /// <summary>
        /// Registers the given scene to this service, as the scene containing the objects being edited.
        /// </summary>
        /// <param name="scene">The scene to register.</param>
        void RegisterScene([NotNull] Scene scene);

        /// <summary>
        /// Called when the game graphics compositor is updated.
        /// </summary>
        /// <param name="game"></param>
        void UpdateGraphicsCompositor([NotNull] EditorServiceGame game);
    }
}
