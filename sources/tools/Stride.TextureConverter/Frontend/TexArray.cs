// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextureTools
{
    class TexArray : TexImage
    {
        public List<TexImage> Array { get; internal set; }

        internal TexArray() : base()
        {
            Array = new List<TexImage>();
        }

        public TexArray(List<TexImage> array)
        {
            Array = array;
        }

        public override Object Clone(bool CopyMemory)
        {
            TexAtlas atlas = (TexAtlas)base.Clone(CopyMemory);

            atlas.Layout = new TexLayout();
            foreach (KeyValuePair<string, TexLayout.Position> entry in Layout.TexList)
            {
                atlas.Layout.TexList.Add(entry.Key, entry.Value);
            }

            return atlas;
        }
    }
}

