using Microsoft.Extensions.Hosting;
using Stride.Games;

namespace Stride.Engine.Hosting;

public interface IStrideBuilder : IHostBuilder
{
    /// <summary>
    /// Gets the game context.
    /// </summary>
    GameContext Context { get; }
    /// <summary>
    /// Gets the game.
    /// </summary>
    GameBase Game { get; }
}
