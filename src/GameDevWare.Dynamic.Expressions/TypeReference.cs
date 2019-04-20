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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// Type reference with type arguments.
	/// </summary>
	public sealed class TypeReference : IEquatable<TypeReference>
	{
		/// <summary>
		/// Empty type reference. Used for open generic types as parameter placeholder.
		/// </summary>
		public static readonly TypeReference Empty = new TypeReference();
		/// <summary>
		/// Empty list of type arguments.
		/// </summary>
		public static readonly IList<TypeReference> EmptyTypeArguments = Empty.TypeArguments;

		private string fullName;
		private readonly int hashCode;
		private readonly ReadOnlyCollection<string> typeName;
		private readonly ReadOnlyCollection<TypeReference> typeArguments;

		/// <summary>
		/// Full type name with namespace and declared types.
		/// </summary>
		public string FullName { get { return this.fullName ?? (this.fullName = this.CombineParts(this.typeName.Count)); } }
		/// <summary>
		/// Type's name without namespace and declared types.
		/// </summary>
		public string Name { get { return this.typeName[this.typeName.Count - 1]; } }
		/// <summary>
		/// Types' namespace if any.
		/// </summary>
		public string Namespace { get { return this.CombineParts(this.typeName.Count - 1); } }
		/// <summary>
		/// Type's generic arguments.
		/// </summary>
		public ReadOnlyCollection<TypeReference> TypeArguments { get { return this.typeArguments; } }
		/// <summary>
		/// Returns true if type has type arguments.
		/// </summary>
		public bool IsGenericType { get { return this.typeArguments.Count > 0; } }

		private TypeReference()
		{
			this.typeName = new ReadOnlyCollection<string>(new[] { string.Empty });
			this.typeArguments = new ReadOnlyCollection<TypeReference>(new TypeReference[0]);
			this.fullName = string.Empty;
		}
		/// <summary>
		/// Creates new type reference from type's path and type's generic arguments.
		/// </summary>
		/// <param name="typeName">Type path.</param>
		/// <param name="typeArguments">Type generic arguments.</param>
		public TypeReference(IList<string> typeName, IList<TypeReference> typeArguments)
		{
			if (typeName == null) throw new ArgumentNullException("typeName");
			if (typeName.Count == 0) throw new ArgumentOutOfRangeException("typeName");
			if (typeArguments == null) throw new ArgumentNullException("typeArguments");

			for (var i = 0; i < typeName.Count; i++) if (string.IsNullOrEmpty(typeName[i])) throw new ArgumentException("Type's name contains empty parts.", "typeName");
			for (var i = 0; i < typeArguments.Count; i++) if (typeArguments[i] == null) throw new ArgumentException("Type's generic arguments contains null values.", "typeArguments");

			this.typeName = typeName as ReadOnlyCollection<string> ?? new ReadOnlyCollection<string>(typeName);
			this.typeArguments = typeArguments as ReadOnlyCollection<TypeReference> ?? new ReadOnlyCollection<TypeReference>(typeArguments);
			this.hashCode = ComputeHashCode(this);

			if (typeName.Count == 1) this.fullName = typeName[0];
		}

		private string CombineParts(int count, StringBuilder builder = null)
		{
			if (count > this.typeName.Count) throw new ArgumentOutOfRangeException("count");

			if (count == 0) return string.Empty;

			var lengthReq = 0;
			for (var i = 0; i < count; i++)
			{
				lengthReq += this.typeName[i].Length;
				if (i != 0) lengthReq++;
			}
			builder = builder ?? new StringBuilder(lengthReq);
			if (builder.Capacity - builder.Length < lengthReq)
				builder.Capacity += lengthReq;
			for (var i = 0; i < count; i++)
			{
				if (i != 0)
					builder.Append('.');
				builder.Append(this.typeName[i]);
			}
			return builder.ToString();
		}

		private void Format(StringBuilder builder)
		{
			this.CombineParts(this.typeName.Count, builder);
			if (this.typeArguments.Count > 0)
			{
				builder.Append('<');
				for (var i = 0; i < this.typeArguments.Count; i++)
				{
					if (i != 0) builder.Append(", ");
					this.typeArguments[i].Format(builder);
				}
				builder.Append('>');
			}
		}

		/// <summary>
		/// Compares two type references by value.
		/// </summary>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as TypeReference);
		}
		/// <summary>
		/// Compares two type references by value.
		/// </summary>
		public bool Equals(TypeReference other)
		{
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;

			return this.typeName.Count == other.typeName.Count && this.typeName.SequenceEqual(other.typeName) &&
				   this.typeArguments.Count == other.typeArguments.Count && this.typeArguments.SequenceEqual(other.typeArguments);
		}
		/// <summary>
		/// Return hash code of type reference.
		/// </summary>
		public override int GetHashCode()
		{
			return this.hashCode;
		}

		/// <summary>
		/// Checks if two type references are equals.
		/// </summary>
		public static bool operator ==(TypeReference x, TypeReference y)
		{
			return ReferenceEquals(x, y) || Equals(x, y);
		}
		/// <summary>
		/// Checks if two type references are not equals.
		/// </summary>
		public static bool operator !=(TypeReference x, TypeReference y)
		{
			return Equals(x, y) == false;
		}

		private static int ComputeHashCode(TypeReference typeReference)
		{
			if (typeReference == null) throw new ArgumentNullException("typeReference");

			var hashCode = 0;
			for (var i = 0; i < typeReference.typeName.Count; i++)
				hashCode = unchecked(hashCode + typeReference.typeName[i].GetHashCode());
			for (var i = 0; i < typeReference.typeArguments.Count; i++)
				hashCode = unchecked(hashCode + typeReference.typeArguments[i].GetHashCode());
			return hashCode;
		}

		/// <summary>
		/// Converts type reference to string representation for debug purpose.
		/// </summary>
		public override string ToString()
		{
			if (ReferenceEquals(this, Empty))
				return string.Empty;

			var builder = new StringBuilder(1000);
			this.Format(builder);
			return builder.ToString();
		}
	}
}
