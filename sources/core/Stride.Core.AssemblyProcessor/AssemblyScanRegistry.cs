
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor;

public class AssemblyScanRegistry
{
    public void Register(TypeDefinition type, TypeReference scanType)
    {
        if (!ScanTypes.TryGetValue(scanType, out var types))
        {
            types = [];
            ScanTypes.Add(scanType, types);
        }

        types.Add(type);
    }

    public bool HasScanTypes => ScanTypes.Count > 0;

    public Dictionary<TypeReference, HashSet<TypeDefinition>> ScanTypes { get; } = new Dictionary<TypeReference, HashSet<TypeDefinition>>(TypeReferenceEqualityComparer.Default);
}
