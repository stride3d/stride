// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Assets.Models
{
    [DataContract("NodeInformation")]
    [DataStyle(DataStyle.Compact)]
    [InlineProperty]
    public class NodeInformation
    {
        /// <summary>
        /// The name of the node.
        /// </summary>
        /// <userdoc>The name of the node (as it is written in the imported file).</userdoc>
        [DataMember(10)]
        public string Name { get; set; }

        /// <summary>
        ///  The index of the parent.
        /// </summary>
        [DataMember(20)]
        public int Depth { get; set; }

        /// <summary>
        /// A flag stating if the node is collapsible.
        /// </summary>
        /// <userdoc>If checked, the node is kept in the runtime version of the model. 
        /// Otherwise, all the meshes of model are merged the node information is lost.
        /// Nodes should be preserved in order to be animated or linked to an entity.</userdoc>
        [DataMember(30)]
        [DefaultValue(true)]
        [InlineProperty]
        public bool Preserve { get; set; }

        public NodeInformation()
        {
            Preserve = true;
        }

        public NodeInformation(string name, int depth, bool preserve)
        {
            Name = name;
            Depth = depth;
            Preserve = preserve;
        }
    }
}
