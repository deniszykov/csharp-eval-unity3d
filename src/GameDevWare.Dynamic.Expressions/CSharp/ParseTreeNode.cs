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
	///     Parse tree node.
	/// </summary>
	public class ParseTreeNode : ILineInfo, IEnumerable<ParseTreeNode>
	{
		internal readonly struct ParseTreeNodes
		{
			public readonly int Count;

			private readonly ParseTreeNode item0;
			private readonly ParseTreeNode item1;
			private readonly ParseTreeNode item2;
			private readonly List<ParseTreeNode> items;

			private ParseTreeNodes(ParseTreeNode item0, ParseTreeNode item1, ParseTreeNode item2)
			{
				if (item2 != null && item1 == null) throw new ArgumentNullException(nameof(item1));
				if (item1 != null && item0 == null) throw new ArgumentNullException(nameof(item0));

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
				if (items == null) throw new ArgumentNullException(nameof(items));

				this.item0 = this.item1 = this.item2 = null;
				this.items = items;
				this.Count = items.Count;
			}

			public ParseTreeNode this[int index]
			{
				get
				{
					if (index >= this.Count || index < 0) throw new ArgumentOutOfRangeException(nameof(index));

					if (this.items != null)
						return this.items[index];

					switch (index)
					{
						case 0: return this.item0;
						case 1: return this.item1;
						case 2: return this.item2;
						default: throw new ArgumentOutOfRangeException(nameof(index));
					}
				}
			}

			public static void Add(ref ParseTreeNodes nodes, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException(nameof(node));

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
					default: throw new ArgumentOutOfRangeException(nameof(node), "Unable to add new node. Tree is full.");
				}
			}
			public static void Insert(ref ParseTreeNodes nodes, int index, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException(nameof(node));

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
							default: throw new ArgumentOutOfRangeException(nameof(index));
						}

						break;
					case 2:
						switch (index)
						{
							case 0: nodes = new ParseTreeNodes(node, nodes.item0, nodes.item1); break;
							case 1: nodes = new ParseTreeNodes(nodes.item0, node, nodes.item1); break;
							case 2: nodes = new ParseTreeNodes(nodes.item0, nodes.item1, node); break;
							default: throw new ArgumentOutOfRangeException(nameof(index));
						}

						break;
					case 3:
						items = new List<ParseTreeNode> { nodes.item0, nodes.item1, nodes.item2 };
						items.Insert(index, node);
						nodes = new ParseTreeNodes(items);
						break;
					default: throw new ArgumentOutOfRangeException(nameof(node), "Unable to add new node. Tree is full.");
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
					default: throw new ArgumentOutOfRangeException(nameof(index));
				}
			}
			public static bool Remove(ref ParseTreeNodes nodes, ParseTreeNode node)
			{
				if (node == null) throw new ArgumentNullException(nameof(node));

				var items = nodes.items;
				if (items != null)
				{
					if (!items.Remove(node))
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

		/// <summary>
		///     Lexeme from which current node is originated.
		/// </summary>
		public readonly Token Token;

		/// <summary>
		///     Type of current node.
		/// </summary>
		public readonly TokenType Type;
		/// <summary>
		///     Value of current node.
		/// </summary>
		public readonly string Value;

		private ParseTreeNodes nodes;

		/// <summary>
		///     Get child node by index.
		/// </summary>
		/// <param name="index">Index of child node.</param>
		/// <returns>Child node at index.</returns>
		public ParseTreeNode this[int index] => this.nodes[index];
		/// <summary>
		///     Returns number of child nodes.
		/// </summary>
		public int Count => this.nodes.Count;

		private ParseTreeNode(TokenType type, ParseTreeNode otherNode)
		{
			if (otherNode == null) throw new ArgumentNullException(nameof(otherNode));

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
				foreach (var node in nodes)
				{
					this.Add(node);
				}
			}
		}

		internal void InsertAt(int index, ParseTreeNode node)
		{
			ParseTreeNodes.Insert(ref this.nodes, index, node);
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
				.Append(this.Type).Append(' ')
				.Append('\'').Append(this.Value).Append('\'');

			for (var i = 0; i < this.nodes.Count; i++)
			{
				sb.Append("\r\n").Append(' ', depth * 4);
				this.nodes[i].Write(sb, depth + 1);
			}
		}

		/// <summary>
		///     Converts parse tree to string for debugging.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder();
			this.Write(sb, 0);
			return sb.ToString();
		}

		IEnumerator<ParseTreeNode> IEnumerable<ParseTreeNode>.GetEnumerator()
		{
			for (var i = 0; i < this.nodes.Count; i++)
				yield return this.nodes[i];
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<ParseTreeNode>)this).GetEnumerator();
		}

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
	}
}
