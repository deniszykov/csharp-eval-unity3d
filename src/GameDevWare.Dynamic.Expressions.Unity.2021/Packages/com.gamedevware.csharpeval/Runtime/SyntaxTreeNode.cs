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
using System.Text;
using GameDevWare.Dynamic.Expressions.CSharp;
using GameDevWare.Dynamic.Expressions.Properties;
// ReSharper disable MemberCanBePrivate.Global

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Abstract syntax tree of expression
	/// </summary>
	public class SyntaxTreeNode : IDictionary<string, object>, ILineInfo
	{
		private readonly Dictionary<string, object> innerDictionary;

		/// <summary>
		///     Returns collection of names of contained nodes.
		/// </summary>
		public ICollection<string> Keys => this.innerDictionary.Keys;

		/// <summary>
		///     Returns collection of contained nodes.
		/// </summary>
		public ICollection<object> Values => this.innerDictionary.Values;

		/// <summary>
		///     Returns contained node by its name;
		/// </summary>
		/// <param name="key">Name of contained node. Can't be null.</param>
		/// <returns></returns>
		public object this[string key] { get => this.innerDictionary[key]; set => throw new NotSupportedException(); }

		/// <summary>
		///     Returns count of contained nodes.
		/// </summary>
		public int Count => this.innerDictionary.Count;

		bool ICollection<KeyValuePair<string, object>>.IsReadOnly => ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).IsReadOnly;

		/// <summary>
		///     Creates new syntax tree from existing dictionary.
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
				if (kv.Value is IDictionary<string, object> syntaxNodeObj && !(syntaxNodeObj is SyntaxTreeNode))
				{
					newNode[kv.Key] = new SyntaxTreeNode(syntaxNodeObj);
				}
				else
				{
					newNode[kv.Key] = kv.Value;
				}
			}

			return newNode;
		}

		/// <summary>
		///     Tries to retrieve contained node by its name and covert it to <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">Type of expected value.</typeparam>
		/// <param name="key">Node name.</param>
		/// <param name="defaultValue">
		///     Default value node node with <paramref name="key" /> doesn't exists or value can't be casted
		///     to <typeparamref name="T" />.
		/// </param>
		/// <returns>True is node exists and value successfully casted to <typeparamref name="T" />, overwise false.</returns>
		public T GetValueOrDefault<T>(string key, T defaultValue = default)
		{
			var value = default(T);
			if (!this.TryGetValue(key, out var valueObj) || !(valueObj is T typedObject))
			{
				value = defaultValue;
			}
			else
			{
				value = typedObject;
			}
			return value;
		}

		internal string GetExpressionType(bool throwOnError)
		{
			if (this.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out var expressionTypeObj) && expressionTypeObj is string expressionType)
			{
				return expressionType;
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE, "<null>"), this);
			}

			return null;

		}
		internal object GetTypeName(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.TYPE_ATTRIBUTE, out var typeNameObj) || (!(typeNameObj is string) && !(typeNameObj is SyntaxTreeNode)))
			{
				if (throwOnError)
				{
					throw new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TYPE_ATTRIBUTE,
							this.GetExpressionType(false) ?? "<null>"), this);
				}

				return null;
			}

			return typeNameObj;
		}
		internal object GetValue(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.VALUE_ATTRIBUTE, out var valueObj))
			{
				if (throwOnError)
				{
					throw new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.VALUE_ATTRIBUTE,
						this.GetExpressionType(false) ?? "<null>", this));
				}

				return null;
			}

			return valueObj;
		}
		internal SyntaxTreeNode GetExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.EXPRESSION_ATTRIBUTE, throwOnError);
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
			return this.GetExpression(Constants.IF_TRUE_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetIfFalseExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.IF_FALSE_ATTRIBUTE, throwOnError);
		}
		internal ArgumentsTree GetArguments(bool throwOnError)
		{
			if (this.TryGetValue(Constants.ARGUMENTS_ATTRIBUTE, out var argumentsObj) && argumentsObj is SyntaxTreeNode syntaxTreeNode)
			{
				var arguments = new Dictionary<string, SyntaxTreeNode>(syntaxTreeNode.Count);
				foreach (var kv in syntaxTreeNode)
				{
					if (!(kv.Value is SyntaxTreeNode argument))
					{
						throw new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_MISSINGORWRONGARGUMENT, kv.Key), this);
					}

					arguments.Add(kv.Key, argument);
				}

				if (arguments.Count > Constants.MAX_ARGUMENTS_COUNT)
				{
					throw new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_TOOMANYARGUMENTS, Constants.MAX_ARGUMENTS_COUNT.ToString()),
						this);
				}

				return new ArgumentsTree(arguments);
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.ARGUMENTS_ATTRIBUTE,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return ArgumentsTree.Empty;
		}
		internal Dictionary<string, string> GetArgumentNames(bool throwOnError)
		{
			if (this.TryGetValue(Constants.ARGUMENTS_ATTRIBUTE, out var argumentsObj) && argumentsObj is SyntaxTreeNode syntaxTreeNode)
			{
				var arguments = new Dictionary<string, string>(syntaxTreeNode.Count);
				foreach (var kv in syntaxTreeNode)
				{
					if (!(kv.Value is string argument))
					{
						throw new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_MISSINGORWRONGARGUMENT, kv.Key), this);
					}

					arguments.Add(kv.Key, argument);
				}

				if (arguments.Count > Constants.MAX_ARGUMENTS_COUNT)
				{
					throw new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_TOOMANYARGUMENTS, Constants.MAX_ARGUMENTS_COUNT.ToString()),
						this);
				}

				return arguments;
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.ARGUMENTS_ATTRIBUTE,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return null;
		}
		internal string GetMemberName(bool throwOnError)
		{
			if (!(this.TryGetValue(Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out var memberNameObj) ||
					this.TryGetValue(Constants.NAME_ATTRIBUTE, out memberNameObj)) ||
				!(memberNameObj is string))
			{
				if (throwOnError)
				{
					throw new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.NAME_ATTRIBUTE,
							this.GetExpressionType(false) ?? "<null>"), this);
				}

				return null;
			}

			var memberName = (string)memberNameObj;
			return memberName;
		}
		internal object GetName(bool throwOnError)
		{
			if (this.TryGetValue(Constants.NAME_ATTRIBUTE, out var nameObj))
			{
				return nameObj;
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.NAME_ATTRIBUTE,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return null;

		}
		internal object GetMethodName(bool throwOnError)
		{
			if (this.TryGetValue(Constants.METHOD_ATTRIBUTE, out var methodNameObj) && methodNameObj != null)
			{
				return methodNameObj;
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.METHOD_ATTRIBUTE,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return null;

		}
		internal SyntaxTreeNode GetConversion(bool throwOnError)
		{
			return this.GetExpression(Constants.CONVERSION_ATTRIBUTE, throwOnError);
		}
		internal IEnumerable<SyntaxTreeNode> EnumerateBindings(bool throwOnError)
		{
			return this.EnumerateOrderedSyntaxNodes(Constants.BINDINGS_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetMember(bool throwOnError)
		{
			return this.GetExpression(Constants.MEMBER_ATTRIBUTE, throwOnError);
		}
		internal SyntaxTreeNode GetNewExpression(bool throwOnError)
		{
			return this.GetExpression(Constants.NEW_ATTRIBUTE, throwOnError);
		}

		internal IEnumerable<SyntaxTreeNode> EnumerateInitializers(bool throwOnError)
		{
			if (this.ContainsKey(Constants.ARGUMENTS_ATTRIBUTE)) // old style initializers
			{
				return this.EnumerateOrderedSyntaxNodes(Constants.ARGUMENTS_ATTRIBUTE, throwOnError);
			}

			return this.EnumerateOrderedSyntaxNodes(Constants.INITIALIZERS_ATTRIBUTE, throwOnError);
		}

		internal SyntaxTreeNode GetTypeArguments(bool throwOnError)
		{
			return this.GetExpression(Constants.ARGUMENTS_ATTRIBUTE, throwOnError);
		}
		internal bool GetUseNullPropagation(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.USE_NULL_PROPAGATION_ATTRIBUTE, out var useNullPropagationObj) || useNullPropagationObj == null)
			{
				if (throwOnError)
				{
					throw new ExpressionParserException(
						string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.USE_NULL_PROPAGATION_ATTRIBUTE,
							this.GetExpressionType(false) ?? "<null>"), this);
				}

				return false;
			}

			var useNullPropagation = Convert.ToBoolean(useNullPropagationObj, Constants.DefaultFormatProvider);
			return useNullPropagation;
		}
		private SyntaxTreeNode GetExpression(string attributeName, bool throwOnError)
		{
			if (this.TryGetValue(attributeName, out var expressionObj) && expressionObj is SyntaxTreeNode syntaxTreeNode)
			{
				return syntaxTreeNode;
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, attributeName, this.GetExpressionType(false) ?? "<null>"),
					this);
			}

			return null;

		}

		internal IEnumerable<SyntaxTreeNode> EnumerateOrderedSyntaxNodes(string propertyName, bool throwOnError)
		{
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

			if (this.TryGetValue(propertyName, out var initializersObj))
			{
				switch (initializersObj)
				{
					case List<object> listOfSyntaxNodes:
					{
						foreach (var syntaxNodeObj in listOfSyntaxNodes)
						{
							yield return syntaxNodeObj as SyntaxTreeNode;
						}

						yield break;
					}
					case IDictionary<string, object> initializersDic:
					{
						for (var index = 0; index < initializersDic.Count; index++)
						{
							initializersDic.TryGetValue(Constants.GetIndexAsString(index), out var initializerObj);
							yield return initializerObj as SyntaxTreeNode;
						}

						yield break;
					}
				}
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.INITIALIZERS_ATTRIBUTE, this.GetExpressionType(false) ?? "<null>"),
					this);
			}
		}

		internal int GetLineNumber(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.EXPRESSION_LINE_NUMBER_OLD, out var valueObj))
				return this.GetExpressionPositionOrDefault(throwOnError).LineNumber;

			if (!int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out var value) && throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_LINE_NUMBER_OLD,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return value;
		}
		internal int GetColumnNumber(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.EXPRESSION_COLUMN_NUMBER_OLD, out var valueObj))
				return this.GetExpressionPositionOrDefault(throwOnError).ColumnNumber;

			if (!int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out var value) && throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_COLUMN_NUMBER_OLD,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return value;
		}
		internal int GetTokenLength(bool throwOnError)
		{
			if (!this.TryGetValue(Constants.EXPRESSION_TOKEN_LENGTH_OLD, out var valueObj))
				return this.GetExpressionPositionOrDefault(throwOnError).TokenLength;

			if (!int.TryParse(Convert.ToString(valueObj, Constants.DefaultFormatProvider), out var value) && throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TOKEN_LENGTH_OLD,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return value;
		}
		internal string GetPositionOrDefault(bool throwOnError)
		{
			return this.GetExpressionPositionOrDefault(throwOnError).ToString();
		}
		internal ExpressionPosition GetExpressionPositionOrDefault(bool throwOnError)
		{
			if (this.TryGetValue(Constants.EXPRESSION_POSITION, out var valueObj) && (valueObj is string || valueObj is ILineInfo))
			{
				var positionString = valueObj as string;
				if (valueObj is ILineInfo lineInfo)
				{
					return new ExpressionPosition(lineInfo.GetLineNumber(), lineInfo.GetColumnNumber(), lineInfo.GetTokenLength());
				}

				return ExpressionPosition.Parse(positionString);
			}

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
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_POSITION,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			return default;
		}
		internal string GetCSharpExpressionOrNull(bool throwOnError)
		{
			if (this.TryGetValue(Constants.EXPRESSION_ORIGINAL_C_SHARP, out var valueObj) ||
				this.TryGetValue(Constants.EXPRESSION_ORIGINAL_ALT, out valueObj) ||
				this.TryGetValue(Constants.EXPRESSION_ORIGINAL_OLD, out valueObj))
			{
				return Convert.ToString(valueObj, Constants.DefaultFormatProvider);
			}

			if (throwOnError)
			{
				throw new ExpressionParserException(
					string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ORIGINAL_C_SHARP,
						this.GetExpressionType(false) ?? "<null>"), this);
			}

			// ReSharper disable once ExpressionIsAlwaysNull
			return null;

		}

		/// <summary>
		///     Compares two syntax tree by reference.
		/// </summary>
		public override bool Equals(object obj)
		{
			return this.innerDictionary.Equals(obj);
		}
		/// <summary>
		///     Get hash code of syntax tree.
		/// </summary>
		public override int GetHashCode()
		{
			return this.innerDictionary.GetHashCode();
		}

		/// <summary>
		///     Format syntax tree as a C# expression. Throw exceptions if exception could not be formed.
		/// </summary>
		/// <returns>C# Expression.</returns>
		public string ToCSharpExpression()
		{
			var expression = this.GetCSharpExpressionOrNull(false);
			if (string.IsNullOrEmpty(expression))
			{
				expression = CSharpExpression.Format(this);
			}
			return expression;
		}

		/// <summary>
		///     Format syntax tree as C# expression.
		/// </summary>
		public override string ToString()
		{
			try
			{
				return this.ToCSharpExpression();
			}
			catch
			{
				var sb = new StringBuilder();
				sb.Append("{ ");
				foreach (var kv in this)
				{
					sb.Append(kv.Key).Append(": ").Append('\'').Append(kv.Value).Append("', ");
				}

				sb.Append('}');
				return sb.ToString();
			}
		}

		void IDictionary<string, object>.Add(string key, object value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///     Check if syntax tree contain node with specified <paramref name="key" />.
		/// </summary>
		/// <param name="key">Name of contained node. Can't be null.</param>
		public bool ContainsKey(string key)
		{
			return this.innerDictionary.ContainsKey(key);
		}

		bool IDictionary<string, object>.Remove(string key)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///     Tries to retrieve node of syntax tree by its name.
		/// </summary>
		/// <returns>True is node exists, overwise false.</returns>
		public bool TryGetValue(string key, out object value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}

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

		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
		{
			return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).Remove(item);
		}

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			return ((ICollection<KeyValuePair<string, object>>)this.innerDictionary).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.innerDictionary).GetEnumerator();
		}

		int ILineInfo.GetLineNumber()
		{
			return this.GetLineNumber(false);
		}
		int ILineInfo.GetColumnNumber()
		{
			return this.GetColumnNumber(false);
		}
		int ILineInfo.GetTokenLength()
		{
			return this.GetTokenLength(false);
		}
	}
}
