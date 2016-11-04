using System;
using System.Linq;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class TokenizerTests
	{
		[Fact]
		public void TokenizeAllTokens()
		{
			var expression = "(MyType) my.name(arg1: +1 >> -2 | 3 & 4 ^ 5, 6 > 7 >= 8 < 9 <= 10 == 11 != 12 && 13 || 14 ?? 15, true ? 16 : 17)(arg1: \" string literal \", arg2: null)" +
							 "[18] + (~19d - !20.0f) * 21m / 22u % 23l << 24UL + emptycall()";
			var expectedTokens = new TokenType[]
			{
				TokenType.Lparen, TokenType.Identifier, TokenType.Rparen, TokenType.Identifier, TokenType.Resolve, TokenType.Identifier, TokenType.Lparen, TokenType.Identifier, TokenType.Colon,
				TokenType.Add, TokenType.Number, TokenType.Rshift, TokenType.Subtract, TokenType.Number, TokenType.Or, TokenType.Number, TokenType.And, TokenType.Number, TokenType.Xor, TokenType.Number, TokenType.Comma,
				TokenType.Number, TokenType.Gt, TokenType.Number, TokenType.Gte, TokenType.Number, TokenType.Lt, TokenType.Number, TokenType.Lte, TokenType.Number, TokenType.Eq, TokenType.Number, TokenType.Neq,
				TokenType.Number, TokenType.AndAlso, TokenType.Number, TokenType.OrElse, TokenType.Number, TokenType.Coalesce, TokenType.Number, TokenType.Comma, TokenType.Identifier, TokenType.Cond, TokenType.Number, TokenType.Colon,
				TokenType.Number, TokenType.Rparen, TokenType.Lparen, TokenType.Identifier, TokenType.Colon, TokenType.Literal, TokenType.Comma, TokenType.Identifier, TokenType.Colon, TokenType.Identifier,
				TokenType.Rparen, TokenType.Lbracket, TokenType.Number, TokenType.Rbracket, TokenType.Add, TokenType.Lparen, TokenType.Compl, TokenType.Number, TokenType.Subtract, TokenType.Not, TokenType.Number,
				TokenType.Rparen, TokenType.Mul, TokenType.Number, TokenType.Div, TokenType.Number, TokenType.Mod, TokenType.Number, TokenType.Lshift, TokenType.Number, TokenType.Add, TokenType.Identifier, TokenType.Lparen, TokenType.Rparen,
			};

			var actialTokens = Tokenizer.Tokenize(expression).Select(l => l.Type).ToArray();

			for (var i = 0; i < Math.Max(expectedTokens.Length, actialTokens.Length); i++)
			{
				var expected = expectedTokens.ElementAtOrDefault(i);
				var actual = actialTokens.ElementAtOrDefault(i);
				Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
			}
		}

		[Fact]
		public void TokenizeLiterals()
		{
			var expression = "\" string literal with numbers and quote \\\" \" " +
							 "\"'%$#!@%^&*))([]\" " +
							 "\"\\\" \" " +
							 "\"\\\"\" " +
							 "\"\\n\" ";

			var expectedValues = new string[]
			{
				" string literal with numbers and quote \\\" ",
				"'%$#!@%^&*))([]",
				"\\",
				"\"",
				"\n"
			};

			var actialValues = Tokenizer.Tokenize(expression).Select(l => l.Value).ToArray();

			for (var i = 0; i < Math.Max(expectedValues.Length, actialValues.Length); i++)
			{
				var expected = actialValues.ElementAtOrDefault(i);
				var actual = actialValues.ElementAtOrDefault(i);
				Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
			}
		}

		[Fact]
		public void TokenizeIdentifiers()
		{
			var expression = "a ab ab1 ab2 ab333 _ _a __a __a3 _3 _a_ a_ a__ zazazaza";

			var expectedValues = new string[]
			{
				"a", "ab", "ab1", "ab2", "ab333", "_", "_a", "__a", "__a3", "_3", "_a_", "a_", "a__", "zazazaza"
			};

			var actialValues = Tokenizer.Tokenize(expression).Select(l => l.Value).ToArray();

			for (var i = 0; i < Math.Max(expectedValues.Length, actialValues.Length); i++)
			{
				var expected = actialValues.ElementAtOrDefault(i);
				var actual = actialValues.ElementAtOrDefault(i);
				Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
			}
		}

		[Fact]
		public void TokenizeNumbers()
		{
			var expression = "1 2222 3.0 3.000 1f 1000f 1000.0f 1000.0d 1d 1m 1l 1ul 1L 1UL 1uL 1Ul";

			var expectedValues = new string[]
			{
				"1", "2222", "3.0", "3.000", "1f", "1000f", "1000.0f", "1000.0d", "1d", "1m", "1l", "1ul", "1l", "1ul", "1ul", "1ul"
			};

			var actialValues = Tokenizer.Tokenize(expression).Select(l => l.Value).ToArray();

			for (var i = 0; i < Math.Max(expectedValues.Length, actialValues.Length); i++)
			{
				var expected = actialValues.ElementAtOrDefault(i);
				var actual = actialValues.ElementAtOrDefault(i);
				Assert.True(expected == actual, string.Format("Tokens at {0} does not match: expected {1}, actual {2}.", i, expected, actual));
			}
		}
	}
}
