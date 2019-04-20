# Introduction

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
If you want to access all types in **UnityEngine** you can pass **AssemblyTypeResolver.UnityEngine** as typeResolver parameter.

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
AotCompilation.RegisterFunc<int, bool>(); // template: RegisterFunc<Arg1T, ResultT>
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

* Expression serialization	
* Void expressions (`System.Action` delegates)
* Parser: Delegate construction from method reference
* Parser: Type inference for generics	
* Parser: Full C#6 syntax
* Parser: Extension methods
* Parser: Type initializers, List initializers
* Custom editor with auto-completion for Unity

## Contacts
Please send any questions at support@gamedevware.com

## License
If you embed this package, you must provide a [link](https://www.assetstore.unity3d.com/#!/content/56706) and warning about embedded *C# Eval()* for your customers in the description of your package.
If your package is free, they could use embedded *C# Eval()* free of charge. In either case, they must acquire this package from Unity Store.
