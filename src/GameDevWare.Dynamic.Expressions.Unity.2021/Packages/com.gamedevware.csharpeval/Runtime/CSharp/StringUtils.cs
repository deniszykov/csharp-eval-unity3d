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

						var escSymbol = i + 1 < end ? stringToUnescape[i + 1] : '\0';
						switch (escSymbol)
						{
							case 'a':
								sb.Append('\a');
								break;
							case 'v':
								sb.Append('\v');
								break;
							case '0':
								sb.Append('\0');
								break;
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

							// unicode symbol '\uXXXX'
							case 'u':
								sb.Append((char)ReadHexEscape(stringToUnescape, i, end, 4, 4));
								seqLength = 5;
								break;

							// unicode symbol '\UXXXXXXXX'
							case 'U':
								var codePoint = ReadHexEscape(stringToUnescape, i, end, 8, 8);
								if (codePoint > 0x10FFFF)
									throw new InvalidOperationException(string.Format(Resources.EXCEPTION_STRINGUTILS_UNEXPECTEDESCAPESEQ, stringToUnescape.Substring(i, 10)));

								if (codePoint > 0xFFFF)
								{
									codePoint -= 0x10000;
									sb.Append((char)(0xD800 + (codePoint >> 10)));
									sb.Append((char)(0xDC00 + (codePoint & 0x3FF)));
								}
								else
									sb.Append((char)codePoint);

								seqLength = 9;
								break;

							// variable length hex encoded symbol '\xX', '\xXX', '\xXXX', '\xXXXX'
							case 'x':
								var hexDigits = 0;
								while (hexDigits < 4 && i + 2 + hexDigits < end && IsHexDigit(stringToUnescape[i + 2 + hexDigits]))
									hexDigits++;

								sb.Append((char)ReadHexEscape(stringToUnescape, i, end, 1, hexDigits));
								seqLength = 1 + hexDigits;
								break;
							default:
								throw new InvalidOperationException(string.Format(Resources.EXCEPTION_STRINGUTILS_UNEXPECTEDESCAPESEQ, "\\" + escSymbol));
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

			if (isChar && resultString.Length != 1)
				throw new InvalidOperationException(Resources.EXCEPTION_TOKENIZER_INVALIDCHARLITERAL);

			return resultString;
		}

		private static uint ReadHexEscape(string value, int escapeStart, int end, int minDigits, int digits)
		{
			// escapeStart points to '\', digits start after '\' and escape symbol
			var digitsStart = escapeStart + 2;
			if (digits < minDigits || digitsStart + digits > end)
			{
				var sequence = value.Substring(escapeStart, Math.Min(end, digitsStart + digits) - escapeStart);
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_STRINGUTILS_UNEXPECTEDESCAPESEQ, sequence));
			}

			for (var i = 0; i < digits; i++)
			{
				if (!IsHexDigit(value[digitsStart + i]))
				{
					throw new InvalidOperationException(string.Format(Resources.EXCEPTION_STRINGUTILS_UNEXPECTEDESCAPESEQ,
						value.Substring(escapeStart, digitsStart + i + 1 - escapeStart)));
				}
			}

			return HexStringToUInt32(value, digitsStart, digits);
		}
		private static bool IsHexDigit(char value)
		{
			return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F');
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
