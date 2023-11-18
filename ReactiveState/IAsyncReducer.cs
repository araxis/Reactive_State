using ReactiveState.Core;

namespace ReactiveState;

internal interface IAsyncReducer<TState> : IReducer<TState>
{
    Type ActionType { get; }
    Task ReduceAsync(TState state, object action,Action<TState> updateState, CancellationToken cancellationToken = default);
}