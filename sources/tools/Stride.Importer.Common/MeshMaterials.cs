using System;
using System.Collections.Generic;
using Stride.Assets.Materials;

namespace Stride.Importer.Common
{
    public class MeshMaterials
    {
        public Dictionary<string, MaterialAsset> Materials;
	    public List<MeshParameters> Models;
	    public List<String> BoneNodes;
    }
}
