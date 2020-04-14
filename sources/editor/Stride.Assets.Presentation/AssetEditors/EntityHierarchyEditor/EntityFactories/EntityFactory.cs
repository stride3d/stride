// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
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
        public static string ComputeNewName([NotNull] EntityHierarchyItemViewModel parent, [NotNull] string baseName)
        {
            // We do not generate unique names for entities anymore, it is not useful at all.
            //return NamingHelper.ComputeNewName(baseName, parent.Children.OfType<EntityViewModel>(), x => x.Name);
            return baseName;
        }

        public abstract Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent);

        protected static async Task<AssetViewModel> PickupAsset([NotNull] SessionViewModel session, [NotNull] IEnumerable<Type> assetTypes)
        {
            var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
            var assetPicker = dialogService.CreateAssetPickerDialog(session);
            assetPicker.Message = "Select the asset to use for this entity";
            assetPicker.AcceptedTypes.AddRange(assetTypes);
            var result = await assetPicker.ShowModal();
            return result == Stride.Core.Presentation.Services.DialogResult.Ok ? assetPicker.SelectedAssets.FirstOrDefault() : null;
        }

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
