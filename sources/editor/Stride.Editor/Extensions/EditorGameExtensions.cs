// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Editor.EditorGame.Game;

namespace Stride.Editor.Extensions
{
    public static class EditorGameExtensions
    {
        /// <summary>
        /// Sorts the services of this <see cref="EditorGameServiceRegistry"/> by dependency order.
        /// </summary>
        /// <param name="serviceRegistry">The service registry.</param>
        /// <returns></returns>
        public static IEnumerable<IEditorGameService> OrderByDependency(this EditorGameServiceRegistry serviceRegistry)
        {
            var visited = new HashSet<IEditorGameService>();
            return serviceRegistry.Services.SelectMany(s => OrderByDependency(s, visited, serviceRegistry));
        }

        /// <summary>
        /// Recursively sorts the dependencies of the provided <paramref name="service"/> in deepest order first.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="visited"></param>
        /// <param name="serviceRegistry"></param>
        /// <returns></returns>
        private static IEnumerable<IEditorGameService> OrderByDependency(IEditorGameService service, ISet<IEditorGameService> visited, EditorGameServiceRegistry serviceRegistry)
        {
            if (!visited.Add(service))
                yield break;

            foreach (var dependencyType in service.Dependencies)
            {
                var dependency = serviceRegistry.Get(dependencyType);
                if (dependency == null)
                    throw new InvalidOperationException($"The service [{service.GetType().Name}] requires a service of type [{dependencyType.Name}].");

                foreach (var item in OrderByDependency(dependency, visited, serviceRegistry))
                    yield return item;
            }
            yield return service;
        }
    }
}
