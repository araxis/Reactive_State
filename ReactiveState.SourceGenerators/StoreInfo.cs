using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.SourceGenerators;

public class ActionInfo
{
    public string ActionType { get; set; }
    public string ActionName { get; set; }
    public string ExecutorMethodName { get; set; }
    public bool IsStatic { get; set; }
}

public class EffectMethodInfo
{
    public string MethodName { get; set; }
    public string MessageType { get; set; }
    public bool IsStatic { get; set; }


}

public class EffectsContainer
{
    public string TargetClassName { get; set; }
    public string TargetNamespace { get; set; }
    public string ClassName { get; set; }
    public string ClassType { get; set; }
    public string InjectParamName => $"_{ClassName.ToLower()}";
    public List<EffectMethodInfo> Effects { get; set; } = new();
    public IReadOnlyList<string> MessageTypes => Effects.Select(e=>e.MessageType).Distinct().ToList();
    public bool ShouldInject => Effects.Any(a => a.IsStatic == false);

    public IReadOnlyList<EffectMethodCallInfo> MethodCalls => Effects.Select(e =>
        {
            var methodCall = e.IsStatic ? ClassName : InjectParamName;
            return new EffectMethodCallInfo
            {
                MessageType = e.MessageType,
                MethodCall = methodCall
            };
        }).ToList();
}

public class EffectMethodCallInfo
{
    public string MessageType { get; set; }
    public string MethodCall { get; set; }
}
public class ReducersContainer
{
    public string ReducerType { get; set; }
    public string ReducerName { get; set; }
    public bool ImplementsReducer  => ReducerInterfaces.Any();
    public IReadOnlyList<string> ReducerInterfaces { get; set; }
    public List<ActionInfo> Actions { get; } = new();
    public bool ShouldInject => Actions.Any(a => a.IsStatic == false);
}
public class StoreInfo
{
    public string RootNamespace { get; set; }
    public string StateType { get; set; }
    public string StateName { get; set; }
    public string StateNamespace { get; set; }
    public string StoreNamespace => StateNamespace;
    public string StoreClassName => $"{StateName}Store";
    public string StoreType => $"{StoreNamespace}.{StoreClassName}";
    public List<ReducersContainer> ReducerInfos { get; } = new();
    public List<EffectsContainer> EffectInfos { get; } = new();
    public IReadOnlyList<ReducersContainer> ShouldInject => ReducerInfos.Where(reducer => reducer.ShouldInject).ToList();
    public IReadOnlyList<EffectsContainer> EffectsShouldInject => EffectInfos.Where(e => e.ShouldInject).ToList();
    public IReadOnlyList<ActionInfo> ActionInfos => ReducerInfos.SelectMany(r => r.Actions).Distinct(new ActionInfoComparer()).ToList();

}

internal class ActionInfoComparer : IEqualityComparer<ActionInfo>
{
    public bool Equals(ActionInfo x, ActionInfo y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.ActionType == y.ActionType;
    }

    public int GetHashCode(ActionInfo obj)
    {
        // If ActionType is null, return 0 (to prevent NullReferenceException)
        return obj.ActionType?.GetHashCode() ?? 0;
    }
}