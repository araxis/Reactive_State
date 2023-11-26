using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using ReactiveState.Core;

namespace ReactiveState;

internal class Dispatcher:IDispatcher
{
    private readonly Subject<object> _actions = new();

    private readonly IServiceProvider _serviceProvider;
    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IDisposable Subscribe(IObserver<object> observer) => _actions.Subscribe(observer);

    public void Dispatch<T>(T message)
    {
        if (message is null) return;
        _actions.OnNext(message);
        ProcessMessage(message);
    }

    private void ProcessMessage<T>(T message)
    {
        var effects = _serviceProvider.GetServices<IEffect<T>>();
        var tasks = effects.Select(effect => effect.Process(message));
        // Start all tasks and attach a continuation to handle exceptions
        Task.WhenAll(tasks).ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                // Handle the aggregate exception
                HandleExceptions(t.Exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    private void HandleExceptions(AggregateException ex)
    {
        foreach (var exception in ex.InnerExceptions)
        {
            // Handle the specific exception
            // For example, you can log the type of the exception
            // and perform different actions based on its type
        }
    }
}