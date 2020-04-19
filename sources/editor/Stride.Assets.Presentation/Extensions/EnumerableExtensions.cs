// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Assets.Presentation.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Generates a sequence of values from a <paramref name="generator"/> that is called repeatedly <paramref name="count"/> times.
        /// </summary>
        /// <typeparam name="TResult">The type of the values to be generated in the result sequence.</typeparam>
        /// <param name="generator">The generator of the values.</param>
        /// <param name="count">The number of times to call the generator.</param>
        /// <returns></returns>
        public static IEnumerable<TResult> Repeat<TResult>([NotNull] this Func<TResult> generator, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return RepeatIterator(generator, count);
        }

        private static IEnumerable<TResult> RepeatIterator<TResult>([NotNull] Func<TResult> generator, int count)
        {
            for (var i = 0; i < count; i++) yield return generator();
        }

        /// <summary>
        /// Groups together elements in a sequence as long as they satisfy the given predicate.
        /// </summary>
        /// <remarks>
        /// See http://stackoverflow.com/a/4682163 for details.
        /// </remarks>
        public static IEnumerable<IEnumerable<T>> GroupAdjacentBy<T>(this IEnumerable<T> source, Func<T, T, bool> predicate)
        {
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    var list = new List<T> { e.Current };
                    var pred = e.Current;
                    while (e.MoveNext())
                    {
                        if (predicate(pred, e.Current))
                        {
                            list.Add(e.Current);
                        }
                        else
                        {
                            yield return list;
                            list = new List<T> { e.Current };
                        }
                        pred = e.Current;
                    }
                    yield return list;
                }
            }
        }
    }
}
