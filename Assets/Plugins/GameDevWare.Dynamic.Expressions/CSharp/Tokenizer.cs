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
namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public static class Tokenizer
	{
		private static readonly Dictionary<string, TokenType> TokensBySymbols;
		private static readonly string[] Symbols;

		static Tokenizer()
		{
			TokensBySymbols = (
				from field in typeof(TokenType).GetFields()
				from tokenAttribute in field.GetCustomAttributes(typeof(TokenAttribute), true).Cast<TokenAttribute>()
				select new KeyValuePair<string, TokenType>(tokenAttribute.Value, (TokenType)Enum.Parse(typeof(TokenType), field.Name))
			).ToDictionary(kv => kv.Key, kv => kv.Value);
			Symbols = TokensBySymbols.Keys.ToArray();
		}

		public static IEnumerable<Token> Tokenize(string expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var line = 1;
			var col = 1;
			for (var i = 0; i < expression.Length; i++, col++)
			{
				var charCode = expression[i];
				var current = default(Token);

				if (charCode == '\n') // newline
				{
					line++;
					col = 0;
				}
				foreach (var token in Symbols)
				{
					if (!Match(expression, i, token) ||
						(current.IsValid && token.Length < current.TokenLength))
					{
						continue;
					}

					current = new Token(TokensBySymbols[token], token, line, col, token.Length);
				}

				if (!current.IsValid)
				{
					if (char.IsDigit(charCode)) // numerics
						current = LookForNumber(expression, i, line, col);
					else if (charCode == '"' || charCode == '\'') // string literal
						current = LookForLiteral(expression, charCode, i, line, col);
					else if (char.IsLetter(charCode) || charCode == '_') // identifier
						current = LookForIdentifier(expression, i, line, col);
					else if (char.IsWhiteSpace(charCode))
						continue;
					else
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_TOKENIZER_UNEXPECTEDSYMBOL, charCode, line, col), new Token(TokenType.None, charCode.ToString(), line, col, 1));
				}

				i += current.TokenLength - 1;
				col += current.TokenLength - 1;

				if (current.IsValid)
					yield return current;
			}
		}

		private static bool Match(string expression, int offset, string tokenToMatch)
		{
			if (tokenToMatch.Length + offset > expression.Length)
				return false;

			for (var v = 0; offset < expression.Length && v < tokenToMatch.Length; offset++, v++)
			{
				if (expression[offset] != tokenToMatch[v])
					return false;
			}
			return true;
		}
		private static Token LookForIdentifier(string expression, int offset, int line, int col)
		{
			var startAt = offset;
			var letterStartAt = -1;
			for (; offset < expression.Length; offset++)
			{
				var charCode = expression[offset];
				if (char.IsLetterOrDigit(charCode) || charCode == '_') // a-z A-Z 0-9
				{
					if (letterStartAt < 0)
						letterStartAt = offset - startAt;
					continue;
				}

				if (TokensBySymbols.ContainsKey(expression[offset].ToString()) || charCode == '"' || letterStartAt >= 0)
					break;
			}
			var result = expression.Substring(startAt, offset - startAt);
			var isWhitespace = letterStartAt < 0;
			var value = isWhitespace ? result : result.Substring(letterStartAt);

			return new Token(isWhitespace ? TokenType.None : TokenType.Identifier, value, line, isWhitespace ? col : col + letterStartAt, result.Length - letterStartAt);
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
		private static Token LookForNumber(string expression, int offset, int line, int col)
		{
			var hasSeparator = false;
			var startAt = offset;
			for (; offset < expression.Length; offset++)
			{
				var charCode = expression[offset];
				if (charCode >= 48 && charCode <= 57)  // numerics
					continue;

				switch (char.ToLowerInvariant(expression[offset]))
				{
					case '.':
						if (hasSeparator)
							goto breakScan;
						else
							hasSeparator = true;
						continue;
					case 'f':
					case 'm':
					case 'u':
					case 'l':
					case 'd':
						offset++;
						if (expression.Length > offset && char.ToLowerInvariant(expression[offset - 1]) == 'u' && char.ToLowerInvariant(expression[offset]) == 'l')
							offset++;
						goto breakScan;
					default:
						goto breakScan;
				}
				breakScan:
				break;
			}
			var result = expression.Substring(startAt, offset - startAt).ToLowerInvariant();
			return new Token(TokenType.Number, result, line, col, result.Length);
		}
	}
}
