using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Scriban;
namespace ReactiveState.SourceGenerators;

[Generator]
public class StateStoreCodeGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //if (!System.Diagnostics.Debugger.IsAttached)
        //{
        //    System.Diagnostics.Debugger.Launch();
        //}
        var reducerMethodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(predicate: IsReducerMethod, transform: GetMethodDeclarationSyntax)
            .Where(m => m is not null);

        var effectMethodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(predicate: IsEffectMethod, transform: GetMethodDeclarationSyntax)
            .Where(m => m is not null);


        var compilationAndMethods
            = context.CompilationProvider
                .Combine(reducerMethodDeclarations.Collect())
                .Combine(context.AnalyzerConfigOptionsProvider);

        context.RegisterSourceOutput(compilationAndMethods, Execute);
        context.RegisterSourceOutput(context.CompilationProvider.Combine(effectMethodDeclarations.Collect()), ExecuteEffectGenerator);
    }

    private void ExecuteEffectGenerator(SourceProductionContext context, (Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods) data)
    {
        var methodInfos = data.methods.Select(m =>
            {
                var semanticModel = data.compilation.GetSemanticModel(m.SyntaxTree);
                return ModelExtensions.GetDeclaredSymbol(semanticModel, m) as IMethodSymbol;
            }).ToList();
        var groups = methodInfos.GroupMethodsByClass();
        var effectContainers = new List<EffectsContainer>();
        foreach (var row in groups)
        {
            var effectClass = row.Key;
            var effectMethods = row.Value;
            var container = new EffectsContainer
            {
                ClassName = effectClass.Name,
                ClassType = effectClass.ToDisplayString(),
                TargetClassName = $"{effectClass.Name}_",
                TargetNamespace = effectClass.ContainingNamespace.ToDisplayString(),
                Effects = effectMethods.Select(e => new EffectMethodInfo
                {
                    MessageType = e.Parameters[0].Type.ToDisplayString(),
                    IsStatic = e.IsStatic,
                    MethodName = e.Name,
                }).ToList()
            };
            effectContainers.Add(container);
        }

        foreach (var container in effectContainers)
        {
            var storeClassCode = GenerateEffectClassCode(container);
            context.AddSource($"{container.TargetClassName}.g.cs", SourceText.From(storeClassCode, Encoding.UTF8));
        }
    }


    private void Execute(SourceProductionContext context, ((Compilation Left, ImmutableArray<MethodDeclarationSyntax> reducerMethods) Left, AnalyzerConfigOptionsProvider Right) meta)
    {
        var (compilation, methodSyntaxes) = meta.Left;
        var stateInfoBag = new Dictionary<string, StoreInfo>();
        meta.Right.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        foreach (var reducerMethodSyntax in methodSyntaxes)
        {
            var semanticModel = compilation.GetSemanticModel(reducerMethodSyntax.SyntaxTree);
            var reducerMethodSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, reducerMethodSyntax) as IMethodSymbol;
            var stateTypeSymbol = reducerMethodSymbol!.ReturnType;
            var sateType = stateTypeSymbol.ToDisplayString();
            var stateTypeName = stateTypeSymbol.Name;
            var reducerClassInfo = reducerMethodSymbol.ContainingType;
            if (!stateInfoBag.ContainsKey(sateType)) stateInfoBag.Add(sateType, new StoreInfo
            {
                RootNamespace = rootNamespace,
                StateName = stateTypeName,
                StateType = sateType,
                StateNamespace = stateTypeSymbol.ContainingNamespace.ToDisplayString(),
            });
            var stateDetails = stateInfoBag[sateType];

            var reducerClassType = reducerClassInfo.ToDisplayString();
            if (!stateDetails.ReducerInfos.Exists(i => i.ReducerType == reducerClassType))
            {
                stateDetails.ReducerInfos.Add(new ReducersContainer
                {
                    ReducerName = reducerClassInfo.Name,
                    ReducerType = reducerClassType,
                    ReducerInterfaces = reducerClassInfo.ReducerInterfaces()
                });
            }

            var reducerInfo = stateDetails.ReducerInfos.Find(i => i.ReducerType == reducerClassType);
            reducerInfo.Actions.Add(new ActionInfo
            {
                ActionType = reducerMethodSymbol.Parameters[1].Type.ToDisplayString(),
                ActionName = reducerMethodSymbol.Parameters[1].Type.Name,
                ExecutorMethodName = reducerMethodSymbol.Name,
                IsStatic = reducerMethodSymbol.IsStatic
            });

        }
        foreach (var stateInfoHolder in stateInfoBag)
        {
            var sateInfo = stateInfoHolder.Value;
            var storeClassCode = GenerateStoreClassCode(sateInfo);
            context.AddSource($"{sateInfo.StoreClassName}.g.cs", SourceText.From(storeClassCode, Encoding.UTF8));
            var moduleClassCode = GenerateRegistrationClassCode(sateInfo);
            context.AddSource($"{sateInfo.StateName}Module.g.cs", SourceText.From(moduleClassCode, Encoding.UTF8));
        }

    }


    private static bool IsReducerMethod(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is not MethodDeclarationSyntax method) return false;
        return (method.IsInternal() | method.IsPublic()) &&
               method.StartWith("Reduce") &&
               method.HasParameterCount(2) &&
               method.HasReturnType(method.ParameterList.Parameters[0].Type?.ToString()) &&
               method.HasParameterName(0, "state") &&
               method.HasParameterName(1, "action");
    }

    private static bool IsEffectMethod(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is not MethodDeclarationSyntax method) return false;
        return (method.IsInternal() | method.IsPublic()) &&
               method.StartWith("Process") &&
               method.HasParameterCount(1) &&
               method.HasTaskReturnType() &&
               method.HasParameterName(0, "message") &&
               !method.ContainingClass().ImplementsIEffect(method.ParameterList.Parameters[0].Type?.ToString());
    }

    private static bool IsOnStateInitializedMethod(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is not MethodDeclarationSyntax method) return false;
        return (method.IsPublic() | method.IsInternal()) &&
               method.HasName("OnStateInitialized") &&
               method.HasParameterCount(1) &&
               method.HasVoidReturnType() &&
               method.HasParameterName(0, "state");
    }

    private static MethodDeclarationSyntax GetMethodDeclarationSyntax(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        return (MethodDeclarationSyntax)ctx.Node;
    }
    private string GenerateRegistrationClassCode(StoreInfo stateInfo)
    {
        // Get the template string

        var text = GetEmbeddedResource("ReactiveState.SourceGenerators.Templates.Module.scriban");
        var template = Template.Parse(text);

        var sourceCode = template.Render(new { state_info = stateInfo });
        return Normalize(sourceCode); ;
    }



    private string GenerateStoreClassCode(StoreInfo stateInfo)
    {
        // Get the template string

        var text = GetEmbeddedResource("ReactiveState.SourceGenerators.Templates.Store.scriban");
        var template = Template.Parse(text);

        var sourceCode = template.Render(new { state_info = stateInfo });
        return Normalize(sourceCode); ;
    }

    private string GenerateEffectClassCode(EffectsContainer info)
    {
        // Get the template string

        var text = GetEmbeddedResource("ReactiveState.SourceGenerators.Templates.Effect.scriban");
        var template = Template.Parse(text);

        var sourceCode = template.Render(new { info });
        return Normalize(sourceCode); ;
    }

    private static string Normalize(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();
        var formattedCode = root.NormalizeWhitespace().ToFullString();
        return formattedCode;

    }

    private string GetEmbeddedResource(string path)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);

        using var streamReader = new StreamReader(stream);

        return streamReader.ReadToEnd();
    }


}