// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Visitors
{
    /// <summary>
    /// Visitor for assets.
    /// </summary>
    public abstract class AssetVisitorBase : DataVisitorBase
    {
        protected AssetVisitorBase() : this(Core.Reflection.TypeDescriptorFactory.Default)
        {
        }

        protected AssetVisitorBase(ITypeDescriptorFactory typeDescriptorFactory) : base(typeDescriptorFactory)
        {
            // Add automatically registered custom data visitors
            CustomVisitors.AddRange(AssetRegistry.GetDataVisitNodes());
        }
    }
}
