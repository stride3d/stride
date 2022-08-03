using Spv.Generator;
using Stride.Shaders.Mixer;

namespace Stride.Shaders.Spirv;

public abstract class StreamStruct : SpvStruct 
{
    public string Name {get; protected set;}
    public Dictionary<string,int> NameToPosition {get;set;} = new();
    public StreamStruct(Module m, IEnumerable<(string,Instruction)> fields)
    {
        for (int i = 0; i < fields.Count(); i++)
        {
            NameToPosition[fields.ElementAt(i).Item1] = i;
        }
        SpvType = m.TypeStruct(false, fields.Select(x => x.Item2).ToArray());
        for (int i = 0; i < fields.Count(); i++)
        {
            m.MemberName(SpvType,i,fields.ElementAt(i).Item1);
        }
    }
}

public class Stream : StreamStruct
{
    
    public Stream(EntryPoints entry, Module m, IEnumerable<(string,Instruction)> fields) : base(m,fields)
    {
        Name = entry switch 
        {
            EntryPoints.VSMain => "VS_STREAMS",
            EntryPoints.PSMain => "PS_STREAMS",
            EntryPoints.GSMain => "GS_STREAMS",
            EntryPoints.CSMain => "CS_STREAMS",
            EntryPoints.HSMain => "HS_STREAMS",
            EntryPoints.DSMain => "DS_STREAMS",
            _ => throw new NotImplementedException()
        };
        m.Name(SpvType, Name);
    }
}

public class StreamIn : StreamStruct
{
    
    public StreamIn(EntryPoints entry, Module m, IEnumerable<(string,Instruction)> fields) : base(m,fields)
    {
        Name = entry switch 
        {
            EntryPoints.VSMain => "VS_STREAMS_IN",
            EntryPoints.PSMain => "PS_STREAMS_IN",
            EntryPoints.GSMain => "GS_STREAMS_IN",
            EntryPoints.CSMain => "CS_STREAMS_IN",
            EntryPoints.HSMain => "HS_STREAMS_IN",
            EntryPoints.DSMain => "DS_STREAMS_IN",
            _ => throw new NotImplementedException()
        };
        m.Name(SpvType, Name);
    }
}
public class StreamOut : StreamStruct
{
    
    public StreamOut(EntryPoints entry, Module m, IEnumerable<(string,Instruction)> fields) : base(m,fields)
    {
        Name = entry switch 
        {
            EntryPoints.VSMain => "VS_STREAMS_OUT",
            EntryPoints.PSMain => "PS_STREAMS_OUT",
            EntryPoints.GSMain => "GS_STREAMS_OUT",
            EntryPoints.CSMain => "CS_STREAMS_OUT",
            EntryPoints.HSMain => "HS_STREAMS_OUT",
            EntryPoints.DSMain => "DS_STREAMS_OUT",
            _ => throw new NotImplementedException()
        };
        m.Name(SpvType, Name);
    }
}