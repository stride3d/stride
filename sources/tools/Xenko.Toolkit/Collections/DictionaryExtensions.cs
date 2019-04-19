using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Collections
{
    /// <summary>
    /// Extension methods for <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds items from one dictionary to the other.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="source">The dictionary items are copied from.</param>
        /// <param name="target">The dictionary items are added to.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="target"/> are <see langword="null"/>.</exception>
        public static void MergeInto<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var item in source)
                target[item.Key] = item.Value;
        }

        /// <summary>
        /// Gets the element with the specified key or a default value if it is not in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dicionary">The dictionary to get element from.</param>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="defaultValue">The value to return if element with specified key does not exist in the <paramref name="dicionary"/>.</param>
        /// <returns>The element with the specified key, or the default value.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="dicionary"/> is <see langword="null"/>.</exception>
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dicionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dicionary == null)
            {
                throw new ArgumentNullException(nameof(dicionary));
            }

            TValue result = default(TValue);

            if (dicionary.TryGetValue(key, out result))
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the element with the specified key or adds it if it is not in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dicionary">The dictionary to get element from.</param>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="getValue">The callback delegate to return value if element with specified key does not exist in the <paramref name="dicionary"/>.</param>
        /// <returns>The element with the specified key, or the added value.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="dicionary"/> or <paramref name="getValue"/> are <see langword="null"/>.</exception>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dicionary, TKey key, Func<TKey, TValue> getValue)
        {
            if (dicionary == null)
            {
                throw new ArgumentNullException(nameof(dicionary));
            }

            if (getValue == null)
            {
                throw new ArgumentNullException(nameof(getValue));
            }

            TValue result = default(TValue);

            if (!dicionary.TryGetValue(key, out result))
            {
                dicionary[key] = result = getValue(key);
            }

            return result;
        }

        /// <summary>
        /// Gets the element with the specified key in the dictionary or the new value returned from the <paramref name="getValue"/> callback.
        /// If the <paramref name="shouldAdd"/> callback returns <see langword="true"/> then the new value is added to the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dicionary">The dictionary to get element from.</param>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="getValue">The callback delegate to return value if element with specified key does not exist in the <paramref name="dicionary"/>.</param>
        /// <param name="shouldAdd">The callback delegate to determine if the new value should be added to the <paramref name="dicionary"/>.</param>
        /// <returns>The element with the specified key, or the new value.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="dicionary"/>, <paramref name="getValue"/> or <paramref name="shouldAdd"/> are <see langword="null"/>.</exception>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dicionary, TKey key, Func<TKey, TValue> getValue, Func<TValue, bool> shouldAdd)
        {
            if (dicionary == null)
            {
                throw new ArgumentNullException(nameof(dicionary));
            }

            if (getValue == null)
            {
                throw new ArgumentNullException(nameof(getValue));
            }

            if (shouldAdd == null)
            {
                throw new ArgumentNullException(nameof(shouldAdd));
            }

            TValue result = default(TValue);

            if (!dicionary.TryGetValue(key, out result))
            {
                result = getValue(key);

                if (shouldAdd(result))
                    dicionary[key] = result;
            }

            return result;
        }

        /// <summary>
        /// Increments integer value in a dictionary by 1.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <param name="dicionary">The dictionary to get element from.</param>
        /// <param name="key">The key of the element to increment and get.</param>
        /// <returns>The element incremented by 1 with the specified key.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="dicionary"/> is <see langword="null"/>.</exception>
        public static int Increment<TKey>(this IDictionary<TKey, int> dicionary, TKey key)
        {
            if (dicionary == null)
            {
                throw new ArgumentNullException(nameof(dicionary));
            }

            int result = default(int);
            dicionary[key] = dicionary.TryGetValue(key, out result) ? result += 1 : result = 1;
            return result;
        }
        
    }
}
