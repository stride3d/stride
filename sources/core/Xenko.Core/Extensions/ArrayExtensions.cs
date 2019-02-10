// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;

namespace Xenko.Core.Extensions
{
    /// <summary>
    /// Extensions for list and arrays.
    /// </summary>
    public static class ArrayExtensions
    {
        // TODO: Merge this file with CollectionExtensions.cs

        /// <summary>
        /// Deeply compares of two <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the object to compare</typeparam>
        /// <param name="a1">The list1 to compare</param>
        /// <param name="a2">The list2 to compare</param>
        /// <param name="comparer">The comparer to use (or default to the default EqualityComparer for T)</param>
        /// <returns><c>true</c> if the list are equal</returns>
        public static bool ArraysEqual<T>(IList<T> a1, IList<T> a2, IEqualityComparer<T> comparer = null)
        {
            // This is not really an extension method, maybe it should go somewhere else.
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < a1.Count; i++)
            {
                if (!comparer.Equals(a1[i], a2[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares of two <see cref="IList{T}"/> using operator !=.
        /// </summary>
        /// <typeparam name="T">Type of the object to compare</typeparam>
        /// <param name="a1">The list1 to compare</param>
        /// <param name="a2">The list2 to compare</param>
        /// <returns><c>true</c> if the list are equal</returns>
        public static bool ArraysReferenceEqual<T>(IList<T> a1, IList<T> a2) where T : class
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            for (var i = 0; i < a1.Count; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares of two <see cref="FastListStruct{T}"/> using operator !=.
        /// </summary>
        /// <typeparam name="T">Type of the object to compare</typeparam>
        /// <param name="a1">The list1 to compare</param>
        /// <param name="a2">The list2 to compare</param>
        /// <returns><c>true</c> if the list are equal</returns>
        public static bool ArraysReferenceEqual<T>(FastListStruct<T> a1, FastListStruct<T> a2) where T : class
        {
            if (ReferenceEquals(a1.Items, a2.Items))
                return true;

            if (a1.Items == null || a2.Items == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            for (var i = 0; i < a1.Count; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares of two <see cref="FastListStruct{T}"/> using operator !=.
        /// </summary>
        /// <typeparam name="T">Type of the object to compare</typeparam>
        /// <param name="a1">The list1 to compare</param>
        /// <param name="a2">The list2 to compare</param>
        /// <returns><c>true</c> if the list are equal</returns>
        public static bool ArraysReferenceEqual<T>(ref FastListStruct<T> a1, ref FastListStruct<T> a2) where T : class
        {
            if (ReferenceEquals(a1.Items, a2.Items))
                return true;

            if (a1.Items == null || a2.Items == null)
                return false;

            if (a1.Count != a2.Count)
                return false;

            for (var i = 0; i < a1.Count; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Computes the hash of a collection using hash of each elements.
        /// </summary>
        /// <typeparam name="T">Type of the object to calculate the hash</typeparam>
        /// <param name="data">The list to generates the hash</param>
        /// <param name="comparer">The comparer to use (or use the default comparer otherwise)</param>
        /// <returns>The hashcode of the collection.</returns>
        public static int ComputeHash<T>(this ICollection<T> data, IEqualityComparer<T> comparer = null)
        {
            unchecked
            {
                if (data == null)
                    return 0;

                if (comparer == null)
                    comparer = EqualityComparer<T>.Default;

                var hash = 17 + data.Count;
                var result = hash;
                foreach (var unknown in data)
                    result = result * 31 + comparer.GetHashCode(unknown);
                return result;
            }
        }

        /// <summary>
        /// Computes the hash of the array.
        /// </summary>
        /// <typeparam name="T">Type of the object to calculate the hash</typeparam>
        /// <param name="data">The array to generates the hash</param>
        /// <param name="comparer">The comparer to use (or use the default comparer otherwise)</param>
        /// <returns>The hashcode of the array.</returns>
        public static int ComputeHash<T>(this T[] data, IEqualityComparer<T> comparer = null)
        {
            unchecked
            {
                if (data == null)
                    return 0;

                if (comparer == null)
                    comparer = EqualityComparer<T>.Default;

                var hash = 17 + data.Length;
                var result = hash;
                foreach (var unknown in data)
                    result = result * 31 + comparer.GetHashCode(unknown);
                return result;
            }
        }

        /// <summary>
        /// Extracts a sub-array from an array.
        /// </summary>
        /// <typeparam name="T">Type of the array element</typeparam>
        /// <param name="data">The array to slice</param>
        /// <param name="index">The start of the index to get the data from.</param>
        /// <param name="length">The length of elements to slice</param>
        /// <returns>A slice of the array.</returns>
        [NotNull]
        public static T[] SubArray<T>([NotNull] this T[] data, int index, int length)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Concats two arrays.
        /// </summary>
        /// <typeparam name="T">Type of the array element</typeparam>
        /// <param name="array1">The array1 to concat</param>
        /// <param name="array2">The array2 to concat</param>
        /// <returns>The concat of the array.</returns>
        [NotNull]
        public static T[] Concat<T>([NotNull] this T[] array1, [NotNull] T[] array2)
        {
            if (array1 == null) throw new ArgumentNullException(nameof(array1));
            if (array2 == null) throw new ArgumentNullException(nameof(array2));
            var result = new T[array1.Length + array2.Length];

            array1.CopyTo(result, 0);
            array2.CopyTo(result, array1.Length);

            return result;
        }
    }
}
