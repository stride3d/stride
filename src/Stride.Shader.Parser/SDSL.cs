using System.Collections.Immutable;

namespace Stride.Shader.Parser;

public interface ISDSL
{
}

public class JsonArray : ISDSL
{
    public ImmutableArray<ISDSL> Elements { get; }
    public JsonArray(ImmutableArray<ISDSL> elements)
    {
        Elements = elements;
    }
    public override string ToString()
        => $"[{string.Join(",", Elements.Select(e => e.ToString()))}]";
}

public class JsonObject : ISDSL
{
    public IImmutableDictionary<string, ISDSL> Members { get; }
    public JsonObject(IImmutableDictionary<string, ISDSL> members)
    {
        Members = members;
    }
    public override string ToString()
        => $"{{{string.Join(",", Members.Select(kvp => $"\"{kvp.Key}\":{kvp.Value.ToString()}"))}}}";
}

public class JsonString : ISDSL
{
    public string Value { get; }
    public JsonString(string value)
    {
        Value = value;
    }

    public override string ToString()
        => $"\"{Value}\"";
}