using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReactiveState.Core;

namespace ReactiveState;

public static class Module
{
    public static ReactiveStateModule AddReactiveState(this IServiceCollection services)
    {
        services.TryAddSingleton<IDispatcher,Dispatcher>();
        return new ReactiveStateModule(services);
    }
}