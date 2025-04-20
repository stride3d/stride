using Stride.Core;
using Stride.Games;

namespace BepuSample.Game.FiniteStateMachine;
public interface IState
{
    public void OnInitialize(IServiceRegistry serviceRegistry);
    public void Enter();
    public void Exit();
    public void Update(GameTime gameTime);
}
