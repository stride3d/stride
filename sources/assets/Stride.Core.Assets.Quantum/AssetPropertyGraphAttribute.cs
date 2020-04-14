// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Quantum
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(AssetPropertyGraph))]
    public class AssetPropertyGraphAttribute : Attribute
    {
        public AssetPropertyGraphAttribute([NotNull] Type assetType)
        {
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (!typeof(Asset).IsAssignableFrom(assetType)) throw new ArgumentException($"The given type must be assignable to the {nameof(Asset)} type.");
            AssetType = assetType;
        }

        public Type AssetType { get; }
    }
}
