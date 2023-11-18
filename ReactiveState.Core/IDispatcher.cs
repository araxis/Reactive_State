namespace ReactiveState.Core;

public interface IDispatcher
{
     void Dispatch<T>(T action);
}
