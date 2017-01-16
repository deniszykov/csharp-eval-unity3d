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
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Dynamic.Expressions.CSharp;

namespace GameDevWare.Dynamic.Expressions
{
	public class ExpressionTree : IDictionary<string, object>, ILineInfo
	{
		private readonly Dictionary<string, object> innerDictionary;

		public ExpressionTree(IDictionary<string, object> node)
		{
			this.innerDictionary = PrepareNode(node);
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

		public T GetValueOrDefault<T>(string key, T defaultValue = default(T))
		{
			var valueObj = default(object);
			var value = default(T);
			if (this.TryGetValue(key, out valueObj) == false || valueObj is T == false)
				value = defaultValue;
			else
				value = (T)valueObj;
			return value;
		}

		public string GetExpressionType(bool throwOnError)
		{
			var expressionTypeObj = default(object);
			if (this.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return null;
			}

			var expressionType = (string)expressionTypeObj;
			return expressionType;
		}
		public object GetTypeName(bool throwOnError)
		{
			var typeNameObj = default(object);
			if (this.TryGetValue(Constants.TYPE_ATTRIBUTE, out typeNameObj) == false || (typeNameObj is string == false && typeNameObj is ExpressionTree == false))
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.TYPE_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);

			return typeNameObj;
		}
		public object GetValue(bool throwOnError)
		{
			var valueObj = default(object);
			if (this.TryGetValue(Constants.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.VALUE_ATTRIBUTE, this.GetExpressionType(throwOnError: true), this));
			return valueObj;
		}
		public ExpressionTree GetExpression(bool throwOnError)
		{
			var expressionObj = default(object);
			if (this.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj == null || expressionObj is ExpressionTree == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, this.GetExpressionType(true)), this);
				else
					return null;
			}

			var expression = (ExpressionTree)expressionObj;
			return expression;
		}
		public ArgumentsTree GetArguments(bool throwOnError)
		{
			var argumentsObj = default(object);
			if (this.TryGetValue(Constants.ARGUMENTS_ATTRIBUTE, out argumentsObj) == false || argumentsObj == null || argumentsObj is ExpressionTree == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.ARGUMENTS_ATTRIBUTE, this.GetExpressionType(true)), this);
				else
					return ArgumentsTree.Empty;
			}

			var arguments = new Dictionary<string, ExpressionTree>(((ExpressionTree)argumentsObj).Count);
			foreach (var kv in (ExpressionTree)argumentsObj)
			{
				var argument = kv.Value as ExpressionTree;
				if (argument == null)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGORWRONGARGUMENT, kv.Key), this);
				arguments.Add(kv.Key, argument);
			}

			if (arguments.Count > Constants.MAX_ARGUMENTS_COUNT)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_TOOMANYARGUMENTS, Constants.MAX_ARGUMENTS_COUNT.ToString()), this);

			return new ArgumentsTree(arguments);
		}
		public string GetPropertyOrFieldName(bool throwOnError)
		{
			var propertyOrFieldNameObj = default(object);
			if (this.TryGetValue(Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return null;
			}

			var propertyOrFieldName = (string)propertyOrFieldNameObj;
			return propertyOrFieldName;
		}
		public bool GetUseNullPropagation(bool throwOnError)
		{
			var useNullPropagationObj = default(object);
			if (this.TryGetValue(Constants.USE_NULL_PROPAGATION_ATTRIBUTE, out useNullPropagationObj) == false || useNullPropagationObj == null)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.USE_NULL_PROPAGATION_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return false;
			}
			var useNullPropagation = Convert.ToBoolean(useNullPropagationObj, Constants.DefaultFormatProvider);
			return useNullPropagation;
		}

		public int GetLineNumber(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_LINE_NUMBER, out valueObj) == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_LINE_NUMBER, this.GetExpressionType(throwOnError: true)), this);
				else
					return value;
			}

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_LINE_NUMBER, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		public int GetColumnNumber(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_COLUMN_NUMBER, out valueObj) == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_COLUMN_NUMBER, this.GetExpressionType(throwOnError: true)), this);
				else
					return value;
			}

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_COLUMN_NUMBER, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		public int GetTokenLength(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_TOKEN_LENGTH, out valueObj) == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_TOKEN_LENGTH, this.GetExpressionType(throwOnError: true)), this);
				else
					return value;
			}

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_TOKEN_LENGTH, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		public string GetPosition(bool throwOnError)
		{
			return string.Format(Constants.DefaultFormatProvider, "[{0}:{1}+{2}]", this.GetLineNumber(throwOnError).ToString(), this.GetColumnNumber(throwOnError).ToString(), this.GetTokenLength(throwOnError).ToString());
		}
		public string GetOriginalExpression(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(string);
			if (this.TryGetValue(Constants.EXPRESSION_ORIGINAL, out valueObj) == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_ORIGINAL, this.GetExpressionType(throwOnError: true)), this);
				else
					// ReSharper disable once ExpressionIsAlwaysNull
					return value;
			}

			return Convert.ToString(valueObj, Constants.DefaultFormatProvider);
		}

		int ILineInfo.GetLineNumber()
		{
			return this.GetLineNumber(throwOnError: false);
		}
		int ILineInfo.GetColumnNumber()
		{
			return this.GetColumnNumber(throwOnError: false);
		}
		int ILineInfo.GetTokenLength()
		{
			return this.GetTokenLength(throwOnError: false);
		}

		#region IDictionary<string,object> Members

		void IDictionary<string, object>.Add(string key, object value)
		{
			throw new NotSupportedException();
		}

		public bool ContainsKey(string key)
		{
			return this.innerDictionary.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get { return this.innerDictionary.Keys; }
		}

		bool IDictionary<string, object>.Remove(string key)
		{
			throw new NotSupportedException();
		}

		public bool TryGetValue(string key, out object value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}

		public ICollection<object> Values
		{
			get { return this.innerDictionary.Values; }
		}

		public object this[string key]
		{
			get { return this.innerDictionary[key]; }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region ICollection<KeyValuePair<string,object>> Members

		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
		{
			throw new NotSupportedException();
		}

		void ICollection<KeyValuePair<string, object>>.Clear()
		{
			throw new NotSupportedException();
		}

		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
		{
			return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).Contains(item);
		}

		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, object>>)this.innerDictionary).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return this.innerDictionary.Count; }
		}

		bool ICollection<KeyValuePair<string, object>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).IsReadOnly; }
		}

		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
		{
			return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,object>> Members

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.innerDictionary).GetEnumerator();
		}

		#endregion

		public override bool Equals(object obj)
		{
			return this.innerDictionary.Equals(obj);
		}

		public override int GetHashCode()
		{
			return this.innerDictionary.GetHashCode();
		}

		public override string ToString()
		{
			var expression = this.GetOriginalExpression(throwOnError: false);
			if (string.IsNullOrEmpty(expression))
				expression = this.Render();
			return expression;
		}
	}
}
