using System;
using System.Linq;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class TokenizerTests
{
	[Theory,
	InlineData("\" string literal with numbers and quote \\\" \""),
	InlineData("\"'%$#!@%^&*))([]\""),
	InlineData("\"\\\"\""),
	InlineData("\"\\n\"")]
	public void TokenizeLiterals(string expression)
	{
		var actual = Tokenizer.Tokenize(expression).Single();
		Assert.Equal(TokenType.Literal, actual.Type);
		Assert.Equal(expression, actual.Value);
	}

	[Theory, InlineData("a"), InlineData("ab"), InlineData("ab1"), InlineData("ab2"), InlineData("ab333"), InlineData("_"), InlineData("__"), InlineData("_1"),
	InlineData("_a"), InlineData("__a"), InlineData("__a3"), InlineData("_a_"), InlineData("a_"), InlineData("a__"), InlineData("zazazaza"), InlineData("@as"),
	InlineData("@is"), InlineData("saas"), InlineData("issi")]
	public void TokenizeIdentifiers(string expression)
	{
		var actual = Tokenizer.Tokenize(expression).Single();
		Assert.Equal(TokenType.Identifier, actual.Type);
		Assert.Equal(expression.TrimStart('@'), actual.Value);
	}

	[Theory, InlineData("1identifier"), InlineData("id;entifier"), InlineData("id#entifier"), InlineData("id@entifier"), InlineData("@1identifier")]
	public void TokenizeWrongIdentifiers(string expression)
	{
		Assert.Throws<ExpressionParserException>(() => { Tokenizer.Tokenize(expression).Single(); });
	}

	[Theory, InlineData("1", "1"), InlineData("2222", "2222"), InlineData("3.0", "3.0"), InlineData(".1", "0.1"), InlineData("1f", "1f"), InlineData("1F", "1f"),
	InlineData("1d", "1d"), InlineData("1D", "1d"), InlineData("1m", "1m"), InlineData("1M", "1m"), InlineData("1l", "1l"), InlineData("1L", "1l"),
	InlineData("1ul", "1ul"), InlineData("1UL", "1ul"), InlineData("1Ul", "1ul"), InlineData("3.000", "3.000"), InlineData("1000f", "1000f"),
	InlineData("1000.0d", "1000.0d")]
	public void TokenizeNumbers(string expression, string expected)
	{
		var actual = Tokenizer.Tokenize(expression).Single();
		Assert.Equal(TokenType.Number, actual.Type);
		Assert.Equal(expected, actual.Value);
	}

	[Theory, InlineData("1x"), InlineData("1.0y"), InlineData(".1y"), InlineData("1z")]
	public void TokenizeWrongNumbers(string expression)
	{
		Assert.Throws<ExpressionParserException>(() => { Tokenizer.Tokenize(expression).Single(); });
	}

	[Theory, InlineData("1.x"), InlineData("a.y"), InlineData("1+0"), InlineData("1-0"), InlineData("a-0"), InlineData("a+0"), InlineData("0:0"), InlineData("0?0"),
	InlineData("0!0"), InlineData("0,0")]
	public void TokenizeBinaryExpressions(string expression)
	{
		var actualTokenCount = Tokenizer.Tokenize(expression).Count();
		Assert.Equal(3, actualTokenCount);
	}
	[Fact]
	public void TokenizeAllTokens()
	{
		var expression =
			"(MyType) my.name(arg1: +1 >> -2 | 3 & 4 ^ 5, 6 > 7 >= 8 < 9 <= 10 == 11 != 12 && 13 || 14 ?? 15, true ? 16 : 17)(arg1: \" string literal \", arg2: null)" +
			"[18] + (~19d - !20.0f) * 21m / 22u % 23l << 24UL + emptycall() ** a?.x + a?[] =>";
		var expectedTokens = new[] {
			TokenType.LeftParentheses, TokenType.Identifier, TokenType.RightParentheses, TokenType.Identifier, TokenType.Resolve, TokenType.Identifier,
			TokenType.LeftParentheses, TokenType.Identifier, TokenType.Colon,
			TokenType.Add, TokenType.Number, TokenType.RightShift, TokenType.Subtract, TokenType.Number, TokenType.Or, TokenType.Number, TokenType.And,
			TokenType.Number, TokenType.Xor, TokenType.Number, TokenType.Comma,
			TokenType.Number, TokenType.GreaterThan, TokenType.Number, TokenType.GreaterThanOrEquals, TokenType.Number, TokenType.LesserThan, TokenType.Number,
			TokenType.LesserThanOrEquals, TokenType.Number, TokenType.EqualsTo, TokenType.Number, TokenType.NotEqualsTo,
			TokenType.Number, TokenType.AndAlso, TokenType.Number, TokenType.OrElse, TokenType.Number, TokenType.Coalesce, TokenType.Number, TokenType.Comma,
			TokenType.Identifier, TokenType.Conditional, TokenType.Number, TokenType.Colon,
			TokenType.Number, TokenType.RightParentheses, TokenType.LeftParentheses, TokenType.Identifier, TokenType.Colon, TokenType.Literal, TokenType.Comma,
			TokenType.Identifier, TokenType.Colon, TokenType.Identifier,
			TokenType.RightParentheses, TokenType.LeftBracket, TokenType.Number, TokenType.RightBracket, TokenType.Add, TokenType.LeftParentheses,
			TokenType.Complement, TokenType.Number, TokenType.Subtract, TokenType.Not, TokenType.Number,
			TokenType.RightParentheses, TokenType.Multiplication, TokenType.Number, TokenType.Division, TokenType.Number, TokenType.Modulo, TokenType.Number,
			TokenType.LeftShift, TokenType.Number, TokenType.Add, TokenType.Identifier, TokenType.LeftParentheses, TokenType.RightParentheses,
			TokenType.Power, TokenType.Identifier, TokenType.NullResolve, TokenType.Identifier, TokenType.Add, TokenType.Identifier, TokenType.NullIndex,
			TokenType.RightBracket, TokenType.Lambda
		};

		var actualTokens = Tokenizer.Tokenize(expression).Select(l => l.Type).ToArray();

		for (var i = 0; i < Math.Max(expectedTokens.Length, actualTokens.Length); i++)
		{
			var expected = expectedTokens.ElementAtOrDefault(i);
			var actual = actualTokens.ElementAtOrDefault(i);
			Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
		}
	}

	[Fact]
	public void TokenizeLambda()
	{
		var expression = "(saas, issi) => saas + issi";
		var expectedTokens = new[] {
			TokenType.LeftParentheses, TokenType.Identifier, TokenType.Comma, TokenType.Identifier, TokenType.RightParentheses, TokenType.Lambda,
			TokenType.Identifier, TokenType.Add, TokenType.Identifier
		};

		var actualTokens = Tokenizer.Tokenize(expression).Select(l => l.Type).ToArray();

		for (var i = 0; i < Math.Max(expectedTokens.Length, actualTokens.Length); i++)
		{
			var expected = expectedTokens.ElementAtOrDefault(i);
			var actual = actualTokens.ElementAtOrDefault(i);
			Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
		}
	}
}
