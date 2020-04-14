// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets.Templates
{
    /// <summary>
    /// Describes if a template is supporting a particular context
    /// </summary>
    [DataContract("TemplateScope")]
    public enum TemplateScope
    {
        // TODO We could use flags instead

        /// <summary>
        /// The template can be applied to an existing <see cref="PackageSession"/>.
        /// </summary>
        Session,

        /// <summary>
        /// The template can be applied to an existing <see cref="Assets.Package"/>.
        /// </summary>
        Package,

        /// <summary>
        /// The template can be applied to certain types of Assets <see cref="Assets.Asset"/>.
        /// </summary>
        Asset
    }
}
