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
	/// <summary>
	/// Expression parsing exception.
	/// </summary>
	public sealed class ExpressionParserException : Exception, ILineInfo
	{
		/// <summary>
		/// Line number related to exception.
		/// </summary>
		public int LineNumber { get; set; }
		/// <summary>
		/// Column number related to exception.
		/// </summary>
		public int ColumnNumber { get; set; }
		/// <summary>
		/// Length of token related to exception.
		/// </summary>
		public int TokenLength { get; set; }

		internal ExpressionParserException()
		{

		}
		internal ExpressionParserException(string message, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
			: base(message)
		{
			this.LineNumber = lineNumber;
			this.ColumnNumber = columnNumber;
			this.TokenLength = tokenLength;
		}
		internal ExpressionParserException(string message, Exception innerException, int lineNumber = 0, int columnNumber = 0, int tokenLength = 0)
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

		/// <summary>
		/// Return data for binary serialization.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("LineNumber", (int)this.LineNumber);
			info.AddValue("ColumnNumber", (int)this.ColumnNumber);
			info.AddValue("TokenLength", (int)this.TokenLength);

			base.GetObjectData(info, context);
		}

		int ILineInfo.GetLineNumber()
		{
			return LineNumber;
		}
		int ILineInfo.GetColumnNumber()
		{
			return ColumnNumber;
		}
		int ILineInfo.GetTokenLength()
		{
			return TokenLength;
		}

		/// <summary>
		/// Converts exception to string representation for debug purpose.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if (this.TokenLength != 0)
				return string.Format("[{0},{1}+{2}]{3}", this.LineNumber.ToString(), this.ColumnNumber.ToString(), this.TokenLength.ToString(), base.ToString());
			else
				return base.ToString();
		}
	}
}
