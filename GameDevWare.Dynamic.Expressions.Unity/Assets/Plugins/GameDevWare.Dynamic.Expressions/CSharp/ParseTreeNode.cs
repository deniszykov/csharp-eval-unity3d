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

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Parse tree node.
	/// </summary>
	public class ParseTreeNode : ILineInfo, IEnumerable<ParseTreeNode>
	{
		[Flags]
		private enum TypeNameOptions
		{
			None = 0,
			Aliases = 0x1 << 0,
			ShortNames = 0x1 << 1,
			Arrays = 0x1 << 2,

			All = Aliases | ShortNames | Arrays
		}

		private struct ParseTreeNodes
		{
			public readonly int Count;

			private readonly ParseTreeNode item0;
			private readonly ParseTreeNode item1;
			private readonly ParseTreeNode item2;
			private readonly List<ParseTreeNode> items;

			private ParseTreeNodes(ParseTreeNode item0, ParseTreeNode item1, ParseTreeNode item2)
			{
				if (item2 != null && item1 == null) throw new ArgumentNullException("item1");
				if (item1 != null && item0 == null) throw new ArgumentNullException("item0");

				this.item0 = item0;
				this.item1 = item1;
				this.item2 = item2;
				this.Count = item2 != null ? 3 :
							 item1 != null ? 2 :
							 item0 != null ? 1 : 0;
				this.items = null;
			}
			private ParseTreeNodes(List<ParseTreeNode> items)
			{
				if (items == null) throw new ArgumentNullException("items");

				this.item0 = this.item1 = this.item2 = null;
				this.items = items;
				this.Count = items.Count;
			}

			public ParseTreeNode this[int index]
			{
				get
				{
					if (index >= this.Count || index < 0) throw new ArgumentOutOfRangeException("index");

					if (this.items != null)
						return items[index];

					switch (index)
					{
						case 0: return item0;
						case 1: return item1;
						case 2: return item2;
						default: throw new ArgumentOutOfRangeException("index");
					}
				}
			}

			public static void Add(ref ParseTreeNodes nodes, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException("node");

				var items = nodes.items;
				if (items != null)
				{
					items.Add(node);
					nodes = new ParseTreeNodes(items);
					return;
				}

				switch (nodes.Count)
				{
					case 0: nodes = new ParseTreeNodes(node, null, null); break;
					case 1: nodes = new ParseTreeNodes(nodes.item0, node, null); break;
					case 2: nodes = new ParseTreeNodes(nodes.item0, nodes.item1, node); break;
					case 3: nodes = new ParseTreeNodes(new List<ParseTreeNode> { nodes.item0, nodes.item1, nodes.item2, node }); break;
					default: throw new ArgumentOutOfRangeException("node", "Unable to add new node. Tree is full.");
				}
			}
			public static void Insert(ref ParseTreeNodes nodes, int index, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException("node");

				var items = nodes.items;
				if (items != null)
				{
					items.Insert(index, node);
					nodes = new ParseTreeNodes(items);
					return;
				}

				switch (nodes.Count)
				{
					case 0: nodes = new ParseTreeNodes(node, null, null); break;
					case 1:
						switch (index)
						{
							case 0: nodes = new ParseTreeNodes(node, nodes.item0, null); break;
							case 1:
							case 2: nodes = new ParseTreeNodes(nodes.item0, node, null); break;
							default: throw new ArgumentOutOfRangeException("index");
						}
						break;
					case 2:
						switch (index)
						{
							case 0: nodes = new ParseTreeNodes(node, nodes.item0, nodes.item1); break;
							case 1: nodes = new ParseTreeNodes(nodes.item0, node, nodes.item1); break;
							case 2: nodes = new ParseTreeNodes(nodes.item0, nodes.item1, node); break;
							default: throw new ArgumentOutOfRangeException("index");
						}
						break;
					case 3:
						items = new List<ParseTreeNode> { nodes.item0, nodes.item1, nodes.item2 };
						items.Insert(index, node);
						nodes = new ParseTreeNodes(items);
						break;
					default: throw new ArgumentOutOfRangeException("node", "Unable to add new node. Tree is full.");
				}
			}
			public static void RemoveAt(ref ParseTreeNodes nodes, int index)
			{

				var items = nodes.items;
				if (items != null)
				{
					items.RemoveAt(index);
					nodes = new ParseTreeNodes(items);
					return;
				}

				switch (index)
				{
					case 0: nodes = new ParseTreeNodes(nodes.item1, nodes.item2, null); break;
					case 1: nodes = new ParseTreeNodes(nodes.item0, nodes.item2, null); break;
					case 2: nodes = new ParseTreeNodes(nodes.item0, nodes.item1, null); break;
					default: throw new ArgumentOutOfRangeException("index");
				}
			}
			public static bool Remove(ref ParseTreeNodes nodes, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException("node");

				var items = nodes.items;
				if (items != null)
				{
					if (items.Remove(node) == false)
						return false;

					nodes = new ParseTreeNodes(items);
					return true;
				}

				if (nodes.item2 == node)
					nodes = new ParseTreeNodes(nodes.item0, nodes.item1, null);
				else if (nodes.item1 == node)
					nodes = new ParseTreeNodes(nodes.item0, nodes.item2, null);
				else if (nodes.item0 == node)
					nodes = new ParseTreeNodes(nodes.item1, nodes.item2, null);
				else
					return false;
				return true;
			}

			public override string ToString()
			{
				return this.Count.ToString();
			}
		}

		private static readonly Dictionary<int, string> ExpressionTypeByToken;
		private static readonly Dictionary<string, string> TypeAliases;

		private ParseTreeNodes nodes;

		/// <summary>
		/// Type of current node.
		/// </summary>
		public readonly TokenType Type;
		/// <summary>
		/// Lexeme from which current node is originated.
		/// </summary>
		public readonly Token Lexeme;
		/// <summary>
		/// Value of current node.
		/// </summary>
		public readonly string Value;

		/// <summary>
		/// Get child node by index.
		/// </summary>
		/// <param name="index">Index of child node.</param>
		/// <returns>Child node at index.</returns>
		public ParseTreeNode this[int index] { get { return this.nodes[index]; } }
		/// <summary>
		/// Returns number of child nodes.
		/// </summary>
		public int Count { get { return this.nodes.Count; } }

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

		static ParseTreeNode()
		{
			ExpressionTypeByToken = new Dictionary<int, string>
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

			TypeAliases = new Dictionary<string, string>
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
		}
		private ParseTreeNode(TokenType type, ParseTreeNode otherNode)
		{
			if (otherNode == null) throw new ArgumentNullException("otherNode");

			this.Type = type;
			this.Lexeme = otherNode.Lexeme;
			this.Value = otherNode.Value;
			this.nodes = otherNode.nodes;
		}
		internal ParseTreeNode(TokenType type, Token lexeme, string value)
		{
			this.Type = type;
			this.Lexeme = lexeme;
			this.Value = value ?? lexeme.Value;
			this.nodes = new ParseTreeNodes();
		}
		internal ParseTreeNode(Token lexeme)
			: this(lexeme.Type, lexeme, lexeme.Value)
		{

		}

		internal void Add(ParseTreeNode node)
		{
			ParseTreeNodes.Add(ref this.nodes, node);
		}
		internal void Insert(int index, ParseTreeNode node)
		{
			ParseTreeNodes.Insert(ref this.nodes, index, node);
		}
		internal void RemoveAt(int index)
		{
			ParseTreeNodes.RemoveAt(ref this.nodes, index);
		}
		internal bool Remove(ParseTreeNode node)
		{
			return ParseTreeNodes.Remove(ref this.nodes, node);
		}
		internal ParseTreeNode ChangeType(TokenType newType)
		{
			return new ParseTreeNode(newType, this);
		}

		/// <summary>
		/// Converts parse tree to syntax tree.
		/// </summary>
		/// <param name="checkedScope">Conversion and arithmetic operation overflow control. True is "throw on overflows", false is "ignore of overflows".</param>
		/// <returns></returns>
		public SyntaxTreeNode ToSyntaxTree(bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			try
			{
				var expressionType = default(string);
				if (ExpressionTypeByToken.TryGetValue((int)this.Type, out expressionType) == false)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENTYPE, this.Type), this);

				var node = new Dictionary<string, object>
				{
					{ Constants.EXPRESSION_POSITION, this.Lexeme.Position },
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, expressionType },
				};

				switch (this.Type)
				{
					case TokenType.NullResolve:
					case TokenType.Resolve:
						Ensure(this, 2, TokenType.None, TokenType.Identifier);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
						node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.nodes[1].Value;
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this.nodes[1], 0);
						node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = this.Type == TokenType.NullResolve ? Constants.TrueObject : Constants.FalseObject;
						break;
					case TokenType.Identifier:
						if (this.nodes.Count == 0 && (this.Value == Constants.VALUE_TRUE_STRING || this.Value == Constants.VALUE_FALSE_STRING || this.Value == Constants.VALUE_NULL_STRING))
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
						node[Constants.TYPE_ATTRIBUTE] = this.nodes[0].ToTypeName(TypeNameOptions.All);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[1].ToSyntaxTree(checkedScope);
						if (checkedScope)
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKEDSUFFIX;
						break;
					case TokenType.CheckedScope:
						Ensure(this, 1);
						// ReSharper disable once RedundantArgumentDefaultValue
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope: true);
						break;
					case TokenType.UncheckedScope:
						Ensure(this, 1);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope: false);
						break;
					case TokenType.As:
					case TokenType.Is:
						Ensure(this, 2);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
						node[Constants.TYPE_ATTRIBUTE] = this.nodes[1].ToTypeName(TypeNameOptions.All);
						break;
					case TokenType.Default:
					case TokenType.Typeof:
						Ensure(this, 1);
						node[Constants.TYPE_ATTRIBUTE] = this.nodes[0].ToTypeName(TypeNameOptions.All);
						break;
					case TokenType.Group:
					case TokenType.Plus:
					case TokenType.Minus:
					case TokenType.Not:
					case TokenType.Compl:
						Ensure(this, 1);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
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
						node[Constants.LEFT_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
						node[Constants.RIGHT_ATTRIBUTE] = this.nodes[1].ToSyntaxTree(checkedScope);
						if (checkedScope && (this.Type == TokenType.Add || this.Type == TokenType.Mul || this.Type == TokenType.Subtract))
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] += Constants.EXPRESSION_TYPE_CHECKEDSUFFIX;
						break;
					case TokenType.Cond:
						Ensure(this, 3);
						node[Constants.TEST_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
						node[Constants.IFTRUE_ATTRIBUTE] = this.nodes[1].ToSyntaxTree(checkedScope);
						node[Constants.IFFALSE_ATTRIBUTE] = this.nodes[2].ToSyntaxTree(checkedScope);
						break;
					case TokenType.Lambda:
						Ensure(this, 2, TokenType.Arguments);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 0, checkedScope);
						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[1].ToSyntaxTree(checkedScope);
						break;
					case TokenType.Call:
						Ensure(this, 1);

						var isNullPropagation = false;
						if (this.Value == "[" || this.Value == "?[")
						{
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] = ExpressionTypeByToken[(int)TokenType.Lbracket];
							isNullPropagation = this.Value == "?[";
						}

						node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToSyntaxTree(checkedScope);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 1, checkedScope);
						node[Constants.USE_NULL_PROPAGATION_ATTRIBUTE] = isNullPropagation ? Constants.TrueObject : Constants.FalseObject;
						break;
					case TokenType.New:
						Ensure(this, 2, TokenType.None, TokenType.Arguments);

						if (this.nodes[1].Value == "[")
							node[Constants.EXPRESSION_TYPE_ATTRIBUTE] = Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS;

						node[Constants.TYPE_ATTRIBUTE] = this.nodes[0].ToTypeName(TypeNameOptions.All);
						node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareArguments(this, 1, checkedScope);

						break;

					default:
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_UNEXPECTEDTOKENWHILEBUILDINGTREE, this.Type), this);
				}

				return new SyntaxTreeNode(node);
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
			if (this.Type == TokenType.Identifier && this.nodes.Count == 0 && allowShortName)
			{
				var typeName = default(string);
				if (allowAliases && TypeAliases.TryGetValue(this.Value, out typeName))
					return typeName;
				else
					return this.Value;
			}

			if (this.Type == TokenType.Call && this.nodes.Count == 2 && this.Value == "[" && this.nodes[1].Count == 0 && allowArrays)
			{
				var arrayNode = new ParseTreeNode(TokenType.Identifier, this.Lexeme, typeof(Array).Name);
				var argumentsNode = new ParseTreeNode(TokenType.Arguments, this.Lexeme, "<");
				argumentsNode.Add(this.nodes[0]);
				arrayNode.Add(argumentsNode);
				return arrayNode.ToTypeName(TypeNameOptions.None);
			}

			var node = new Dictionary<string, object>
			{
				{ Constants.EXPRESSION_POSITION, this.Lexeme.Position },
				{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD },
			};

			if (this.Type == TokenType.Resolve)
			{
				Ensure(this, 2, TokenType.None, TokenType.Identifier);
				node[Constants.EXPRESSION_ATTRIBUTE] = this.nodes[0].ToTypeName(TypeNameOptions.None);
				node[Constants.PROPERTY_OR_FIELD_NAME_ATTRIBUTE] = this.nodes[1].Value;
				node[Constants.ARGUMENTS_ATTRIBUTE] = PrepareTypeArguments(this.nodes[1], 0);
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

			return new SyntaxTreeNode(node);
		}

		private static Dictionary<string, object> PrepareArguments(ParseTreeNode node, int argumentChildIndex, bool checkedScope)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= node.Count || node[argumentChildIndex].Count == 0)
				return null;

			var argIdx = 0;
			var argumentsNode = node[argumentChildIndex];
			args = new Dictionary<string, object>(argumentsNode.Count);
			for (var i = 0; i < argumentsNode.Count; i++)
			{
				var argNode = argumentsNode[i];
				if (argNode.Type == TokenType.Colon)
				{
					Ensure(argNode, 2, TokenType.Identifier);

					var argName = argNode[0].Value;
					args[argName] = argNode[1].ToSyntaxTree(checkedScope);
				}
				else
				{
					var argName = Constants.GetIndexAsString(argIdx++);
					args[argName] = argNode.ToSyntaxTree(checkedScope);
				}
			}
			return args;
		}
		private static Dictionary<string, object> PrepareTypeArguments(ParseTreeNode node, int argumentChildIndex)
		{
			var args = default(Dictionary<string, object>);
			if (argumentChildIndex >= node.Count || node[argumentChildIndex].Count == 0)
				return null;

			var argIdx = 0;
			var argumentsNode = node[argumentChildIndex];
			args = new Dictionary<string, object>(argumentsNode.Count);
			for (var i = 0; i < argumentsNode.Count; i++)
			{
				var argNode = argumentsNode[i];
				var argName = Constants.GetIndexAsString(argIdx++);
				args[argName] = argNode.ToTypeName(TypeNameOptions.Aliases | TypeNameOptions.Arrays);
			}
			return args;
		}
		private static void Ensure(ParseTreeNode node, int childCount, TokenType childType0 = 0, TokenType childType1 = 0, TokenType childType2 = 0)
		{
			// ReSharper disable HeapView.BoxingAllocation
			if (node.Count < childCount)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_PARSER_INVALIDCHILDCOUNTOFNODE, node.Type, node.Count, childCount), node);

			for (int i = 0, ct = Math.Min(3, childCount); i < ct; i++)
			{
				var childNode = node[i];
				var childNodeType = node[i].Type;
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

			for (var i = 0; i < this.nodes.Count; i++)
			{
				sb.Append("\r\n").Append(' ', depth * 4);
				this.nodes[i].Write(sb, depth + 1);
			}
		}

		IEnumerator<ParseTreeNode> IEnumerable<ParseTreeNode>.GetEnumerator()
		{
			for (var i = 0; i < this.nodes.Count; i++)
				yield return this.nodes[i];
		}

		/// <summary>
		/// Converts parse tree to string for debugging.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder();
			this.Write(sb, 0);
			return sb.ToString();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<ParseTreeNode>)this).GetEnumerator();
		}
	}
}
