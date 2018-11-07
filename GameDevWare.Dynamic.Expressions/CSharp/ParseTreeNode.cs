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
		internal enum TypeNameOptions
		{
			None = 0,
			Aliases = 0x1 << 0,
			ShortNames = 0x1 << 1,
			Arrays = 0x1 << 2,

			All = Aliases | ShortNames | Arrays
		}

		internal struct ParseTreeNodes
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
						return this.items[index];

					switch (index)
					{
						case 0: return this.item0;
						case 1: return this.item1;
						case 2: return this.item2;
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
		public readonly Token Token;
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
			return this.Token.LineNumber;
		}
		int ILineInfo.GetColumnNumber()
		{
			return this.Token.ColumnNumber;
		}
		int ILineInfo.GetTokenLength()
		{
			return this.Token.TokenLength;
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
				{ (int)TokenType.Plus, Constants.EXPRESSION_TYPE_UNARY_PLUS },
				{ (int)TokenType.Minus, Constants.EXPRESSION_TYPE_NEGATE },
				{ (int)TokenType.Not, Constants.EXPRESSION_TYPE_NOT },
				{ (int)TokenType.Complement, Constants.EXPRESSION_TYPE_COMPLEMENT },
				{ (int)TokenType.Division, Constants.EXPRESSION_TYPE_DIVIDE },
				{ (int)TokenType.Multiplication, Constants.EXPRESSION_TYPE_MULTIPLY },
				{ (int)TokenType.Power, Constants.EXPRESSION_TYPE_POWER },
				{ (int)TokenType.Modulo, Constants.EXPRESSION_TYPE_MODULO },
				{ (int)TokenType.Add, Constants.EXPRESSION_TYPE_ADD },
				{ (int)TokenType.Subtract, Constants.EXPRESSION_TYPE_SUBTRACT },
				{ (int)TokenType.LeftShift, Constants.EXPRESSION_TYPE_LEFT_SHIFT },
				{ (int)TokenType.RightShift, Constants.EXPRESSION_TYPE_RIGHT_SHIFT},
				{ (int)TokenType.GreaterThan, Constants.EXPRESSION_TYPE_GREATER_THAN },
				{ (int)TokenType.GreaterThanOrEquals, Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL },
				{ (int)TokenType.LesserThan, Constants.EXPRESSION_TYPE_LESS_THAN },
				{ (int)TokenType.LesserThanOrEquals, Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL },
				{ (int)TokenType.Is, Constants.EXPRESSION_TYPE_TYPE_IS  },
				{ (int)TokenType.As, Constants.EXPRESSION_TYPE_TYPE_AS },
				{ (int)TokenType.EqualsTo, Constants.EXPRESSION_TYPE_EQUAL },
				{ (int)TokenType.NotEqualsTo, Constants.EXPRESSION_TYPE_NOT_EQUAL },
				{ (int)TokenType.And, Constants.EXPRESSION_TYPE_AND },
				{ (int)TokenType.Or, Constants.EXPRESSION_TYPE_OR },
				{ (int)TokenType.Xor, Constants.EXPRESSION_TYPE_EXCLUSIVE_OR },
				{ (int)TokenType.AndAlso, Constants.EXPRESSION_TYPE_AND_ALSO },
				{ (int)TokenType.OrElse, Constants.EXPRESSION_TYPE_OR_ELSE },
				{ (int)TokenType.Coalesce, Constants.EXPRESSION_TYPE_COALESCE },
				{ (int)TokenType.Conditional, Constants.EXPRESSION_TYPE_CONDITION },
				{ (int)TokenType.Call, Constants.EXPRESSION_TYPE_INVOKE },
				{ (int)TokenType.Typeof, Constants.EXPRESSION_TYPE_TYPE_OF },
				{ (int)TokenType.Default, Constants.EXPRESSION_TYPE_DEFAULT },
				{ (int)TokenType.New, Constants.EXPRESSION_TYPE_NEW },
				{ (int)TokenType.LeftBracket, Constants.EXPRESSION_TYPE_INDEX },
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
			this.Token = otherNode.Token;
			this.Value = otherNode.Value;
			this.nodes = otherNode.nodes;
		}
		internal ParseTreeNode(Token token, TokenType? type = null, string value = null, IEnumerable<ParseTreeNode> nodes = null)
		{
			this.Token = token;
			this.Type = type ?? token.Type;
			this.Value = value ?? token.Value;
			this.nodes = new ParseTreeNodes();
			if (nodes != null)
			{
				foreach(var node in nodes)
					this.Add(node);
			}
		}

		internal void Add(ParseTreeNode node)
		{
			ParseTreeNodes.Add(ref this.nodes, node);
		}
		internal void Replace(int index, ParseTreeNode node)
		{
			ParseTreeNodes.RemoveAt(ref this.nodes, index);
			ParseTreeNodes.Insert(ref this.nodes, index, node);
		}
		
		internal ParseTreeNode WithOtherType(TokenType newType)
		{
			return new ParseTreeNode(newType, this);
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
