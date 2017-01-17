
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;
using Xunit.Sdk;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ParserTests
	{
		[Theory]
		[InlineData("1", 1)]
		[InlineData("0.0001f", 0.0001f)]
		[InlineData("01d", 01d)]
		[InlineData("0.1d", 0.1d)]
		[InlineData("1u", 1u)]
		[InlineData("1ul", 1ul)]
		[InlineData("1L", 1L)]
		[InlineData("9.15E+09", 9.15E+09)]
		[InlineData("9.15E09", 9.15E09)]
		[InlineData("9.15E-09", 9.15E-09)]
		[InlineData("9.15e+09", 9.15e+09)]
		[InlineData("9.15e09", 9.15e09)]
		[InlineData("9.15e-09", 9.15e-09)]
		[InlineData("9e-09", 9e-09)]
		//[InlineData(".01", .01)]
		//[InlineData(".9e-09", .9e-09)]
		public void ParseNumbersTest(string expression, object expected)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			var valueStr = node.Value.TrimEnd('d', 'f', 'u', 'l', 'm');
			Assert.Equal(TokenType.Number, node.Type);
			Assert.Equal(expected, Convert.ChangeType(valueStr, expected.GetType(), CultureInfo.InvariantCulture));
		}

		[Theory]
		[InlineData("a + b", TokenType.Add)]
		[InlineData("+a", TokenType.Plus)]
		[InlineData("a - b", TokenType.Subtract)]
		[InlineData("-a", TokenType.Minus)]
		[InlineData("a * b", TokenType.Mul)]
		[InlineData("a ** b", TokenType.Pow)]
		[InlineData("a / b", TokenType.Div)]
		[InlineData("a % b", TokenType.Mod)]
		[InlineData("a & b", TokenType.And)]
		[InlineData("a | b", TokenType.Or)]
		[InlineData("a ^ b", TokenType.Xor)]
		[InlineData("~a", TokenType.Compl)]
		[InlineData("a << b", TokenType.Lshift)]
		[InlineData("a >> b", TokenType.Rshift)]
		[InlineData("a && b", TokenType.AndAlso)]
		[InlineData("a || b", TokenType.OrElse)]
		[InlineData("!a", TokenType.Not)]
		[InlineData("a > b", TokenType.Gt)]
		[InlineData("a >= b", TokenType.Gte)]
		[InlineData("a < b", TokenType.Lt)]
		[InlineData("a <= b", TokenType.Lte)]
		[InlineData("a == b", TokenType.Eq)]
		[InlineData("a != b", TokenType.Neq)]
		[InlineData("a ?? b", TokenType.Coalesce)]
		[InlineData("a.b", TokenType.Resolve)]
		[InlineData("a?.b", TokenType.NullResolve)]
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

		[Theory]
		[InlineData("a?[b]", TokenType.Call)]
		[InlineData("a[b]", TokenType.Call)]
		[InlineData("a(b)", TokenType.Call)]
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

		[Theory]
		[InlineData("a.b + c", TokenType.Add)]
		[InlineData("+a.b", TokenType.Plus)]
		[InlineData("a.b / c", TokenType.Div)]
		[InlineData("a.b << c", TokenType.Lshift)]
		[InlineData("a.b >= c", TokenType.Gte)]
		[InlineData("a.b == c", TokenType.Eq)]
		[InlineData("a.b & c", TokenType.And)]
		[InlineData("a.b | c", TokenType.Or)]
		[InlineData("a.b ^ c", TokenType.Xor)]
		[InlineData("a.b && c", TokenType.AndAlso)]
		[InlineData("a.b ?? c", TokenType.Coalesce)]
		[InlineData("a.b ? c : d", TokenType.Cond)]
		[InlineData("a ?? b + c", TokenType.Coalesce)]
		[InlineData("+a ?? b", TokenType.Coalesce)]
		[InlineData("a ?? b / c", TokenType.Coalesce)]
		[InlineData("a ?? b << c", TokenType.Coalesce)]
		[InlineData("a ?? b >= c", TokenType.Coalesce)]
		[InlineData("a ?? b == c", TokenType.Coalesce)]
		[InlineData("a ?? b & c", TokenType.Coalesce)]
		[InlineData("a ?? b | c", TokenType.Coalesce)]
		[InlineData("a ?? b ^ c", TokenType.Coalesce)]
		[InlineData("a ?? b && c", TokenType.Coalesce)]
		[InlineData("a ?? b ? c : d", TokenType.Cond)]
		public void ParseAlgPrecedence(string expression, TokenType type)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(type, node.Type);
		}

		[Theory]
		[InlineData("a ? b : c", TokenType.Identifier, TokenType.Identifier, TokenType.Identifier)]
		[InlineData("a ? 1 : b + c", TokenType.Identifier, TokenType.Number, TokenType.Add)]
		[InlineData("!a ? ~b : +c", TokenType.Not, TokenType.Compl, TokenType.Plus)]
		[InlineData("a * b ? b / c : c % d", TokenType.Mul, TokenType.Div, TokenType.Mod)]
		[InlineData("a ? b ? c : d : e", TokenType.Identifier, TokenType.Cond, TokenType.Identifier)]
		public void ParseCondExpression(string expression, TokenType test, TokenType left, TokenType right)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(node.Type, TokenType.Cond);
			Assert.Equal(3, node.Count);
			Assert.Equal(test, node[0].Type);
			Assert.Equal(left, node[1].Type);
			Assert.Equal(right, node[2].Type);
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

		[Theory]
		[InlineData("() => 0", typeof(Func<int>))]
		[InlineData("a => 0", typeof(Func<int, int>))]
		[InlineData("(a) => 0", typeof(Func<int, int>))]
		[InlineData("(a,b) => 0", typeof(Func<int, int, int>))]
		public void ParseLambda(string expression, Type lambdaType)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));
			var lambdaSig = lambdaType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);

			Assert.Equal(TokenType.Lambda, node.Type);
			Assert.Equal(2, node.Count);
			Assert.Equal(TokenType.Arguments, node[0].Type);
			Assert.Equal(lambdaSig.GetParameters().Length, node[0].Count);
			Assert.Equal(TokenType.Number, node[1].Type);
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

		[Theory]
		[InlineData("(x)i")]
		[InlineData("(x.y.z)i")]
		[InlineData("(x)1")]
		[InlineData("(x)\"text\"")]
		[InlineData("(x)a()")]
		[InlineData("(x)a(b)")]
		[InlineData("(x)(a + 1)")]
		[InlineData("(x)+a")]
		[InlineData("(x)+1")]
		[InlineData("(x)~1")]
		[InlineData("(x)!1")]
		public void ParseConvert(string expression)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(TokenType.Convert, node.Type);
			Assert.Equal(2, node.Count);
			if (node[0].Type != TokenType.Resolve)
				Assert.Equal(TokenType.Identifier, node[0].Type);
		}

		[Theory]
		[InlineData("a + (b + c)")]
		[InlineData("a * (b + c)")]
		[InlineData("a.b * (c + d)")]
		public void ParseEnclose(string expression)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(2, node.Count);
			Assert.Equal(TokenType.Group, node[1].Type);

		}

		[Theory]
		[InlineData("System.Func<A> < 1", 3, TokenType.Lt)]
		[InlineData("System<A>.Func<B,C>", 5, TokenType.Resolve)]
		[InlineData("MeMethod<A,B,C>()", 4, TokenType.Call)]
		[InlineData("MeMythod<Type1<Type2<B, C>, D, E, F>>() != 0", 8, TokenType.Neq)]
		[InlineData("MyMethod<Type1<Type2<B, C>, D, E, Type3<E>>>() > 0", 9, TokenType.Gt)]
		[InlineData("MyMethod<Type1<Type2<B, C>, D, E, Type3<Type4<E>>>>() < 0", 10, TokenType.Lt)]
		[InlineData("arg1.MyMethod<System.Random.Type1<Type2<B, C>, Ns.D, NS.E, Type3<System.Type4<E.F>>>>()", 17, TokenType.Call)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceGenericMethod1<int>()", 12, TokenType.Call)]
		[InlineData("new TestGenericClass<int>.TestSubClass<int,int>(1,2,3).InstanceGenericMethod1<int>(1,2,3)", 7, TokenType.Call)]
		[InlineData("typeof(TestGenericClass<int>.TestSubClass<int,int>)", 5, TokenType.Typeof)]
		[InlineData("default(TestGenericClass<int>.TestSubClass<int,int>)", 5, TokenType.Default)]
		[InlineData("x is TestGenericClass<int>.TestSubClass<int,int>", 6, TokenType.Is)]
		[InlineData("x is TestGenericClass<int>", 3, TokenType.Is)]
		[InlineData("x as TestGenericClass<int>", 3, TokenType.As)]
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

		[Theory]
		[InlineData("Func<>", 1)]
		[InlineData("Func<,>", 2)]
		[InlineData("Func<,,>", 3)]
		[InlineData("Func<,,,>", 4)]
		[InlineData("Func<,,,,>", 5)]
		[InlineData("Func<,,,,,>", 6)]
		[InlineData("Func<,,,,,,>", 7)]
		[InlineData("Func<,,,,,,,>", 8)]
		[InlineData("Func<,,,,,,,,>", 9)]
		[InlineData("Func<,,,,,,,,,>", 10)]
		public void ParseOpenGenericType(string expression, int expectedGenericArguments)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(1, node.Count);
			Assert.Equal(TokenType.Arguments, node[0].Type);

			var arguments = node[0];
			var actual = arguments.Count;

			Assert.Equal(expectedGenericArguments, actual);
		}

		[Theory]
		[InlineData("Func<A,>", 1, 1)]
		[InlineData("Func<, ,A>", 2, 1)]
		[InlineData("Func< ,A,A,>", 2, 2)]
		[InlineData("Func<,A,A,A,A>", 1, 4)]
		[InlineData("Func< ,A<B>,,, , >", 5, 1)]
		[InlineData("Func<A,A,A,,,, >", 4, 3)]
		[InlineData("Func<  ,,, ,A,A,A,A>", 4, 4)]
		[InlineData("Func<A,A,A,A,A,A,,A,A>", 1, 8)]
		[InlineData("Func<A,A,,   ,,,,, A,A >", 6, 4)]
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

		[Theory]
		[InlineData("int?")]
		[InlineData("System.Int32?")]
		[InlineData("typeof(int?)")]
		[InlineData("typeof(System.Int32?)")]
		[InlineData("Func<A,int?>")]
		[InlineData("Func<A,System.Int32?>")]
		[InlineData("Func<int?,System.Int32?>")]
		[InlineData("Func<System.Int32?,int?>")]
		public void ParseNullableType(string expression)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));
			var countNullableTypes = default(Func<ParseTreeNode, int>);
			countNullableTypes = n => (n.Type == TokenType.Identifier && n.Value == typeof(Nullable).Name ? 1 : 0) + n.Sum(sn => countNullableTypes(sn));
			var nullableTypes = countNullableTypes(node);

			Assert.True(nullableTypes > 0, "Nullable type is not found.");
		}
	}
}
