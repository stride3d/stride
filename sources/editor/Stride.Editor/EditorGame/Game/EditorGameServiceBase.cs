// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.Editor.EditorGame.Game
{
    /// <summary>
    /// Base class for the <see cref="IEditorGameService"/> interface.
    /// </summary>
    public abstract class EditorGameServiceBase : IEditorGameService
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public virtual bool IsActive { get { return true; } set { throw new InvalidOperationException("This service cannot be deactivated."); } }

        /// <inheritdoc/>
        public virtual IEnumerable<Type> Dependencies => Enumerable.Empty<Type>();

        public EditorGameServiceRegistry Services { get; } = new EditorGameServiceRegistry();

        /// <summary>
        /// Gets whether this service has been disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public virtual Task DisposeAsync()
        {
            IsDisposed = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeService(EditorServiceGame game)
        {
            EnsureNotDestroyed(nameof(EditorGameServiceBase));

            foreach (var dependency in Dependencies)
            {
                Services.Add(game.EditorServices.Get(dependency));
            }
            IsInitialized = await Initialize(game);
            return IsInitialized;
        }

        /// <inheritdoc/>
        public virtual void RegisterScene(Scene scene)
        {
            EnsureNotDestroyed(nameof(EditorGameServiceBase));
            // Do nothing by default.
        }

        /// <summary>
        /// Checks whether this service has been disposed, and throws an <see cref="ObjectDisposedException"/> if it is the case. 
        /// </summary>
        /// <param name="name">The name to supply to the <see cref="ObjectDisposedException"/>.</param>
        protected void EnsureNotDestroyed(string name = null)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(name ?? nameof(EditorGameServiceBase), "This service has already been disposed.");
            }
        }

        /// <summary>
        /// Initializes the service. This method is invoked by <see cref="InitializeService"/>.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        protected abstract Task<bool> Initialize([NotNull] EditorServiceGame game);

        /// <summary>
        /// Called when the game graphics compositor is updated.
        /// </summary>
        /// <param name="game"></param>
        public virtual void UpdateGraphicsCompositor(EditorServiceGame game)
        {
        }
    }
}
