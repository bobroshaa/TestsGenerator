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
        
        var allUsings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
        

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(node => node.Modifiers.Any(n => n.Kind() == SyntaxKind.PublicKeyword)).ToList();
        
        MemberDeclarationSyntax? @namespace = null;
        var mainUnit = CompilationUnit();
        
        foreach (var _class in classes)
        {
            
            var methods = _class.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(node => node.Modifiers.Any(n => n.Kind() == SyntaxKind.PublicKeyword)).ToList();;
            //add namespace
            @namespace = NamespaceDeclaration(IdentifierName(GetNamespace(_class) + ".Test"));

            UsingDirectiveSyntax[] usings;

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
            var interfaceParams = new List<ParameterSyntax>();
            foreach (var constructor in constructors)
            {
                @params = constructor.ParameterList.Parameters;
                foreach (var param in @params)
                {
                    var text = param.Type.ToString();
                    if (param.Type.ToString().StartsWith("I"))
                    {
                        interfaces.Add(param.Type.ToString());
                        interfaceParams.Add(param);
                    }
                }
            }


            // add setup
            if (interfaces.Count > 0)
            {
                classDeclaration = CreateTestWithSetUp(_class, classDeclaration, interfaces, @params,interfaceParams);
                classDeclaration = GetHardMethod(_class, classDeclaration, methods);
                usings = 
                    new UsingDirectiveSyntax[]
                    {
                        UsingDirective(
                            IdentifierName(GetNamespace(_class))),
                        UsingDirective(
                            QualifiedName(
                                IdentifierName("NUnit"),
                                IdentifierName("Framework"))),
                        UsingDirective(
                            IdentifierName("Moq"))
                    };
            }
            else
            {
                classDeclaration = GetSimpleMethod(classDeclaration, methods);
                usings = 
                    new UsingDirectiveSyntax[]
                    {
                        UsingDirective(
                            IdentifierName(GetNamespace(_class))),
                        UsingDirective(
                            QualifiedName(
                                IdentifierName("NUnit"),
                                IdentifierName("Framework")))
                    };
            }

            @namespace = ((NamespaceDeclarationSyntax)@namespace).AddMembers(classDeclaration);

            mainUnit = mainUnit.WithUsings(new SyntaxList<UsingDirectiveSyntax>(allUsings.Concat(usings)));
            mainUnit = mainUnit.WithMembers(SingletonList<MemberDeclarationSyntax>(@namespace)).NormalizeWhitespace();
            _testInfo.Add(new TestInfo
            {
                ClassName = _class.Identifier + "Tests",
                GeneratedCode = mainUnit.NormalizeWhitespace().ToFullString()
            });
        }

        return _testInfo;
    }

    private ClassDeclarationSyntax GetSimpleMethod(ClassDeclarationSyntax classDeclaration,List<MethodDeclarationSyntax> methods)
    {
        foreach (var method in methods)
        {
            var syntax = ParseStatement("Assert.Fail(\"autogenerated\");");
            classDeclaration = classDeclaration.AddMembers(
                MethodDeclaration(ParseTypeName("void"), method.Identifier + "Test")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAttributeLists(
                        AttributeList(
                            SingletonSeparatedList<AttributeSyntax>(
                                Attribute(
                                    IdentifierName("Test")))))
                    .WithBody(Block(syntax)));
        }

        return classDeclaration;
    }

    private ClassDeclarationSyntax GetHardMethod(ClassDeclarationSyntax @class, ClassDeclarationSyntax classDeclaration, List<MethodDeclarationSyntax> methods)
    {
        foreach (var method in methods)
        {
            if (method.ReturnType.ToString() == "void")
            {
                classDeclaration = GetSimpleMethod(classDeclaration, new List<MethodDeclarationSyntax>() { method });
                continue;
            } 
            var classObjectName = @class.Identifier.ToString().Insert(1, @class.Identifier.ToString().Substring(0, 1).ToLower()).Remove(0, 1);
            var methodStatements = new List<StatementSyntax>();
            var parameters = method.ParameterList.Parameters;
            string paramsStr = "";
            // arrange 
            foreach (var parameter in parameters)
            {
                methodStatements.Add(ParseStatement($"{parameter.Type.ToString()} {parameter.Identifier} = default;"));
                paramsStr += parameter.Identifier + ",";
            }
            if (paramsStr != "")
                paramsStr = paramsStr.Remove(paramsStr.Length - 1, 1);
            
            // act
            methodStatements.Add(ParseStatement($"{method.ReturnType} actual = _{classObjectName}UnderTest.{method.Identifier}({paramsStr});"));
            
            // assert
            methodStatements.Add(ParseStatement($"{method.ReturnType} expected = default;"));
            methodStatements.Add(ParseStatement("Assert.That(actual, Is.EqualTo(expected));"));
            methodStatements.Add(ParseStatement("Assert.Fail(\"autogenerated\");"));

            classDeclaration = classDeclaration.AddMembers(
                MethodDeclaration(ParseTypeName("void"), method.Identifier + "Test")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAttributeLists(
                        AttributeList(
                            SingletonSeparatedList<AttributeSyntax>(
                                Attribute(
                                    IdentifierName("Test")))))
                    .WithBody(Block(methodStatements)));
        }

        return classDeclaration;
    }



    // add fields and setup
    private ClassDeclarationSyntax CreateTestWithSetUp(ClassDeclarationSyntax @class, ClassDeclarationSyntax newClass, List<string> interfaces, SeparatedSyntaxList<ParameterSyntax> @params, List<ParameterSyntax> intParams)
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
        foreach (var intParam in intParams)
        {
            variableDeclaration = VariableDeclaration(ParseTypeName($"Mock<{intParam.Type.ToString()}>"))
                .AddVariables(VariableDeclarator($"{intParam.Identifier}"));
            fields.Add(FieldDeclaration(variableDeclaration)
                .AddModifiers(Token(SyntaxKind.PrivateKeyword)));
            
            // text for assignments variable
            setupStatements.Add(ParseStatement($"{intParam.Identifier} = new Mock<{intParam.Type.ToString()}>();"));
            classObject += $"{intParam.Identifier}.Object, ";
        }
        
        foreach (var param in @params)
        {
            if (!@interfaces.Contains(param.Type.ToString()))
            {
                setupStatements.Add(ParseStatement($"{param.Type.ToString()} {param.Identifier} = default;"));
                classObject += $"{param.Identifier}, ";
            }
        }

        setupStatements.Add(ParseStatement(classObject.Remove(classObject.Length - 2, 2) + ");"));

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
            nameSpace = namespaceParent.Name.ToString();
            
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }
                
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }
        return nameSpace;
    }
}