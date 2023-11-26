// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using ReactiveState;
using ReactiveState.Core;
using SampleApp;
using Action = SampleApp.Action;

//var services = new ServiceCollection();
//services
//    .AddSingleton<Effects>()
//    .AddSingleton<IEffect<Action>, Effects_>()
//    .AddSingleton<IStateInitializer<MyState>, MyStateInitializer>()
//    .AddReactiveState()
//    .AddMyState(ServiceLifetime.Singleton);
//var provider = services.BuildServiceProvider();
//var dispatcher = provider.GetRequiredService<IDispatcher>();

//var state = provider.GetRequiredService<IState<MyState>>();
//dispatcher.Dispatch(new Action("Test"));
Console.WriteLine($"Hello, World!");
