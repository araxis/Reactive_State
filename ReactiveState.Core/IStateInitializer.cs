namespace ReactiveState.Core;

public interface IStateInitializer<out TState>
{
    TState GetInitializeState();
}