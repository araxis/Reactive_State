namespace ReactiveState.Core;

public interface IReducer<TState, in TAction>
{
    TState Reduce(TState state, TAction action);
}