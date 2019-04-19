using Xenko.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Collections
{
    /// <summary>
    /// Extensions for <see cref="Random"/>.
    /// </summary>
    public static class RandomListExtensions
    {
       
        /// <summary>
        /// Chooses a random item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="collection">Collection to choose item from.</param>
        /// <returns>A random item from collection</returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        /// <exception cref="ArgumentNullException">If the collection is null.</exception>
        public static T Choose<T>(this Random random, IList<T> collection)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection[random.Next(collection.Count)];
        }

        /// <summary>
        /// Chooses a random item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="collection">Collection to choose item from.</param>
        /// <returns>A random item from collection</returns>
        /// <exception cref="ArgumentNullException">If the random  is null.</exception>
        /// <exception cref="ArgumentNullException">If the collection is null.</exception>
        public static T Choose<T>(this Random random, params T[] collection)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            return collection[random.Next(collection.Length)];
        }

        /// <summary>
        /// Shuffles the collection in place.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="collection">Collection to shuffle.</param>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        /// <exception cref="ArgumentNullException">If the random collection is null.</exception>
        public static void Shuffle<T>(this Random random, IList<T> collection)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            int n = collection.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = collection[k];
                collection[k] = collection[n];
                collection[n] = value;
            }
        }
    }
}
