# 3.1.0
feature: Added extension method invocation and LINQ queries in expressions, with type argument inference for generic methods.
feature: Added hexadecimal integer literals (`0x1F`, `0xFFu`, `0x10L`, `0x10ul`).
feature: Added verbatim string literals (`@"c:\temp"`, `""` as escaped quote).
BREAKING CHANGE: Applied C# cast disambiguation rule. `(x) - 1` and `(x) + 1` are now binary operations, not casts. `(T)y` is a cast only if `T` is a keyword type (`int`, `int?`, `T[]`) or the token after `)` is `~`, `!`, `(`, identifier, literal, number or `new`. Write `(Int32)(-1)` or `(int)-1` instead of `(Int32)-1`.
BREAKING CHANGE: String escapes follow C#: added `\a`, `\v`, `\0`, `\U00000000`, `\x` takes 1-4 hex digits (`"\x038"` is `"8"`), removed non-C# decimal escapes like `"\112"`.
BREAKING CHANGE: `**` operator is right-associative and binds tighter than `*`, `/`, `%`: `2 ** 3 ** 2` is `512`, `2 * 3 ** 2` is `18`.
fix: Integer literals take the first fitting type per C# spec: `int`, `uint`, `long`, `ulong` (with `u` suffix: `uint`, `ulong`; with `l` suffix: `long`, `ulong`). Values beyond `ulong` report "Integral constant is too large".
fix: Exponent-only number literals are `double` (`1e5`, `2E3`). Exponent is allowed in `decimal` literals (`1e5m`).
fix: Exponent accepts a sign only right after `e`, so `1e5+3` is an addition.
fix: Keyword followed by a digit or `_` is part of an identifier (`is_valid`, `new_value`, `as_x`, `is2`).
fix: `?.` followed by a digit is a conditional with a short number (`x?.5:1`).
fix: Closing bracket must match the opening one, `(1 + 2]` and `a[0)` are errors now.
fix: Lambda is right-associative, curried lambdas `x => y => x` are supported.
fix: Expression nesting depth is limited to 256, deeply nested expressions throw `ExpressionParserException` instead of crashing with `StackOverflowException`. Errors deep inside nested expressions no longer exhaust the stack.
fix: Char literal must contain exactly one character.
fix: Correct line/column positions for identifiers, tokenizer errors and tokens after multiline string literals.
fix: `f(a < b, c > d)` is parsed as comparisons, not generic arguments.
fix: Cast to array type `(int[])x`, including jagged arrays.

# 3.0.2
feature: Added out/ref parameter write-back for Invoke node and AOT expression executor.
fix: Fixed multi-dimensional array packing/unpacking.
chore: Added better documentation.

# 3.0.1
fix: Added formatting of new expression types (Member Init/List Init) from the syntax tree.
fix: changed the name of `Assignment` binding name to `AssignmentBinding`, the code is backwards compatible with the old name.

# 3.0.0
BREAKING CHANGE: Changed the distribution form to Unity Package instead of a library.
feature: Added parser support for object initializers and collections initializers.
fix: fix for Mono failture to return underlying type code for Enum types
fix: fixed detection of IsByRefLike types in reflection on Mono.
chore: Increased C# language support to version 7.3 and formatted the code.
BREAKING CHANGE: Raised the minimum .NET Framework version to 4.6.
chore: Added new compilation targets to support modern .NET versions.

# 2.3.0
fix: fixed netcore related error with enumerable.empty<T>
feature: added  optional `global` parameter to all CSharpExpression methods to allow specify global object for expression.
test: removed flaky test with double.tostring() comparison
fix: fixed typo in `arg4Name` in `CSharpExpression.ParseFunc{4}` and `CSharpExpression.ParseAction{4}`

# 2.2.9
fix: fixed error with instantiated generic method on types (which is impossible in normal conditions, but fine for Unity AOT runtime).

# 2.2.8
- made AotCompilation.IsAotRuntime is mutable, this will allow to signal for AOT runtime and suppress further checks.

# 2.2.7
- added public CSharpExpression.Format method for SyntaxTreeNode

# 2.2.6
- changed order or SyntaxTreeNode fields and added "original C# expression" field to parsed AST.
- refactored C# expression rendering to support null-propagation expressions, type aliases (int, byte, object ...),
- renamed "Render" methods to "FormatAsCSharp". Now it is "formatting"
- moved c# "formatting" methods to CSharpExpression class
- mark old "Parse" functions as errors
- mark old "Render" methods as obsolete
- renamed CSharpExpressionFormatter to CSharpExpressionFormatter
- fixed indexer experssion rendering
- refactored NameUtils to properly render C# type names

# 2.2.5
renamed ParseTreeNode.Lexeme to .Token
renamed few member of TokenType for better clarity
added documentation file in Unity project assets
changed 'propertyOrFieldName' attribute to 'name' in SyntaxTreeNode
renamed PropertyOfFieldBinder to MemberBinder
changed 'PropertyOrField' expression type to 'MemberResolve' in SyntaxTreeNode
added backward compatibility checks in all related classes

# 2.2.4
added protection against wrong expressions like 'a b' which later bound as 'b'
fixed some tokenization errors:
- 'issa'scanned as 'is'[Operator] and 'sa'[Identifier], now as 'issa'
- '.09' scanned as '.'[Operator] and '09'[Number], now as '0.09'
- '0.1x' scanned as '0.1'[Number] and 'x'[Identifier], now cause error
added method call support for numbers (example 1.ToString())
added short number notation (examples '.9' for '0.9')
added '@' prefix for identifiers (example '@is') https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/verbatim
done small Tokenizer optimization (reduced string allocation during scanning)

# 2.2.3
added ExpressionPacker type. This type allows packing/unpacking expressions into primitive structures (Dictionaries, Arrays...). These structures  could be serialized and wired by network or stored for future use.
added better error message for some binding cases
denying call to 'Type.InvokeMember' if 'Type' is not within 'known types'.

# 2.2.2
* fixed conditional operator (a ? b : c) parsing with method call in place of 'b'

# 2.2.1
* fixed IL2CPP compilation error due _Attribute interface complilation failure
* added few interfaces to AOT.cs file for better AOT coverage

# 2.2.0
Features
* added support for void expressions (Action<> delegates)
* added support of '.NET Standart 1.3' and '.NET Core 2.0' platforms
