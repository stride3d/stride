// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.EntityFactories
{
    public abstract class EntityFactory : IEntityFactory
    {
        /// <summary>
        /// Generates a name for a new entity that is guaranteed to be unique among the existing sibling entities of the specified <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="baseName"></param>
        /// <returns></returns>
        [NotNull]
        public static string ComputeNewName([NotNull] IEntityHierarchyItemViewModel parent, [NotNull] string baseName)
        {
            return EntityNames.ComputeNewName(parent, baseName);
        }

        public abstract Task<Entity> CreateEntity(IEntityHierarchyItemViewModel parent);


        protected static Task<Entity> CreateEntityWithComponent(string name, EntityComponent component, params EntityComponent[] additionalComponents)
        {
            var newEntity = new Entity { Name = name };
            newEntity.Components.Add(component);
            if (additionalComponents != null)
            {
                foreach (var additionalComponent in additionalComponents)
                {
                    newEntity.Components.Add(additionalComponent);
                }
            }
            return Task.FromResult(newEntity);
        }
    }
}
