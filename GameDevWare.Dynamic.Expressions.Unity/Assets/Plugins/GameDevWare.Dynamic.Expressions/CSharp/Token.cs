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

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Tokenizer's token data.
	/// </summary>
	public struct Token : ILineInfo
	{
		/// <summary>
		/// Type of token.
		/// </summary>
		public readonly TokenType Type;
		/// <summary>
		/// Value of token.
		/// </summary>
		public readonly string Value;
		/// <summary>
		/// Line number of token (position).
		/// </summary>
		public readonly int LineNumber;
		/// <summary>
		/// Column number of token (position).
		/// </summary>
		public readonly int ColumnNumber;
		/// <summary>
		/// Length of token (position).
		/// </summary>
		public readonly int TokenLength;

		/// <summary>
		/// Returns true if token is valid.
		/// </summary>
		public bool IsValid { get { return this.Type != TokenType.None; } }
		/// <summary>
		/// Returns token's position as string.
		/// </summary>
		public string Position { get { return string.Format("{0}:{1}+{2}", this.LineNumber.ToString(), this.ColumnNumber.ToString(), this.TokenLength.ToString()); } }

		int ILineInfo.GetLineNumber()
		{
			return this.LineNumber;
		}
		int ILineInfo.GetColumnNumber()
		{
			return this.ColumnNumber;
		}
		int ILineInfo.GetTokenLength()
		{
			return this.TokenLength;
		}

		/// <summary>
		/// Creates new token.
		/// </summary>
		public Token(TokenType type, string value, int line, int col, int len)
		{
			this.Type = type;
			this.Value = value;
			this.LineNumber = line;
			this.ColumnNumber = col;
			this.TokenLength = len;
		}

		/// <summary>
		/// Converts token to string for debugging.
		/// </summary>
		public override string ToString()
		{
			return this.Type + (this.Type == TokenType.Number || this.Type == TokenType.Identifier || this.Type == TokenType.Literal ? "(" + this.Value + ")" : "");
		}
	}
}
