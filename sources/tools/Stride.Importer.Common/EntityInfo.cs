using System.Collections.Generic;
using Stride.Assets.Materials;

namespace Stride.Importer.Common
{
    public class EntityInfo
    {
        public List<string> TextureDependencies;
        public Dictionary<string, MaterialAsset> Materials;
        public List<string> AnimationNodes;
        public List<MeshParameters> Models;
        public List<NodeInfo> Nodes;
    }
}
