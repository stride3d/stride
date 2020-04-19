// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Data
{
    [DataContract(Inherited = true)]
    public abstract class Configuration
    {
        [DataMemberIgnore]
        public bool OfflineOnly { get; protected set; }
    }
}
