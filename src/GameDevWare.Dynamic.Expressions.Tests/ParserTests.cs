using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class ParserTests
{
	[Theory, InlineData("1", 1), InlineData("0.0001f", 0.0001f), InlineData("01d", 01d), InlineData("0.1d", 0.1d), InlineData("1u", 1u), InlineData("1ul", 1ul),
	InlineData("1L", 1L), InlineData("9.15E+09", 9.15E+09), InlineData("9.15E09", 9.15E09), InlineData("9.15E-09", 9.15E-09), InlineData("9.15e+09", 9.15e+09),
	InlineData("9.15e09", 9.15e09), InlineData("9.15e-09", 9.15e-09), InlineData("9e-09", 9e-09), InlineData(".01", .01), InlineData(".9e-09", .9e-09)]
	public void ParseNumbersTest(string expression, object expected)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		var valueStr = node.Value.TrimEnd('d', 'f', 'u', 'l', 'm');
		Assert.Equal(TokenType.Number, node.Type);
		Assert.Equal(expected, Convert.ChangeType(valueStr, expected.GetType(), CultureInfo.InvariantCulture));
	}

	[Theory, InlineData("1.ToString()", 1), InlineData("0.0001f.ToString()", 0.0001f), InlineData("01d.ToString()", 01d), InlineData("0.1d.ToString()", 0.1d),
	InlineData("1u.ToString()", 1u), InlineData("1ul.ToString()", 1ul), InlineData("1L.ToString()", 1L), InlineData("9.15E+09.ToString()", 9.15E+09),
	InlineData("9.15E09.ToString()", 9.15E09), InlineData("9.15E-09.ToString()", 9.15E-09), InlineData("9.15e+09.ToString()", 9.15e+09),
	InlineData("9.15e09.ToString()", 9.15e09), InlineData("9.15e-09.ToString()", 9.15e-09), InlineData("9e-09.ToString()", 9e-09), InlineData(".01.ToString()", .01),
	InlineData(".9e-09.ToString()", .9e-09)]
	public void ParseNumberInvocationTest(string expression, object expected)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Call, node.Type);

		var target = node[0][0];
		var valueStr = target.Value.TrimEnd('d', 'f', 'u', 'l', 'm');
		Assert.Equal(TokenType.Number, target.Type);
		Assert.Equal(expected, Convert.ChangeType(valueStr, expected.GetType(), CultureInfo.InvariantCulture));
	}

	[Theory, InlineData("a + b", TokenType.Add), InlineData("+a", TokenType.Plus), InlineData("a - b", TokenType.Subtract), InlineData("-a", TokenType.Minus),
	InlineData("a * b", TokenType.Multiplication), InlineData("a ** b", TokenType.Power), InlineData("a / b", TokenType.Division),
	InlineData("a % b", TokenType.Modulo), InlineData("a & b", TokenType.And), InlineData("a | b", TokenType.Or), InlineData("a ^ b", TokenType.Xor),
	InlineData("~a", TokenType.Complement), InlineData("a << b", TokenType.LeftShift), InlineData("a >> b", TokenType.RightShift),
	InlineData("a && b", TokenType.AndAlso), InlineData("a || b", TokenType.OrElse), InlineData("!a", TokenType.Not), InlineData("a > b", TokenType.GreaterThan),
	InlineData("a >= b", TokenType.GreaterThanOrEquals), InlineData("a < b", TokenType.LesserThan), InlineData("a <= b", TokenType.LesserThanOrEquals),
	InlineData("a == b", TokenType.EqualsTo), InlineData("a != b", TokenType.NotEqualsTo), InlineData("a ?? b", TokenType.Coalesce),
	InlineData("a.b", TokenType.Resolve), InlineData("a?.b", TokenType.NullResolve)]
	public void ParseAlgExpression(string expression, TokenType type)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.True(node.Count >= 1 || node.Count <= 2, "Wrong child count of expression");

		Assert.Equal(type, node.Type);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("a", node[0].Value);
		if (node.Count > 1)
		{
			Assert.Equal(TokenType.Identifier, node[1].Type);
			Assert.Equal("b", node[1].Value);
		}
	}

	[Theory, InlineData("a?[b]", TokenType.Call), InlineData("a[b]", TokenType.Call), InlineData("a(b)", TokenType.Call)]
	public void ParseCallExpression(string expression, TokenType type)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.True(node.Count >= 1 || node.Count <= 2, "Wrong child count of expression");

		Assert.Equal(type, node.Type);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("a", node[0].Value);
		if (node.Count > 1)
		{
			Assert.Equal(TokenType.Arguments, node[1].Type);
			Assert.Equal(TokenType.Identifier, node[1][0].Type);
			Assert.Equal("b", node[1][0].Value);
		}
	}

	[Theory, InlineData("a b"), InlineData("a b c"), InlineData("a.ToString() b"), InlineData("1 a"), InlineData("a 1"), InlineData("1 1"), InlineData("\"\" 1"),
	InlineData("\"\" \"\""), InlineData("a + b c"), InlineData("a b + c"), InlineData("a + b  c + d"), InlineData("(a + b) c"), InlineData("(a + b) c + d"),
	InlineData("a[b] c"), InlineData("a[b] c[d]")]
	public void ParseWrongExpression(string expression)
	{
		Assert.Throws<ExpressionParserException>(() =>
			Parser.Parse(Tokenizer.Tokenize(expression))
		);
	}

	[Theory, InlineData("a.b + c", TokenType.Add), InlineData("+a.b", TokenType.Plus), InlineData("a.b / c", TokenType.Division),
	InlineData("a.b << c", TokenType.LeftShift), InlineData("a.b >= c", TokenType.GreaterThanOrEquals), InlineData("a.b == c", TokenType.EqualsTo),
	InlineData("a.b & c", TokenType.And), InlineData("a.b | c", TokenType.Or), InlineData("a.b ^ c", TokenType.Xor), InlineData("a.b && c", TokenType.AndAlso),
	InlineData("a.b ?? c", TokenType.Coalesce), InlineData("a.b ? c : d", TokenType.Conditional), InlineData("a ?? b + c", TokenType.Coalesce),
	InlineData("+a ?? b", TokenType.Coalesce), InlineData("a ?? b / c", TokenType.Coalesce), InlineData("a ?? b << c", TokenType.Coalesce),
	InlineData("a ?? b >= c", TokenType.Coalesce), InlineData("a ?? b == c", TokenType.Coalesce), InlineData("a ?? b & c", TokenType.Coalesce),
	InlineData("a ?? b | c", TokenType.Coalesce), InlineData("a ?? b ^ c", TokenType.Coalesce), InlineData("a ?? b && c", TokenType.Coalesce),
	InlineData("a ?? b ? c : d", TokenType.Conditional)]
	public void ParseAlgPrecedence(string expression, TokenType type)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(type, node.Type);
	}

	[Theory, InlineData("a ? b : c", TokenType.Identifier, TokenType.Identifier, TokenType.Identifier),
	InlineData("a ? Math.Max(1,1) : c", TokenType.Identifier, TokenType.Call, TokenType.Identifier),
	InlineData("a ? Math.Max(1,1) : Math.Max(1,1)", TokenType.Identifier, TokenType.Call, TokenType.Call),
	InlineData("a ? Math.Max(1 > 0 ? 1 : 2, 1 > 0 ? 1 : 2) : Math.Max(1 > 0 ? 1 : 2, 1 > 0 ? 1 : 2)", TokenType.Identifier, TokenType.Call, TokenType.Call),
	InlineData("a ? Math.Max(((1 > 0) ? (1) : (2)), (1 > 0) ? (1) : (2)) : Math.Max(((1 > 0) ? (1) : (2)), (1 > 0) ? (1) : (2))", TokenType.Identifier,
		TokenType.Call, TokenType.Call), InlineData("a ? x[1] : y", TokenType.Identifier, TokenType.Call, TokenType.Identifier),
	InlineData("a ? x?[1] : y", TokenType.Identifier, TokenType.Call, TokenType.Identifier),
	InlineData("a ? x[z ? m : n] : y", TokenType.Identifier, TokenType.Call, TokenType.Identifier),
	InlineData("a ? 1 : b + c", TokenType.Identifier, TokenType.Number, TokenType.Add),
	InlineData("!a ? ~b : +c", TokenType.Not, TokenType.Complement, TokenType.Plus),
	InlineData("a * b ? b / c : c % d", TokenType.Multiplication, TokenType.Division, TokenType.Modulo),
	InlineData("a ? b ? c : d : e", TokenType.Identifier, TokenType.Conditional, TokenType.Identifier)]
	public void ParseCondExpression(string expression, TokenType test, TokenType left, TokenType right)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Conditional, node.Type);
		Assert.Equal(3, node.Count);
		Assert.Equal(test, node[0].Type);
		Assert.Equal(left, node[1].Type);
		Assert.Equal(right, node[2].Type);
	}

	[Theory, InlineData("() => 0", typeof(Func<int>)), InlineData("a => 0", typeof(Func<int, int>)), InlineData("(a) => 0", typeof(Func<int, int>)),
	InlineData("(a,b) => 0", typeof(Func<int, int, int>))]
	public void ParseLambda(string expression, Type lambdaType)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));
		var lambdaSig = lambdaType.GetTypeInfo().GetDeclaredMethod("Invoke");

		Assert.Equal(TokenType.Lambda, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Arguments, node[0].Type);
		Assert.Equal(lambdaSig.GetParameters().Length, node[0].Count);
		Assert.Equal(TokenType.Number, node[1].Type);
	}

	[Theory, InlineData("(x)i"), InlineData("(x.y.z)i"), InlineData("(x)1"), InlineData("(x)\"text\""), InlineData("(x)a()"), InlineData("(x)a(b)"),
	InlineData("(x)(a + 1)"), InlineData("(x)~1"), InlineData("(x)!1")]
	public void ParseConvert(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Convert, node.Type);
		Assert.Equal(2, node.Count);
		if (node[0].Type != TokenType.Resolve)
			Assert.Equal(TokenType.Identifier, node[0].Type);
	}

	[Theory, InlineData("(int[])x"), InlineData("(x[])i"), InlineData("(x.y.z[])i"), InlineData("(x[][])i"), InlineData("(x[])a.b"), InlineData("(x[])(a)")]
	public void ParseConvertToArrayType(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Convert, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Call, node[0].Type);
		Assert.Equal("[", node[0].Value);
		Assert.Equal(0, node[0][1].Count);
	}

	[Theory, InlineData("new List<int> { 1, 2 }"), InlineData("new List<int[]> { }"), InlineData("new Dictionary<string, int[][]> { }"),
	InlineData("List<int[]>"), InlineData("typeof(List<int[]>)")]
	public void ParseGenericTypeWithArrayTypeArgument(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.NotNull(node);
	}

	[Theory, InlineData("a ? new List<int> { 1, 2 } : null"), InlineData("a ? b : new List<int> { 1, 2 }"),
	InlineData("a ? new int[] { 1, 2 } : new int[] { 3, 4 }")]
	public void ParseInitializerInConditional(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Conditional, node.Type);
		Assert.Equal(3, node.Count);
	}

	[Theory, InlineData("f(a < b, c > d)"), InlineData("f(a < b, c > 1)"), InlineData("f(a < b, c > -d)"), InlineData("f(a < b, c > !d)"),
	InlineData("f(a < b, c > \"d\")"), InlineData("f(a < b, c > new D())"), InlineData("f(a < b.c, d.e > f)")]
	public void ParseComparisonsNotGenericArguments(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(TokenType.Arguments, node[1].Type);
		Assert.Equal(2, node[1].Count);
		Assert.Equal(TokenType.LesserThan, node[1][0].Type);
		Assert.Equal(TokenType.GreaterThan, node[1][1].Type);
	}

	[Theory, InlineData("f(a<b, c>(d))", TokenType.Call), InlineData("f(a<b, c>.d)", TokenType.Resolve), InlineData("f(a<b, c>)", TokenType.Identifier),
	InlineData("f(a<b<c>>)", TokenType.Identifier), InlineData("f(a<b<c> >)", TokenType.Identifier)]
	public void ParseGenericArgumentsInCall(string expression, TokenType argumentType)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(1, node[1].Count);
		Assert.Equal(argumentType, node[1][0].Type);
	}

	[Theory, InlineData("a + (b + c)"), InlineData("a * (b + c)"), InlineData("a.b * (c + d)")]
	public void ParseEnclose(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Group, node[1].Type);
	}

	[Theory, InlineData("System.Func<A> < 1", 3, TokenType.LesserThan), InlineData("System<A>.Func<B,C>", 5, TokenType.Resolve),
	InlineData("MeMethod<A,B,C>()", 4, TokenType.Call), InlineData("MeMythod<Type1<Type2<B, C>, D, E, F>>() != 0", 8, TokenType.NotEqualsTo),
	InlineData("MyMethod<Type1<Type2<B, C>, D, E, Type3<E>>>() > 0", 9, TokenType.GreaterThan),
	InlineData("MyMethod<Type1<Type2<B, C>, D, E, Type3<Type4<E>>>>() < 0", 10, TokenType.LesserThan),
	InlineData("arg1.MyMethod<System.Random.Type1<Type2<B, C>, Ns.D, NS.E, Type3<System.Type4<E.F>>>>()", 17, TokenType.Call),
	InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceGenericMethod1<int>()", 12,
		TokenType.Call), InlineData("new TestGenericClass<int>.TestSubClass<int,int>(1,2,3).InstanceGenericMethod1<int>(1,2,3)", 7, TokenType.Call),
	InlineData("typeof(TestGenericClass<int>.TestSubClass<int,int>)", 5, TokenType.Typeof),
	InlineData("default(TestGenericClass<int>.TestSubClass<int,int>)", 5, TokenType.Default),
	InlineData("x is TestGenericClass<int>.TestSubClass<int,int>", 6, TokenType.Is), InlineData("x is TestGenericClass<int>", 3, TokenType.Is),
	InlineData("x as TestGenericClass<int>", 3, TokenType.As)]
	public void ParseClosedGenericType(string expression, int expectedIdentifiers, TokenType expectedTokenType)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));
		var countIdentifiers = default(Func<ParseTreeNode, int>);
		countIdentifiers = n => (n.Type == TokenType.Identifier ? 1 : 0) + n.Sum(sn => countIdentifiers(sn));
		var actual = countIdentifiers(node);
		var actualTokenType = node.Type;

		Assert.Equal(expectedIdentifiers, actual);
		Assert.Equal(expectedTokenType, actualTokenType);
	}

	[Theory, InlineData("Func<>", 1), InlineData("Func<,>", 2), InlineData("Func<,,>", 3), InlineData("Func<,,,>", 4), InlineData("Func<,,,,>", 5),
	InlineData("Func<,,,,,>", 6), InlineData("Func<,,,,,,>", 7), InlineData("Func<,,,,,,,>", 8), InlineData("Func<,,,,,,,,>", 9), InlineData("Func<,,,,,,,,,>", 10)]
	public void ParseOpenGenericType(string expression, int expectedGenericArguments)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(1, node.Count);
		Assert.Equal(TokenType.Arguments, node[0].Type);

		var arguments = node[0];
		var actual = arguments.Count;

		Assert.Equal(expectedGenericArguments, actual);
	}

	[Theory, InlineData("Func<A,>", 1, 1), InlineData("Func<, ,A>", 2, 1), InlineData("Func< ,A,A,>", 2, 2), InlineData("Func<,A,A,A,A>", 1, 4),
	InlineData("Func< ,A<B>,,, , >", 5, 1), InlineData("Func<A,A,A,,,, >", 4, 3), InlineData("Func<  ,,, ,A,A,A,A>", 4, 4),
	InlineData("Func<A,A,A,A,A,A,,A,A>", 1, 8), InlineData("Func<A,A,,   ,,,,, A,A >", 6, 4)]
	public void ParseHalfOpenGenericType(string expression, int expectedGenericParameters, int expectedGenericArguments)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));

		Assert.Equal(1, node.Count);
		Assert.Equal(TokenType.Arguments, node[0].Type);

		var arguments = node[0];
		var actualGenParams = arguments.Count(n => n.Value == string.Empty);
		var actualGenArgs = arguments.Count(n => n.Value != string.Empty);

		Assert.Equal(expectedGenericParameters, actualGenParams);
		Assert.Equal(expectedGenericArguments, actualGenArgs);
	}

	[Theory, InlineData("int?"), InlineData("System.Int32?"), InlineData("typeof(int?)"), InlineData("typeof(System.Int32?)"), InlineData("Func<A,int?>"),
	InlineData("Func<A,System.Int32?>"), InlineData("Func<int?,System.Int32?>"), InlineData("Func<System.Int32?,int?>")]
	public void ParseNullableType(string expression)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));
		var countNullableTypes = default(Func<ParseTreeNode, int>);
		countNullableTypes = n => (n.Type == TokenType.Identifier && n.Value == typeof(Nullable).Name ? 1 : 0) + n.Sum(sn => countNullableTypes(sn));
		var nullableTypes = countNullableTypes(node);

		Assert.True(nullableTypes > 0, "Nullable type is not found.");
	}
	[Fact]
	public void ParseDeepHierarchyCall()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("a.b.c.d()"));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Resolve, node[0].Type);
		Assert.Equal(2, node[0].Count);
		Assert.Equal(TokenType.Identifier, node[0][1].Type);
		Assert.Equal("d", node[0][1].Value);
	}

	[Fact]
	public void ParseEmptyCall()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("call()"));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("call", node[0].Value);
		Assert.Equal(TokenType.Arguments, node[1].Type);
		Assert.Equal(0, node[1].Count);
	}

	[Fact]
	public void ParseFourLambdaCalls()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("call(a => a + 1, (x,y) => null, () => true || false, (b) => b)"));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("call", node[0].Value);
		Assert.Equal(TokenType.Arguments, node[1].Type);
		var callArgumentsNode = node[1];
		Assert.Equal(4, callArgumentsNode.Count); // call arguments count

		var lambda1 = node[1][0];
		var lambda1Args = lambda1[0];
		var lambda1Body = lambda1[1];
		Assert.Equal(TokenType.Lambda, lambda1.Type);
		Assert.Equal(1, lambda1Args.Count); // lambda arguments count
		Assert.Equal(TokenType.Identifier, lambda1Args[0].Type);
		Assert.Equal("a", lambda1Args[0].Value);
		Assert.Equal(TokenType.Add, lambda1Body.Type); // lambda body type

		var lambda2 = node[1][1];
		var lambda2Args = lambda2[0];
		var lambda2Body = lambda2[1];
		Assert.Equal(TokenType.Lambda, lambda2.Type);
		Assert.Equal(2, lambda2Args.Count); // lambda arguments count
		Assert.Equal(TokenType.Identifier, lambda2Args[0].Type);
		Assert.Equal("x", lambda2Args[0].Value);
		Assert.Equal(TokenType.Identifier, lambda2Args[1].Type);
		Assert.Equal("y", lambda2Args[1].Value);
		Assert.Equal(TokenType.Identifier, lambda2Body.Type); // lambda body type
		Assert.Equal("null", lambda2Body.Value); // lambda body type

		var lambda3 = node[1][2];
		var lambda3Args = lambda3[0];
		var lambda3Body = lambda3[1];
		Assert.Equal(TokenType.Lambda, lambda3.Type);
		Assert.Equal(0, lambda3Args.Count); // lambda arguments count
		Assert.Equal(TokenType.OrElse, lambda3Body.Type); // lambda body type
		Assert.Equal(TokenType.Identifier, lambda3Body[0].Type);
		Assert.Equal("true", lambda3Body[0].Value);
		Assert.Equal(TokenType.Identifier, lambda3Body[1].Type);
		Assert.Equal("false", lambda3Body[1].Value);

		var lambda4 = node[1][3];
		var lambda4Args = lambda4[0];
		var lambda4Body = lambda4[1];
		Assert.Equal(TokenType.Lambda, lambda4.Type);
		Assert.Equal(1, lambda4Args.Count); // lambda arguments count
		Assert.Equal(TokenType.Identifier, lambda4Args[0].Type);
		Assert.Equal("b", lambda4Args[0].Value);
		Assert.Equal(TokenType.Identifier, lambda4Body.Type); // lambda body type
		Assert.Equal("b", lambda4Body.Value); // lambda body type
	}
	[Fact]
	public void ParseTwoArgsCall()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("call(a, b)"));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("call", node[0].Value);
		Assert.Equal(TokenType.Arguments, node[1].Type);
		Assert.Equal(2, node[1].Count);
		Assert.Equal(TokenType.Identifier, node[1][0].Type);
		Assert.Equal(TokenType.Identifier, node[1][1].Type);
	}
	[Fact]
	public void ParseTwoNamedArgsCall()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("call(arg1: a, arg2: b)"));

		Assert.Equal(TokenType.Call, node.Type);
		Assert.Equal(2, node.Count);
		Assert.Equal(TokenType.Identifier, node[0].Type);
		Assert.Equal("call", node[0].Value);
		Assert.Equal(TokenType.Arguments, node[1].Type);
		Assert.Equal(2, node[1].Count);
		Assert.Equal(TokenType.Colon, node[1][0].Type);
		Assert.Equal(TokenType.Colon, node[1][1].Type);
		Assert.Equal(2, node[1][0].Count);
		Assert.Equal(2, node[1][1].Count);
		Assert.Equal(TokenType.Identifier, node[1][0][0].Type);
		Assert.Equal(TokenType.Identifier, node[1][0][1].Type);
		Assert.Equal("arg1", node[1][0][0].Value);
		Assert.Equal("a", node[1][0][1].Value);
		Assert.Equal(TokenType.Identifier, node[1][1][0].Type);
		Assert.Equal(TokenType.Identifier, node[1][1][1].Type);
		Assert.Equal("arg2", node[1][1][0].Value);
		Assert.Equal("b", node[1][1][1].Value);
	}

	private static void AssertConstant(object expected, object actual)
	{
		Assert.NotNull(actual);
		Assert.Equal(expected.GetType(), actual.GetType());
		Assert.Equal(expected, actual);
	}

	// P1
	[Theory, InlineData("(a) - b", TokenType.Subtract), InlineData("(a) + b", TokenType.Add), InlineData("(a)-b", TokenType.Subtract), InlineData("(a.b) - c", TokenType.Subtract),
	InlineData("(a) * b", TokenType.Multiplication), InlineData("(a)b", TokenType.Convert), InlineData("(a)!b", TokenType.Convert), InlineData("(a)~b", TokenType.Convert),
	InlineData("(a)(b)", TokenType.Convert), InlineData("(a)1", TokenType.Convert), InlineData("(a)\"s\"", TokenType.Convert), InlineData("(a)new b()", TokenType.Convert),
	InlineData("(a)typeof(b)", TokenType.Convert), InlineData("(a)default(b)", TokenType.Convert), InlineData("(a)true", TokenType.Convert),
	InlineData("(a)null", TokenType.Convert), InlineData("(int)-b", TokenType.Convert), InlineData("(int)+b", TokenType.Convert), InlineData("(System.Int32)b", TokenType.Convert),
	InlineData("(a[])b", TokenType.Convert)]
	public void ParseCastOrGroup(string expression, TokenType expected)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));
		Assert.Equal(expected, node.Type);
	}

	[Theory, InlineData("(x) - 1", 9), InlineData("(x) + 1", 11), InlineData("(x)-1", 9), InlineData("(x) * 2", 20), InlineData("(int)-x", -10), InlineData("(Int32)x", 10)]
	public void EvaluateCastOrGroup(string expression, int expected)
	{
		var actual = CSharpExpression.ParseFunc<int, int>(expression, "x").Compile().Invoke(10);
		Assert.Equal(expected, actual);
	}

	// P2
	[Theory, InlineData("(1 + 2]"), InlineData("(1 + 2}"), InlineData("a[0)"), InlineData("a[0}"), InlineData("a?[0)"), InlineData("f(1}"), InlineData("f(1]"),
	InlineData("new a(1]"), InlineData("new a(){ b = 1 )"), InlineData("new a(){ b = 1 ]")]
	public void ParseMismatchedBrackets(string expression)
	{
		Assert.Throws<ExpressionParserException>(() => Parser.Parse(Tokenizer.Tokenize(expression)));
	}

	// P3
	[Fact]
	public void ParseCurriedLambda()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("x => y => x"));
		Assert.Equal(TokenType.Lambda, node.Type);
		Assert.Equal(TokenType.Arguments, node[0].Type);
		Assert.Equal("x", node[0][0].Value);
		Assert.Equal(TokenType.Lambda, node[1].Type);
		Assert.Equal(TokenType.Arguments, node[1][0].Type);
		Assert.Equal("y", node[1][0][0].Value);
		Assert.Equal(TokenType.Identifier, node[1][1].Type);
		Assert.Equal("x", node[1][1].Value);
	}

	[Fact]
	public void EvaluateCurriedLambda()
	{
		var add = CSharpExpression.ParseFunc<Func<int, Func<int, int>>>("x => y => x - y").Compile().Invoke();
		Assert.Equal(7, add(10)(3));
	}

	// P4
	[Fact]
	public void ParsePowerIsRightAssociative()
	{
		var node = Parser.Parse(Tokenizer.Tokenize("a ** b ** c"));
		Assert.Equal(TokenType.Power, node.Type);
		Assert.Equal("a", node[0].Value);
		Assert.Equal(TokenType.Power, node[1].Type);
		Assert.Equal("b", node[1][0].Value);
		Assert.Equal("c", node[1][1].Value);
	}

	[Theory, InlineData("a * b ** c", TokenType.Multiplication), InlineData("a ** b * c", TokenType.Multiplication), InlineData("a / b ** c", TokenType.Division),
	InlineData("a ** b + c", TokenType.Add)]
	public void ParsePowerPrecedence(string expression, TokenType expected)
	{
		var node = Parser.Parse(Tokenizer.Tokenize(expression));
		Assert.Equal(expected, node.Type);
	}

	[Theory, InlineData("2 ** 3 ** 2", 512d), InlineData("2 * 3 ** 2", 18d), InlineData("3 ** 2 * 2", 18d)]
	public void EvaluatePower(string expression, double expected)
	{
		var actual = CSharpExpression.Evaluate<double>(expression);
		Assert.Equal(expected, actual);
	}

	// P5
	[Theory, InlineData(5000), InlineData(100000)]
	public void ParseTooDeepExpression(int depth)
	{
		var expression = new string('(', depth) + "1" + new string(')', depth);
		Assert.Throws<ExpressionParserException>(() => Parser.Parse(Tokenizer.Tokenize(expression)));
	}

	[Fact]
	public void ParseTooDeepUnaryExpression()
	{
		var expression = new string('-', 100000) + "1";
		Assert.Throws<ExpressionParserException>(() => Parser.Parse(Tokenizer.Tokenize(expression)));
	}

	[Fact]
	public void ParseDeepButAllowedExpression()
	{
		var expression = new string('(', 100) + "1" + new string(')', 100);
		Assert.Equal(1, CSharpExpression.Evaluate<int>(expression));
	}

	// L1
	[Fact]
	public void EvaluateExponentOnlyLiterals()
	{
		AssertConstant(1e5, CSharpExpression.Evaluate<object>("1e5"));
		AssertConstant(1e-3, CSharpExpression.Evaluate<object>("1e-3"));
		AssertConstant(2E3, CSharpExpression.Evaluate<object>("2E3"));
		AssertConstant(2e+3, CSharpExpression.Evaluate<object>("2e+3"));
		AssertConstant(1e5f, CSharpExpression.Evaluate<object>("1e5f"));
		AssertConstant(1e5m, CSharpExpression.Evaluate<object>("1e5m"));
		AssertConstant(1e5 + 3, CSharpExpression.Evaluate<object>("1e5+3"));
	}

	// L2
	[Fact]
	public void EvaluateIntegerLiteralsWidening()
	{
		AssertConstant(2147483647, CSharpExpression.Evaluate<object>("2147483647"));
		AssertConstant(2147483648u, CSharpExpression.Evaluate<object>("2147483648"));
		AssertConstant(4294967295u, CSharpExpression.Evaluate<object>("4294967295"));
		AssertConstant(4294967296L, CSharpExpression.Evaluate<object>("4294967296"));
		AssertConstant(9007199254740993L, CSharpExpression.Evaluate<object>("9007199254740993"));
		AssertConstant(9223372036854775807L, CSharpExpression.Evaluate<object>("9223372036854775807"));
		AssertConstant(9223372036854775808UL, CSharpExpression.Evaluate<object>("9223372036854775808"));
		AssertConstant(18446744073709551615UL, CSharpExpression.Evaluate<object>("18446744073709551615"));

		AssertConstant(3000000000u, CSharpExpression.Evaluate<object>("3000000000u"));
		AssertConstant(5000000000UL, CSharpExpression.Evaluate<object>("5000000000u"));
		AssertConstant(1L, CSharpExpression.Evaluate<object>("1L"));
		AssertConstant(9223372036854775808UL, CSharpExpression.Evaluate<object>("9223372036854775808L"));
		AssertConstant(1UL, CSharpExpression.Evaluate<object>("1ul"));
		AssertConstant(1UL, CSharpExpression.Evaluate<object>("1lu"));
	}

	[Theory, InlineData("18446744073709551616"), InlineData("18446744073709551616u"), InlineData("18446744073709551616L"), InlineData("18446744073709551616ul"),
	InlineData("0x10000000000000000")]
	public void EvaluateIntegerLiteralOutOfRange(string expression)
	{
		Assert.Throws<ExpressionParserException>(() => CSharpExpression.Evaluate<object>(expression));
	}

	// T4
	[Fact]
	public void EvaluateHexLiterals()
	{
		AssertConstant(0x10, CSharpExpression.Evaluate<object>("0x10"));
		AssertConstant(0x1F, CSharpExpression.Evaluate<object>("0X1f"));
		AssertConstant(0x1e, CSharpExpression.Evaluate<object>("0x1e"));
		AssertConstant(0x7FFFFFFF, CSharpExpression.Evaluate<object>("0x7FFFFFFF"));
		AssertConstant(0xFFFFFFFF, CSharpExpression.Evaluate<object>("0xFFFFFFFF"));
		AssertConstant(0x100000000, CSharpExpression.Evaluate<object>("0x100000000"));
		AssertConstant(0xFFFFFFFFFFFFFFFF, CSharpExpression.Evaluate<object>("0xFFFFFFFFFFFFFFFF"));
		AssertConstant(0x10u, CSharpExpression.Evaluate<object>("0x10u"));
		AssertConstant(0x10L, CSharpExpression.Evaluate<object>("0x10L"));
		AssertConstant(0x10UL, CSharpExpression.Evaluate<object>("0x10ul"));
		AssertConstant(0x1e + 1, CSharpExpression.Evaluate<object>("0x1e+1"));
	}

	// T5
	[Theory, InlineData("@\"c:\\temp\"", "c:\\temp"), InlineData("@\"say \"\"hi\"\"\"", "say \"hi\""), InlineData("@\"\"", ""), InlineData("@\"\\\"", "\\"),
	InlineData("@\"a\nb\"", "a\nb"), InlineData("@\"\\n\\t\\u0041\"", "\\n\\t\\u0041")]
	public void EvaluateVerbatimStringLiterals(string expression, string expected)
	{
		AssertConstant(expected, CSharpExpression.Evaluate<object>(expression));
	}

	// T6
	[Fact]
	public void EvaluateConditionalWithShortNumber()
	{
		Assert.Equal(0.5, CSharpExpression.ParseFunc<bool, double>("x?.5:1", "x").Compile().Invoke(true));
		Assert.Equal(1, CSharpExpression.ParseFunc<bool, double>("x?.5:1", "x").Compile().Invoke(false));
	}

	// T1
	[Fact]
	public void EvaluateIdentifiersStartingWithKeyword()
	{
		var actual = CSharpExpression.ParseFunc<int, int, int>("is_valid + new_value", "is_valid", "new_value").Compile().Invoke(1, 2);
		Assert.Equal(3, actual);
	}

	// L3
	[Theory, InlineData(@"""\a""", "\a"), InlineData(@"""\v""", "\v"), InlineData(@"""\U00000041""", "A"), InlineData(@"""\U0001F600""", "\U0001F600"),
	InlineData(@"""\0""", "\0"), InlineData(@"""\0ab""", "\0ab"), InlineData(@"""\0123""", "\0" + "123"), InlineData(@"""\x4""", "\x4"), InlineData(@"""\x41""", "A"),
	InlineData(@"""\x041""", "A"), InlineData(@"""\x0041""", "A"), InlineData(@"""\x00410""", "A0"), InlineData(@"""\x41g""", "Ag"), InlineData(@"""\u0041""", "A"),
	InlineData(@"""it\'s""", "it's"), InlineData(@"""\""""", "\""), InlineData(@"""\\""", "\\"), InlineData(@"""\n\r\t\b\f""", "\n\r\t\b\f"), InlineData("\"a\tb\"", "a\tb")]
	public void EvaluateStringEscapes(string expression, string expected)
	{
		AssertConstant(expected, CSharpExpression.Evaluate<object>(expression));
	}

	[Theory, InlineData(@"'\0'", '\0'), InlineData(@"'\a'", '\a'), InlineData(@"'\v'", '\v'), InlineData(@"'\''", '\''), InlineData(@"'""'", '"'), InlineData(@"'\x41'", 'A'),
	InlineData(@"'\u0041'", 'A'), InlineData(@"'\U00000041'", 'A')]
	public void EvaluateCharEscapes(string expression, char expected)
	{
		AssertConstant(expected, CSharpExpression.Evaluate<object>(expression));
	}

	[Theory, InlineData(@"""\1"""), InlineData(@"""\101"""), InlineData(@"""\9"""), InlineData(@"""\q"""), InlineData(@"""\x"""), InlineData(@"""\xg"""), InlineData(@"""\u004"""),
	InlineData(@"""\U0000004"""), InlineData(@"""\U00110000"""), InlineData(@"'\U0001F600'"), InlineData(@"'ab'"), InlineData(@"''")]
	public void EvaluateWrongEscapes(string expression)
	{
		Assert.Throws<ExpressionParserException>(() => CSharpExpression.Evaluate<object>(expression));
	}
}
