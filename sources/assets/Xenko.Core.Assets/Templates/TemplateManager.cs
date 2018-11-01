// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xenko.Core.Assets.Templates
{
    /// <summary>
    /// Handle templates for creating <see cref="Package"/>, <see cref="ProjectReference"/>
    /// </summary>
    public class TemplateManager
    {
        private static readonly object ThisLock = new object();
        private static readonly List<ITemplateGenerator> Generators = new List<ITemplateGenerator>();
        private static readonly PackageCollection ExtraPackages = new PackageCollection();

        public static void RegisterPackage(Package package)
        {
            ExtraPackages.Add(package);
        }

        /// <summary>
        /// Registers the specified factory.
        /// </summary>
        /// <param name="generator">The factory.</param>
        /// <exception cref="System.ArgumentNullException">factory</exception>
        public static void Register(ITemplateGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            lock (ThisLock)
            {
                if (!Generators.Contains(generator))
                {
                    Generators.Add(generator);
                }
            }
        }

        /// <summary>
        /// Unregisters the specified factory.
        /// </summary>
        /// <param name="generator">The factory.</param>
        /// <exception cref="System.ArgumentNullException">factory</exception>
        public static void Unregister(ITemplateGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            lock (ThisLock)
            {
                Generators.Remove(generator);
            }
        }

        /// <summary>
        /// Finds all template descriptions.
        /// </summary>
        /// <returns>A sequence containing all registered template descriptions.</returns>
        public static IEnumerable<TemplateDescription> FindTemplates(PackageSession session = null)
        {
            var packages = session?.Packages.Concat(ExtraPackages) ?? ExtraPackages;
            // TODO this will not work if the same package has different versions
            return packages.SelectMany(package => package.Templates).OrderBy(tpl => tpl.Order).ThenBy(tpl => tpl.Name).ToList();
        }

        /// <summary>
        /// Finds template descriptions that match the given scope.
        /// </summary>
        /// <returns>A sequence containing all registered template descriptions that match the given scope.</returns>
        public static IEnumerable<TemplateDescription> FindTemplates(TemplateScope scope, PackageSession session = null)
        {

            return FindTemplates(session).Where(x => x.Scope == scope);
        }

        /// <summary>
        /// Finds a template generator supporting the specified template description
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>A template generator supporting the specified description or null if not found.</returns>
        public static ITemplateGenerator<TParameters> FindTemplateGenerator<TParameters>(TemplateDescription description) where TParameters : TemplateGeneratorParameters
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            lock (ThisLock)
            {
                // From most recently registered to older
                for (int i = Generators.Count - 1; i >= 0; i--)
                {
                    var generator = Generators[i] as ITemplateGenerator<TParameters>;
                    if (generator != null && generator.IsSupportingTemplate(description))
                    {
                        return generator;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Finds a template generator supporting the specified template description
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A template generator supporting the specified description or null if not found.</returns>
        public static ITemplateGenerator<TParameters> FindTemplateGenerator<TParameters>(TParameters parameters) where TParameters : TemplateGeneratorParameters
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            lock (ThisLock)
            {
                // From most recently registered to older
                for (int i = Generators.Count - 1; i >= 0; i--)
                {
                    var generator = Generators[i] as ITemplateGenerator<TParameters>;
                    if (generator != null && generator.IsSupportingTemplate(parameters.Description))
                    {
                        return generator;
                    }
                }
            }
            return null;
        }
    }
}
