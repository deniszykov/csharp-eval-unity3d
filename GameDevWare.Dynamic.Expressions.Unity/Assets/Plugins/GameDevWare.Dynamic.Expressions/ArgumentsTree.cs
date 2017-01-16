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
	public class ArgumentsTree : IDictionary<string, ExpressionTree>
	{
		public static readonly ArgumentsTree Empty = new ArgumentsTree();

		private static readonly Dictionary<string, ExpressionTree> EmptyDictionary = new Dictionary<string, ExpressionTree>();

		private readonly Dictionary<string, ExpressionTree> innerDictionary;

		public ArgumentsTree()
		{
			this.innerDictionary = EmptyDictionary;
		}
		public ArgumentsTree(Dictionary<string, ExpressionTree> innerDictionary)
		{
			if (innerDictionary == null)
				throw new ArgumentNullException("innerDictionary");

			this.innerDictionary = innerDictionary;
		}

		#region IDictionary<string,ExpressionTree> Members

		void IDictionary<string, ExpressionTree>.Add(string key, ExpressionTree value)
		{
			throw new NotSupportedException();
		}

		public bool ContainsKey(string key)
		{
			return this.innerDictionary.ContainsKey(key);
		}

		public bool ContainsKey(int position)
		{
			return this.innerDictionary.ContainsKey(Constants.GetIndexAsString(position));
		}

		public ICollection<string> Keys
		{
			get { return this.innerDictionary.Keys; }
		}

		bool IDictionary<string, ExpressionTree>.Remove(string key)
		{
			throw new NotSupportedException();
		}

		public bool TryGetValue(string key, out ExpressionTree value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}

		public bool TryGetValue(int position, out ExpressionTree value)
		{
			return this.innerDictionary.TryGetValue(Constants.GetIndexAsString(position), out value);
		}

		public ICollection<ExpressionTree> Values
		{
			get { return this.innerDictionary.Values; }
		}

		public ExpressionTree this[string key]
		{
			get { return this.innerDictionary[key]; }
			set { throw new NotSupportedException(); }
		}

		public ExpressionTree this[int position]
		{
			get { return this.innerDictionary[Constants.GetIndexAsString(position)]; }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region ICollection<KeyValuePair<string,ExpressionTree>> Members

		void ICollection<KeyValuePair<string, ExpressionTree>>.Add(KeyValuePair<string, ExpressionTree> item)
		{
			throw new NotSupportedException();
		}

		void ICollection<KeyValuePair<string, ExpressionTree>>.Clear()
		{
			throw new NotSupportedException();
		}

		bool ICollection<KeyValuePair<string, ExpressionTree>>.Contains(KeyValuePair<string, ExpressionTree> item)
		{
			return ((ICollection<KeyValuePair<string, ExpressionTree>>)this.innerDictionary).Contains(item);
		}

		void ICollection<KeyValuePair<string, ExpressionTree>>.CopyTo(KeyValuePair<string, ExpressionTree>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, ExpressionTree>>)this.innerDictionary).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return this.innerDictionary.Count; }
		}

		bool ICollection<KeyValuePair<string, ExpressionTree>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, ExpressionTree>>)this.innerDictionary).IsReadOnly; }
		}

		bool ICollection<KeyValuePair<string, ExpressionTree>>.Remove(KeyValuePair<string, ExpressionTree> item)
		{
			return ((ICollection<KeyValuePair<string, ExpressionTree>>)this.innerDictionary).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<string,ExpressionTree>> Members

		IEnumerator<KeyValuePair<string, ExpressionTree>> IEnumerable<KeyValuePair<string, ExpressionTree>>.GetEnumerator()
		{
			return ((ICollection<KeyValuePair<string, ExpressionTree>>)this.innerDictionary).GetEnumerator();
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
			return this.innerDictionary.ToString();
		}
	}
}
