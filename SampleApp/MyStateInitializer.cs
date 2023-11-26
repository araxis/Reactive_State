using ReactiveState.Core;

namespace SampleApp;

public class MyStateInitializer : IStateInitializer<MyState>
{
    public MyState GetInitializeState() => new(string.Empty);
}