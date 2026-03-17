// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class AbstractNodeEntryData
    {
        public const string AbstractNodeMatchingEntries = nameof(AbstractNodeMatchingEntries);
        public static readonly PropertyKey<IEnumerable<AbstractNodeEntry>> Key = new PropertyKey<IEnumerable<AbstractNodeEntry>>(AbstractNodeMatchingEntries, typeof(AbstractNodeEntryData), new PropertyCombinerMetadata(CombineProperties<AbstractNodeEntry>));
        
        [Obsolete("Use the generic version of CombineProperties instead, which allows to specify the type of the properties to combine. This method is kept for backward compatibility, but it is recommended to use the generic version instead.")]
        public static object CombineProperty(IEnumerable<object> properties)
        {
            HashSet<AbstractNodeEntry> result;
            var hashSets = new List<HashSet<AbstractNodeEntry>>();
            hashSets.AddRange(properties.Cast<IEnumerable<AbstractNodeEntry>>().Select(x => new HashSet<AbstractNodeEntry>(x)));
            result = hashSets[0];
            // We display only component types that are available for all entities
            for (var i = 1; i < hashSets.Count; ++i)
            {
                result.IntersectWith(hashSets[i]);
            }
            return result.OrderBy(x => x.Order).ThenBy(x => x.DisplayValue);
        }
        
        /// <summary>
        /// Combines the properties of type <typeparamref name="TAbstractNodeEntry"/> by intersecting them and ordering them by their order and display value.
        /// This method allows to specify the type of the properties to combine, which can be useful when the properties are of a more specific type than <see cref="AbstractNodeEntry"/>.
        /// E.g. WPF cannot coerce IEnumerable&lt;AbstractNodeEntry&gt; into IEnumerable&lt;AbstractNodeType&gt; since covariance only goes the other direction — you can widen to AbstractNodeEntry, not narrow back to AbstractNodeType.
        /// In those cases, the binding would silently fall back to the DependencyProperty's default value: null.
        /// </summary>
        /// <param name="properties"></param>
        /// <typeparam name="TAbstractNodeEntry"></typeparam>
        /// <returns></returns>
        public static IEnumerable<TAbstractNodeEntry> CombineProperties<TAbstractNodeEntry>(IEnumerable<object> properties)
            where TAbstractNodeEntry : AbstractNodeEntry
        {
            HashSet<TAbstractNodeEntry> result;
            var hashSets = new List<HashSet<TAbstractNodeEntry>>();
            hashSets.AddRange(properties.Cast<IEnumerable<TAbstractNodeEntry>>().Select(x => new HashSet<TAbstractNodeEntry>(x)));
            result = hashSets[0];
            // We display only component types that are available for all entities
            for (var i = 1; i < hashSets.Count; ++i)
            {
                result.IntersectWith(hashSets[i]);
            }
            return result.OrderBy(x => x.Order).ThenBy(x => x.DisplayValue);
        }
    }
}
