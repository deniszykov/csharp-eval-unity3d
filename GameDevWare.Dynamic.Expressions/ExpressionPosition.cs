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

namespace GameDevWare.Dynamic.Expressions
{
	internal struct ExpressionPosition : ILineInfo, IEquatable<ExpressionPosition>, IEquatable<ILineInfo>
	{
		public readonly int LineNumber;
		public readonly int ColumnNumber;
		public readonly int TokenLength;

		public ExpressionPosition(int lineNumber, int columnNumber, int tokenLength)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}

		public static ExpressionPosition Parse(string positionString)
		{
			if (positionString == null) throw new ArgumentNullException("positionString");

			var colonIdx = positionString.IndexOf(':');
			var plusIdx = positionString.IndexOf('+');
			if (colonIdx < 0 || plusIdx < 0)
				throw new FormatException();

			var line = int.Parse(positionString.Substring(0, colonIdx), Constants.DefaultFormatProvider);
			var column = int.Parse(positionString.Substring(colonIdx + 1, plusIdx - colonIdx - 1), Constants.DefaultFormatProvider);
			var length = int.Parse(positionString.Substring(plusIdx + 1), Constants.DefaultFormatProvider);

			return new ExpressionPosition(line, column, length);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is ExpressionPosition)
				return this.Equals((ExpressionPosition)obj);
			else if (obj is ILineInfo)
				return this.Equals((ILineInfo)obj);
			else
				return false;
		}
		/// <inheritdoc />
		public bool Equals(ExpressionPosition other)
		{
			return this.LineNumber == other.LineNumber && this.ColumnNumber == other.ColumnNumber && this.TokenLength == other.TokenLength;
		}
		/// <inheritdoc />
		public bool Equals(ILineInfo other)
		{
			if (other == null) return false;

			return this.LineNumber == other.GetLineNumber() && this.ColumnNumber == other.GetColumnNumber() && this.TokenLength == other.GetTokenLength();
		}
		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = this.LineNumber;
				hashCode = (hashCode * 397) ^ this.ColumnNumber;
				hashCode = (hashCode * 397) ^ this.TokenLength;
				return hashCode;
			}
		}

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

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format(Constants.DefaultFormatProvider, "{0}:{1}+{2}", this.LineNumber.ToString(), this.ColumnNumber.ToString(), this.TokenLength.ToString());
		}

	}
}
