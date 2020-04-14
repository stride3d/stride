using System;
using System.Collections.ObjectModel;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Rendering
{
    /// <summary>
    /// A collection of <see cref="MaterialPass"/>.
    /// </summary>
    [DataSerializer(typeof(ListAllSerializer<MaterialPassCollection, MaterialPass>))]
    public sealed class MaterialPassCollection : FastCollection<MaterialPass>
    {
        private readonly Material material;

        internal MaterialPassCollection(Material material)
        {
            this.material = material;
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, MaterialPass item)
        {
            if (item.Material != null)
                throw new InvalidOperationException($"A {nameof(MaterialPass)} can only belong to a single {nameof(Material)}");

            base.InsertItem(index, item);
            item.Material = material;
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            this[index].Material = null;
            base.RemoveItem(index);
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, MaterialPass item)
        {
            // Note: Changing CollectionChanged is not thread-safe
            var oldItem = this[index];
            if (oldItem != null)
                oldItem.Material = null;

            if (item.Material != null)
                throw new InvalidOperationException($"A {nameof(MaterialPass)} can only belong to a single {nameof(Material)}");

            base.SetItem(index, item);

            item.Material = material;
        }
    }
}
