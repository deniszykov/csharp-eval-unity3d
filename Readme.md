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
	* AotRuntime
	* RegisterForFastCall

## Example
Parsing C# expression:
```csharp
CSharpExpression.Parse<double, double, double>("Math.Max(value1,value2)", arg1Name: "value1", arg2Name: "value2") 
// -> Expression<Func<double, double, double>>
```
Evaluating C# expression:
```csharp
CSharpExpression.Evaluate<int>("2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10"); 
// -> 19
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
* "true", "false", "null"

Nullable types are **partially** supported (see roadmap). 

Enumerations are supported.

Generics are **not** supported (see roadmap).

**Known Types**

For security reasons the parser does not provide access to static types, except:
* argument types
* primitive types
* Math class

To access other types your should pass **typeResolutionService** parameter in **Parse** or **Evaluate** method:
```csharp
CSharpExpression.Evaluate<int>("Mathf.Clamp(Time.time, 1.0F, 3.0F)", typeResolutionService: new KnownTypeResolutionService(typeof(Mathf), typeof(Time))); 
```
If you want to access all types in **UnityEngine** you can pass **AssemblyTypeResolutionService.UnityEngine** as typeResolutionService parameter.

## AOT Execution
You can compile expression created by **System.Linq.Expression** and execute it in AOT environment where it is usually impossible. 
Any expression can be compiled, even those that are currently not supported by the parser. For example, expression with type constructor is not supported by Parser, but it can be compiled and executed on IOS (and other AOT environment).
```csharp
Expression<Func<Vector3>> expression = () => new Vector3(1.0f, 1.0f, 1.0f);
Func<Vector3> compiledExpression = expression.CompileAot();

compiledExpression();
// -> Vector3(1.0f, 1.0f, 1.0f)
```

IOS, WebGL and most consoles use AOT compilation which imposes following restrictions on the dynamic code execution:

* only **Expression&lt;Func&lt;...&gt;&gt;** could be used with **CompileAot()**
* only static methods using primitives (int, float, string, object ...) are optimized for fast calls
* all used classes/methods/properties should be visible to Unity's static analyser

**See Also**
* [AOT Exception Patterns and Hacks](https://github.com/neuecc/UniRx/wiki/AOT-Exception-Patterns-and-Hacks)
* [Ahead of Time Compilation (AOT)](http://www.mono-project.com/docs/advanced/runtime/docs/aot/)

### WebGL

Since WebGL has problems with unsigned types and *long* type, the use of these types in expressions are disabled with *#if !UNITY_WEBGL* define. Once the problems are solved, this "define" will be removed.

Building under WebGL bears same limitations and recommendations as building under IOS.

### IOS

* Lambda Expressions are not supported (see roadmap)
* Instance methods invocation performs slowly due reflection
* Moderate boxing for value types (see roadmap)

**Improving methods invocation performance**

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
Example code:
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

The roadmap depends entirely on how many copies of the package will be sold:

* &gt;50
	* Parser: Specifying generic arguments for types and methods
	* Parser: Nullable types specification (like Int32?)
* &gt;100
	* Expression serialization	
	* Void expressions (System.Action delegates)
* &gt;200
	* AOT: Lambda expressions
	* Parser: Delegate construction from method reference
	* Parser: Type inference for generics	
* &gt;300
	* Parser: Full C#6 syntax
	* Parser: Extension methods
	* Parser: Type initializers, List initializers
	* Custom editor with auto-completion for Unity

## Changes
# 1.0.1.9
* added [Null-conditional Operators](https://msdn.microsoft.com/en-us/library/dn986595.aspx) (example: a.?b.?.c)
* fixed array indexing expressions
* added array types support in type expressions ('convert', 'typeof', 'is' etc.)

## Installation
Nuget:
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
