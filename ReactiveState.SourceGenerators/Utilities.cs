using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReactiveState.SourceGenerators;

internal static class Utilities
{
    public static bool ImplementsGenericInterface(this INamedTypeSymbol classSymbol, string interfaceName, int paramCount)
    {
        return classSymbol.AllInterfaces.Any(i =>
            i.Name.Equals(interfaceName, StringComparison.Ordinal) &&
            i.TypeArguments.Length == paramCount);
    }

    public static bool IsPublic(this MethodDeclarationSyntax method) =>
       method.Modifiers.Any(SyntaxKind.PublicKeyword);
    public static bool IsInternal(this MethodDeclarationSyntax method) =>
        method.Modifiers.Any(SyntaxKind.InternalKeyword);
    public static bool IsStatic(this MethodDeclarationSyntax method) =>
   method.Modifiers.Any(SyntaxKind.StaticKeyword);
    public static bool HasName(this MethodDeclarationSyntax method, string name) =>
        method.Identifier.Text == name;
    public static bool HasName(this IMethodSymbol  method, string name) =>
        method.Name == name;
    public static bool StartWith(this MethodDeclarationSyntax method, string name) =>
        method.Identifier.Text.StartsWith(name);
    public static bool HasParameterCount(this MethodDeclarationSyntax method, int count) =>
        method.ParameterList.Parameters.Count == count;
    public static bool HasReturnType(this MethodDeclarationSyntax method, string returnType) =>
        method.ReturnType.ToString() == returnType;
    public static bool HasVoidReturnType(this MethodDeclarationSyntax method) =>
        method.ReturnType is PredefinedTypeSyntax returnType && returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);

    public static bool HasParameterName(this MethodDeclarationSyntax method, int index, string name) =>
        method.ParameterList.Parameters[index].Identifier.Text == name;
    public static bool HasParameterType(this MethodDeclarationSyntax method, int index, string type) =>
        method.ParameterList.Parameters[index].Type?.ToString() == type;
    public static bool HasTaskReturnType(this MethodDeclarationSyntax methodDeclaration)
    {
        var returnType = methodDeclaration.ReturnType.ToString();
        return returnType == "Task";
    }
    public static IReadOnlyList<string> ReducerInterfaces(this INamedTypeSymbol classSymbol)
    {
        var reducerInterfaces = classSymbol.AllInterfaces
            .Where(i => i.ContainingNamespace.ToDisplayString() == "ReactiveState.Core" &&
                        i.Name == "IReducer" &&
                        i.TypeArguments.Length == 2)
            .Select(s => s.ToDisplayString())
            .ToList();

        return reducerInterfaces;
    }

    public static ClassDeclarationSyntax ContainingClass(this MethodDeclarationSyntax methodDeclaration) =>
        methodDeclaration.Parent as ClassDeclarationSyntax;
    public static bool ImplementsIEffect(this ClassDeclarationSyntax classDeclaration, string parameterType)
    {
        return (from baseType in classDeclaration.BaseList?.Types ?? Enumerable.Empty<BaseTypeSyntax>()
                select baseType.Type as GenericNameSyntax)
            .Any(namedType => namedType?.Identifier.Text is  "IEffect" or "ReactiveState.Core.IEffect"
                              && namedType.TypeArgumentList.Arguments.Count == 1 &&
                              namedType.TypeArgumentList.Arguments[0].ToString() == parameterType);
    }

    public static Dictionary<INamedTypeSymbol, List<IMethodSymbol>> GroupMethodsByClass(this IEnumerable<IMethodSymbol> methodSymbols)
    {
        return methodSymbols
            .Where(methodSymbol => methodSymbol != null && methodSymbol.ContainingType != null)
            .GroupBy(methodSymbol => methodSymbol.ContainingType, SymbolEqualityComparer.Default)
            .ToDictionary(group => group.Key as INamedTypeSymbol, group => group.ToList());
    }
}
