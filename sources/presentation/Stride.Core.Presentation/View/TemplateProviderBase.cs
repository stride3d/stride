// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.View
{
    /// <summary>
    /// An abstract implementation of the <see cref="ITemplateProvider"/> interface.
    /// </summary>
    [ContentProperty("Template")]
    public abstract class TemplateProviderBase : DependencyObject, ITemplateProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateProviderBase"/> class.
        /// </summary>
        protected TemplateProviderBase()
        {
            OverriddenProviderNames = new List<string>();
            OverrideRule = OverrideRule.Some;
        }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public DataTemplate Template { get; set; }

        /// <inheritdoc/>
        public OverrideRule OverrideRule { get; set; }

        /// <inheritdoc/>
        public List<string> OverriddenProviderNames { get; }

        /// <inheritdoc/>
        public abstract bool Match(object obj);

        public int CompareTo([NotNull] ITemplateProvider other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            // Both overrides all: undeterminated.
            if (OverrideRule == OverrideRule.All && other.OverrideRule == OverrideRule.All)
                return 0;

            // This overrides all: this is first.
            if (OverrideRule == OverrideRule.All)
                return -1;

            // Other overrides all: other is first.
            if (other.OverrideRule == OverrideRule.All)
                return 1;

            // Both overrides none: undeterminated.
            if (OverrideRule == OverrideRule.None && other.OverrideRule == OverrideRule.None)
                return 0;

            // This overrides none: other is first.
            if (OverrideRule == OverrideRule.None)
                return 1;

            // Other overrides none: this is first.
            if (other.OverrideRule == OverrideRule.None)
                return -1;
            
            // From this point, both have either the "Some" rule or the "Most" rule.
            var thisOverrides = OverriddenProviderNames.Contains(other.Name);
            var otherOverrides = other.OverriddenProviderNames.Contains(Name);

            // Both overrides each other: undeterminated
            if (thisOverrides && otherOverrides)
                return 0;

            // None overrides the other...
            if (!thisOverrides && !otherOverrides)
            {
                // ...but this overrides most: this is first.
                if (OverrideRule == OverrideRule.Most && other.OverrideRule == OverrideRule.Some)
                    return -1;

                // ...but other overrides most: other is first.
                if (OverrideRule == OverrideRule.Some && other.OverrideRule == OverrideRule.Most)
                    return 1;

                // ...and both have the same rule: undeterminated
                return 0;
            }

            // Result: whichever overrides the other is first.
            return thisOverrides ? -1 : 1;
        }
    }
}
