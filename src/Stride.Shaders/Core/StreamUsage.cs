namespace Stride.Shaders.Core;


public record struct StreamData(EntryPoint EntryPoint, StreamIO IO);

public class StreamUsage
{
    Dictionary<SymbolID, List<StreamData>> usages { get; } = [];

    public List<StreamData> this[SymbolID id] => usages[id];

    public bool ContainsKey(SymbolID symbolID) => usages.ContainsKey(symbolID);
    public void Add(SymbolID symbolID, StreamData streamData)
    {
        if(!usages.TryAdd(symbolID, [streamData]))
            usages[symbolID].Add(streamData);
    }
}