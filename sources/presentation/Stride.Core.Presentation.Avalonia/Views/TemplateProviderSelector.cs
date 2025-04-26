// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.Avalonia.Views;

/// <summary>
/// An implementation of <see cref="IDataTemplate"/> that can select a template from a set of statically registered <see cref="ITemplateProvider"/> objects.
/// </summary>
public class TemplateProviderSelector : IDataTemplate
{
    /// <summary>
    /// The list of all template providers registered for the <see cref="TemplateProviderSelector"/>, indexed by their name.
    /// </summary>
    protected readonly List<TemplateProviderBase> templateProviders = [];

    /// <summary>
    /// A hashset of template provider names, used only to ensure unicity.
    /// </summary>
    private readonly HashSet<string> templateProviderNames = [];

    /// <summary>
    /// Registers the given template into the static <see cref="TemplateProviderSelector"/>.
    /// </summary>
    /// <param name="templateProvider"></param>
    public void RegisterTemplateProvider(TemplateProviderBase templateProvider)
    {
        ArgumentNullException.ThrowIfNull(templateProvider);

        if (!templateProviderNames.Add(templateProvider.Name))
            throw new InvalidOperationException("A template provider with the same name has already been registered in this template selector.");

        InsertTemplateProvider(templateProviders, templateProvider, []);
    }

    /// <summary>
    /// Unregisters the given template into the static <see cref="TemplateProviderSelector"/>.
    /// </summary>
    /// <param name="templateProvider"></param>
    public void UnregisterTemplateProvider(TemplateProviderBase templateProvider)
    {
        ArgumentNullException.ThrowIfNull(templateProvider);

        if (templateProviderNames.Remove(templateProvider.Name))
            templateProviders.Remove(templateProvider);
    }

    /// <inheritdoc/>
    public virtual Control? Build(object? item)
    {
        return templateProviders.FirstOrDefault(x => x.Match(item))?.Template.Build(item);
    }

    /// <inheritdoc/>
    public bool Match(object? data)
    {
        return templateProviders.Any(x => x.Match(data));
    }

    private static void InsertTemplateProvider(List<TemplateProviderBase> list, TemplateProviderBase templateProvider, List<TemplateProviderBase> movedItems)
    {
        movedItems.Add(templateProvider);
        // Find the first index where we can insert
        var index = 1 + list.LastIndexOf(x => x.CompareTo(templateProvider) <= 0);
        list.Insert(index, templateProvider);
        // Every following providers may have an override rule against the new template provider, we must potentially resort them.
        for (var i = index + 1; i < list.Count; ++i)
        {
            var followingProvider = list[i];
            if (followingProvider.CompareTo(templateProvider) < 0)
            {
                if (!movedItems.Contains(followingProvider))
                {
                    list.RemoveAt(i);
                    InsertTemplateProvider(list, followingProvider, movedItems);
                }
            }
        }
    }
}
