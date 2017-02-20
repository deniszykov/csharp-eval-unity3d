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

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// List of expression's arguments by name or position. This collection is read-only.
	/// </summary>
	public class ArgumentsTree : IDictionary<string, SyntaxTreeNode>
	{
		/// <summary>
		/// Empty arguments list
		/// </summary>
		public static readonly ArgumentsTree Empty = new ArgumentsTree();

		private readonly Dictionary<string, SyntaxTreeNode> innerDictionary;

		/// <summary>
		/// Creates empty list of arguments.
		/// </summary>
		private ArgumentsTree()
		{
			this.innerDictionary = new Dictionary<string, SyntaxTreeNode>();
		}
		/// <summary>
		/// Create list of arguments from existing dictionary.
		/// </summary>
		/// <param name="innerDictionary"></param>
		public ArgumentsTree(Dictionary<string, SyntaxTreeNode> innerDictionary)
		{
			if (innerDictionary == null) throw new ArgumentNullException("innerDictionary");

			this.innerDictionary = innerDictionary;
		}

		#region IDictionary<string,ExpressionTree> Members

		void IDictionary<string, SyntaxTreeNode>.Add(string key, SyntaxTreeNode value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Check if passes named argument is exists in list.
		/// </summary>
		/// <returns>true is exists, overwise false.</returns>
		public bool ContainsKey(string key)
		{
			return this.innerDictionary.ContainsKey(key);
		}
		/// <summary>
		/// Check if passes positional argument is exists in list.
		/// </summary>
		/// <returns>true is exists, overwise false.</returns>
		public bool ContainsKey(int position)
		{
			return this.innerDictionary.ContainsKey(Constants.GetIndexAsString(position));
		}
		/// <summary>
		/// Returns list of arguments names/positions.
		/// </summary>
		public ICollection<string> Keys
		{
			get { return this.innerDictionary.Keys; }
		}

		bool IDictionary<string, SyntaxTreeNode>.Remove(string key)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Tries to retrieve argument by its name.
		/// </summary>
		/// <returns>true is exists, overwise false.</returns>
		public bool TryGetValue(string key, out SyntaxTreeNode value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}
		/// <summary>
		/// Tries to retrieve argument by its position.
		/// </summary>
		/// <returns>true is exists, overwise false.</returns>
		public bool TryGetValue(int position, out SyntaxTreeNode value)
		{
			return this.innerDictionary.TryGetValue(Constants.GetIndexAsString(position), out value);
		}
		/// <summary>
		/// Returns all arguments in this list.
		/// </summary>
		public ICollection<SyntaxTreeNode> Values
		{
			get { return this.innerDictionary.Values; }
		}
		/// <summary>
		/// Returns argument by its name.
		/// </summary>
		public SyntaxTreeNode this[string key]
		{
			get { return this.innerDictionary[key]; }
			set { throw new NotSupportedException(); }
		}
		/// <summary>
		/// Returns argument by its position.
		/// </summary>
		public SyntaxTreeNode this[int position]
		{
			get { return this.innerDictionary[Constants.GetIndexAsString(position)]; }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region ICollection<KeyValuePair<string,ExpressionTree>> Members

		void ICollection<KeyValuePair<string, SyntaxTreeNode>>.Add(KeyValuePair<string, SyntaxTreeNode> item)
		{
			throw new NotSupportedException();
		}

		void ICollection<KeyValuePair<string, SyntaxTreeNode>>.Clear()
		{
			throw new NotSupportedException();
		}

		bool ICollection<KeyValuePair<string, SyntaxTreeNode>>.Contains(KeyValuePair<string, SyntaxTreeNode> item)
		{
			return ((ICollection<KeyValuePair<string, SyntaxTreeNode>>)this.innerDictionary).Contains(item);
		}

		void ICollection<KeyValuePair<string, SyntaxTreeNode>>.CopyTo(KeyValuePair<string, SyntaxTreeNode>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, SyntaxTreeNode>>)this.innerDictionary).CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns number of arguments in list.
		/// </summary>
		public int Count
		{
			get { return this.innerDictionary.Count; }
		}

		bool ICollection<KeyValuePair<string, SyntaxTreeNode>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, SyntaxTreeNode>>)this.innerDictionary).IsReadOnly; }
		}

		bool ICollection<KeyValuePair<string, SyntaxTreeNode>>.Remove(KeyValuePair<string, SyntaxTreeNode> item)
		{
			return ((ICollection<KeyValuePair<string, SyntaxTreeNode>>)this.innerDictionary).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,ExpressionTree>> Members

		IEnumerator<KeyValuePair<string, SyntaxTreeNode>> IEnumerable<KeyValuePair<string, SyntaxTreeNode>>.GetEnumerator()
		{
			return ((ICollection<KeyValuePair<string, SyntaxTreeNode>>)this.innerDictionary).GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.innerDictionary).GetEnumerator();
		}

		#endregion

		/// <summary>
		/// Compares two arguments list by reference.
		/// </summary>
		public override bool Equals(object obj)
		{
			return this.innerDictionary.Equals(obj);
		}
		/// <summary>
		/// Returns hash code of arguments list.
		/// </summary>
		public override int GetHashCode()
		{
			return this.innerDictionary.GetHashCode();
		}

		/// <summary>
		/// Converts argument list to string.
		/// </summary>
		public override string ToString()
		{
			return this.innerDictionary.ToString();
		}
	}
}
