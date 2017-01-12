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
using System.Text;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public struct ParserNode : ILineInfo
	{
		public class ParserNodeCollection : List<ParserNode>
		{
		}

		[Flags]
		private enum TypeNameOptions
		{
			None = 0,
			Aliases = 0x1 << 0,
			ShortNames = 0x1 << 1,
			Arrays = 0x1 << 2,

			All = Aliases | ShortNames | Arrays
		}

		private static readonly Dictionary<int, string> ExpressionTypeByToken = new Dictionary<int, string>
		{
			{ (int)TokenType.Resolve, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			{ (int)TokenType.NullResolve, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			{ (int)TokenType.Identifier, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			{ (int)TokenType.Literal, Constants.EXPRESSION_TYPE_CONSTANT },
			{ (int)TokenType.Number, Constants.EXPRESSION_TYPE_CONSTANT },
			{ (int)TokenType.Convert, Constants.EXPRESSION_TYPE_CONVERT },
			{ (int)TokenType.Group, Constants.EXPRESSION_TYPE_GROUP },
			{ (int)TokenType.UncheckedScope, Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE },
			{ (int)TokenType.CheckedScope, Constants.EXPRESSION_TYPE_CHECKED_SCOPE },
			{ (int)TokenType.Plus, Constants.EXPRESSION_TYPE_UNARYPLUS },
			{ (int)TokenType.Minus, Constants.EXPRESSION_TYPE_NEGATE },
			{ (int)TokenType.Not, Constants.EXPRESSION_TYPE_NOT },
			{ (int)TokenType.Compl, Constants.EXPRESSION_TYPE_COMPLEMENT },
			{ (int)TokenType.Div, Constants.EXPRESSION_TYPE_DIVIDE },
			{ (int)TokenType.Mul, Constants.EXPRESSION_TYPE_MULTIPLY },
			{ (int)TokenType.Pow, Constants.EXPRESSION_TYPE_POWER },
			{ (int)TokenType.Mod, Constants.EXPRESSION_TYPE_MODULO },
			{ (int)TokenType.Add, Constants.EXPRESSION_TYPE_ADD },
			{ (int)TokenType.Subtract, Constants.EXPRESSION_TYPE_SUBTRACT },
			{ (int)TokenType.Lshift, Constants.EXPRESSION_TYPE_LEFTSHIFT },
			{ (int)TokenType.Rshift, Constants.EXPRESSION_TYPE_RIGHTSHIFT},
			{ (int)TokenType.Gt, Constants.EXPRESSION_TYPE_GREATERTHAN },
			{ (int)TokenType.Gte, Constants.EXPRESSION_TYPE_GREATERTHAN_OR_EQUAL },
			{ (int)TokenType.Lt, Constants.EXPRESSION_TYPE_LESSTHAN },
			{ (int)TokenType.Lte, Constants.EXPRESSION_TYPE_LESSTHAN_OR_EQUAL },
			{ (int)TokenType.Is, Constants.EXPRESSION_TYPE_TYPEIS  },
			{ (int)TokenType.As, Constants.EXPRESSION_TYPE_TYPEAS },
			{ (int)TokenType.Eq, Constants.EXPRESSION_TYPE_EQUAL },
			{ (int)TokenType.Neq, Constants.EXPRESSION_TYPE_NOTEQUAL },
			{ (int)TokenType.And, Constants.EXPRESSION_TYPE_AND },
			{ (int)TokenType.Or, Constants.EXPRESSION_TYPE_OR },
			{ (int)TokenType.Xor, Constants.EXPRESSION_TYPE_EXCLUSIVEOR },
			{ (int)TokenType.AndAlso, Constants.EXPRESSION_TYPE_ANDALSO },
			{ (int)TokenType.OrElse, Constants.EXPRESSION_TYPE_ORELSE },
			{ (int)TokenType.Coalesce, Constants.EXPRESSION_TYPE_COALESCE },
			{ (int)TokenType.Cond, Constants.EXPRESSION_TYPE_CONDITION },
			{ (int)TokenType.Call, Constants.EXPRESSION_TYPE_INVOKE },
			{ (int)TokenType.Typeof, Constants.EXPRESSION_TYPE_TYPEOF },
			{ (int)TokenType.Default, Constants.EXPRESSION_TYPE_DEFAULT },
			{ (int)TokenType.New, Constants.EXPRESSION_TYPE_NEW },
			{ (int)TokenType.Lbracket, Constants.EXPRESSION_TYPE_INDEX },
			{ (int)TokenType.Lambda, Constants.EXPRESSION_TYPE_LAMBDA },
		};

		private static readonly Dictionary<string, string> TypeAliases = new Dictionary<string, string>
		{
			// ReSharper disable StringLiteralTypo
			{ "void", typeof(void).FullName },
			{ "char", typeof(char).FullName },
			{ "bool", typeof(bool).FullName },
			{ "byte", typeof(byte).FullName },
			{ "sbyte", typeof(sbyte).FullName },
			{ "decimal", typeof(decimal).FullName },
			{ "double", typeof(double).FullName },
			{ "float", typeof(float).FullName },
			{ "int", typeof(int).FullName },
			{ "uint", typeof(uint).FullName },
			{ "long", typeof(long).FullName },
			{ "ulong", typeof(ulong).FullName },
			{ "object", typeof(object).FullName },
			{ "short", typeof(short).FullName },
			{ "ushort", typeof(ushort).FullName },
			{ "string", typeof(string).FullName }
			// ReSharper restore StringLiteralTypo
		};

		public readonly TokenType Type;
		public readonly Token Lexeme;
		public readonly string Value;
		public readonly ParserNodeCollection Nodes;

		int ILineInfo.GetLineNumber()
		{
			return Lexeme.LineNumber;
		}
		int ILineInfo.GetColumnNumber()
		{
			return Lexeme.ColumnNumber;
		}
		int ILineInfo.GetTokenLength()
		{
			return Lexeme.TokenLength;
		}

		public ParserNode(TokenType type, Token lexeme, string value, ParserNodeCollection nodes = null)
		{
			this.Type = type;
			this.Lexeme = lexeme;
			this.Value = value ?? lexeme.Value;
			this.Nodes = nodes ?? new ParserNodeCollection();
		}
		public ParserNode(Token lexeme)
			: this(lexeme.Type, lexeme, lexeme.Value)
		{

		}

		public ExpressionTree ToExpressionTree(bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			try
			{
				var expressionType = default(string);
				if (ExpressionTypeByToken.TryGetValue((int)this.Type, out expressionType) == false)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENTYPE, this.Type), this);

				var node = new Dictionary<string, object>
				{
					{ Constants.EXPRESSION_LINE_NUMBER, this.Lexeme.LineNumber },
					{ Constants.EXPRESSION_COLUMN_NUMBER, this.Lexeme.ColumnNumber },
					{ Constants.EXPRESSION_TOKEN_LENGTH, this.Lexeme.TokenLength },
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, expressionType },
				};

				switch (this.Type)
				{
					case TokenType.NullResolve:
					case TokenType.Resolve:
						Ensure(this, 2, TokenType.None, TokenType.Identifier);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.Nodes[1].Value;
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this.Nodes[1], 0);
						node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = this.Type == TokenType.NullResolve ? Constants.TrueObject : Constants.FalseObject;
						break;
					case TokenType.Identifier:
						if (this.Nodes.Count == 0 && (this.Value == Constants.VALUE_TRUE_STRING || this.Value == Constants.VALUE_FALSE_STRING || this.Value == Constants.VALUE_NULL_STRING))
						{
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Literal]; // constant
							node[Constants.TYPE_ATTRIBUTE] = this.Value == Constants.VALUE_NULL_STRING ? typeof(object).FullName : typeof(bool).FullName;
							node[Constants.VALUE_ATTRIBUTE] = this.Value == Constants.VALUE_TRUE_STRING ? Constants.TrueObject : this.Value == Constants.VALUE_FALSE_STRING ? Constants.FalseObject : null;
						}
						node[Constants.EXPRESSION_ATTRIBUTE] = null;
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this, 0);
						node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.Value;
						break;
					case TokenType.Literal:
						node[Constants.TYPE_ATTRIBUTE] = string.IsNullOrEmpty(this.Value) == false && this.Value[0] == '\'' ? typeof(char).FullName : typeof(string).FullName;
						node[Constants.VALUE_ATTRIBUTE] = this.UnescapeAndUnquote(this.Value);
						break;
					case TokenType.Number:
						var floatTrait = this.Value.IndexOf('f') >= 0;
						var doubleTrait = this.Value.IndexOf('.') >= 0 || this.Value.IndexOf('d') >= 0;
						var longTrait = this.Value.IndexOf('l') >= 0;
						var unsignedTrait = this.Value.IndexOf('u') >= 0;
						var decimalTrait = this.Value.IndexOf('m') >= 0;

						if (decimalTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(decimal).FullName;
						else if (floatTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(float).FullName;
						else if (doubleTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(double).FullName;
						else if (longTrait && !unsignedTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(long).FullName;
						else if (longTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(ulong).FullName;
						else if (unsignedTrait)
							node[Constants.TYPE_ATTRIBUTE] = typeof(uint).FullName;
						else
							node[Constants.TYPE_ATTRIBUTE] = typeof(int).FullName;

						node[Constants.VALUE_ATTRIBUTE] = this.Value.TrimEnd('f', 'F', 'd', 'd', 'l', 'L', 'u', 'U', 'm', 'M');
						break;
					case TokenType.Convert:
						Ensure(this, 2);
						node[Constants.TYPE_ATTRIBUTE] = this.Nodes[0].ToTypeName(TypeNameOptions.All);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[1].ToExpressionTree(checkedScope);
						if (checkedScope)
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKEDSUFFIX;
						break;
					case TokenType.CheckedScope:
						Ensure(this, 1);
						// ReSharper disable once RedundantArgumentDefaultValue
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope: true);
						break;
					case TokenType.UncheckedScope:
						Ensure(this, 1);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope: false);
						break;
					case TokenType.As:
					case TokenType.Is:
						Ensure(this, 2);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						node[Constants.TYPE_ATTRIBUTE] = this.Nodes[1].ToTypeName(TypeNameOptions.All);
						break;
					case TokenType.Default:
					case TokenType.Typeof:
						Ensure(this, 1);
						node[Constants.TYPE_ATTRIBUTE] = this.Nodes[0].ToTypeName(TypeNameOptions.All);
						break;
					case TokenType.Group:
					case TokenType.Plus:
					case TokenType.Minus:
					case TokenType.Not:
					case TokenType.Compl:
						Ensure(this, 1);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						if (checkedScope && this.Type == TokenType.Minus)
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKEDSUFFIX;
						break;
					case TokenType.Div:
					case TokenType.Mul:
					case TokenType.Pow:
					case TokenType.Mod:
					case TokenType.Add:
					case TokenType.Subtract:
					case TokenType.Lshift:
					case TokenType.Rshift:
					case TokenType.Gt:
					case TokenType.Gte:
					case TokenType.Lt:
					case TokenType.Lte:
					case TokenType.Eq:
					case TokenType.Neq:
					case TokenType.And:
					case TokenType.Or:
					case TokenType.Xor:
					case TokenType.AndAlso:
					case TokenType.OrElse:
					case TokenType.Coalesce:
						Ensure(this, 2);
						node[Constants.LEFT_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						node[Constants.RIGHT_ATTRIBUTE] = this.Nodes[1].ToExpressionTree(checkedScope);
						if (checkedScope && (this.Type == TokenType.Add || this.Type == TokenType.Mul || this.Type == TokenType.Subtract))
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKEDSUFFIX;
						break;
					case TokenType.Cond:
						Ensure(this, 3);
						node[Constants.TEST_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						node[Constants.IFTRUE_ATTRIBUTE] = this.Nodes[1].ToExpressionTree(checkedScope);
						node[Constants.IFFALSE_ATTRIBUTE] = this.Nodes[2].ToExpressionTree(checkedScope);
						break;
					case TokenType.Lambda:
						Ensure(this, 2, TokenType.Arguments);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 0, checkedScope);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[1].ToExpressionTree(checkedScope);
						break;
					case TokenType.Call:
						Ensure(this, 1);

						var isNullPropagation = false;
						if (this.Value == "[" || this.Value == "?[")
						{
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Lbracket];
							isNullPropagation = this.Value == "?[";
						}

						node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToExpressionTree(checkedScope);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 1, checkedScope);
						node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = isNullPropagation ? Constants.TrueObject : Constants.FalseObject;
						break;
					case TokenType.New:
						Ensure(this, 2, TokenType.None, TokenType.Arguments);

						if (this.Nodes[1].Value == "[")
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] = Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS;

						node[Constants.TYPE_ATTRIBUTE] = this.Nodes[0].ToTypeName(TypeNameOptions.All);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 1, checkedScope);

						break;

					default:
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEBUILDINGTREE, this.Type), this);
				}

				return new ExpressionTree(node);
			}
			catch (ExpressionParserException)
			{
				throw;
			}
			catch (System.Threading.ThreadAbortException)
			{
				throw;
			}
			catch (Exception exception)
			{
				throw new ExpressionParserException(exception.Message, exception, this);
			}
		}

		private object ToTypeName(TypeNameOptions options)
		{
			var allowShortName = (options & TypeNameOptions.ShortNames) != 0;
			var allowAliases = (options & TypeNameOptions.Aliases) != 0;
			var allowArrays = (options & TypeNameOptions.Arrays) != 0;
			if (this.Type == TokenType.Identifier && this.Nodes.Count == 0 && allowShortName)
			{
				var typeName = default(string);
				if (allowAliases && TypeAliases.TryGetValue(this.Value, out typeName))
					return typeName;
				else
					return this.Value;
			}

			if (this.Type == TokenType.Call && this.Nodes.Count == 2 && this.Value == "[" && this.Nodes[1].Nodes.Count == 0 && allowArrays)
			{
				var arrayNode = new ParserNode(TokenType.Identifier, this.Lexeme, typeof(Array).Name);
				var argumentsNode = new ParserNode(TokenType.Arguments, this.Lexeme, "<");
				argumentsNode.Nodes.Add(this.Nodes[0]);
				arrayNode.Nodes.Add(argumentsNode);
				return arrayNode.ToTypeName(TypeNameOptions.None);
			}

			var node = new Dictionary<string, object>
			{
				{ Constants.EXPRESSION_LINE_NUMBER, this.Lexeme.LineNumber },
				{ Constants.EXPRESSION_COLUMN_NUMBER, this.Lexeme.ColumnNumber },
				{ Constants.EXPRESSION_TOKEN_LENGTH, this.Lexeme.TokenLength },
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			};

			if (this.Type == TokenType.Resolve)
			{
				Ensure(this, 2, TokenType.None, TokenType.Identifier);
				node[Constants.EXPRESSION_ATTRIBUTE] = this.Nodes[0].ToTypeName(TypeNameOptions.None);
				node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.Nodes[1].Value;
				node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this.Nodes[1], 0);
				node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = Constants.FalseObject;
			}
			else if (this.Type == TokenType.Identifier)
			{
				var typeName = this.Value;
				if (allowAliases && TypeAliases.TryGetValue(this.Value, out typeName) == false)
					typeName = this.Value;

				node[Constants.EXPRESSION_ATTRIBUTE] = null;
				node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = typeName;
				node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = Constants.FalseObject;
				node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this, 0);
			}
			else
			{
				throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TYPENAMEEXPECTED, this);
			}

			return new ExpressionTree(node);
		}

		private static Dictionary<string, object> PrepareArguments(ParserNode node, int argumentChildIndex, bool checkedScope)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= node.Nodes.Count || node.Nodes[argumentChildIndex].Nodes.Count == 0)
				return null;

			args = new Dictionary<string, object>();
			var argIdx = 0;
			foreach (var argNode in node.Nodes[argumentChildIndex].Nodes)
			{
				if (argNode.Type == TokenType.Colon)
				{
					Ensure(argNode, 2, TokenType.Identifier);

					var argName = argNode.Nodes[0].Value;
					args[argName] = argNode.Nodes[1].ToExpressionTree(checkedScope);
				}
				else
				{
					var argName = Constants.GetIndexAsString(argIdx++);
					args[argName] = argNode.ToExpressionTree(checkedScope);
				}
			}
			return args;
		}
		private static Dictionary<string, object> PrepareTypeArguments(ParserNode node, int argumentChildIndex)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= node.Nodes.Count || node.Nodes[argumentChildIndex].Nodes.Count == 0)
				return null;

			args = new Dictionary<string, object>();
			var argIdx = 0;
			foreach (var argNode in node.Nodes[argumentChildIndex].Nodes)
			{
				var argName = Constants.GetIndexAsString(argIdx++);
				args[argName] = argNode.ToTypeName(TypeNameOptions.Aliases | TypeNameOptions.Arrays);
			}
			return args;
		}
		private static void Ensure(ParserNode node, int childCount, TokenType childType0 = TokenType.None, TokenType childType1 = TokenType.None, TokenType childType2 = TokenType.None)
		{
			// ReSharper disable HeapView.BoxingAllocation
			if (node.Nodes.Count < childCount)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDCOUNTOFNODE, node.Type, node.Nodes.Count, childCount), node);

			for (int i = 0, ct = Math.Min(3, childCount); i < ct; i++)
			{
				var childNode = node.Nodes[i];
				var childNodeType = node.Nodes[i].Type;
				if (i == 0 && childType0 != TokenType.None && childType0 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, node.Type, childNodeType, childType0), childNode);
				if (i == 1 && childType1 != TokenType.None && childType1 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, node.Type, childNodeType, childType1), childNode);
				if (i == 2 && childType2 != TokenType.None && childType2 != childNodeType)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, node.Type, childNodeType, childType2), childNode);
			}
			// ReSharper restore HeapView.BoxingAllocation
		}
		private object UnescapeAndUnquote(string value)
		{
			if (value == null) return null;
			try
			{
				return StringUtils.UnescapeAndUnquote(value);
			}
			catch (InvalidOperationException e)
			{
				throw new ExpressionParserException(e.Message, e, this.Lexeme);
			}
		}

		private void Write(StringBuilder sb, int depth)
		{
			sb.Append(' ', depth * 4)
				.Append(this.Type)
				.Append('\'').Append(this.Value).Append('\'');

			for (var i = 0; i < this.Nodes.Count; i++)
			{
				sb.Append("\r\n").Append(' ', depth * 4);
				this.Nodes[i].Write(sb, depth + 1);
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			this.Write(sb, 0);
			return sb.ToString();
		}
	}
}
