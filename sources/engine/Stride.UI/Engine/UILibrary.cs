// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.UI;

namespace Stride.Engine
{
    [DataContract("UIlibrary")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<UILibrary>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<UILibrary>), Profile = "Content")]
    public class UILibrary : ComponentBase
    {
        public UILibrary()
        {
            UIElements = new Dictionary<string, UIElement>();
        }

        /// <summary>
        /// Gets the UI elements.
        /// </summary>
        public Dictionary<string, UIElement> UIElements { get; }
    }
}
