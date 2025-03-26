using System.Text.Json.Serialization;

namespace Stride.Shaders.Spirv.Generators;


public struct OpKind
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
    [JsonPropertyName("category")]
    public string Category { get; set; }
}


public struct OperandData
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("quantifier")]
    public string? Quantifier { get; set; }
}

public struct InstructionData
{
    [JsonPropertyName("opname")]
    public string OpName { get; set; }
    [JsonPropertyName("class")]
    public string Class { get; set; }
    [JsonPropertyName("opcode")]
    public int OpCode { get; set; }
    [JsonPropertyName("operands")]
    public List<OperandData> Operands { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
}

public class SpirvGrammar
{
    [JsonPropertyName("magic_number")]
    public string MagicNumber { get; set; } = "";
    [JsonPropertyName("major_version")]
    public int MajorVersion { get; set; }
    [JsonPropertyName("minor_version")]
    public int MinorVersion { get; set; }
    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("instructions")]
    public List<InstructionData> Instructions { get; set; } = [];

    [JsonPropertyName("operand_kinds")]
    public List<OpKind> OperandKinds { get; set; } = [];
}