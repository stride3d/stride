// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.UI;

namespace Stride.Engine
{
    /// <summary>
    /// A page containing a UI hierarchy.
    /// </summary>
    [DataContract("UIPage")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<UIPage>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<UIPage>), Profile = "Content")]
    public sealed class UIPage : ComponentBase
    {
        /// <summary>
        /// Gets or sets the root element of the page.
        /// </summary>
        /// <userdoc>The root element of the page.</userdoc>
        [DataMember]
        public UIElement RootElement { get; set; }
    }
}
