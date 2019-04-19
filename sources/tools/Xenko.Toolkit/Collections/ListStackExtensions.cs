using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Collections
{
    /// <summary>
    /// Extension methods to use stack like methods for <see cref="IList{T}"/>.
    /// </summary>
    public static class ListStackExtensions
    {
        /// <summary>
        /// Gets the last item in the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="stack">The <see cref="IList{T}"/> to use as a stack.</param>
        /// <returns>The last item in the collection.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="stack"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="stack"/> is empty.</exception>
        public static T Peek<T>(this IList<T> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            if(stack.Count == 0)
            {
                throw new ArgumentException("The stack is empty.", nameof(stack));
            }

            return stack[stack.Count - 1];
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="stack">The <see cref="IList{T}"/> to use as a stack.</param>
        /// <param name="item">The item to push on to the stack. The value can be <see langword="null"/> for reference types.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="stack"/> is <see langword="null"/>.</exception>
        public static void Push<T>(this IList<T> stack, T item)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            stack.Add(item);
        }

        /// <summary>
        /// Removes and returns the object at the end of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="stack">The <see cref="IList{T}"/> to use as a stack.</param>
        /// <returns>The object removed from the end of the <see cref="IList{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="stack"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="stack"/> is empty.</exception>
        public static T Pop<T>(this IList<T> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            if (stack.Count == 0)
            {
                throw new ArgumentException("The stack is empty.", nameof(stack));
            }

            var item = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return item;
        }

        /// <summary>
        /// Removes and returns the object at the start of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="stack">The <see cref="IList{T}"/> to use as a stack.</param>
        /// <returns>The object removed from the start of the <see cref="IList{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="stack"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="stack"/> is empty.</exception>
        public static T PopFront<T>(this IList<T> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException(nameof(stack));
            }

            if (stack.Count == 0)
            {
                throw new ArgumentException("The stack is empty.", nameof(stack));
            }

            var item = stack[0];
            stack.RemoveAt(0);
            return item;
        }
    }
}
