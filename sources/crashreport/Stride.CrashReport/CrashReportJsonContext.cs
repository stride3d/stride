using System.Text.Json.Serialization;

namespace Stride.CrashReport;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(StoredCrash))]
[JsonSerializable(typeof(HashSet<string>))]
internal sealed partial class CrashReportJsonContext : JsonSerializerContext
{
}