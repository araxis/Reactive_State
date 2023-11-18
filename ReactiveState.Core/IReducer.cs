namespace ReactiveState.Core;

public interface IReducer<TState, in TAction>
{
    bool ShouldReduce(TState state, TAction action) => true;
    TState Reduce(TState state, TAction action);
}
public interface IReducer<TState> : IReducer<TState, object>
{
}