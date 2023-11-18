using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveState.Core;

namespace ReactiveState;

internal partial class State<TState> : IState<TState>
{
    private readonly IEnumerable<IAsyncReducer<TState>> _reducers;
    private readonly BehaviorSubject<TState> _stateSubject;
    private readonly CompositeDisposable _disposable = new();
    private TState CurrentState => _stateSubject.Value;
    public State(IStateInitializer<TState>? initializer, Dispatcher dispatcher, IEnumerable<IAsyncReducer<TState>> reducers)
    {
        ArgumentNullException.ThrowIfNull(initializer);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(reducers);
        var initState = initializer.GetInitializeState();
        _stateSubject = new BehaviorSubject<TState>(initState);
        _reducers = reducers;
        var validActionTypes = _reducers.Select(x => x.GetType()).ToArray();
         dispatcher.Actions
            .Where(action => validActionTypes.Contains(action.GetType()))
            .Select(action => Observable.FromAsync(()=>HandleAction(action)))
            .Concat()
            .Subscribe();
    }
    private Task HandleAction(object action, CancellationToken cancellationToken = default)
    {
        var reducer = _reducers.SingleOrDefault(r => r.ActionType == action.GetType() && r.ShouldReduce(CurrentState, action));
        if (reducer == null) return Task.CompletedTask;
        return reducer.ReduceAsync(CurrentState, action, s => _stateSubject.OnNext(s), cancellationToken);
    }
    public IDisposable Subscribe(IObserver<TState> observer) => _stateSubject.Subscribe(observer);
    public void Dispose()
    {
        _disposable.Dispose();
        _stateSubject.Dispose();
    }

    partial void RegisterToDispatcher();
}