
using System;
using System.Globalization;
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
		public void ParseAlgExpression(string expression, TokenType type)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.True(node.Childs.Count >= 1 || node.Childs.Count <= 2, "Wrong child count of expression");

			Assert.Equal(type, node.Type);
			Assert.Equal(TokenType.Identifier, node.Childs[0].Type);
			Assert.Equal("a", node.Childs[0].Value);
			if (node.Childs.Count > 1)
			{
				Assert.Equal(TokenType.Identifier, node.Childs[1].Type);
				Assert.Equal("b", node.Childs[1].Value);
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
			Assert.Equal(3, node.Childs.Count);
			Assert.Equal(test, node.Childs[0].Type);
			Assert.Equal(left, node.Childs[1].Type);
			Assert.Equal(right, node.Childs[2].Type);
		}

		[Fact]
		public void ParseEmptyCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call()"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[0].Type);
			Assert.Equal("call", node.Childs[0].Value);
			Assert.Equal(TokenType.Arguments, node.Childs[1].Type);
			Assert.Equal(0, node.Childs[1].Childs.Count);
		}
		[Fact]
		public void ParseTwoArgsCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call(a, b)"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[0].Type);
			Assert.Equal("call", node.Childs[0].Value);
			Assert.Equal(TokenType.Arguments, node.Childs[1].Type);
			Assert.Equal(2, node.Childs[1].Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[0].Type);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[1].Type);
		}
		[Fact]
		public void ParseTwoNamedArgsCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("call(arg1: a, arg2: b)"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[0].Type);
			Assert.Equal("call", node.Childs[0].Value);
			Assert.Equal(TokenType.Arguments, node.Childs[1].Type);
			Assert.Equal(2, node.Childs[1].Childs.Count);
			Assert.Equal(TokenType.Colon, node.Childs[1].Childs[0].Type);
			Assert.Equal(TokenType.Colon, node.Childs[1].Childs[1].Type);
			Assert.Equal(2, node.Childs[1].Childs[0].Childs.Count);
			Assert.Equal(2, node.Childs[1].Childs[1].Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[0].Childs[0].Type);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[0].Childs[1].Type);
			Assert.Equal("arg1", node.Childs[1].Childs[0].Childs[0].Value);
			Assert.Equal("a", node.Childs[1].Childs[0].Childs[1].Value);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[1].Childs[0].Type);
			Assert.Equal(TokenType.Identifier, node.Childs[1].Childs[1].Childs[1].Type);
			Assert.Equal("arg2", node.Childs[1].Childs[1].Childs[0].Value);
			Assert.Equal("b", node.Childs[1].Childs[1].Childs[1].Value);
		}
		[Fact]
		public void ParseDeepHierarchyCall()
		{
			var node = Parser.Parse(Tokenizer.Tokenize("a.b.c.d()"));

			Assert.Equal(TokenType.Call, node.Type);
			Assert.Equal(2, node.Childs.Count);
			Assert.Equal(TokenType.Resolve, node.Childs[0].Type);
			Assert.Equal(2, node.Childs[0].Childs.Count);
			Assert.Equal(TokenType.Identifier, node.Childs[0].Childs[1].Type);
			Assert.Equal("d", node.Childs[0].Childs[1].Value);
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
			Assert.Equal(2, node.Childs.Count);
			if (node.Childs[0].Type != TokenType.Resolve)
				Assert.Equal(TokenType.Identifier, node.Childs[0].Type);
		}

		[Theory]
		[InlineData("a + (b + c)")]
		[InlineData("a * (b + c)")]
		[InlineData("a.b * (c + d)")]
		public void ParseEnclose(string expression)
		{
			var node = Parser.Parse(Tokenizer.Tokenize(expression));

			Assert.Equal(2, node.Childs.Count);
			Assert.Equal(TokenType.Group, node.Childs[1].Type);

		}
	}
}
