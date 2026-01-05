// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Stride.Core.Serialization;

namespace Stride.Navigation;

internal class DtMeshDataSerializer(SerializationStream stream)
{
    internal void WriteDtMeshOffMeshCons(DtOffMeshConnection[] dataOffMeshCons)
    {
        stream.Write(dataOffMeshCons.Length);
        foreach (var d in dataOffMeshCons)
        {
            for (int i = 0; i < 2; i++)
            {
                stream.Write(d.pos[i].X);
                stream.Write(d.pos[i].Y);
                stream.Write(d.pos[i].Z);
            }

            stream.Write(d.rad);
            stream.Write(d.poly);
            stream.Write(d.flags);
            stream.Write(d.side);
            stream.Write(d.userId);
        }
    }

    internal void WriteDtMeshBvTree(DtBVNode[] dataBvTree)
    {
        stream.Write(dataBvTree.Length);
        foreach (var t in dataBvTree)
        {
            bool isNull = (t == null);
            stream.Write(isNull);

            if (isNull)
                continue;
            
            for (int i = 0; i < 3; i++)
            {
                stream.Write(t.bmin[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                stream.Write(t.bmax[i]);
            }
            stream.Write(t.i);
        }
    }

    internal void WriteDtMeshDetailTris(int[] dataDetailTris)
    {
        stream.Write(dataDetailTris.Length);
        foreach (var t in dataDetailTris)
        {
            stream.Write(t);
        }
    }

    internal void WriteDtMeshDetailVerts(float[] dataDetailVerts)
    {
        stream.Write(dataDetailVerts.Length);
        foreach (var t in dataDetailVerts)
        {
            stream.Write(t);
        }
    }

    internal void WriteDtMeshDetailMeshes(DtPolyDetail[] dataDetailMeshes)
    {
        stream.Write(dataDetailMeshes.Length);
        foreach (var t in dataDetailMeshes)
        {
            stream.Write(t.vertBase);
            stream.Write(t.triBase);
            stream.Write(t.vertCount);
            stream.Write(t.triCount);
        }
    }

    internal void WriteDtMeshPolys(DtPoly[] dataPolys)
    {
        stream.Write(dataPolys.Length);
        foreach (var t in dataPolys)
        {
            stream.Write(t.index);
            stream.Write(t.verts.Length);
            foreach (var v in t.verts)
                stream.Write(v);
            foreach (var n in t.neis)
                stream.Write(n);
            stream.Write(t.flags);
            stream.Write(t.vertCount);
            stream.Write(t.areaAndtype);
        }
    }

    internal void WriteDtMeshVerts(float[] dataVerts)
    {
        stream.Write(dataVerts.Length);
        foreach (var d in dataVerts)
        {
            stream.Write(d);
        }
    }

    internal void WriteDtMeshHeader(DtMeshHeader h)
    {
        stream.Write(h.magic);
        stream.Write(h.version);
        stream.Write(h.x);
        stream.Write(h.y);
        stream.Write(h.layer);
        stream.Write(h.userId);
        stream.Write(h.polyCount);
        stream.Write(h.vertCount);
        stream.Write(h.maxLinkCount);
        stream.Write(h.detailMeshCount);
        stream.Write(h.detailVertCount);
        stream.Write(h.detailTriCount);
        stream.Write(h.bvNodeCount);
        stream.Write(h.offMeshConCount);
        stream.Write(h.offMeshBase);
        stream.Write(h.walkableHeight);
        stream.Write(h.walkableRadius);
        stream.Write(h.walkableClimb);
        stream.Write(h.bmin.X); stream.Write(h.bmin.Y); stream.Write(h.bmin.Z);
        stream.Write(h.bmax.X); stream.Write(h.bmax.Y); stream.Write(h.bmax.Z);
        stream.Write(h.bvQuantFactor);
    }

    internal DtOffMeshConnection[] ReadDtMeshOffMeshCons()
    {
        int count = stream.Read<int>();
        var arr = new DtOffMeshConnection[count];
        for (int i = 0; i < count; i++)
        {
            var c = new DtOffMeshConnection();
            for (int j = 0; j < 2; j++)
            {
                c.pos[j] = new RcVec3f
                {
                    X = stream.Read<float>(),
                    Y = stream.Read<float>(),
                    Z = stream.Read<float>()
                };
            }

            c.rad = stream.Read<float>();
            c.poly = stream.Read<int>();
            c.flags = stream.Read<int>();
            c.side = stream.Read<int>();
            c.userId = stream.Read<int>();
            
            arr[i] = c;
        }
        return arr;
    }

    internal DtBVNode[] ReadDtMeshBvTree()
    {
        int count = stream.Read<int>();
        var arr = new DtBVNode[count];
        for (int i = 0; i < count; i++)
        {
            var isNull = stream.Read<bool>();
            if (isNull)
            {
                continue;
            }
            var node = new DtBVNode();
            for (int j = 0; j < 3; j++) 
                node.bmin[j] = stream.Read<int>();
            for (int j = 0; j < 3; j++) 
                node.bmax[j] = stream.Read<int>();
            node.i = stream.Read<int>();
            
            arr[i] = node;
        }
        return arr;
    }

    internal int[] ReadDtMeshDetailTris()
    {
        int count = stream.Read<int>();
        var arr = new int[count];
        for (int i = 0; i < count; i++)
            arr[i] = stream.Read<int>();
        return arr;
    }

    internal float[] ReadDtMeshDetailVerts()
    {
        int count = stream.Read<int>();
        var arr = new float[count];
        for (int i = 0; i < count; i++)
            arr[i] = stream.Read<float>();
        return arr;
    }

    internal DtPolyDetail[] ReadDtMeshDetailMeshes()
    {
        int count = stream.Read<int>();
        var arr = new DtPolyDetail[count];
        for (int i = 0; i < count; i++)
            arr[i] = new DtPolyDetail(stream.Read<int>(),stream.Read<int>(),stream.Read<int>(),stream.Read<int>());
        return arr;
    }

    internal DtPoly[] ReadDtMeshPolys()
    {
        int count = stream.Read<int>();
        var arr = new DtPoly[count];
        for (int i = 0; i < count; i++)
        {
            int index = stream.Read<int>();
            int maxVertsPerPoly = stream.Read<int>();
            arr[i] = new DtPoly(index, maxVertsPerPoly);
            for (int j = 0; j < maxVertsPerPoly; j++)
            {
                arr[i].verts[j] = stream.Read<int>();
            }
            for (int j = 0; j < maxVertsPerPoly; j++)
            {
                arr[i].neis[j] = stream.Read<int>();
            }
            arr[i].flags = stream.Read<int>();
            arr[i].vertCount = stream.Read<int>();
            arr[i].areaAndtype = stream.Read<int>();
        }
        
        return arr;
    }

    internal float[] ReadDtMeshVerts()
    {
        int count = stream.Read<int>();
        var verts = new float[count];

        for (int i = 0; i < count; i++)
            verts[i] = stream.Read<float>();

        return verts;
    }

    internal DtMeshHeader ReadDtMeshHeader()
    {
        DtMeshHeader h = new DtMeshHeader 
        { 
            magic = stream.Read<int>(), 
            version = stream.Read<int>(), 
            x = stream.Read<int>(), 
            y = stream.Read<int>(),
            layer = stream.Read<int>(),
            userId = stream.Read<int>(),
            polyCount = stream.Read<int>(),
            vertCount = stream.Read<int>(),
            maxLinkCount = stream.Read<int>(),
            detailMeshCount = stream.Read<int>(),
            detailVertCount = stream.Read<int>(),
            detailTriCount = stream.Read<int>(),
            bvNodeCount = stream.Read<int>(),
            offMeshConCount = stream.Read<int>(),
            offMeshBase = stream.Read<int>(),
            walkableHeight = stream.Read<float>(),
            walkableRadius = stream.Read<float>(),
            walkableClimb = stream.Read<float>()
        };

        h.bmin.X = stream.Read<float>(); h.bmin.Y = stream.Read<float>(); h.bmin.Z = stream.Read<float>();
        h.bmax.X = stream.Read<float>(); h.bmax.Y = stream.Read<float>(); h.bmax.Z = stream.Read<float>();
        h.bvQuantFactor = stream.Read<float>();
        return h;
    }
}
