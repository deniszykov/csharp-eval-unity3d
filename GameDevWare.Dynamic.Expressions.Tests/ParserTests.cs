
using System;
using System.Globalization;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

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

			Assert.True(node.Nodes.Count >= 1 || node.Nodes.Count <= 2, "Wrong child count of expression");

			Assert.Equal(type, node.Type);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("a", node.Nodes[0].Value);
			if (node.Nodes.Count > 1)
			{
				Assert.Equal(TokenType.Identifier, node.Nodes[1].Type);
				Assert.Equal("b", node.Nodes[1].Value);
			}
		}

		[Theory]
		[InlineData("a?[b]", TokenType.Call)]
		[InlineData("a[b]", TokenType.Call)]
		[InlineData("a(b)", TokenType.Call)]
		public void ParseCallExpression(string expression, TokenType type)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.True(node.Nodes.Count >= 1 || node.Nodes.Count <= 2, "Wrong child count of expression");

			Assert.Equal(type, node.Type);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("a", node.Nodes[0].Value);
			if (node.Nodes.Count > 1)
			{
				Assert.Equal(TokenType.Arguments, node.Nodes[1].Type);
				Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[0].Type);
				Assert.Equal("b", node.Nodes[1].Nodes[0].Value);
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
			Assert.Equal(3, node.Nodes.Count);
			Assert.Equal(test, node.Nodes[0].Type);
			Assert.Equal(left, node.Nodes[1].Type);
			Assert.Equal(right, node.Nodes[2].Type);
		}

		[Fact]
		public void ParseEmptyCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call()"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("call", node.Nodes[0].Value);
			Assert.Equal(TokenType.Arguments, node.Nodes[1].Type);
			Assert.Equal(0, node.Nodes[1].Nodes.Count);
		}
		[Fact]
		public void ParseTwoArgsCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call(a, b)"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("call", node.Nodes[0].Value);
			Assert.Equal(TokenType.Arguments, node.Nodes[1].Type);
			Assert.Equal(2, node.Nodes[1].Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[0].Type);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[1].Type);
		}
		[Fact]
		public void ParseTwoNamedArgsCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call(arg1: a, arg2: b)"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("call", node.Nodes[0].Value);
			Assert.Equal(TokenType.Arguments, node.Nodes[1].Type);
			Assert.Equal(2, node.Nodes[1].Nodes.Count);
			Assert.Equal(TokenType.Colon, node.Nodes[1].Nodes[0].Type);
			Assert.Equal(TokenType.Colon, node.Nodes[1].Nodes[1].Type);
			Assert.Equal(2, node.Nodes[1].Nodes[0].Nodes.Count);
			Assert.Equal(2, node.Nodes[1].Nodes[1].Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[0].Nodes[0].Type);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[0].Nodes[1].Type);
			Assert.Equal("arg1", node.Nodes[1].Nodes[0].Nodes[0].Value);
			Assert.Equal("a", node.Nodes[1].Nodes[0].Nodes[1].Value);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[1].Nodes[0].Type);
			Assert.Equal(TokenType.Identifier, node.Nodes[1].Nodes[1].Nodes[1].Type);
			Assert.Equal("arg2", node.Nodes[1].Nodes[1].Nodes[0].Value);
			Assert.Equal("b", node.Nodes[1].Nodes[1].Nodes[1].Value);
		}
		[Fact]
		public void ParseDeepHierarchyCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("a.b.c.d()"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Resolve, node.Nodes[0].Type);
			Assert.Equal(2, node.Nodes[0].Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Nodes[1].Type);
			Assert.Equal("d", node.Nodes[0].Nodes[1].Value);
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
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Arguments, node.Nodes[0].Type);
			Assert.Equal(lambdaSig.GetParameters().Length, node.Nodes[0].Nodes.Count);
			Assert.Equal(TokenType.Number, node.Nodes[1].Type);
		}

		[Fact]
		public void ParseFourLambdaCalls()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call(a => a + 1, (x,y) => null, () => true || false, (b) => b)"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
			Assert.Equal("call", node.Nodes[0].Value);
			Assert.Equal(TokenType.Arguments, node.Nodes[1].Type);
			var callArgumentsNode = node.Nodes[1];
			Assert.Equal(4, callArgumentsNode.Nodes.Count); // call arguments count

			var lambda1 = node.Nodes[1].Nodes[0];
			var lambda1Args = lambda1.Nodes[0];
			var lambda1Body = lambda1.Nodes[1];
			Assert.Equal(TokenType.Lambda, lambda1.Type);
			Assert.Equal(1, lambda1Args.Nodes.Count); // lambda arguments count
			Assert.Equal(TokenType.Identifier, lambda1Args.Nodes[0].Type);
			Assert.Equal("a", lambda1Args.Nodes[0].Value);
			Assert.Equal(TokenType.Add, lambda1Body.Type); // lambda body type

			var lambda2 = node.Nodes[1].Nodes[1];
			var lambda2Args = lambda2.Nodes[0];
			var lambda2Body = lambda2.Nodes[1];
			Assert.Equal(TokenType.Lambda, lambda2.Type);
			Assert.Equal(2, lambda2Args.Nodes.Count); // lambda arguments count
			Assert.Equal(TokenType.Identifier, lambda2Args.Nodes[0].Type);
			Assert.Equal("x", lambda2Args.Nodes[0].Value);
			Assert.Equal(TokenType.Identifier, lambda2Args.Nodes[1].Type);
			Assert.Equal("y", lambda2Args.Nodes[1].Value);
			Assert.Equal(TokenType.Identifier, lambda2Body.Type); // lambda body type
			Assert.Equal("null", lambda2Body.Value); // lambda body type

			var lambda3 = node.Nodes[1].Nodes[2];
			var lambda3Args = lambda3.Nodes[0];
			var lambda3Body = lambda3.Nodes[1];
			Assert.Equal(TokenType.Lambda, lambda3.Type);
			Assert.Equal(0, lambda3Args.Nodes.Count); // lambda arguments count
			Assert.Equal(TokenType.OrElse, lambda3Body.Type); // lambda body type
			Assert.Equal(TokenType.Identifier, lambda3Body.Nodes[0].Type);
			Assert.Equal("true", lambda3Body.Nodes[0].Value);
			Assert.Equal(TokenType.Identifier, lambda3Body.Nodes[1].Type);
			Assert.Equal("false", lambda3Body.Nodes[1].Value);

			var lambda4 = node.Nodes[1].Nodes[3];
			var lambda4Args = lambda4.Nodes[0];
			var lambda4Body = lambda4.Nodes[1];
			Assert.Equal(TokenType.Lambda, lambda4.Type);
			Assert.Equal(1, lambda4Args.Nodes.Count); // lambda arguments count
			Assert.Equal(TokenType.Identifier, lambda4Args.Nodes[0].Type);
			Assert.Equal("b", lambda4Args.Nodes[0].Value);
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
			Assert.Equal(2, node.Nodes.Count);
			if (node.Nodes[0].Type != TokenType.Resolve)
				Assert.Equal(TokenType.Identifier, node.Nodes[0].Type);
		}

		[Theory]
		[InlineData("a + (b + c)")]
		[InlineData("a * (b + c)")]
		[InlineData("a.b * (c + d)")]
		public void ParseEnclose(string expression)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(2, node.Nodes.Count);
			Assert.Equal(TokenType.Group, node.Nodes[1].Type);

		}
	}
}
