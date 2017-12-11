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
	/// <summary>
	/// Abstract syntax tree of expression
	/// </summary>
	public class SyntaxTreeNode : IDictionary<string, object>, ILineInfo
	{
		private readonly Dictionary<string, object> innerDictionary;

		/// <summary>
		/// Creates new syntax tree from existing dictionary.
		/// </summary>
		/// <param name="node">Dictionary containing a valid syntax tree.</param>
		public SyntaxTreeNode(IDictionary<string, object> node)
		{
			this.innerDictionary = PrepareNode(node);
		}

		private static Dictionary<string, object> PrepareNode(IDictionary<string, object> node)
		{
			var newNode = new Dictionary<string, object>(node.Count);
			foreach (var kv in node)
			{
				if (kv.Value is IDictionary<string, object> && kv.Value is SyntaxTreeNode == false)
					newNode[kv.Key] = new SyntaxTreeNode((IDictionary<string, object>)kv.Value);
				else
					newNode[kv.Key] = kv.Value;
			}
			return newNode;
		}

		/// <summary>
		/// Tries to retrieve contained node by its name and covert it to <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Type of expected value.</typeparam>
		/// <param name="key">Node name.</param>
		/// <param name="defaultValue">Default value node node with <paramref name="key"/> doesn't exists or value can't be casted to <typeparamref name="T"/>.</param>
		/// <returns>True is node exists and value successfully casted to <typeparamref name="T"/>, overwise false.</returns>
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

		internal string GetExpressionType(bool throwOnError)
		{
			var expressionTypeObj = default(object);
			if (this.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return null;
			}

			var expressionType = (string)expressionTypeObj;
			return expressionType;
		}
		internal object GetTypeName(bool throwOnError)
		{
			var typeNameObj = default(object);
			if (this.TryGetValue(Constants.TYPE_ATTRIBUTE, out typeNameObj) == false || (typeNameObj is string == false && typeNameObj is SyntaxTreeNode == false))
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TYPE_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);

			return typeNameObj;
		}
		internal object GetValue(bool throwOnError)
		{
			var valueObj = default(object);
			if (this.TryGetValue(Constants.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.VALUE_ATTRIBUTE, this.GetExpressionType(throwOnError: true), this));
			return valueObj;
		}
		internal SyntaxTreeNode GetExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.EXPRESSION_ATTRIBUTE, throwOnError);
		}
		private SyntaxTreeNode GetExpression(string attributeName, bool throwOnError)
		{
			var expressionObj = default(object);
			if (this.TryGetValue(attributeName, out expressionObj) == false || expressionObj == null || expressionObj is SyntaxTreeNode == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, attributeName, this.GetExpressionType(true)), this);
				else
					return null;
			}

			var expression = (SyntaxTreeNode)expressionObj;
			return expression;
		}
		internal SyntaxTreeNode GetLeftExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.LEFT_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetRightExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.RIGHT_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetTestExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.TEST_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetIfTrueExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.IFTRUE_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetIfFalseExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.IFFALSE_ATTRIBUTE, throwOnError);
		}
		internal ArgumentsTree GetArguments(bool throwOnError)
		{
			var argumentsObj = default(object);
			if (this.TryGetValue(Constants.ARGUMENTS_ATTRIBUTE, out argumentsObj) == false || argumentsObj == null || argumentsObj is SyntaxTreeNode == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.ARGUMENTS_ATTRIBUTE, this.GetExpressionType(true)), this);
				else
					return ArgumentsTree.Empty;
			}

			var arguments = new Dictionary<string, SyntaxTreeNode>(((SyntaxTreeNode)argumentsObj).Count);
			foreach (var kv in (SyntaxTreeNode)argumentsObj)
			{
				var argument = kv.Value as SyntaxTreeNode;
				if (argument == null)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGORWRONGARGUMENT, kv.Key), this);
				arguments.Add(kv.Key, argument);
			}

			if (arguments.Count > Constants.MAX_ARGUMENTS_COUNT)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_TOOMANYARGUMENTS, Constants.MAX_ARGUMENTS_COUNT.ToString()), this);

			return new ArgumentsTree(arguments);
		}
		internal string GetPropertyOrFieldName(bool throwOnError)
		{
			var propertyOrFieldNameObj = default(object);
			if (this.TryGetValue(Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return null;
			}

			var propertyOrFieldName = (string)propertyOrFieldNameObj;
			return propertyOrFieldName;
		}
		internal object GetMethodName(bool throwOnError)
		{
			var methodNameObj = default(object);
			if (this.TryGetValue(Constants.METHOD_ATTRIBUTE, out methodNameObj) == false || methodNameObj == null)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.METHOD_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return null;
			}

			return methodNameObj;
		}
		internal bool GetUseNullPropagation(bool throwOnError)
		{
			var useNullPropagationObj = default(object);
			if (this.TryGetValue(Constants.USE_NULL_PROPAGATION_ATTRIBUTE, out useNullPropagationObj) == false || useNullPropagationObj == null)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.USE_NULL_PROPAGATION_ATTRIBUTE, this.GetExpressionType(throwOnError: true)), this);
				else
					return false;
			}
			var useNullPropagation = Convert.ToBoolean(useNullPropagationObj, Constants.DefaultFormatProvider);
			return useNullPropagation;
		}

		internal int GetLineNumber(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_LINE_NUMBER_OLD, out valueObj) == false)
				return this.GetExpressionPosition(throwOnError).LineNumber;

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_LINE_NUMBER_OLD, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		internal int GetColumnNumber(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_COLUMN_NUMBER_OLD, out valueObj) == false)
				return this.GetExpressionPosition(throwOnError).ColumnNumber;

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_COLUMN_NUMBER_OLD, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		internal int GetTokenLength(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(int);
			if (this.TryGetValue(Constants.EXPRESSION_TOKEN_LENGTH_OLD, out valueObj) == false)
				return this.GetExpressionPosition(throwOnError).TokenLength;

			if (int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out value) == false && throwOnError)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TOKEN_LENGTH_OLD, this.GetExpressionType(throwOnError: true)), this);

			return value;
		}
		internal string GetPosition(bool throwOnError)
		{
			return this.GetExpressionPosition(throwOnError).ToString();
		}
		internal ExpressionPosition GetExpressionPosition(bool throwOnError)
		{
			var valueObj = default(object);
			if (this.TryGetValue(Constants.EXPRESSION_POSITION, out valueObj) == false || (valueObj is string == false && valueObj is ILineInfo == false))
			{
				if (this.ContainsKey(Constants.EXPRESSION_LINE_NUMBER_OLD) &&
					this.ContainsKey(Constants.EXPRESSION_COLUMN_NUMBER_OLD) &&
					this.ContainsKey(Constants.EXPRESSION_TOKEN_LENGTH_OLD))
				{
					valueObj = string.Format(
						Constants.DefaultFormatProvider, "{0}:{1}+{2}",
						this.GetValueOrDefault(Constants.EXPRESSION_LINE_NUMBER_OLD, "0"),
						this.GetValueOrDefault(Constants.EXPRESSION_COLUMN_NUMBER_OLD, "0"),
						this.GetValueOrDefault(Constants.EXPRESSION_TOKEN_LENGTH_OLD, "0")
					);
				}

				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_POSITION, this.GetExpressionType(throwOnError: true)), this);
				else
					return default(ExpressionPosition);
			}

			var lineInfo = valueObj as ILineInfo;
			var positionString = valueObj as string;
			if (lineInfo != null)
				return new ExpressionPosition(lineInfo.GetLineNumber(), lineInfo.GetColumnNumber(), lineInfo.GetTokenLength());
			else
				return ExpressionPosition.Parse(positionString);
		}
		internal string GetOriginalExpression(bool throwOnError)
		{
			var valueObj = default(object);
			var value = default(string);
			if (this.TryGetValue(Constants.EXPRESSION_ORIGINAL, out valueObj) == false && this.TryGetValue(Constants.EXPRESSION_ORIGINAL_OLD, out valueObj) == false)
			{
				if (throwOnError)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ORIGINAL, this.GetExpressionType(throwOnError: true)), this);
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

		/// <summary>
		/// Check if syntax tree contain node with specified <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Name of contained node. Can't be null.</param>
		public bool ContainsKey(string key)
		{
			return this.innerDictionary.ContainsKey(key);
		}

		/// <summary>
		/// Returns collection of names of contained nodes.
		/// </summary>
		public ICollection<string> Keys
		{
			get { return this.innerDictionary.Keys; }
		}

		bool IDictionary<string, object>.Remove(string key)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Tries to retrieve node of syntax tree by its name.
		/// </summary>
		/// <returns>True is node exists, overwise false.</returns>
		public bool TryGetValue(string key, out object value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}

		/// <summary>
		/// Returns collection of contained nodes.
		/// </summary>
		public ICollection<object> Values
		{
			get { return this.innerDictionary.Values; }
		}

		/// <summary>
		/// Returns contained node by its name;
		/// </summary>
		/// <param name="key">Name of contained node. Can't be null.</param>
		/// <returns></returns>
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

		/// <summary>
		/// Returns count of contained nodes.
		/// </summary>
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

		/// <summary>
		/// Compares two syntax tree by reference.
		/// </summary>
		public override bool Equals(object obj)
		{
			return this.innerDictionary.Equals(obj);
		}
		/// <summary>
		/// Get hash code of syntax tree.
		/// </summary>
		public override int GetHashCode()
		{
			return this.innerDictionary.GetHashCode();
		}

		/// <summary>
		/// Renders syntax tree as C# expression.
		/// </summary>
		public override string ToString()
		{
			var expression = this.GetOriginalExpression(throwOnError: false);
			try
			{
				if (string.IsNullOrEmpty(expression))
					expression = this.Render();
			}
			catch (Exception error)
			{
				expression = "/failed to render expression '" + error.Message + "'/";
			}
			return expression;
		}
	}
}
