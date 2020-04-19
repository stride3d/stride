// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Assets.Tests
{
    /// <summary>
    /// Fake a ParameterCollection using a dictionary of PropertyKey, object
    /// </summary>
    [DataContract]
    public class CustomParameterCollection : Dictionary<PropertyKey, object>
    {

        public void Set(PropertyKey key, object value)
        {
            this[key] = value;
        }
    }
}
