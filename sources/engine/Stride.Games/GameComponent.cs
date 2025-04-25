using Stride.Core;

namespace Stride.Games;

/// <summary>
/// Core components of the created game that will be used for base level initialization.
/// </summary>
public abstract class GameComponent : ComponentBase
{
    public virtual void Initialize(IServiceRegistry services) { }
}
