using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Collections
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Checks if <paramref name="enumerable"/> is <see langword="null"/> or empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="enumerable">The <see cref="IEnumerable{T}"/> to check.</param>
        /// <returns>Returns <see langword="true"/> if <paramref name="enumerable"/> is <see langword="null"/> or empty, otherwise <see langword="false"/>.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        /// <summary>
        /// Concatenates two sequences.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.</exception>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params T[] second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            return Enumerable.Concat(first, second);
        }

        /// <summary>
        /// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequence.</typeparam>
        /// <param name="source">The sequence of elements to execute the <see cref="IEnumerable{T}"/>.</param>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the <see cref="IEnumerable{T}"/>1.</param>
        /// <exception cref="ArgumentException"><paramref name="source"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}
