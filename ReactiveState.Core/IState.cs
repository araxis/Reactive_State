namespace ReactiveState.Core;

public interface IState<out TState> : IObservable<TState>, IDisposable
{
    TState CurrentState { get; }
}

