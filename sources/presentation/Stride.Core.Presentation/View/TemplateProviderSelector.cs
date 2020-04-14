// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.View
{
    /// <summary>
    /// An implementation of <see cref="DataTemplateSelector"/> that can select a template from a set of statically registered <see cref="ITemplateProvider"/> objects.
    /// </summary>
    public class TemplateProviderSelector : DataTemplateSelector
    {
       
        /// <summary>
        /// The list of all template providers registered for the <see cref="TemplateProviderSelector"/>, indexed by their name.
        /// </summary>
        private readonly List<ITemplateProvider> templateProviders = new List<ITemplateProvider>();

        /// <summary>
        /// A hashset of template provider names, used only to ensure unicity.
        /// </summary>
        private readonly HashSet<string> templateProviderNames = new HashSet<string>();

        /// <summary>
        /// A map of all providers that have already been used for each object, indexed by <see cref="Guid"/>.
        /// </summary>
        private readonly ConditionalWeakTable<object, List<string>> usedProviders = new ConditionalWeakTable<object, List<string>>();

        /// <summary>
        /// A map containing the last container for a given object.
        /// </summary>
        private readonly ConditionalWeakTable<object, WeakReference> lastContainers = new ConditionalWeakTable<object, WeakReference>();

        /// <summary>
        /// Registers the given template into the static <see cref="TemplateProviderSelector"/>.
        /// </summary>
        /// <param name="templateProvider"></param>
        public void RegisterTemplateProvider([NotNull] ITemplateProvider templateProvider)
        {
            if (templateProvider == null) throw new ArgumentNullException(nameof(templateProvider));

            if (templateProviderNames.Contains(templateProvider.Name))
                throw new InvalidOperationException("A template provider with the same name has already been registered in this template selector.");

            InsertTemplateProvider(templateProviders, templateProvider, new List<ITemplateProvider>());
            templateProviderNames.Add(templateProvider.Name);
        }

        /// <summary>
        /// Unregisters the given template into the static <see cref="TemplateProviderSelector"/>.
        /// </summary>
        /// <param name="templateProvider"></param>
        public void UnregisterTemplateProvider([NotNull] ITemplateProvider templateProvider)
        {
            if (templateProviderNames.Remove(templateProvider.Name))
            {
                templateProviders.Remove(templateProvider);
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            var element = container as FrameworkElement;
            if (element == null)
                throw new ArgumentException(@"Container must be of type FrameworkElement", nameof(container));

            var provider = FindTemplateProvider(item, container);
            if (provider == null)
                return null;

            var template = provider.Template;
            // We set the template we found into the content presenter itself to avoid re-entering the template selector if the property is refreshed.
            //var contentPresenter = container as ContentPresenter;
            //if (contentPresenter != null)
            //{
            //    contentPresenter.ContentTemplate = template;
            //}
            return template;
        }
        
        private static void InsertTemplateProvider([NotNull] List<ITemplateProvider> list, ITemplateProvider templateProvider, [NotNull] List<ITemplateProvider> movedItems)
        {
            movedItems.Add(templateProvider);
            // Find the first index where we can insert
            var insertIndex = 1 + list.LastIndexOf(x => x.CompareTo(templateProvider) < 0);
            list.Insert(insertIndex, templateProvider);
            // Every following providers may have an override rule against the new template provider, we must potentially resort them.
            for (var i = insertIndex + 1; i < list.Count; ++i)
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

        [CanBeNull]
        private ITemplateProvider FindTemplateProvider([NotNull] object item, DependencyObject container)
        {
            var usedProvidersForItem = usedProviders.GetOrCreateValue(item);

            var shouldClear = true;
            WeakReference lastContainer;
            // We check if this item has been templated recently.
            if (lastContainers.TryGetValue(item, out lastContainer) && lastContainer.IsAlive)
            {
                // If so, check if the last container used is a parent of the container to use now.
                var parent = VisualTreeHelper.GetParent(container);
                while (parent != null)
                {
                    // If so, we are applying template recursively. We want don't want to use the same template
                    // provider that the previous time, so we don't clear the list of providers already used.
                    if (Equals(lastContainer.Target, parent))
                    {
                        shouldClear = false;
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            // In any other case, we clear the list of used providers.
            if (shouldClear)
            {
                usedProvidersForItem.Clear();
            }

            lastContainers.Remove(item);

            var availableSelectors = templateProviders.Where(x => x.Match(item)).ToList();

            var result = availableSelectors.FirstOrDefault(x => !usedProvidersForItem.Contains(x.Name));

            if (result != null)
            {
                usedProvidersForItem.Add(result.Name);
                lastContainers.Add(item, new WeakReference(container));
            }
            return result;
        }
    }
}
