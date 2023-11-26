namespace ReactiveState.Core;

public interface IDispatcher:IObservable<object>
{
     void Dispatch<T>(T message);
}
