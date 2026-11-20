/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	///     Expression string tokenizer. Produces stream of <see cref="Token" /> from expression <see cref="string" />.
	/// </summary>
	public static class Tokenizer
	{
		private static readonly bool[] IsLiteralSymbol;
		private static readonly string[] Symbols;
		private static readonly short[] TerminationCharacter;
		private static readonly Dictionary<string, TokenType> TokensBySymbols;

		static Tokenizer()
		{
			TokensBySymbols = (
				from field in typeof(TokenType).GetTypeInfo().GetDeclaredFields()
				from tokenAttribute in field.GetCustomAttributes(typeof(TokenAttribute), true).Cast<TokenAttribute>()
				select new KeyValuePair<string, TokenType>(tokenAttribute.Value, (TokenType)Enum.Parse(typeof(TokenType), field.Name))
			).ToDictionary(kv => kv.Key, kv => kv.Value);

			Symbols = TokensBySymbols.Keys.ToArray();
			IsLiteralSymbol = Symbols.ConvertAll(s => s.All(char.IsLetter));
			TerminationCharacter = Symbols
				.Select(s => s[0])
				.Where(c => !char.IsLetter(c))
				.Select(c => (short)c)
				.Distinct()
				.ToArray();
			Array.Sort(TerminationCharacter);
		}

		/// <summary>
		///     Produces stream of <see cref="Token" /> from <paramref name="expression" />.
		/// </summary>
		/// <param name="expression">A valid expression string.</param>
		/// <returns>Stream of <see cref="Token" />.</returns>
		public static IEnumerable<Token> Tokenize(string expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var line = 1;
			var col = 1;
			for (var i = 0; i < expression.Length; i++, col++)
			{
				var charValue = expression[i];
				var current = default(Token);

				if (charValue == '\n') // newline
				{
					line++;
					col = 0;
				}

				for (var t = 0; t < Symbols.Length; t++)
				{
					var token = Symbols[t];
					if (!IsMatching(expression, i, token, IsLiteralSymbol[t]) ||
						(current.IsValid && token.Length < current.TokenLength))
						continue;

					if (token == "." && i + 1 < expression.Length && char.IsDigit(expression[i + 1])) // skip: short number notation
						continue;

					if (token == "?." && i + 2 < expression.Length && char.IsDigit(expression[i + 2])) // skip: conditional with short number 'a?.5:1'
						continue;

					current = new Token(TokensBySymbols[token], token, line, col, token.Length);
				}

				if (!current.IsValid) // not found in known symbols
				{
					if (char.IsDigit(charValue) || charValue == '.') // numerics
						current = LookForNumber(expression, i, line, col);
					else if (charValue == '"' || charValue == '\'') // string literal
						current = LookForLiteral(expression, charValue, i, line, col);
					else if (charValue == '@' && i + 1 < expression.Length && expression[i + 1] == '"') // verbatim string literal
						current = LookForVerbatimLiteral(expression, i, line, col);
					else if (char.IsLetter(charValue) || charValue == '_' || charValue == '@') // identifier
						current = LookForIdentifier(expression, i, line, col);
					else if (char.IsWhiteSpace(charValue))
						continue;
					else
					{
						throw new ExpressionParserException(string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, charValue, line, col),
							new Token(TokenType.None, charValue.ToString(), line, col, 1));
					}
				}

				if (current.Type == TokenType.Literal)
				{
					// string literal could span multiple lines
					for (var c = i + 1; c < i + current.TokenLength; c++)
					{
						if (expression[c] == '\n')
						{
							line++;
							col = 0;
						}
						else
							col++;
					}
				}
				else
					col += current.TokenLength - 1;

				i += current.TokenLength - 1;

				if (current.IsValid)
					yield return current;
			}
		}

		private static bool IsMatching(string expression, int offset, string tokenToMatch, bool isLiteralToken)
		{
			if (tokenToMatch.Length + offset > expression.Length)
				return false;

			for (var v = 0; offset < expression.Length && v < tokenToMatch.Length; offset++, v++)
			{
				if (expression[offset] != tokenToMatch[v])
					return false;
			}

			var isEndOfExpression = offset >= expression.Length;
			if (isEndOfExpression)
				return true;

			var terminalCharacter = expression[offset];

			return !isLiteralToken || !(char.IsLetterOrDigit(terminalCharacter) || terminalCharacter == '_');
		}
		private static Token LookForIdentifier(string expression, int offset, int line, int col)
		{
			var startAt = offset;
			var lettersOffsetFromStartAt = -1;
			for (; offset < expression.Length; offset++)
			{
				var charCode = expression[offset];
				if (char.IsLetterOrDigit(charCode) || charCode == '_' || charCode == '@') // a-z A-Z 0-9
				{
					if (lettersOffsetFromStartAt < 0) lettersOffsetFromStartAt = offset - startAt;

					if (charCode == '@' && offset - (startAt + lettersOffsetFromStartAt) != 0)
					{
						var errorCol = col + (offset - startAt);
						throw new ExpressionParserException(
							string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, expression[offset], line, errorCol),
							new Token(TokenType.None, expression[offset].ToString(), line, errorCol, 1));
					}

					continue;
				}

				if (IsTerminationCharacter(expression[offset]) || lettersOffsetFromStartAt >= 0) break;
			}

			var isWhitespace = lettersOffsetFromStartAt < 0;
			var start = isWhitespace ? startAt : startAt + lettersOffsetFromStartAt;
			var length = offset - start;
			var startCol = col + (start - startAt);

			if (length == 1 && expression[start] == '@')
			{
				throw new ExpressionParserException(string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, "@", line, startCol),
					new Token(TokenType.None, "@", line, startCol, length));
			}

			if (length > 1 && expression[start] == '@' && char.IsDigit(expression[start + 1]))
			{
				throw new ExpressionParserException(string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, expression[start + 1], line, startCol + 1),
					new Token(TokenType.None, expression[start + 1].ToString(), line, startCol + 1, 1));
			}

			var value = expression.Substring(start, length).TrimStart('@');
			return new Token(isWhitespace ? TokenType.None : TokenType.Identifier, value, line, startCol, length);
		}
		private static Token LookForLiteral(string expression, char termChar, int offset, int line, int col)
		{
			var startAt = offset;
			for (offset++; offset < expression.Length; offset++)
			{
				var charCode = expression[offset];
				if (charCode != termChar)
					continue;

				var slashes = 0;
				for (var o = offset - 1; o >= 0 && expression[o] == '\\'; o--)
					slashes++;

				if (slashes % 2 != 0)
					continue;

				offset++;
				break;
			}

			var result = expression.Substring(startAt, offset - startAt);
			return new Token(TokenType.Literal, result, line, col, result.Length);
		}
		private static Token LookForVerbatimLiteral(string expression, int offset, int line, int col)
		{
			var startAt = offset;
			for (offset += 2; offset < expression.Length; offset++) // skip '@"'
			{
				if (expression[offset] != '"')
					continue;

				if (offset + 1 < expression.Length && expression[offset + 1] == '"') // escaped quote '""'
				{
					offset++;
					continue;
				}

				offset++;
				break;
			}

			var result = expression.Substring(startAt, offset - startAt);
			return new Token(TokenType.Literal, result, line, col, result.Length);
		}
		private static Token LookForNumber(string expression, int offset, int line, int col)
		{
			const int STATE_INTEGER = 0;
			const int STATE_FRACTION = 1;
			const int STATE_EXPONENT = 2;
			const int STATE_COMPLETE = 3;

			if (expression[offset] == '0' && offset + 1 < expression.Length && (expression[offset + 1] == 'x' || expression[offset + 1] == 'X'))
				return LookForHexNumber(expression, offset, line, col);

			var state = STATE_INTEGER;
			var startAt = offset;
			var fractionStartAt = -1;
			for (; offset < expression.Length; offset++)
			{
				if (state == STATE_COMPLETE)
					break;

				var charValue = expression[offset];
				var charCode = expression[offset];
				if (charCode >= 48 && charCode <= 57) // numerics
					continue;

				switch (char.ToLowerInvariant(charValue))
				{
					case '.':
						if (state == STATE_INTEGER)
						{
							fractionStartAt = offset;
							state = STATE_FRACTION;
						}
						else
						{
							state = STATE_COMPLETE;
							offset--;
						}

						break;
					case 'e':
						if (state != STATE_EXPONENT)
							state = STATE_EXPONENT;
						else
						{
							offset--;
							state = STATE_COMPLETE;
						}

						break;
					case '+':
					case '-':
						// sign is part of number only right after exponent mark: '1e-3', but not '1e5-3'
						if (state != STATE_EXPONENT || char.ToLowerInvariant(expression[offset - 1]) != 'e')
						{
							state = STATE_COMPLETE;
							offset--;
						}

						break;
					case 'f':
					case 'm':
					case 'u':
					case 'l':
					case 'd':
						// check for UL or LU sequence
						if (expression.Length > offset + 1 &&
							((char.ToLowerInvariant(expression[offset]) == 'u' && char.ToLowerInvariant(expression[offset + 1]) == 'l') ||
								(char.ToLowerInvariant(expression[offset]) == 'l' && char.ToLowerInvariant(expression[offset + 1]) == 'u')))
							offset++;

						state = STATE_COMPLETE;
						break;
					default:
						if (char.IsLetter(charValue))
						{
							if (state == STATE_FRACTION && offset - fractionStartAt == 1)
							{
								offset -= 2; // rewind offset because dot and letter are not part of number
								state = STATE_COMPLETE;
								break;
							}

							var invalidNumber = expression.Substring(startAt, offset - startAt);
							throw new ExpressionParserException(
								string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, charValue, line, col + invalidNumber.Length - 1),
								new Token(TokenType.None, invalidNumber, line, col, invalidNumber.Length));
						}

						offset--;
						state = STATE_COMPLETE;
						break;
				}
			}

			var length = offset - startAt;
			var result = expression.Substring(startAt, length).ToLowerInvariant();
			if (fractionStartAt == startAt)
				result = "0" + result;

			return new Token(TokenType.Number, result, line, col, length);
		}
		private static Token LookForHexNumber(string expression, int offset, int line, int col)
		{
			var startAt = offset;
			offset += 2; // skip '0x'

			var digitsStartAt = offset;
			while (offset < expression.Length && IsHexDigit(expression[offset]))
				offset++;
			var hasDigits = offset > digitsStartAt;

			// suffix: 'u', 'l', 'ul' or 'lu'
			if (offset < expression.Length && (char.ToLowerInvariant(expression[offset]) == 'u' || char.ToLowerInvariant(expression[offset]) == 'l'))
			{
				var first = char.ToLowerInvariant(expression[offset]);
				offset++;
				if (offset < expression.Length)
				{
					var second = char.ToLowerInvariant(expression[offset]);
					if ((first == 'u' && second == 'l') || (first == 'l' && second == 'u'))
						offset++;
				}
			}

			var isInvalidEnd = offset < expression.Length && (
				char.IsLetterOrDigit(expression[offset]) || expression[offset] == '_' ||
				(expression[offset] == '.' && offset + 1 < expression.Length && char.IsDigit(expression[offset + 1])));
			if (!hasDigits || isInvalidEnd)
			{
				var errorOffset = Math.Min(offset, expression.Length - 1);
				var invalidNumber = expression.Substring(startAt, errorOffset - startAt + 1);
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, expression[errorOffset], line, col + (errorOffset - startAt)),
					new Token(TokenType.None, invalidNumber, line, col, invalidNumber.Length));
			}

			var length = offset - startAt;
			var result = expression.Substring(startAt, length).ToLowerInvariant();
			return new Token(TokenType.Number, result, line, col, length);
		}
		private static bool IsHexDigit(char value)
		{
			return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F');
		}
		private static bool IsTerminationCharacter(char value)
		{
			return Array.BinarySearch(TerminationCharacter, (short)value) >= 0 || value == '\"' || char.IsWhiteSpace(value);
		}
	}
}
