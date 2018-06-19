// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core.Annotations;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(AssetViewModel))]
    public sealed class AssetViewModelAttribute : Attribute
    {
        public AssetViewModelAttribute(params Type[] assetTypes)
        {
            if (assetTypes == null) throw new ArgumentNullException(nameof(assetTypes));
            AssetTypes = assetTypes.ToArray();
        }

        public Type[] AssetTypes { get; set; }
    }
}
