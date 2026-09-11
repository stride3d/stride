using System.Text.Json.Serialization;

namespace Stride.Cli;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}