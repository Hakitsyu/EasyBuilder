using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

namespace EasyBuilder.NET
{
    [Generator]
    public class BuilderGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var builderDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (sn, _) => IsClassWithAttributes(sn),
                    transform: static (gn, _) => BuilderDeclarationLoader.Load(gn))
                .Where(v => v.Item1 != null);

            context.RegisterImplementationSourceOutput(builderDeclarations, BuilderSourceGenerator.Generate);
        }

        static bool IsClassWithAttributes(SyntaxNode node)
            => node is ClassDeclarationSyntax classSyntax && classSyntax.AnyAttribute();
    }

    internal static class BuilderAttributesConstants
    {
        public const string BuilderAttributeFullName = "EasyBuilder.NET.Attributes.BuilderAttribute";
        public const string BuilderIgnoreMemberFullName = "EasyBuilder.NET.Attributes.BuilderIgnoreMemberAttribute";
    }

    internal static class BuilderDeclarationLoader
    {
        internal static (ClassDeclarationSyntax, List<IComplexMember>) Load(GeneratorSyntaxContext context)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;
            if (!IsBuilderClass(classSyntax, context.SemanticModel))
                return (null, null);

            var fields = classSyntax
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(f => !IsBuilderIgnoreMember(f, context.SemanticModel));

            var properties = classSyntax
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => !IsBuilderIgnoreMember(p, context.SemanticModel));

            var members = fields.Cast<MemberDeclarationSyntax>()
                .Concat(properties.Cast<MemberDeclarationSyntax>())
                .Select(m => ComplexMembers.Of(m, context.SemanticModel))
                .ToList();

            return (classSyntax, members);
        }

        static bool IsBuilderClass(ClassDeclarationSyntax classSyntax, SemanticModel model)
            => classSyntax.AnyAttribute(a => model.EqualsAttribute(a, BuilderAttributesConstants.BuilderAttributeFullName));

        static bool IsBuilderIgnoreMember(MemberDeclarationSyntax memberSyntax, SemanticModel model)
            => memberSyntax.AnyAttribute(a => model.EqualsAttribute(a, BuilderAttributesConstants.BuilderIgnoreMemberFullName));
    }

    internal static class BuilderSourceGenerator
    {
        internal static void Generate(SourceProductionContext context, (ClassDeclarationSyntax, List<IComplexMember> members) entries)
        {
            var (classSyntax, members) = entries;

            var namespaceDisplay = classSyntax.GetNamespaceDisplay();
            var classModifier = classSyntax.Modifiers.FirstOrDefault().ToString();
            var classKeyword = classSyntax.Keyword.ToString();
            var className = classSyntax.Identifier.ValueText;

            var (builderClassName, builderClassSource) = BuilderClassSourceGenerator.Generate(classSyntax, members);
            var constructor = BuilderConstructorSourceGenerator.Generate(classSyntax, members, builderClassName);

            var source = $$"""
                namespace {{namespaceDisplay}} {
                    {{classModifier}} partial {{classKeyword}} {{className}} {
                        {{constructor}}
                        
                        {{builderClassSource}}

                        {{classModifier}} static {{builderClassName}} Builder()
                            => new {{builderClassName}}();
                    }
                }
            """;

            context.AddSource($"{className}.g.cs", FormatCode(source));
        }

        static string FormatCode(string str)
            => CSharpSyntaxTree.ParseText(str)
                .GetRoot()
                .NormalizeWhitespace()
                .SyntaxTree
                .ToString();
    }

    internal static class BuilderConstructorSourceGenerator
    {
        internal static string Generate(ClassDeclarationSyntax classSyntax, 
            List<IComplexMember> members,
            string builderClassName)
        {
            var className = classSyntax.Identifier.ValueText;
            var constructorSetters = members
                .Select(m =>
                {
                    return $"this.{m.GetName()} = builder.{m.GetLocalField()};";
                })
                .Aggregate((a, b) => a + "\n" + b);

            var source = $$"""
                private {{className}} ({{builderClassName}} builder) {
                    {{constructorSetters}}
                }
            """;

            return source;
        }
    }

    internal static class BuilderClassSourceGenerator
    {
        internal static (string, string) Generate(ClassDeclarationSyntax classSyntax, List<IComplexMember> members)
        {
            var className = classSyntax.Identifier.ValueText;
            var builderClassName = GetBuilderClassName(classSyntax);
            var builderClassMembers = members
                .Select(m =>
                {
                    var type = m.GetDisplayType();
                    var localField = m.GetLocalField();
                    var methodField = m.GetMethodField();

                    return $$"""
                        internal {{type}} {{localField}};

                        public {{builderClassName}} {{m.GetMethod()}} ({{type}} {{methodField}}) {
                            this.{{localField}} = {{methodField}};
                            return this;
                        }
                    """;
                })
                .Aggregate((a, b) => a + "\n" + b);

            var source = $$""" 
                public partial class {{builderClassName}} {
                    {{builderClassMembers}}

                    public {{className}} Build() {
                        return new {{className}}(this);
                    }
                }
            """;

            return (builderClassName, source);
        }

        internal static string GetBuilderClassName(ClassDeclarationSyntax classSyntax)
            => $"{classSyntax.Identifier.ValueText}Builder";
    }

    internal interface IComplexMember
    {
        string GetName();
        string GetLocalField();
        string GetMethodField();
        string GetMethod();
        string GetMethodSetter();
        string GetDisplayType();
    }

    internal static class ComplexMembers
    {
        internal static IComplexMember? Of(MemberDeclarationSyntax syntax, SemanticModel model)
            => syntax switch
            {
                PropertyDeclarationSyntax propertySyntax => ComplexProperty.Of(propertySyntax, model),
                FieldDeclarationSyntax fieldSyntax => ComplexField.Of(fieldSyntax, model),
                _ => null
            };
    }

    internal record ComplexProperty : IComplexMember
    {
        public PropertyDeclarationSyntax Syntax { get; init; }
        public IPropertySymbol Symbol { get; init; }

        internal static ComplexProperty Of(PropertyDeclarationSyntax syntax, SemanticModel model)
        {
            var symbol = model.GetDeclaredSymbol(syntax);
            if (symbol == null || symbol is not IPropertySymbol propertySymbol)
                return null;

            return new ComplexProperty
            {
                Syntax = syntax,
                Symbol = propertySymbol
            };
        }

        public string GetName()
            => Syntax.Identifier.ValueText;

        public string GetMethodField()
            => GetName();

        public string GetLocalField()
            => $"_{GetMethodField()}";

        public string GetMethod()
        {
            var name = Syntax.Identifier.ValueText;
            var first = name[0];
            return $"{first.ToString().ToUpper()}{name.Substring(1)}";
        }

        public string GetMethodSetter()
            => $"this.{GetLocalField()} = {GetMethodField()}";


        public string GetDisplayType()
            => Symbol.Type.ToDisplayString();
    }

    internal record ComplexField : IComplexMember
    {
        public FieldDeclarationSyntax Syntax { get; init; }
        public IFieldSymbol Symbol { get; init; }

        internal static ComplexField Of(FieldDeclarationSyntax syntax, SemanticModel model)
        {
            var symbol = syntax.Declaration.Variables
                .Select(v => model.GetDeclaredSymbol(v))
                .OfType<IFieldSymbol>()
                .FirstOrDefault();

            if (symbol == null)
                return null;

            return new ComplexField
            {
                Syntax = syntax,
                Symbol = symbol
            };
        }

        private VariableDeclaratorSyntax? GetDeclarator()
            => Syntax.Declaration.ChildNodes()
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault();

        public string GetName()
            => GetDeclarator()?.Identifier.ValueText;

        public string GetMethodField()
            => GetName();

        public string GetLocalField()
            => $"_{GetMethodField()}";

        public string GetMethod()
        {
            var name = GetDeclarator()?.Identifier.ValueText; ;
            var first = name[0];
            return $"{first.ToString().ToUpper()}{name.Substring(1)}";
        }

        public string GetMethodSetter()
            => $"this.{GetLocalField()} = {GetMethodField()}";

        public string GetDisplayType()
            => Symbol.Type.ToDisplayString();
    }

    internal static class ClassDeclarationSyntaxEntesions
    {
        internal static bool AnyAttribute(this ClassDeclarationSyntax classSyntax)
            => classSyntax.AttributeLists.Any(l => l.Attributes.Any());

        internal static bool AnyAttribute(this ClassDeclarationSyntax classSyntax, Func<AttributeSyntax, bool> predicate)
            => classSyntax.AttributeLists.Any(l => l.Attributes.Any(predicate));

        internal static string GetNamespaceDisplay(this ClassDeclarationSyntax syntax)
        {
            var parent = syntax.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax)
                    break;

                parent = parent?.Parent;
            }

            return ((NamespaceDeclarationSyntax?)parent)?.Name?.ToFullString();
        }
    }

    internal static class MemberDeclarationSyntaxEntesions 
    {
        internal static bool AnyAttribute(this MemberDeclarationSyntax classSyntax)
            => classSyntax.AttributeLists.Any(l => l.Attributes.Any());

        internal static bool AnyAttribute(this MemberDeclarationSyntax classSyntax, Func<AttributeSyntax, bool> predicate)
            => classSyntax.AttributeLists.Any(l => l.Attributes.Any(predicate));
    }

    internal static class SemanticModelExtensions
    {
        internal static bool EqualsAttribute(this SemanticModel model, AttributeSyntax attributeSyntax, string attributeFullName)
        {
            var symbol = (IMethodSymbol?)model.GetSymbolInfo(attributeSyntax).Symbol;
            if (symbol == null)
                return false;

            var namedSymbol = symbol.ContainingType;
            return namedSymbol.ToDisplayString() == attributeFullName;
        }
    }
}
