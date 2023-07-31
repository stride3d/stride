// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Contains code from TerraFX Framework, Copyright (c) Tanner Gooding and Contributors
// Licensed under the MIT License (MIT).

using System;

namespace Stride.Core
{
    /// <summary>
    ///   Wraps a pointer to <typeparamref name="T"/> so it can be used as a generic type parameter.
    /// </summary>
    /// <typeparam name="T">The type of the pointer being wrapped.</typeparam>
    public unsafe struct Pointer<T> : IComparable<Pointer<T>>, IEquatable<Pointer<T>>, IComparable, IFormattable
        where T : unmanaged
    {
        /// <summary>
        ///   The pointer value wrapped by the instance.
        /// </summary>
        public T* Value;


        /// <summary>
        ///   Initializes a new instance of the <see cref="Pointer{T}"/> struct.
        /// </summary>
        /// <param name="value">The pointer to be wrapped by the instance.</param>
        public Pointer(T* value)
        {
            Value = value;
        }

        /// <summary>
        ///   Implicitly converts a <typeparamref name="T"/> pointer to a new pointer instance.
        /// </summary>
        /// <param name="value">The <typeparamref name="T"/> pointer for which to wrap.</param>
        public static implicit operator Pointer<T>(T* value) => new(value);

        /// <summary>
        ///   Implicitly converts a pointer instance to the <typeparamref name="T"/> pointer it wraps.
        /// </summary>
        /// <param name="value">The pointer for which to get the wrapped value.</param>
        public static implicit operator T*(Pointer<T> value) => value.Value;


        /// <summary>
        ///   Compares two pointers for equality.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Pointer<T> left, Pointer<T> right) => left.Value == right.Value;

        /// <summary>
        ///   Compares two pointers for inequality.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Pointer<T> left, Pointer<T> right) => left.Value != right.Value;

        /// <summary>
        ///   Compares two pointers to determine relative sort-order.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator >(Pointer<T> left, Pointer<T> right) => left.Value > right.Value;

        /// <summary>
        ///   Compares two pointers to determine relative sort-order.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator >=(Pointer<T> left, Pointer<T> right) => left.Value >= right.Value;

        /// <summary>
        ///   Compares two pointers to determine relative sort-order.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator <(Pointer<T> left, Pointer<T> right) => left.Value < right.Value;

        /// <summary>
        ///   Compares two pointers to determine relative sort-order.
        /// </summary>
        /// <param name="left">The pointer to compare with <paramref name="right"/>.</param>
        /// <param name="right">The pointer to compare with <paramref name="left"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator <=(Pointer<T> left, Pointer<T> right) => left.Value <= right.Value;

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            if (obj is Pointer<T> other)
            {
                return CompareTo(other);
            }
            else
            {
                if (obj is not null)
                {
                    ThrowInvalidType(obj.GetType(), typeof(Pointer<T>));
                }
                return 1;
            }

            /// <summary>
            ///   Throws an exception indicating a type mismatch.
            /// </summary>
            static void ThrowInvalidType(Type type, Type expectedType)
            {
                var message = $"'{type}' is not an instance of '{expectedType}'";
                throw new ArgumentOutOfRangeException(nameof(obj), type, message);
            }
        }

        /// <inheritdoc />
        public int CompareTo(Pointer<T> other) => ((nuint) Value).CompareTo((nuint) other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => (obj is Pointer<T> other) && Equals(other);

        /// <inheritdoc />
        public bool Equals(Pointer<T> other) => this == other;

        /// <inheritdoc />
        public override int GetHashCode() => ((nuint) Value).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => ((nuint) Value).ToString();

        /// <inheritdoc />
        public string ToString(string format, IFormatProvider formatProvider) => ((nuint) Value).ToString(format, formatProvider);
    }
}
