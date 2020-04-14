// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Collections;

namespace Xenko.UI
{
    /// <summary>
    /// A collection of strip definitions
    /// </summary>
    [DataContract(nameof(StripDefinitionCollection))]
    public class StripDefinitionCollection : TrackingCollection<StripDefinition>
    {
        
    }
}
