using Stride.Core;
using Stride.Data;

namespace Stride.BepuPhysics;
[DataContract]
[Display("Bepu Configuration")]
public class BepuConfiguration : Configuration
{
    public List<BepuSimulation> BepuSimulations = new();
}
