[![Actions Status](https://github.com/deniszykov/csharp-eval-unity3d/workflows/dotnet_build/badge.svg)](https://github.com/deniszykov/csharp-eval-unity3d/actions)

# Licensing

This package is available for [purchase](https://assetstore.unity.com/packages/tools/visual-scripting/c-eval-56706) on
the Unity Asset Store. A valid license is required for use in any project.

# Overview

This package delivers a C# parsing and expression execution API designed for [Unity](http://unity3d.com/) compatibility
across multiple platforms. Implemented in C# 3.5 with no external dependencies, it maintains broad compatibility with
Unity versions and .NET frameworks.

## Verified Platform Support

• iOS  
• Android  
• WebGL  
• Windows/macOS/Linux

The solution should function on additional Unity-supported platforms.

> **Important Note for AOT Platforms (iOS, WebGL, IL2CPP):**  
> Projects targeting AOT compilation require inclusion of
>
a [link.xml](https://github.com/deniszykov/csharp-eval-unity3d/blob/master/src/GameDevWare.Dynamic.Expressions.Unity.2021/Assets/Plugins/GameDevWare.Dynamic.Expressions/link.xml)
> file in the project root directory. Refer to Unity's documentation
> on [IL code stripping](https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html) for additional context.

## Core API

• **CSharpExpression**

- Evaluate
- Parse  
  • **AotCompilation**
- RegisterFunc
- RegisterForFastCall (performance optimization)

## Implementation Examples

**Expression Parsing:**

```csharp
var mathExpr = "Math.Max(x, y)";
var exprTree = CSharpExpression.Parse<double, double, double>(mathExpr, arg1Name: "x", arg2Name: "y");
// Returns Expression<Func<double, double, double>>  
var expr = exprTree.CompileAot();
// Returns Func<double, double, double>  
```

**Expression Evaluation:**

```csharp
var arifExpr = "2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10";
var result = CSharpExpression.Evaluate<int>(arifExpr); 
// Returns 19
```

## Parser Specifications

The parser implements C# 4 grammar with support for:

• Arithmetic, bitwise, and logical operations  
• [Conditional](https://msdn.microsoft.com/en-us/library/ty67wk28.aspx)
and [null-coalescing operators](https://msdn.microsoft.com/en-us/library/ms173224.aspx)  
• Method/delegate/constructor invocation  
• [Property/field access](https://msdn.microsoft.com/en-us/library/6zhxzbds.aspx)
and [indexers](https://msdn.microsoft.com/en-gb/library/6x16t2tx.aspx)  
• Type
operations ([casting, conversion](https://msdn.microsoft.com/en-us/library/ms173105.aspx), [is](https://msdn.microsoft.com/en-us/library/scekt9xw.aspx)/[as](https://msdn.microsoft.com/en-us/library/cscsdfbt.aspx)/[typeof](https://msdn.microsoft.com/en-us/library/58918ffs.aspx)/[default](https://msdn.microsoft.com/en-us/library/xwth0h0d.aspx))  
• [Expression grouping](https://msdn.microsoft.com/en-us/library/0z4503sa.aspx)
and [checked/unchecked contexts](https://msdn.microsoft.com/en-us/library/khy08726.aspx)  
• [Type aliases](https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx)
and [null-conditional operators](https://msdn.microsoft.com/en-us/library/dn986595.aspx)  
• Power operator (**)  
• [Lambda expressions](https://msdn.microsoft.com/en-us/library/bb397687.aspx)  
• Literal values (true, false, null)

**Type Support:**  
• Nullable types  
• Generics  
• Enumerations

> **Note:** [Type inference](https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/type-inference)
> is not implemented - explicit generic parameter specification is required.

## Type Resolution

For security, the parser restricts type access to:  
• Argument types  
• Primitive types  
• Math, Array, and Func<> types

Additional types require specification through the typeResolver parameter:

```csharp
var typeResolver = new KnownTypeResolver(typeof(Mathf), typeof(Time));
CSharpExpression.Evaluate<int>("Mathf.Clamp(Time.time, 1.0f, 3.0f)", typeResolver);
```

Full namespace access can be enabled via AssemblyTypeResolver:

```csharp
var typeResolver = new AssemblyTypeResolver(typeof(UnityEngine.Application).Assembly);
```

> **Security Note:** System.Type operations will throw exceptions unless **System.Type** explicitly added as a known
> type.

## AOT Compilation Support

The package enables compilation and execution of System.Linq.Expression expressions in AOT environments:

```csharp
var expr = (Expression<Func<Vector3>>)(() => new Vector3(1.0f, 1.0f, 1.0f));
var fn = expr.CompileAot();
// Returns Func<Vector3>
```

**AOT Environment Requirements (iOS, WebGL and most console platforms):**

1. Only Expression<Func<...>> delegate types supported
2. Only static methods using primitives arguments receive optimization
3. All referenced members must be visible
   to [Unity's static analyzer](https://docs.unity3d.com/Manual/ScriptingRestrictions.html) to prevent
   eager [IL code stripping](https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html)
4. ⚠️
   Required [link.xml](https://github.com/deniszykov/csharp-eval-unity3d/blob/master/src/GameDevWare.Dynamic.Expressions.Unity.2021/Assets/Plugins/GameDevWare.Dynamic.Expressions/link.xml)
   configuration (see above)

**See Also**

* [AOT Exception Patterns and Hacks](https://github.com/neuecc/UniRx/wiki/AOT-Exception-Patterns-and-Hacks)
* [Ahead of Time Compilation (AOT)](http://www.mono-project.com/docs/advanced/runtime/docs/aot/)

### Platform-Specific Considerations

**WebGL and iOS:**  
• Func<> lambdas limited to 4 arguments  
• Instance methods incur reflection overhead  
• Value types experience moderate boxing

**Preparation for AOT Execution:**

```csharp
AotCompilation.RegisterFunc<int, bool>(); // Enables Func<int, bool> in expressions
```

**Unity 2020.3.2 Workaround:**

```csharp
#if ((UNITY_WEBGL || UNITY_IOS || ENABLE_IL2CPP) && !UNITY_EDITOR
GameDevWare.Dynamic.Expressions.AotCompilation.IsAotRuntime = true;
#endif
```

## Performance Optimization

Method invocation performance can be enhanced through signature registration:

```csharp
// Supports up to 3 arguments
AotCompilation.RegisterForFastCall<MyClass, ReturnType>();
AotCompilation.RegisterForFastCall<MyClass, Arg1Type, ReturnType>();
// Additional signatures...
```

Example:

```csharp
public class MyVectorMath
{
    public Vector4 Dot(Vector4 vector, Vector4 vector);
    public Vector4 Cross(Vector4 vector, Vector4 vector);
    public Vector4 Scale(Vector4 vector, float scale);    
}

// register Dot and Cross method signatures
AotCompilation.RegisterForFastCall<MyVectorMath, Vector4, Vector4, Vector4>();
// register Scale method signature
AotCompilation.RegisterForFastCall<MyVectorMath, Vector4, float, Vector4>();
```

## Development Roadmap

• Expression serialization (completed)  
• Void expression support (completed)  
• Future enhancements:

- Delegate construction from method references
- Generic type inference
- C#6 syntax support
- Extension method support
- Type/list initializers

Feature suggestions may be submitted to support@gamedevware.com.

## Roadmap

You can send suggestions at support@gamedevware.com

* ~Expression serialization~
* ~Void expressions (`System.Action` delegates)~
* Parser: Delegate construction from method reference
* Parser: Type inference for generics
* Parser: Full C#6 expression syntax
* Parser: Extension methods
* Parser: Type initializers, List initializers

## Changes

# 2.3.0

* fix: fixed netcore related error with enumerable.empty<T>
* feature: added optional `global` parameter to all CSharpExpression methods to allow specify global object for
  expression.
* test: removed flaky test with double.tostring() comparison
* fix: fixed typo in `arg4Name` in `CSharpExpression.ParseFunc{4}` and `CSharpExpression.ParseAction{4}`

# 2.2.9

* fix: fixed error with instantiated generic method on types (which is impossible in normal conditions, but fine for
  Unity AOT runtime).

# 2.2.8

* feature:made AotCompilation.IsAotRuntime is mutable, this will allow to signal for AOT runtime and suppress further
  checks.

# 2.2.7

* feature: added public CSharpExpression.Format method for SyntaxTreeNode

# 2.2.6

* changed order or SyntaxTreeNode fields and added "original C# expression" field to parsed AST.
* refactored C# expression rendering to support null-propagation expressions, type aliases (int, byte, object ...),
* renamed "Render" methods to "FormatAsCSharp". Now it is "formatting"
* moved c# "formatting" methods to CSharpExpression class
* mark old "Parse" functions as errors
* mark old "Render" methods as obsolete
* renamed CSharpExpressionFormatter to CSharpExpressionFormatter
* fixed indexer experssion rendering
* refactored NameUtils to properly render C# type names

# 2.2.5

* renamed ParseTreeNode.Lexeme to .Token
* renamed few member of TokenType for better clarity
* added documentation file in Unity project assets
* changed 'propertyOrFieldName' attribute to 'name' in SyntaxTreeNode
* renamed PropertyOfFieldBinder to MemberBinder
* changed 'PropertyOrField' expression type to 'MemberResolve' in SyntaxTreeNode
* added backward compatibility checks in all related classes

# 2.2.4

* added protection against wrong expressions like 'a b' which later bound as 'b'
* fixed some tokenization errors:
*
    * 'issa'scanned as 'is'[Operator] and 'sa'[Identifier], now as 'issa'
*
    * '.09' scanned as '.'[Operator] and '09'[Number], now as '0.09'
*
    * '0.1x' scanned as '0.1'[Number] and 'x'[Identifier], now cause error
* added *method calls* on numbers (example 1.ToString())
* added short number notation (example '.9' for '0.9')
* added '@' prefix for identifiers (example '
  @is') https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
* done small Tokenizer optimization (reduced string allocation during scanning)

# 2.2.3

* added ExpressionPacker type. This type allows packing/unpacking expressions into primitive structures (Dictionaries,
  Arrays...). These structures could be serialized and wired by network or stored for future use.
* added better error message for some binding cases
* denying call to 'Type.InvokeMember' if 'Type' is not within 'known types'.

# 2.2.2

* fixed conditional operator (a ? b : c) parsing with method call in place of 'b'

# 2.2.1

* fixed IL2CPP compilation error due _Attribute interface complilation failure
* added few interfaces to AOT.cs file for better AOT coverage

# 2.2.0

Features

* added support for void expressions (Action<> delegates)
* added support of '.NET Standart 1.3' and '.NET Core 2.0' platforms

# 2.1.4

* Release version, no actual changes except readme.md

# 2.1.2-rc

### Features

* added more descriptive message for member binding error
* added autodoc comments for public members
* hid `ReadOnlyDictionary` from public access
* removed Unity WebGL #if for unsigned types. Unity's bug was fixed.
* added support for generic types and generic methods
* added nullable types and `?` suffix support

```csharp
CSharpExpression.Evaluate<int?>("default(int?)"); // -> null
```

* added lambda expressions. Syntax is `() => x` and `new Func(a => x)`
* added support for expression's parameter re-mapping with lambda expression syntax:

```csharp
CSharpExpression.Evaluate<int, int, int>("(x,y) => x + y", 2, 2); // -> 4
```

* added support for `Func<>` lambdas for AOT environments
* added additional constructor to Binder class

```csharp
public Binder(Type lambdaType, ITypeResolver typeResolver = null);
```

* added `ArgumentsTree.ToString()` method
* added aliases for build-in types. Aliases resolved during binding phase inside `Binder.Bind()` method.

```csharp
CSharpExpression.Evaluate<int>("int.MaxValue");
```

### Bugs

* fixed error with wrongly resolved types (only by name) in KnownTypeResolver
* fixed bug with ACCESS_VIOLATION on iOS (Unity 5.x.x IL2CPP)
* fixed few Unity 3.4 related errors in code
* fixed 'new' expression parsed with error on chained calls new a().b().c()
* fixed some cases of lifted binary/unary/conversion operations
* fixed some AOT'ed operations on System.Boolean type
* fixed null-propagation chains generate invalid code
* fixed some edge cases of resolving nested generic types
* fixed error with types without type.FullName value
* fixed Condition operator types promotion
* fixed Power operator types promotion and null-lifting
* fixed enum constants threated as underlying types during binary/unary operations

### Breaking changes in 2.0

* ParserNode renamed to ParseTreeNode
* ExpressionTree renamed to SyntaxTreeNode
* ExpressionBuilder renamed to Binder
* ITypeResolutionService renamed to ITypeResolver
* ITypeResolver.GetType removed
* ITypeResolver now could be configured with TypeDiscoveryOptions

# 1.0.1.11

* fixed error them creating nullable types via "new" keyword
* fixed Embedded Resource addressing problem on IL2CPP WebGL (localized error messages)
* fixed some cases of nullable types binding
* fixed Enum member resolution
* added Power(``**``) operator into C# syntax

```csharp
CSharpExpression.Evaluate<int>("2 ** 2"); // -> 4
```

* added TypeResolutionService chaining for better KnownTypes re-use

# 1.0.1.10

* fixed error with nulled constants after first call for AOT-ted expression.
* added ToString() impl for ExpressionTree

# 1.0.1.9

* added [Null-conditional Operators](https://msdn.microsoft.com/en-us/library/dn986595.aspx) (example: a.?b.?.c)
* fixed array indexing expressions
* added array types support in type expressions ('convert', 'typeof', 'is' etc.)

## Installation

[Nuget](https://www.nuget.org/packages/GameDevWare.Dynamic.Expressions/):

```
Install-Package GameDevWare.Dynamic.Expressions
```

Unity:

[Package at Unity Asset Store](https://assetstore.unity.com/packages/tools/visual-scripting/c-eval-56706)

## Contacts

Please send any questions at support@gamedevware.com

## License

[Asset Store Terms of Service and EULA](LICENSE.md)
