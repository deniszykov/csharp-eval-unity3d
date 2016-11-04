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
			public ParserNodeCollection()
			{
			}
		}

		private static readonly Dictionary<int, string> ExpressionTypeByToken = new Dictionary<int, string>
		{
			{ (int)TokenType.Resolve, "PropertyOrField" },
			{ (int)TokenType.NullResolve, "PropertyOrField" },
			{ (int)TokenType.Identifier, "PropertyOrField" },
			{ (int)TokenType.Literal, "Constant" },
			{ (int)TokenType.Number, "Constant" },
			{ (int)TokenType.Convert, "Convert" },
			{ (int)TokenType.Group, "Group" },
			{ (int)TokenType.UncheckedScope, "UncheckedScope" },
			{ (int)TokenType.CheckedScope, "CheckedScope" },
			{ (int)TokenType.Plus, "UnaryPlus" },
			{ (int)TokenType.Minus, "Negate" },
			{ (int)TokenType.Not, "Not" },
			{ (int)TokenType.Compl, "Complement" },
			{ (int)TokenType.Div, "Divide" },
			{ (int)TokenType.Mul, "Multiply" },
			{ (int)TokenType.Mod, "Modulo" },
			{ (int)TokenType.Add, "Add" },
			{ (int)TokenType.Subtract, "Subtract" },
			{ (int)TokenType.Lshift, "LeftShift" },
			{ (int)TokenType.Rshift, "RightShift" },
			{ (int)TokenType.Gt, "GreaterThan" },
			{ (int)TokenType.Gte, "GreaterThanOrEqual" },
			{ (int)TokenType.Lt, "LessThan" },
			{ (int)TokenType.Lte, "LessThanOrEqual" },
			{ (int)TokenType.Is, "TypeIs" },
			{ (int)TokenType.As, "TypeAs" },
			{ (int)TokenType.Eq, "Equal" },
			{ (int)TokenType.Neq, "NotEqual" },
			{ (int)TokenType.And, "And" },
			{ (int)TokenType.Or, "Or" },
			{ (int)TokenType.Xor, "ExclusiveOr" },
			{ (int)TokenType.AndAlso, "AndAlso" },
			{ (int)TokenType.OrElse, "OrElse" },
			{ (int)TokenType.Coalesce, "Coalesce" },
			{ (int)TokenType.Cond, "Condition" },
			{ (int)TokenType.Call, "Invoke" },
			{ (int)TokenType.Typeof, "TypeOf" },
			{ (int)TokenType.Default, "Default" },
			{ (int)TokenType.New, "New" },
			{ (int)TokenType.Lbracket, "Index" },
		};

		private static readonly Dictionary<string, string> TypeAliases = new Dictionary<string, string>
		{
			// ReSharper disable StringLiteralTypo
			{ "void", "System.Void" },
			{ "char", "System.Char" },
			{ "bool", "System.Boolean" },
			{ "byte", "System.Byte" },
			{ "sbyte", "System.SByte" },
			{ "decimal", "System.Decimal" },
			{ "double", "System.Double" },
			{ "float", "System.Single" },
			{ "int", "System.Int32" },
			{ "uint", "System.UInt32" },
			{ "long", "System.Int64" },
			{ "ulong", "System.UInt64" },
			{ "object", "System.Object" },
			{ "short", "System.Int16" },
			{ "ushort", "System.UInt16" },
			{ "string", "System.String" }
			// ReSharper restore StringLiteralTypo
		};

		public readonly TokenType Type;
		public readonly Token Lexeme;
		public readonly string Value;
		public readonly ParserNodeCollection Childs;

		int ILineInfo.LineNumber { get { return Lexeme.LineNumber; } }
		int ILineInfo.ColumnNumber { get { return Lexeme.ColumnNumber; } }
		int ILineInfo.TokenLength { get { return Lexeme.TokenLength; } }

		public ParserNode(TokenType type, Token lexeme, string value, ParserNodeCollection childs = null)
		{
			this.Type = type;
			this.Lexeme = lexeme;
			this.Value = value ?? lexeme.Value;
			this.Childs = childs ?? new ParserNodeCollection();
		}
		public ParserNode(Token lexeme)
			: this(lexeme.Type, lexeme, lexeme.Value)
		{

		}

		public ExpressionTree ToExpressionTree(bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			try
			{
				var node = new Dictionary<string, object>
				{
					{ ExpressionTree.EXPRESSION_LINE, this.Lexeme.LineNumber },
					{ ExpressionTree.EXPRESSION_COLUMN, this.Lexeme.ColumnNumber },
					{ ExpressionTree.EXPRESSION_LENGTH, this.Lexeme.TokenLength },
					{ ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, ExpressionTypeByToken[(int)this.Type] },
				};

				switch (this.Type)
				{
					case TokenType.NullResolve:
					case TokenType.Resolve:
						Ensure(this, 2, TokenType.None, TokenType.Identifier);
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						node[ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.Childs[1].Value;
						node[ExpressionTree.USE_NULL_PROPAGATION_ATTRIBUTE] = this.Type == TokenType.NullResolve ? ExpressionTree.TrueConst : ExpressionTree.FalseConst;
						break;
					case TokenType.Identifier:
						if (this.Value == "true" || this.Value == "false" || this.Value == "null")
						{
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Literal]; // constant
							node[ExpressionTree.TYPE_ATTRIBUTE] = this.Value == "null" ? "Object" : "Boolean";
							node[ExpressionTree.VALUE_ATTRIBUTE] = this.Value == "true" ? ExpressionTree.TrueConst :
																	this.Value == "false" ? ExpressionTree.FalseConst : null;
						}
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = null;
						node[ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.Value;
						break;
					case TokenType.Literal:
						node[ExpressionTree.TYPE_ATTRIBUTE] = string.IsNullOrEmpty(this.Value) == false && this.Value[0] == '\'' ? "Char" : "String";
						node[ExpressionTree.VALUE_ATTRIBUTE] = this.UnescapeAndUnquote(this.Value);
						break;
					case TokenType.Number:
						var floatTrait = this.Value.IndexOf('f') >= 0;
						var doubleTrait = this.Value.IndexOf('.') >= 0 || this.Value.IndexOf('d') >= 0;
						var longTrait = this.Value.IndexOf('l') >= 0;
						var unsignedTrait = this.Value.IndexOf('u') >= 0;
						var decimalTrait = this.Value.IndexOf('m') >= 0;

						if (decimalTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "Decimal";
						else if (floatTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "Single";
						else if (doubleTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "Double";
						else if (longTrait && !unsignedTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "Int64";
						else if (longTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "UInt64";
						else if (unsignedTrait)
							node[ExpressionTree.TYPE_ATTRIBUTE] = "UInt32";
						else
							node[ExpressionTree.TYPE_ATTRIBUTE] = "Int32";

						node[ExpressionTree.VALUE_ATTRIBUTE] = this.Value.TrimEnd('f', 'F', 'd', 'd', 'l', 'L', 'u', 'U', 'm', 'M');
						break;
					case TokenType.Convert:
						Ensure(this, 2);
						node[ExpressionTree.TYPE_ATTRIBUTE] = this.Childs[0].ToTypeName();
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[1].ToExpressionTree(checkedScope);
						if (checkedScope)
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] += "Checked";
						break;
					case TokenType.CheckedScope:
						Ensure(this, 1);
						// ReSharper disable once RedundantArgumentDefaultValue
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope: true);
						break;
					case TokenType.UncheckedScope:
						Ensure(this, 1);
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope: false);
						break;
					case TokenType.As:
					case TokenType.Is:
						Ensure(this, 2);
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						node[ExpressionTree.TYPE_ATTRIBUTE] = this.Childs[1].ToTypeName();
						break;
					case TokenType.Default:
					case TokenType.Typeof:
						Ensure(this, 1);
						node[ExpressionTree.TYPE_ATTRIBUTE] = this.Childs[0].ToTypeName();
						break;
					case TokenType.Group:
					case TokenType.Plus:
					case TokenType.Minus:
					case TokenType.Not:
					case TokenType.Compl:
						Ensure(this, 1);
						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						if (checkedScope && this.Type == TokenType.Minus)
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] += "Checked";
						break;
					case TokenType.Div:
					case TokenType.Mul:
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
						node[ExpressionTree.LEFT_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						node[ExpressionTree.RIGHT_ATTRIBUTE] = this.Childs[1].ToExpressionTree(checkedScope);
						if (checkedScope && (this.Type == TokenType.Add || this.Type == TokenType.Mul || this.Type == TokenType.Subtract))
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] += "Checked";
						break;
					case TokenType.Cond:
						Ensure(this, 3);
						node[ExpressionTree.TEST_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						node[ExpressionTree.IFTRUE_ATTRIBUTE] = this.Childs[1].ToExpressionTree(checkedScope);
						node[ExpressionTree.IFFALSE_ATTRIBUTE] = this.Childs[2].ToExpressionTree(checkedScope);
						break;
					case TokenType.Call:
						Ensure(this, 1);

						var isNullPropagation = false;
						if (this.Value == "[" || this.Value == "?[")
						{
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Lbracket];
							isNullPropagation = this.Value == "?[";
						}

						node[ExpressionTree.EXPRESSION_ATTRIBUTE] = this.Childs[0].ToExpressionTree(checkedScope);
						node[ExpressionTree.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, checkedScope);
						node[ExpressionTree.USE_NULL_PROPAGATION_ATTRIBUTE] = isNullPropagation ? ExpressionTree.TrueConst : ExpressionTree.FalseConst;
						break;
					case TokenType.New:
						Ensure(this, 1, TokenType.Call);

						if (this.Childs[0].Value == "[")
							node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE] = "NewArrayBounds";

						node[ExpressionTree.TYPE_ATTRIBUTE] = this.Childs[0].Childs[0].ToTypeName();
						node[ExpressionTree.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this.Childs[0], checkedScope);

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
		public string ToTypeName(bool denyAliases = false)
		{
			var typeName = default(string);
			switch (this.Type)
			{
				case TokenType.Identifier:
					if (denyAliases || TypeAliases.TryGetValue(this.Value, out typeName) == false)
						typeName = this.Value;
					break;
				case TokenType.Resolve:
					typeName = this.Childs[0].ToTypeName(denyAliases: true) + "." + this.Childs[1].ToTypeName(denyAliases: true);
					break;
				case TokenType.Call:
					if (this.Childs.Count != 2 || this.Value != "[" || this.Childs[1].Childs.Count != 0) // array syntax
						goto default;
					typeName = this.Childs[0].ToTypeName(denyAliases) + "[]";
					break;
				default: throw new ExpressionParserException(Properties.Resources.EXCEPTION_PARSER_TYPENAMEEXPECTED, this);
			}
			return typeName;
		}

		private static Dictionary<string, object> PrepareArguments(ParserNode node, bool checkedScope)
		{
			var args = default(Dictionary<string, object>);
			if (node.Childs.Count <= 1 || node.Childs[1].Childs.Count <= 0)
				return null;

			args = new Dictionary<string, object>();
			var argIdx = 0;
			foreach (var argNode in node.Childs[1].Childs)
			{
				if (argNode.Type == TokenType.Colon)
				{
					Ensure(argNode, 2, TokenType.Identifier);

					var argName = argNode.Childs[0].Value;
					args[argName] = argNode.Childs[1].ToExpressionTree(checkedScope);
				}
				else
				{
					var argName = (argIdx++).ToString();
					args[argName] = argNode.ToExpressionTree(checkedScope);
				}
			}
			return args;
		}
		private static void Ensure(ParserNode node, int childCount, params TokenType[] childTypes)
		{
			if (node.Childs.Count < childCount)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDCOUNTOFNODE, node.Type, node.Childs.Count, childCount), node);

			for (var i = 0; childTypes != null && i < childTypes.Length && i < node.Childs.Count; i++)
			{
				if (childTypes[i] == TokenType.None)
					continue;

				if (node.Childs[i].Type != childTypes[i])
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDTYPESOFNODE, node.Type, node.Childs[i].Type, childTypes[i]), node.Childs[i]);
			}
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

			for (var i = 0; i < this.Childs.Count; i++)
			{
				sb.Append("\r\n").Append(' ', depth * 4);
				this.Childs[i].Write(sb, depth + 1);
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
