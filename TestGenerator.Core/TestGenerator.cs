﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGenerator.Core;

public class TestGenerator
{
    public class TestInfo
    {
        public string ClassName;
        public string GeneratedCode = "";
    }

    private List<TestInfo> _testInfo;

    public TestGenerator()
    {
        _testInfo = new List<TestInfo>();
    }

    public List<TestInfo> Generate(string programText)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(programText);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

        // create namespace
        MemberDeclarationSyntax? @namespace = null;
        var mainUnit = CompilationUnit();
        foreach (var _class in classes)
        {
            //add namespace
            @namespace = NamespaceDeclaration(IdentifierName(GetNamespace(_class) + ".Test"));
            
            var usings = new SyntaxList<UsingDirectiveSyntax>(
                new UsingDirectiveSyntax[]
                {
                    UsingDirective(
                        IdentifierName(GetNamespace(_class))),
                    UsingDirective(
                        QualifiedName(
                            IdentifierName("NUnit"),
                            IdentifierName("Framework")))
                });

            
            
            var test = Attribute(IdentifierName("Test"));
            
            // add class
            var classDeclaration = ClassDeclaration(_class.Identifier + "Tests")
                .AddModifiers(Token(SyntaxKind.PublicKeyword)).AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList<AttributeSyntax>(
                            Attribute(
                                IdentifierName("TestFixture")))));
            
            // get interfaces
            var constructors = _class.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            List<string> interfaces = new List<string>();
            var @params = new SeparatedSyntaxList<ParameterSyntax>();
            foreach (var constructor in constructors)
            {
                @params = constructor.ParameterList.Parameters;
                foreach (var param in @params)
                {
                    var text = param.Type.ToString();
                    if (param.Type.ToString().StartsWith("I"))
                        interfaces.Add(param.Type.ToString());
                }
            }

            List<MethodDeclarationSyntax> methodDeclaration = new List<MethodDeclarationSyntax>();
            // add setup
            if (interfaces.Count > 0)
            {
                classDeclaration = CreateTestWithSetUp(_class, classDeclaration,interfaces, @params);
            }
            
            var syntax = ParseStatement("Assert.Fail(\"autogenerated\");");
            
            foreach (var method in methods)
            {
                var parentClass = method.Parent;
                string text = "";
                if (parentClass is ClassDeclarationSyntax methodClass)
                    text = methodClass.Identifier.ToString();

                if (text == _class.Identifier.ToString())
                {
                    // add method
                    methodDeclaration.Add(MethodDeclaration(ParseTypeName("void"), method.Identifier + "Test")
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddAttributeLists(
                            AttributeList(
                                SingletonSeparatedList<AttributeSyntax>(
                                    Attribute(
                                        IdentifierName("Test")))))
                        .WithBody(Block(syntax)));
                }
            }

            if (interfaces.Count > 0)
                //classDeclaration = classDeclaration.AddMembers(fieldDeclaration.ToArray());


                if (methodDeclaration != null)
                    classDeclaration = classDeclaration.AddMembers(methodDeclaration.ToArray());

            @namespace = ((NamespaceDeclarationSyntax)@namespace).AddMembers(classDeclaration);
            
            mainUnit = mainUnit.WithUsings(usings);
            mainUnit = mainUnit.WithMembers(SingletonList<MemberDeclarationSyntax>(@namespace)).NormalizeWhitespace();
            _testInfo.Add(new TestInfo
            {
                ClassName = _class.Identifier + "Tests",
                GeneratedCode = mainUnit.NormalizeWhitespace().ToFullString()
            });
        }
        return _testInfo;
    }
    
    // TODO: GetHardMethod()
    // TODO: GetSimpleMethod()

    // add fields and setup
    private ClassDeclarationSyntax CreateTestWithSetUp(ClassDeclarationSyntax @class, ClassDeclarationSyntax newClass, List<string> interfaces, SeparatedSyntaxList<ParameterSyntax> @params)
    {
        // add fields
        var fields = new List<FieldDeclarationSyntax>();
        var setupStatements = new List<StatementSyntax>();
        var classObjectName = @class.Identifier.ToString().Insert(1, @class.Identifier.ToString().Substring(0, 1).ToLower()).Remove(0, 1);
        var variableDeclaration = VariableDeclaration(ParseTypeName(@class.Identifier.ToString()))
            .AddVariables(VariableDeclarator($"_{classObjectName}UnderTest"));
        fields.Add(FieldDeclaration(variableDeclaration)
            .AddModifiers(Token(SyntaxKind.PrivateKeyword)));
        string setupBlock = "";
        string classObject = $"_{classObjectName}UnderTest = new {@class.Identifier}(";
        foreach (var @interface in interfaces)
        {
            string varName = @interface.Insert(2, @interface.Substring(1, 1).ToLower()).Remove(0, 2);
            variableDeclaration = VariableDeclaration(ParseTypeName($"Mock<{@interface}>"))
                .AddVariables(VariableDeclarator($"_{varName}"));
            fields.Add(FieldDeclaration(variableDeclaration)
                .AddModifiers(Token(SyntaxKind.PrivateKeyword)));
            
            // text for assignments variable
            setupStatements.Add(ParseStatement($"_{varName} = new Mock<{@interface}>();"));
            classObject += $"{varName}.Object, ";
        }
        
        foreach (var param in @params)
        {
            if (!@interfaces.Contains(param.Type.ToString()))
            {
                setupBlock += $"\r\n{param.Type.ToString()} {param.Identifier} = default;";
                classObject += $"{param.Identifier}, ";
            }
        }

        setupStatements.Add(ParseStatement(classObject.Remove(classObject.Length - 2, 2) + ");"));
        
        /*return MethodDeclaration(ParseTypeName("void"), "SetUp")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(
                        Attribute(
                            IdentifierName("SetUp")))))
            .WithBody(Block(setupStatements)).NormalizeWhitespace();*/

        newClass = newClass.AddMembers(fields.ToArray());
        newClass = newClass.AddMembers(MethodDeclaration(ParseTypeName("void"), "SetUp")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(
                        Attribute(
                            IdentifierName("SetUp")))))
            .WithBody(Block(setupStatements)).NormalizeWhitespace());
        return newClass;
    }
    
    private string GetNamespace(ClassDeclarationSyntax @class)
    {
        string nameSpace = string.Empty;
        SyntaxNode? potentialNamespaceParent = @class.Parent;
        while (potentialNamespaceParent != null &&
               potentialNamespaceParent is not NamespaceDeclarationSyntax
               && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }
        
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // We have a namespace. Use that as the type
            nameSpace = namespaceParent.Name.ToString();
        
            // Keep moving "out" of the namespace declarations until we 
            // run out of nested namespace declarations
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                // Add the outer namespace as a prefix to the final namespace
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }
        return nameSpace;
    }
}