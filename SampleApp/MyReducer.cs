using ReactiveState.Core;

namespace SampleApp;

public class MyReducer : IReducer<MyState, Action>, IReducer<MyState, Action3>
{
    public MyState Reduce(MyState state, Action action)
    {
        Console.WriteLine($"Reducer:{action}");
        return new MyState(Value: action.Value);
    }

    public MyState Reduce(MyState state, Action3 action)
    {
        return state;
    }
}
