// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Avalonia.Views;

namespace Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// The template providers of <see cref="DefaultPropertyTemplateProviders"/>, registered in the selectors of <see cref="PropertyViewHelper"/>.
/// </summary>
internal static class DefaultProviders
{
    private static bool registered;

    /// <summary>
    /// Registers the providers once per process, as the editor does for the plugins (PluginService).
    /// </summary>
    public static void Register()
    {
        if (registered)
            return;

        registered = true;
        foreach (var provider in new DefaultPropertyTemplateProviders().Values.OfType<TemplateProviderBase>())
        {
            var selector = PropertyViewHelper.GetTemplateCategory(provider) switch
            {
                PropertyViewHelper.Category.PropertyHeader => PropertyViewHelper.HeaderProviders,
                PropertyViewHelper.Category.PropertyFooter => PropertyViewHelper.FooterProviders,
                _ => PropertyViewHelper.EditorProviders,
            };
            selector.RegisterTemplateProvider(provider);
        }
    }
}
