// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.View;

namespace Stride.Core.Assets.Editor.Services
{
    public abstract class AssetsPlugin
    {
        protected static readonly Dictionary<Type, object> TypeImages = new Dictionary<Type, object>();
        internal static readonly List<AssetsPlugin> RegisteredPlugins = new List<AssetsPlugin>();

        // TODO: give access to this differently
        public readonly List<PackageSettingsEntry> ProfileSettings = new List<PackageSettingsEntry>();

        public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

        public static void RegisterPlugin([NotNull] Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException("The given type does not have a parameterless constructor.");

            if (!typeof(AssetsPlugin).IsAssignableFrom(type))
                throw new ArgumentException("The given type does not inherit from AssetsPlugin.");

            if (RegisteredPlugins.Any(x => x.GetType() == type))
                throw new InvalidOperationException("The plugin type is already registered.");

            var plugin = (AssetsPlugin)Activator.CreateInstance(type);
            RegisteredPlugins.Add(plugin);
        }

        public void RegisterAssetViewModelTypes([NotNull] IDictionary<Type, Type> assetViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(AssetViewModel).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetViewModelAttribute>();
                    if (attribute != null)
                    {
                        Type closureType = type;
                        attribute.AssetTypes.ForEach(x => assetViewModelTypes.Add(x, closureType));
                    }
                }
            }
        }

        public void RegisterAssetPropertyGraphViewModelTypes([NotNull] IDictionary<Type, Type> assetViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(AssetViewModel).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetViewModelAttribute>();
                    if (attribute != null)
                    {
                        Type closureType = type;
                        attribute.AssetTypes.ForEach(x => assetViewModelTypes.Add(x, closureType));
                    }
                }
            }
        }

        public void RegisterAssetEditorViewModelTypes([NotNull] IDictionary<Type, Type> assetEditorViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetEditorViewModel).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetEditorViewModelAttribute>();
                    if (attribute != null)
                    {
                        assetEditorViewModelTypes.Add(attribute.AssetType, type);
                    }
                }
            }
        }

        public abstract void InitializePlugin(ILogger logger);

        public abstract void InitializeSession([NotNull] SessionViewModel session);

        public abstract void RegisterPrimitiveTypes([NotNull] ICollection<Type> primitiveTypes);

        public abstract void RegisterEnumImages([NotNull] IDictionary<object, object> enumImages);

        public abstract void RegisterCopyProcessors([NotNull] ICollection<ICopyProcessor> copyProcessors, SessionViewModel session);

        public abstract void RegisterPasteProcessors([NotNull] ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session);

        public abstract void RegisterPostPasteProcessors([NotNull] ICollection<IAssetPostPasteProcessor> postePasteProcessors, SessionViewModel session);

        public abstract void RegisterTemplateProviders([NotNull] ICollection<ITemplateProvider> templateProviders);

        protected internal virtual void SessionLoaded(SessionViewModel session)
        {
            // Intentionally does nothing
        }

        protected internal virtual void SessionDisposed(SessionViewModel session)
        {
            // Intentionally does nothing
        }
    }
}
