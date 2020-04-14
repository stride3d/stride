// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// Information about a drag & drop command.
    /// </summary>
    // TODO: Move this in a ViewModel-dedicated assembly
    [DataContract]
    public class DropCommandParameters
    {
        public string DataType { get; set; }
        public object Data { get; set; }
        public object Parent { get; set; }
        public int SourceIndex { get; set; }
        public int TargetIndex { get; set; }
        public object Sender { get; set; }
    }
}
