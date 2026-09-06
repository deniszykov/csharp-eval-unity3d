[![Actions Status](https://github.com/deniszykov/csharp-eval-unity3d/workflows/dotnet_build/badge.svg)](https://github.com/deniszykov/csharp-eval-unity3d/actions)

# C# Eval() for Unity

This package delivers a robust C# parsing and expression execution API designed for [Unity](http://unity3d.com/) compatibility across multiple platforms. Implemented in C# with no external dependencies, it maintains broad compatibility with modern Unity versions and .NET frameworks.

## Licensing

This package is available for [purchase](https://assetstore.unity.com/packages/tools/visual-scripting/c-eval-56706) on the Unity Asset Store. A valid license is required for use in any project. See [License.md](License.md) for details.

## Quick Start

### Expression Evaluation
Evaluate simple C# expressions immediately:
```csharp
using GameDevWare.Dynamic.Expressions.CSharp;

var result = CSharpExpression.Evaluate<int>("2 * (2 + 3) << 1"); 
// Returns 20
```

### Expression Parsing and AOT Compilation
Parse an expression once and execute it multiple times with high performance:
```csharp
using GameDevWare.Dynamic.Expressions.CSharp;
using System.Linq.Expressions;

var mathExpr = "Math.Max(x, y)";
var exprTree = CSharpExpression.ParseFunc<double, double, double>(mathExpr, arg1Name: "x", arg2Name: "y");
// Returns Expression<Func<double, double, double>>

var func = exprTree.CompileAot();
// Returns Func<double, double, double>

var result = func(10.5, 20.0); // 20.0
```

## Verified Platform Support

- **iOS** (IL2CPP)
- **Android** (Mono/IL2CPP)
- **WebGL**
- **Windows / macOS / Linux**
- **Consoles** (Nintendo Switch, PS4/PS5, Xbox)

> [!IMPORTANT]
> **AOT Platforms (iOS, WebGL, IL2CPP):**  
> Projects targeting AOT compilation require a `link.xml` file to prevent code stripping. A sample is provided in the package. Refer to Unity's documentation on [IL code stripping](https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html) for more context.

## Core API Features

The parser implements C# 4.0+ grammar with support for:

- **Operations:** Arithmetic, bitwise, logical, [Conditional (?:)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator), and [Null-coalescing (??)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator).
- **Invocations:** Methods, delegates, and constructors.
- **Access:** Properties, fields, and [indexers](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/).
- **Types:** Casting, [is/as](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/type-testing-and-cast) operators, `typeof()`, and `default()`.
- **Contexts:** [Checked/Unchecked](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/checked-and-unchecked) blocks.
- **Advanced:** [Null-conditional (?. and ?[])](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-), Power operator (`**`), and [Lambda expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions).
- **Generics:** Generic types and methods, with [limited inference](#generic-type-argument-inference) of a method's type arguments when they are not given explicitly.
- **Extension methods:** [Extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods) declared by known types, including [LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/linq/) queries over sequences.
- **Serialization:** Pack/Unpack expressions into serializable structures.

## Type Resolution & Security

For security, the parser restricts type access by default. Additional types must be registered:

```csharp
// Register specific types
var typeResolver = new KnownTypeResolver(typeof(Mathf), typeof(Time));
CSharpExpression.Evaluate<float>("Mathf.Clamp(Time.time, 0f, 1f)", typeResolver);

// Or allow an entire assembly
var assemblyResolver = new AssemblyTypeResolver(typeof(UnityEngine.Vector3).Assembly);
```

A registered static class also makes its extension methods available on the types they extend. `System.Linq.Enumerable` is registered by default, so LINQ queries work without any registration:

```csharp
var items = new[] { 1, 2, 3 };
CSharpExpression.Evaluate<int[], int>("a.Where(x => x > 1).Sum()", items, "a"); // 5
```

An extension method is only considered when the target's own type declares no applicable method, so a declared method always wins. A member of a delegate type is invoked in preference to an extension method of the same name.

## Generic Type Argument Inference

A generic method called without explicit type arguments has them inferred from the types of its arguments, including the receiver of an extension method. The result type of a lambda argument is inferred by binding its body, so projections stay strongly typed rather than degrading to `object`:

```csharp
var items = new[] { 1, 2, 3 };
// binds Enumerable.Select<int, string>, the expression's type is IEnumerable<string>
CSharpExpression.Evaluate<int[], IEnumerable<string>>("a.Select(x => x.ToString())", items, "a");
```

This is a limited form of inference, enough for `Enumerable` queries and ordinary extension methods, but not the full C# algorithm:

- A type parameter is fixed by the first argument mentioning it; there is no best common type across several arguments. Given `Pair<T>(T a, T b)`, the call `Pair(2.0, 1)` binds `T` to `double`, while `Pair(1, 2.0)` fails to bind.
- Type parameters are inferred from parameters only. One appearing solely in the return type has to be given explicitly, as it does in C#.
- Constraints are verified after inference rather than used to drive it.
- A lambda argument contributes only once its own parameter types are known, which they are for the `Enumerable` operators.

Explicit type arguments are always honoured and skip inference entirely: `a.Where<int>(x => x > 1)`.

On AOT runtimes an inferred call still instantiates the method at bind time, so see [AOT Considerations](#aot-considerations) for the registrations LINQ queries need.

## AOT Compilation Support

High-performance execution on AOT platforms (iOS, WebGL, Consoles) is achieved via `CompileAot()`:

```csharp
var expr = (Expression<Func<Vector3>>)(() => new Vector3(1f, 1f, 1f));
var fn = expr.CompileAot(); // Uses AOT-safe execution if needed
```

### AOT Considerations
1. Only `Expression<Func<...>>` and `Expression<Action<...>>` are supported.
2. Register required delegate types: `AotCompilation.RegisterFunc<int, string>();`.
3. Use `AotCompilation.RegisterForFastCall<TTarget, TResult>()` for maximum performance on critical paths.
4. Register the element type of every sequence a LINQ query runs over: `AotCompilation.RegisterLinqFunc<int>();`, and use `AotCompilation.RegisterLinqFunc<int, string>()` when the query projects elements to another type (`Select`, `OrderBy`).

## Installation

### Unity Asset Store
1. Open **Window > Package Manager**.
2. Select **Packages: My Assets**.
3. Find **C# Eval()** and click **Download/Import**.

### NuGet (Standalone .NET)
```powershell
Install-Package GameDevWare.Dynamic.Expressions
```

## Roadmap & Support

We are continuously improving the package. Planned features include:
- Generic type inference
- C# 6.0+ extended syntax support
- Extension method support

**Support:** [support@gamedevware.com](mailto:support@gamedevware.com)

---

## Change Log (Highlights)

### 3.0.2
- **Feature:** Added out/ref parameter write-back for Invoke node and AOT expression executor.
- **Fix:** Fixed multi-dimensional array packing/unpacking.

### 3.0.1
- **Fix:** Correctly format new expression types (Member/List Init) from syntax trees.
- **Change:** Internal renaming of `Assignment` to `AssignmentBinding` (backward compatible).

### 3.0.0
- **Breaking:** Distributed as a **Unity Package** instead of a legacy `.dll`.
- **Feature:** Added support for **Object and Collection Initializers**.
- **Chore:** Increased C# language support and raised minimum .NET version to 4.6+.

### 2.3.0
- **Feature:** Added `global` parameter for context-aware expressions.
- **Fix:** Standardized `arg4Name` naming across methods.
