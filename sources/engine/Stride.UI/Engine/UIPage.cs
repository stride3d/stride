// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.UI;

namespace Xenko.Engine
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
