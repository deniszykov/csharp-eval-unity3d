[![Build Status](https://travis-ci.org/deniszykov/csharp-eval-unity3d.svg?branch=master)](https://github.com/deniszykov/csharp-eval-unity3d)

# Introduction

**Attention!** This is a paid [package](https://www.assetstore.unity3d.com/#!/content/56706), you can not use it in your project if you have not purchased it through [Unity Asset Store](https://www.assetstore.unity3d.com/en/#!/content/56706).

This package provides the API for parsing and expression execution written in C#. It is specially designed to work with the [Unity](http://unity3d.com/) on various platforms. Since it is written in C# 3.5, it should work with any version of Unity.

It is tested to work on:
* IOS
* Android
* WebGL
* PC/Mac

It should work on any other platforms. 

**API**
* CSharpExpression
	* Evaluate
	* Parse
* AotCompilation
	* RegisterFunc
	* RegisterForFastCall

## Example
Parsing C# expression into **System.Linq.Expression.Expression[T]**:
```csharp
var mathExpr = "Math.Max(x, y)";
var exprTree = CSharpExpression.Parse<double, double, double>(mathExpr, arg1Name: "x", arg2Name: "y") 
// exprTree -> Expression<Func<double, double, double>>
```
Evaluating C# expression:
```csharp
var arifExpr = "2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10";
var result = CSharpExpression.Evaluate<int>(arifExpr); 
// result -> 19
```

## Parser
The parser recognizes the C# 4 grammar only. It includes:

* Arithmetic operations
* Bitwise operations
* Logical operations
* [Conditional operator](https://msdn.microsoft.com/en-us/library/ty67wk28.aspx)
* [Null-coalescing operator](https://msdn.microsoft.com/en-us/library/ms173224.aspx)
* Method/Delegate/Constructor call
* [Property/Field access](https://msdn.microsoft.com/en-us/library/6zhxzbds.aspx)
* [Indexers](https://msdn.microsoft.com/en-gb/library/6x16t2tx.aspx)
* [Casting and Conversion](https://msdn.microsoft.com/en-us/library/ms173105.aspx)
* [Is Operator](https://msdn.microsoft.com/en-us/library/scekt9xw.aspx)
* [As Operator](https://msdn.microsoft.com/en-us/library/cscsdfbt.aspx)
* [TypeOf Operator](https://msdn.microsoft.com/en-us/library/58918ffs.aspx)
* [Default Operator](https://msdn.microsoft.com/en-us/library/xwth0h0d.aspx)
* [Expression grouping with parentheses](https://msdn.microsoft.com/en-us/library/0z4503sa.aspx)
* [Checked/Unchecked scopes](https://msdn.microsoft.com/en-us/library/khy08726.aspx)
* [Aliases for Built-In Types](https://msdn.microsoft.com/en-us/library/ya5y69ds.aspx)
* [Null-conditional Operators](https://msdn.microsoft.com/en-us/library/dn986595.aspx)
* Power operator ``**``
* [Lambda expressions](https://msdn.microsoft.com/en-us/library/bb397687.aspx)
* "true", "false", "null"

Nullable types are supported. 
Generics are supported.
Enumerations are supported.
[Type inference](https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/type-inference) is not available and your should always specify generic parameters on types and methods.

**Known Types**

For security reasons the parser does not provide access to any types except:
* argument types
* primitive types
* Math, Array, Func<> (up to 4 arguments) types

To access other types your should pass **typeResolver** parameter in **Parse** and **Evaluate** method:
```csharp
var typeResolver = new KnownTypeResolver(typeof(Mathf), typeof(Time));
CSharpExpression.Evaluate<int>("Mathf.Clamp(Time.time, 1.0f, 3.0f)", typeResolver); 
```
If you want to access all types in **UnityEngine** you can pass custom **AssemblyTypeResolver** as typeResolver parameter.
```csharp
var typeResolver = new AssemblyTypeResolver(typeof(UnityEngine.Application).Assembly);
```

For security reasons any member invocation on **System.Type** will throw exceptions until **System.Type** is added as known type.

## AOT Execution
You can compile and evaluate expression created by **System.Linq.Expression** and execute it in AOT environment where it is usually impossible. 
```csharp
var expr = (Expression<Func<Vector3>>)(() => new Vector3(1.0f, 1.0f, 1.0f));
var fn = expr.CompileAot();

fn; // -> Func<Vector3>
fn(); // -> Vector3(1.0f, 1.0f, 1.0f)
```

iOS, WebGL and most console platforms use AOT compilation which imposes following restrictions on the dynamic code execution:

* only **Expression&lt;Func&lt;...&gt;&gt;** could be used with **CompileAot()** and Lambda types
* only static methods using primitives (int, float, string, object ...) are optimized for fast calls
* all used classes/methods/properties should be visible to [Unity's static code analyser](https://docs.unity3d.com/Manual/ScriptingRestrictions.html)

**See Also**
* [AOT Exception Patterns and Hacks](https://github.com/neuecc/UniRx/wiki/AOT-Exception-Patterns-and-Hacks)
* [Ahead of Time Compilation (AOT)](http://www.mono-project.com/docs/advanced/runtime/docs/aot/)

### WebGL and iOS

* Only [Func<>](https://msdn.microsoft.com/en-us/library/bb534960(v=vs.110).aspx) (up to 4 arguments) Lambdas are supported
* Instance methods invocation performs slowly due reflection
* Moderate boxing for value types (see roadmap)

You can ensure that your generic [Func<>](https://msdn.microsoft.com/en-us/library/bb534960(v=vs.110).aspx) pass AOT compilation by registering it with **AotCompilation.RegisterFunc**

```csharp
AotCompilation.RegisterFunc<int, bool>(); // will enable Func<int, bool> lambdas anywhere in expressions
```

**Improving Performance**

You can improve the performance of methods invocation by registering their signatures in **AotCompilation.RegisterForFastCall()**. 

```
// Supports up to 3 arguments.
// First generic argument is your class type.
// Last generic argument is return type.

AotCompilation.RegisterForFastCall<InstanceT, ResultT>()
AotCompilation.RegisterForFastCall<InstanceT, Arg1T, ResultT>()
AotCompilation.RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>()
AotCompilation.RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
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

## Roadmap

You can send suggestions at support@gamedevware.com

* Expression serialization (in-progress)
* Void expressions (`System.Action` delegates) (done)
* Parser: Delegate construction from method reference
* Parser: Type inference for generics	
* Parser: Full C#6 syntax
* Parser: Extension methods
* Parser: Type initializers, List initializers
* Custom editor with auto-completion for Unity

## Changes
# 2.2.4
* added protection against wrong expressions like 'a b' which later bound as 'b'
* fixed some tokenization errors:
* * 'issa'scanned as 'is'[Operator] and 'sa'[Identifier], now as 'issa'
* * '.09' scanned as '.'[Operator] and '09'[Number], now as '0.09'
* * '0.1x' scanned as '0.1'[Number] and 'x'[Identifier], now cause error
* added method call support for numbers (example 1.ToString())
* added short number notation (examples '.9' for '0.9')
* added '@' prefix for identifiers (example '@is') https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
* done small Tokenizer optimization (reduced string allocation during scanning)

# 2.2.3
* added ExpressionPacker type. This type allows packing/unpacking expressions into primitive structures (Dictionaries, Arrays...). These structures  could be serialized and wired by network or stored for future use.
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

[Package at Unity Asset Store](https://www.assetstore.unity3d.com/en/#!/content/56706)

## Contacts
Please send any questions at support@gamedevware.com

## License
If you embed this package, you must provide a [link](https://www.assetstore.unity3d.com/#!/content/56706) and warning about embedded *C# Eval()* for your customers in the description of your package.
If your package is free, they could use embedded *C# Eval()* free of charge. In either case, they must acquire this package from Unity Store.

[Asset Store Terms of Service and EULA](LICENSE.md)
