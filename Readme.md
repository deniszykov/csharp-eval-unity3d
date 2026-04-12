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
- **Generics:** Support for generic types and methods (requires explicit type specification; type inference is not supported).
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
