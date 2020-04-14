// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Windows;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.View
{
    /// <summary>
    /// This enum describes how an <see cref="ITemplateProvider"/> should override other providers that matches the same object.
    /// </summary>
    public enum OverrideRule
    {
        /// <summary>
        /// The <see cref="ITemplateProvider"/> will override providers whose name is in its <see cref="ITemplateProvider.OverriddenProviderNames"/> collection.
        /// </summary>
        Some,
        /// <summary>
        /// The <see cref="ITemplateProvider"/> will never override other providers.
        /// </summary>
        None,
        /// <summary>
        /// The <see cref="ITemplateProvider"/> will override other providers unless they have the <see cref="All"/> or <see cref="Most"/> rule, or they have the <see cref="Some"/> rule and override this provider specifically.
        /// </summary>
        Most,
        /// <summary>
        /// The <see cref="ITemplateProvider"/> will always override other providers.
        /// </summary>
        All,
    }

    /// <summary>
    /// An interface for a class that can provide a template for a given object that matches some prerequisites.
    /// </summary>
    public interface ITemplateProvider : IComparable<ITemplateProvider>
    {
        /// <summary>
        /// Gets an identifier name for this instance of <see cref="ITemplateProvider"/>.
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        /// Gets or sets the template associated with this <see cref="ITemplateProvider"/>
        /// </summary>
        DataTemplate Template { get; set; }
        
        /// <summary>
        /// Gets or sets the rule to use when this provider can potentially override other providers that matches the same object.
        /// </summary>
        /// <remarks>If two providers should override each other, an exception will be thrown in the related template selector.</remarks>
        OverrideRule OverrideRule { get; set; }

        /// <summary>
        /// Gets the collection of names of <see cref="ITemplateProvider"/> to override when they matches the same object
        /// than this provider, when <see cref="OverrideRule"/> is <see cref="Stride.Core.Presentation.View.OverrideRule.Some"/>.
        /// </summary>
        List<string> OverriddenProviderNames { get; }

        /// <summary>
        /// Indicates whether this instance of <see cref="ITemplateProvider"/> can provide a template for the given object.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns><c>true</c> if this template provider can provide a template for the given object, <c>false</c> otherwise.</returns>
        bool Match(object obj);
    }
}
