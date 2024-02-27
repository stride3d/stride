// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.View;

namespace Stride.Core.Assets.Editor
{
    public class AssetsEditorPlugin : AssetsPlugin
    {
        private ResourceDictionary imageDictionary;

        /// <inheritdoc />
        public override void InitializePlugin(ILogger logger)
        {
            imageDictionary ??= (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Core.Assets.Editor;component/View/ImageDictionary.xaml", UriKind.RelativeOrAbsolute));
        }

        /// <inheritdoc />
        public override void InitializeSession(SessionViewModel session)
        {
        }

        public void RegisterAssetEditorViewModelTypes([NotNull] IDictionary<Type, Type> assetEditorViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetEditorViewModel).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
                {
                    assetEditorViewModelTypes.Add(attribute.ViewModelType, type);
                }
            }
        }

        public void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IEditorView).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
                {
                    assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
                }
            }
        }

        /// <inheritdoc />
        public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
        {
        }

        /// <inheritdoc />
        public override void RegisterEnumImages(IDictionary<object, object> enumImages)
        {
            foreach (object entry in imageDictionary.Keys)
            {
                if (entry is Enum)
                {
                    enumImages.Add(entry, imageDictionary[entry]);
                }
            }
        }

        /// <inheritdoc />
        public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
        {
            pasteProcessors.Add(new AssetPropertyPasteProcessor());
            pasteProcessors.Add(new AssetItemPasteProcessor(session));
        }

        /// <inheritdoc />
        public override void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
        {
        }
    }
}
