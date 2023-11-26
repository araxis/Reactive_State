namespace ReactiveState.Core;

public interface IEffect<in TMessage>
{
    Task Process(TMessage message);
}