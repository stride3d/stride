// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    /// <summary>
    /// A static class that provides extension methods on the <see cref="object"/> type.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// An extension method that checks for nullity before invoking <see cref="object.ToString"/> on a given object and catches exception thrown by this method.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The return value of <see cref="object.ToString"/>, or "(null)" if <see ref="obj"/> is null, or (ExceptionInToString)" if <see cref="object.ToString"/> thrown an exception.</returns>
        [NotNull]
        public static string ToStringSafe([CanBeNull] this object obj)
        {
            try
            {
                return obj?.ToString() ?? "(null)";
            }
            catch
            {
                return "(ExceptionInToString)";
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> that contains the given object as its single item.
        /// </summary>
        /// <typeparam name="T">The type argument for the <see cref="IEnumerable{T}"/> to generate</typeparam>
        /// <param name="obj">The object to yield.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the given object as its single item.</returns>
        /// <remarks>This method uses <b>yield return</b> to return the given object as an enumerable.</remarks>
        [NotNull]
        public static IEnumerable<T> Yield<T>(this T obj)
        {
            yield return obj;
        }

        /// <summary>
        /// Returns the given object if it is an enumerable. Otherwise, returns an <see cref="IEnumerable{T}"/> that contains the given object as its single item.
        /// </summary>
        /// <typeparam name="T">The type argument for the <see cref="IEnumerable{T}"/> to generate</typeparam>
        /// <param name="obj">The object to convert to an <see cref="IEnumerable{T}"/>.</param>
        /// <returns>the given object if it is an enumerable, an <see cref="IEnumerable{T}"/> that contains the given object as its single item otherwise.</returns>
        /// <remarks>This method uses <see cref="Yield{T}"/> to return the given object as an enumerable.</remarks>
        [NotNull]
        public static IEnumerable<T> ToEnumerable<T>(this object obj)
        {
            var enumerableT = obj as IEnumerable<T>;
            if (enumerableT != null)
                return enumerableT;

            var enumerable = obj as IEnumerable;
            return enumerable?.Cast<T>() ?? Yield((T)obj);
        }

        /// <summary>
        /// This method checks if the given <c>this</c> object is <c>null</c>, and throws a <see cref="ArgumentNullException"/> with the given argument name if so.
        /// It returns the given this object.
        /// </summary>
        /// <typeparam name="T">The type of object to test.</typeparam>
        /// <param name="obj">The object to test.</param>
        /// <param name="argumentName">The name of the argument, in case an <see cref="ArgumentNullException"/> must be thrown.</param>
        /// <returns>The given object.</returns>
        /// <remarks>This method can be used to test for null argument when forwarding members of the object to the <c>base</c> or <c>this</c> constructor.</remarks>
        [NotNull]
        public static T SafeArgument<T>([NotNull] this T obj, [NotNull] string argumentName) where T : class
        {
            if (argumentName == null) throw new ArgumentNullException(nameof(argumentName));
            if (obj == null) throw new ArgumentNullException(argumentName);
            return obj;
        }
    }
}
