using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveState.Core;

namespace ReactiveState;

internal class Dispatcher : IDispatcher
{
    private readonly Subject<object> _actions = new();
    internal IObservable<object> Actions => _actions.AsObservable();
    public void Dispatch<T>(T action)
    {
        if (action != null) { _actions.OnNext(action); }
    }
}