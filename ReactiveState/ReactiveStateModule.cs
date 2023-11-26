using Microsoft.Extensions.DependencyInjection;

namespace ReactiveState;

public class ReactiveStateModule
{
    public IServiceCollection Services { get; }
    internal ReactiveStateModule(IServiceCollection services)
    {
        Services = services;
    }
}