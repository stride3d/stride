// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Stride.Updater
{
    /// <summary>
    /// Resolver for <see cref="IList{T}"/> in property path.
    /// </summary>
    /// <typeparam name="T">The type of list items.</typeparam>
    public class ListUpdateResolver<T> : UpdateMemberResolver
    {
        public override Type SupportedType => typeof(IList<T>);

        /// <inheritdoc/>
        public override UpdatableMember ResolveIndexer(string indexerName)
        {
            // Transform index into integer
            int indexerValue;
            if (!int.TryParse(indexerName, NumberStyles.Any, CultureInfo.InvariantCulture, out indexerValue))
                throw new InvalidOperationException(string.Format("Property path parse error: could not parse indexer value '{0}'", indexerName));

            return new UpdatableListAccessor<T>(indexerValue);
        }
    }
}
