using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Engine;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{EntityComponent}"/> and <see cref="IEnumerable{ActivableEntityComponent}"/>.
    /// </summary>
    public static class EntityComponentCollectionExtensions
    {
        /// <summary>
        /// Enables all <see cref="ActivableEntityComponent"/> in the collection.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="components">A collection of <see cref="ActivableEntityComponent"/> to enable.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="components"/> is <see langword="null"/>.</exception>
        public static void Enable<T>(this IEnumerable<T> components)
            where T : ActivableEntityComponent
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            foreach (var component in components)
            {
                component.Enabled = true;
            }
        }

        /// <summary>
        /// Disables all <see cref="ActivableEntityComponent"/> in the collection.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="components">A collection of <see cref="ActivableEntityComponent"/> to enable.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="components"/> is <see langword="null"/>.</exception>
        public static void Disable<T>(this IEnumerable<T> components)
            where T : ActivableEntityComponent
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            foreach (var component in components)
            {
                component.Enabled = false;
            }
        }

        /// <summary>
        /// Toggles the <see cref="ActivableEntityComponent.Enabled"/> state all <see cref="ActivableEntityComponent"/> in the collection.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="components">A collection of <see cref="ActivableEntityComponent"/> to enable.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="components"/> is <see langword="null"/>.</exception>
        public static void Toggle<T>(this IEnumerable<T> components)
            where T : ActivableEntityComponent
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            foreach (var component in components)
            {
                component.Enabled = !component.Enabled;
            }
        }
    }
}
