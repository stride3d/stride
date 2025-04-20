using Stride.Core;
using Stride.Games;

namespace BepuSample.Game.FiniteStateMachine;

[DataContract(Inherited = true)]
public abstract class StateMachine
{
    [DataMemberIgnore]
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void Update(GameTime gameTime)
    {
        CurrentState?.Update(gameTime);
    }
}
