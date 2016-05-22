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

namespace GameDevWare.Dynamic.Expressions
{
	public class ExpressionTree : ReadOnlyDictionary<string, object>, ILineInfo
	{
		public const string EXPRESSION_LINE = "$lineNum";
		public const string EXPRESSION_COLUMN = "$columnNum";
		public const string EXPRESSION_LENGTH = "$tokenLength";
		public const string EXPRESSION_ORIGINAL = "$originalExpression";

		public const string EXPRESSION_TYPE_ATTRIBUTE = "expressionType";
		public const string EXPRESSION_ATTRIBUTE = "expression";
		public const string ARGUMENTS_ATTRIBUTE = "arguments";
		public const string LEFT_ATTRIBUTE = "left";
		public const string RIGHT_ATTRIBUTE = "right";
		public const string TEST_ATTRIBUTE = "test";
		public const string IFTRUE_ATTRIBUTE = "ifTrue";
		public const string IFFALSE_ATTRIBUTE = "ifFalse";
		public const string TYPE_ATTRIBUTE = "type";
		public const string VALUE_ATTRIBUTE = "value";
		public const string PROPERTY_OR_FIELD_NAME_ATTRIBUTE = "propertyOrFieldName";
		public const string METHOD_ATTRIBUTE = "method";

		public int LineNumber { get { var valueObj = default(object); if (this.TryGetValue(EXPRESSION_LINE, out valueObj) == false) return 0; else return Convert.ToInt32(valueObj); } }
		public int ColumnNumber { get { var valueObj = default(object); if (this.TryGetValue(EXPRESSION_COLUMN, out valueObj) == false) return 0; else return Convert.ToInt32(valueObj); } }
		public int TokenLength { get { var valueObj = default(object); if (this.TryGetValue(EXPRESSION_LENGTH, out valueObj) == false) return 0; else return Convert.ToInt32(valueObj); } }
		public string Position { get { return string.Format("[{0}:{1}+{2}]", this.LineNumber, this.ColumnNumber, this.TokenLength); } }
		public string OriginalExpression { get { return this.GetValueOrDefault(EXPRESSION_ORIGINAL, default(string)); } }

		public ExpressionTree(IDictionary<string, object> node) : base(PrepareNode(node))
		{
		}

		private static Dictionary<string, object> PrepareNode(IDictionary<string, object> node)
		{
			var newNode = new Dictionary<string, object>(node.Count);
			foreach (var kv in node)
			{
				if (kv.Value is IDictionary<string, object> && kv.Value is ExpressionTree == false)
					newNode[kv.Key] = new ExpressionTree((IDictionary<string, object>)kv.Value);
				else
					newNode[kv.Key] = kv.Value;
			}
			return newNode;
		}

		private T GetValueOrDefault<T>(string key, T defaultValue = default(T))
		{
			var valueObj = default(object);
			var value = default(T);
			if (this.TryGetValue(key, out valueObj) == false || valueObj is T == false)
				value = defaultValue;
			else
				value = (T)valueObj;
			return value;
		}

	}
}
