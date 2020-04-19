// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Stride.Core.Assets.Editor
{
    /// <summary>
    /// Provides argument checking at runtime.
    /// </summary>
    /// <example> <list type="table">
    /// <item>
    /// This sample shows how to do null-checking by using
    /// <code>ArgumentCheck.NotNull(variable, nameof(variable));</code>
    /// instead of
    /// <code>if (variable == null) throw new ArgumentNullException(nameof(variable));</code>
    /// </item>
    /// <item>
    /// This sample shows how to do empty-checking of collection by using
    /// <code>ArgumentCheck.NotEmpty(collection, nameof(collection));</code>
    /// instead of
    /// <code>
    /// if (collection == null) throw new ArgumentNullException(nameof(collection));
    /// if (collection.Count == 0) throw new ArgumentOutOfRangeException(nameof(collection), "collection cannot be empty.");
    /// </code>
    /// </item>
    /// </list> </example>
    /// <remarks>
    /// Since the exceptions are thrown in the methods of this class, it might add another level in the stack trace. Therefore, using this class
    /// might not be suited for every case.
    /// All methods have the <see cref="MethodImplAttribute"/> set with <see cref="MethodImplOptions.AggressiveInlining"/> in order to mitigate this behavior.
    /// </remarks>
    public static class ArgumentCheck
    {
        /// <summary>
        /// Checks wether the <paramref name="variable"/> meets the <see cref="predicate"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="variable"/>.</typeparam>
        /// <param name="variable">The value to check.</param>
        /// <param name="predicate">The predicate that implements the condition.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> doesn't meet the <paramref name="predicate"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Condition<T>(T variable, Predicate<T> predicate, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable and predicate cannot be null
            NotNull(variable, paramName);
            NotNull(predicate, "predicate");

            if (!predicate.Invoke(variable))
            {
                throw new ArgumentException($"{paramName} doesn't meet the condition.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="collection"/> is not empty.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="collection">The collection to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="collection"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="collection"/> is empty.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty(ICollection collection, string variableName)
        {
            var paramName = variableName ?? "The collection";
            // collection cannot be null
            NotNull(collection, "collection");
            if (collection.Count == 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be empty.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="collection"/> is not empty.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="collection"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="collection"/> is empty.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty<T>(ICollection<T> collection, string variableName)
        {
            var paramName = variableName ?? "The collection";
            // collection cannot be null
            NotNull(collection, "collection");
            if (collection.Count == 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be empty.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not an empty string.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <remarks>
        /// Before checking the <paramref name="variable"/>, a call is made to
        /// <see cref="NotNull"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="variable"/> cannot be an empty <see cref="string"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty(string variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable cannot be null
            NotNull(variable, paramName);
            if (variable.Length == 0)
            {
                throw new ArgumentException($"{paramName} can't be an empty string.");
            }
        }

        /// <summary>
        /// Checks whether the <paramref name="variable"/> is different from <paramref name="value"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="variable"/>.</typeparam>
        /// <param name="variable">The value to check.</param>
        /// <param name="value">The value to compare to for inequality.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> is equal to the <paramref name="value"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEquals<T>(T variable, T value, string variableName)
        {
            var paramName = variableName ?? "The variable";
            if (EqualityComparer<T>.Default.Equals(variable, value))
            {
                throw new ArgumentException($"{paramName} cannot be equal to {value}.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not <see langword="null"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            if (null == variable)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not a white-space string.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <remarks>
        /// Before checking the <paramref name="variable"/>, a call is made to
        /// <see cref="NotNull"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="variable"/> cannot be an empty <see cref="string"/> or consists only of white-space characters.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotWhiteSpace(string variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable cannot be null
            NotNull(variable, paramName);
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentException($"{paramName} can't be null, empty, or consist only of whitespace characters.");
            }
        }
    }

    /// <summary>
    /// Same as <see cref="ArgumentCheck"/> but only in DEBUG release.
    /// </summary>
    public static class ArgumentDebugCheck
    {
        /// <summary>
        /// Checks wether the <paramref name="variable"/> meets the <see cref="predicate"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="variable"/>.</typeparam>
        /// <param name="variable">The value to check.</param>
        /// <param name="predicate">The predicate that implements the condition.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> doesn't meet the <paramref name="predicate"/>.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Condition<T>(T variable, Predicate<T> predicate, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable and predicate cannot be null
            NotNull(variable, paramName);
            NotNull(predicate, "predicate");

            if (!predicate.Invoke(variable))
            {
                throw new ArgumentException($"{paramName} doesn't meet the condition.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="collection"/> is not empty.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="collection">The collection to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="collection"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="collection"/> is empty.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty(ICollection collection, string variableName)
        {
            var paramName = variableName ?? "The collection";
            // collection cannot be null
            NotNull(collection, "collection");
            if (collection.Count == 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be empty.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="collection"/> is not empty.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="collection"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="collection"/> is empty.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty<T>(ICollection<T> collection, string variableName)
        {
            var paramName = variableName ?? "The collection";
            // collection cannot be null
            NotNull(collection, "collection");
            if (collection.Count == 0)
            {
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} cannot be empty.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not an empty string.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <remarks>
        /// Before checking the <paramref name="variable"/>, a call is made to
        /// <see cref="NotNull"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="variable"/> cannot be an empty <see cref="string"/>.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEmpty(string variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable cannot be null
            NotNull(variable, paramName);
            if (variable.Length == 0)
            {
                throw new ArgumentException($"{paramName} can't be an empty string.");
            }
        }

        /// <summary>
        /// Checks whether the <paramref name="variable"/> is different from <paramref name="value"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="variable"/>.</typeparam>
        /// <param name="variable">The value to check.</param>
        /// <param name="value">The value to compare to for inequality.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="variable"/> is equal to the <paramref name="value"/>.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotEquals<T>(T variable, T value, string variableName)
        {
            var paramName = variableName ?? "The variable";
            if (EqualityComparer<T>.Default.Equals(variable, value))
            {
                throw new ArgumentException($"{paramName} cannot be equal to {value}.");
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not <see langword="null"/>.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            if (null == variable)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Checks wether the <paramref name="variable"/> is not a white-space string.
        /// Otherwise throws an exception.
        /// </summary>
        /// <param name="variable">The value to check.</param>
        /// <param name="variableName">The name of the variable being checked.</param>
        /// <remarks>
        /// Before checking the <paramref name="variable"/>, a call is made to
        /// <see cref="NotNull"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="variable"/> cannot be <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="variable"/> cannot be an empty <see cref="string"/> or consists only of white-space characters.
        /// </exception>
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotWhiteSpace(string variable, string variableName)
        {
            var paramName = variableName ?? "The variable";
            // Variable cannot be null
            NotNull(variable, paramName);
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentException($"{paramName} can't be null, empty, or consist only of whitespace characters.");
            }
        }
    }
}
