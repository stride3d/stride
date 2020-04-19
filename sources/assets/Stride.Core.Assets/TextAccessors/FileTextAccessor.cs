// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets.TextAccessors
{
    [DataContract]
    public class FileTextAccessor : ISerializableTextAccessor
    {
        [DataMember]
        public string FilePath { get; set; }

        public ITextAccessor Create()
        {
            return new DefaultTextAccessor { FilePath = FilePath };
        }
    }
}
