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
using System.Text;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	internal static class StringUtils
	{
		public static string UnescapeAndUnquote(string stringToUnescape)
		{
			if (stringToUnescape == null) throw new ArgumentNullException(nameof(stringToUnescape));

			var start = 0;
			var len = stringToUnescape.Length;
			var isChar = false;
			var resultString = stringToUnescape;

			if (stringToUnescape.Length > 0 && (stringToUnescape[0] == '"' || stringToUnescape[0] == '\''))
			{
				start += 1;
				len -= 2;
				isChar = stringToUnescape[0] == '\'';
			}

			if (start != 0 || len != stringToUnescape.Length || stringToUnescape.IndexOf('\\') >= 0)
			{
				var sb = new StringBuilder(len);
				var plainTextStart = start;
				var plainTextLen = 0;
				var end = start + len;
				for (var i = start; i < end; i++)
				{
					var ch = stringToUnescape[i];
					if (ch == '\\')
					{
						var seqLength = 1;

						// append unencoded chunk
						if (plainTextLen != 0)
						{
							sb.Append(stringToUnescape, plainTextStart, plainTextLen);
							plainTextLen = 0;
						}

						var escSymbol = stringToUnescape[i + 1];
						switch (escSymbol)
						{
							case 'n':
								sb.Append('\n');
								break;
							case 'r':
								sb.Append('\r');
								break;
							case 'b':
								sb.Append('\b');
								break;
							case 'f':
								sb.Append('\f');
								break;
							case 't':
								sb.Append('\t');
								break;
							case '\\':
								sb.Append('\\');
								break;
							case '\'':
								sb.Append('\'');
								break;
							case '\"':
								sb.Append('\"');
								break;

							// unicode symbol
							case 'u':
								sb.Append((char)HexStringToUInt32(stringToUnescape, i + 2, 4));
								seqLength = 5;
								break;

							// latin hex encoded symbol
							case 'x':
								sb.Append((char)HexStringToUInt32(stringToUnescape, i + 2, 2));
								seqLength = 3;
								break;

							// latin dec encoded symbol
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
							case '0':
								sb.Append((char)StringToInt32(stringToUnescape, i + 1, 3));
								seqLength = 3;
								break;
							default:
								throw new InvalidOperationException(string.Format(Resources.EXCEPTION_STRINGUTILS_UNEXPECTEDESCAPESEQ, "//" + escSymbol));
						}

						// set next chunk start right after this escape
						plainTextStart = i + seqLength + 1;
						i += seqLength;
					}
					else
						plainTextLen++;
				}

				// append last unencoded chunk
				if (plainTextLen != 0)
					sb.Append(stringToUnescape, plainTextStart, plainTextLen);

				resultString = sb.ToString();
			}

			if (isChar && string.IsNullOrEmpty(resultString) && resultString.Length != 1)
				throw new InvalidOperationException(Resources.EXCEPTION_TOKENIZER_INVALIDCHARLITERAL);

			return resultString;
		}

		private static int StringToInt32(string value, int offset, int count)
		{
			const uint ZERO = '0';

			var result = 0u;
			var neg = false;
			for (var i = 0; i < count; i++)
			{
				var c = value[offset + i];
				if (i == 0 && c == '-')
				{
					neg = true;
					continue;
				}

				if (c < '0' || c > '9')
					throw new FormatException();

				result = checked(10u * result + (c - ZERO));
			}

			if (neg)
				return -(int)result;

			return (int)result;
		}
		private static uint HexStringToUInt32(string value, int offset, int count)
		{
			var result = 0u;
			for (var i = 0; i < count; i++)
			{
				var c = value[offset + i];
				var d = 0u;
				if (c >= '0' && c <= '9')
					d = c - (uint)'0';
				else if (c >= 'a' && c <= 'f')
					d = 10u + (c - (uint)'a');
				else if (c >= 'A' && c <= 'F')
					d = 10u + (c - (uint)'A');
				else
					throw new FormatException();

				result = 16u * result + d;
			}

			return result;
		}
	}
}
