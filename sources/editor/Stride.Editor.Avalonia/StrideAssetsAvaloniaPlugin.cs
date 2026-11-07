// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;

namespace Stride.Editor.Avalonia;

/// <summary>
/// A <see cref="StrideAssetsPlugin"/> whose enum images, template providers and control themes come from Avalonia resource
/// dictionaries: the Avalonia counterpart of <c>StrideAssetsWpfPlugin</c>.
/// </summary>
public abstract class StrideAssetsAvaloniaPlugin : StrideAssetsPlugin
{
    /// <summary>
    /// Registers in <paramref name="context"/> the enum images and the template providers found in an Avalonia resource dictionary.
    /// </summary>
    /// <param name="context">The context of the plugin, given to <see cref="StrideAssetsPlugin.Initialize(IEditorPluginContext)"/>.</param>
    /// <param name="dictionary">The resource dictionary: its enum-keyed entries are enum images, its <see cref="ITemplateProvider"/> values template providers.</param>
    protected static void RegisterResourceDictionary(IEditorPluginContext context, IResourceDictionary dictionary)
    {
        foreach (var entry in dictionary.Keys)
        {
            if (entry is Enum enumValue && dictionary.TryGetValue(entry, out var image) && image is not null)
            {
                context.EnumImages.Register(enumValue, image);
            }
        }

        foreach (var value in dictionary.Values)
        {
            if (value is ITemplateProvider provider)
            {
                context.TemplateProviders.Register(provider);
            }
        }
    }

    /// <summary>
    /// Registers the enum images and the template providers found in an Avalonia resource dictionary, from
    /// <see cref="StrideAssetsPlugin.Initialize(ILogger)"/>.
    /// </summary>
    /// <param name="dictionary">The resource dictionary: its enum-keyed entries are enum images, its <see cref="ITemplateProvider"/> values template providers.</param>
    /// <remarks>A plugin that uses the plugin context calls <see cref="RegisterResourceDictionary(IEditorPluginContext, IResourceDictionary)"/>.</remarks>
    protected virtual void RegisterResourceDictionary(IResourceDictionary dictionary)
    {
        foreach (var entry in dictionary.Keys)
        {
            if (entry is Enum enumValue && dictionary.TryGetValue(entry, out var image) && image is not null)
            {
                RegisterEnumImage(enumValue, image);
            }
        }

        foreach (var value in dictionary.Values)
        {
            if (value is ITemplateProvider provider)
            {
                RegisterTemplateProvider(provider);
            }
        }
    }

    /// <summary>
    /// Merges a dictionary of control themes (the equivalent of a WPF <c>Themes/Generic.xaml</c>) into the resources of the
    /// running application, so the controls of this plugin's assembly find their templates. Nothing happens without an
    /// application, as in the tests of the plugin's view models.
    /// </summary>
    /// <param name="logger">The logger of the plugin initialization (<see cref="IEditorPluginContext.Logger"/>).</param>
    /// <param name="themes">The dictionary of control themes.</param>
    protected static void RegisterControlThemes(ILogger logger, IResourceProvider themes)
    {
        var application = Application.Current;
        if (application is null)
        {
            logger.Debug("No Avalonia application is running: the control themes are not merged.");
            return;
        }

        var merged = application.Resources.MergedDictionaries;
        if (merged.Contains(themes))
            return;

        // A plugin can be initialized once per session: the same dictionary, loaded again, is the same source
        if (themes is ResourceInclude { Source: { } source } && merged.OfType<ResourceInclude>().Any(x => x.Source == source))
            return;

        merged.Add(themes);
    }
}
