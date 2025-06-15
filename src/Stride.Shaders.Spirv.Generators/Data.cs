using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stride.Shaders.Spirv.Generators;


public class EnumerantValueConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();
        else if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value != null && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                return int.Parse(value);
            }
        }
        else throw new Exception($"Unexpected token type {reader.TokenType} for Enumerant value.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class OperandKindConverter : JsonConverter<EquatableDictionary<string, OpKind>>
{
    public override EquatableDictionary<string, OpKind> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            return [];
        else
        {
            var array = JsonSerializer.Deserialize<OpKind[]>(ref reader, options) ?? [];
            return array.ToDictionary(
                x => x.Kind,
                x => x
            );
        }
    }

    public override void Write(Utf8JsonWriter writer, EquatableDictionary<string, OpKind> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.AsDictionary()?.Values.ToArray() ?? [], options);
    }
}


public record struct Enumerant
{
    [JsonPropertyName("enumerant")]
    public string Name { get; set; }
    [JsonPropertyName("value")]
    [JsonConverter(typeof(EnumerantValueConverter))]
    public int Value { get; set; }
    [JsonPropertyName("capabilities")]
    public EquatableList<string>? Capabilities { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
}
public record struct OpKind
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("enumerants")]
    public EquatableList<Enumerant>? Enumerants { get; set; }
}


public record struct OperandData
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("quantifier")]
    public string? Quantifier { get; set; }
    public string? Class { get; set; }
    public string? TypeName { get; set; }
    public bool IsIndexKnown { get; set; }
}

public record struct InstructionData
{
    [JsonPropertyName("opname")]
    public string OpName { get; set; }
    [JsonPropertyName("class")]
    public string Class { get; set; }
    [JsonPropertyName("opcode")]
    public int OpCode { get; set; }
    [JsonPropertyName("operands")]
    public EquatableList<OperandData>? Operands { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
    public string Documentation { get; set; }
}

public record struct AdditionalEnum(string Original, string New);

public record struct SpirvGrammar
{
    [JsonPropertyName("magic_number")]
    public string MagicNumber { get; set; }
    [JsonPropertyName("major_version")]
    public int MajorVersion { get; set; }
    [JsonPropertyName("minor_version")]
    public int MinorVersion { get; set; }
    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("instructions")]
    public EquatableList<InstructionData>? Instructions { get; set; }

    // public EquatableList<OpKind>? OperandKinds { get; set; }
    [JsonPropertyName("operand_kinds")]
    [JsonConverter(typeof(OperandKindConverter))]
    public EquatableDictionary<string, OpKind>? OperandKinds { get; set; }
    public string CoreDoc { get; set; }
    public string GLSLDoc { get; set; }

    public SpirvGrammar()
    {
        MagicNumber = "";
        MajorVersion = 0;
        MinorVersion = 0;
        Revision = 0;
        Instructions = new([]);
        OperandKinds = new([]);
        CoreDoc = "";
        GLSLDoc = "";
    }

}