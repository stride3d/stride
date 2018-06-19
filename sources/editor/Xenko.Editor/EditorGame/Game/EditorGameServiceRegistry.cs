// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;

namespace Xenko.Editor.EditorGame.Game
{
    public sealed class EditorGameServiceRegistry : IAsyncDisposable
    {
        public List<IEditorGameService> Services { get; } = new List<IEditorGameService>();

        [CanBeNull]
        public T Get<T>()
        {
            return Services.OfType<T>().FirstOrDefault();
        }

        [CanBeNull]
        public IEditorGameService Get([NotNull] Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (!serviceType.HasInterface(typeof(IEditorGameService)))
                throw new ArgumentException($@"The given type must be a type that implement {nameof(IEditorGameService)}", nameof(serviceType));

            return Services.FirstOrDefault(serviceType.IsInstanceOfType);
        }

        public void Add<T>([NotNull] T service)
            where T : IEditorGameService
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Services.Add(service);
        }

        /// <inheritdoc/>
        public async Task DisposeAsync()
        {
            for (var index = Services.Count - 1; index >= 0; index--)
            {
                var service = Services[index];
                await service.DisposeAsync();
            }
            Services.Clear();
        }
    }
}
