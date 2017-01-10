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
using System.Runtime.Serialization;

namespace GameDevWare.Dynamic.Expressions
{
	public sealed class ExpressionParserException : Exception, ILineInfo
	{
		private int _tokenLength;
		private int _columnNumber;
		private int _lineNumber;

		public int LineNumber
		{
			set { _lineNumber = value; }
		}

		public int GetLineNumber()
		{
			return _lineNumber;
		}

		public int ColumnNumber
		{
			set { _columnNumber = value; }
		}

		public int GetColumnNumber()
		{
			return _columnNumber;
		}

		public int TokenLength
		{
			set { _tokenLength = value; }
		}

		public int GetTokenLength()
		{
			return _tokenLength;
		}

		public ExpressionParserException()
		{

		}
		public ExpressionParserException(string message, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
			: base(message)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}
		public ExpressionParserException(string message, Exception innerException, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
			: base(message, innerException)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}
		internal ExpressionParserException(string message, ILineInfo lineInfo)
			: base(message)
		{
			if (lineInfo == null)
				return;

			this.LineNumber = lineInfo.GetLineNumber();
			this.ColumnNumber = lineInfo.GetColumnNumber();
			this.TokenLength = lineInfo.GetTokenLength();
		}
		internal ExpressionParserException(string message, Exception innerException, ILineInfo lineInfo)
			: base(message, innerException)
		{
			if (lineInfo == null)
				return;

			this.LineNumber = lineInfo.GetLineNumber();
			this.ColumnNumber = lineInfo.GetColumnNumber();
			this.TokenLength = lineInfo.GetTokenLength();
		}
		// ReSharper disable once UnusedMember.Local
		private ExpressionParserException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.LineNumber = info.GetInt32("LineNumber");
			this.ColumnNumber = info.GetInt32("ColumnNumber");
			this.TokenLength = info.GetInt32("TokenLength");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("LineNumber", (int)this.GetLineNumber());
			info.AddValue("ColumnNumber", (int)this.GetColumnNumber());
			info.AddValue("TokenLength", (int)this.GetTokenLength());

			base.GetObjectData(info, context);
		}

		public override string ToString()
		{
			if (this.GetTokenLength() != 0)
				return string.Format("[{0},{1}+{2}]{3}", this.GetLineNumber(), this.GetColumnNumber(), this.GetTokenLength(), base.ToString());
			else
				return base.ToString();
		}
	}
}
