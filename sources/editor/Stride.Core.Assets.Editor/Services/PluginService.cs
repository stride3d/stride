// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.View;

namespace Stride.Core.Assets.Editor.Services
{
    public class PluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => AssetsPlugin.RegisteredPlugins;

        private readonly Dictionary<object, object> enumImages = new Dictionary<object, object>();

        private readonly HashSet<Type> enumTypesWithImages = new HashSet<Type>();

        private readonly List<Type> primitiveTypes = new List<Type>();

        private readonly Dictionary<Type, Type> assetEditorViewModelTypes = new Dictionary<Type, Type>();

        internal void RegisterSession(SessionViewModel session, ILogger logger)
        {
            foreach (var plugin in Plugins)
            {
                plugin.InitializePlugin(logger);

                // Asset view models types
                var assetViewModelsTypes = new Dictionary<Type, Type>();
                plugin.RegisterAssetViewModelTypes(assetViewModelsTypes);
                AssertType(typeof(Asset), assetViewModelsTypes.Select(x => x.Key));
                AssertType(typeof(AssetViewModel), assetViewModelsTypes.Select(x => x.Value));
                session.AssetViewModelTypes.AddRange(assetViewModelsTypes);

                // Primitive types
                var registeredPrimitiveTypes = new List<Type>();
                plugin.RegisterPrimitiveTypes(registeredPrimitiveTypes);
                primitiveTypes.AddRange(registeredPrimitiveTypes);

                // Enum images
                var images = new Dictionary<object, object>();
                plugin.RegisterEnumImages(images);
                AssertType(typeof(Enum), images.Select(x => x.Key.GetType()));
                enumImages.AddRange(images);
                enumTypesWithImages.AddRange(images.Select(x => x.Key.GetType()));

                // Asset editor view models types
                var registeredAssetEditorViewModelTypes = new Dictionary<Type, Type>();
                plugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelTypes);
                AssertType(typeof(Asset), registeredAssetEditorViewModelTypes.Select(x => x.Key));
                AssertType(typeof(IAssetEditorViewModel), registeredAssetEditorViewModelTypes.Select(x => x.Value));
                assetEditorViewModelTypes.AddRange(registeredAssetEditorViewModelTypes);

                // Editor and property item template providers
                var providers = new List<ITemplateProvider>();
                plugin.RegisterTemplateProviders(providers);
                var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
                foreach (var provider in providers)
                {
                    dialogService.RegisterAdditionalTemplateProvider(provider);
                }

                var copyPasteService = session.ServiceProvider.TryGet<ICopyPasteService>();
                if (copyPasteService != null)
                {
                    // Copy processors
                    var copyProcessors = new List<ICopyProcessor>();
                    plugin.RegisterCopyProcessors(copyProcessors, session);
                    foreach (var processor in copyProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                    // Paste processors
                    var pasteProcessors = new List<IPasteProcessor>();
                    plugin.RegisterPasteProcessors(pasteProcessors, session);
                    foreach (var processor in pasteProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                    // Post paste processors
                    var postPasteProcessors = new List<IAssetPostPasteProcessor>();
                    plugin.RegisterPostPasteProcessors(postPasteProcessors, session);
                    foreach (var processor in postPasteProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                }
            }
        }

        public bool HasImagesForEnum(SessionViewModel session, Type enumType)
        {
            return session != null && enumTypesWithImages.Contains(enumType);
        }

        public object GetImageForEnum(SessionViewModel session, object value)
        {
            if (session == null)
                return null;

            object image;
            enumImages.TryGetValue(value, out image);
            return image;
        }

        public IEnumerable<Type> GetPrimitiveTypes(SessionViewModel session)
        {
            return primitiveTypes;
        }

        public IEditorView ConstructEditionView(AssetViewModel asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (asset.Session == null) throw new ArgumentException(@"The asset is currently deleted.", nameof(asset));

            var currentMatch = new KeyValuePair<Type, Type>();
            foreach (var keyValuePair in assetEditorViewModelTypes)
            {
                // Exact type match, we stop here.
                if (keyValuePair.Key == asset.AssetType)
                {
                    currentMatch = keyValuePair;
                    break;
                }
                // Parent type...
                if (keyValuePair.Key.IsAssignableFrom(asset.AssetType))
                {
                    // ... we keep it only if we have no match yet, or if it is closer to the asset type in the inheritance hierarchy
                    if (currentMatch.Key == null || currentMatch.Key.IsAssignableFrom(keyValuePair.Key))
                    {
                        currentMatch = keyValuePair;
                    }
                }
            }

            if (currentMatch.Key == null)
                return null;

            var attribute = currentMatch.Value.GetCustomAttribute<AssetEditorViewModelAttribute>();
            if (attribute == null)
                return null;

            return (IEditorView)Activator.CreateInstance(attribute.EditorViewType);
        }

        public bool HasEditorView(SessionViewModel session, Type assetType)
        {
            return assetEditorViewModelTypes.Any(x => x.Key.IsAssignableFrom(assetType));
        }

        private static void AssertType(Type baseType, Type specificType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));
            if (specificType == null)
                throw new ArgumentNullException(nameof(specificType));

            if (!baseType.IsAssignableFrom(specificType))
                throw new ArgumentException("Type [{0}] must be assignable to {1}".ToFormat(specificType.FullName, baseType.FullName), nameof(specificType));
        }

        private static void AssertType(Type baseType, IEnumerable<Type> specificTypes)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));
            if (specificTypes == null)
                throw new ArgumentNullException(nameof(specificTypes));

            specificTypes.ForEach(x => AssertType(baseType, x));
        }
    }
}
