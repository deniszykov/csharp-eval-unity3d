using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GameDevWare.Dynamic.Expressions
{
	public sealed class TypeReference : IEquatable<TypeReference>
	{
		public static readonly TypeReference Empty = new TypeReference();
		public static readonly IList<TypeReference> EmptyGenericArguments = Empty.TypeArguments;

		private string fullName;
		private readonly int hashCode;
		private readonly ReadOnlyCollection<string> typeName;
		private readonly ReadOnlyCollection<TypeReference> typeArguments;

		public string FullName { get { return this.fullName ?? (this.fullName = this.CombineParts(this.typeName.Count)); } }
		public string Name { get { return this.typeName[typeName.Count - 1]; } }
		public string Namespace { get { return this.CombineParts(this.typeName.Count - 1); } }
		public ReadOnlyCollection<TypeReference> TypeArguments { get { return this.typeArguments; } }

		private TypeReference()
		{
			this.typeName = new ReadOnlyCollection<string>(new[] { string.Empty });
			this.typeArguments = new ReadOnlyCollection<TypeReference>(new TypeReference[0]);
			this.fullName = string.Empty;
		}
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

		public override bool Equals(object obj)
		{
			return this.Equals(obj as TypeReference);
		}
		public bool Equals(TypeReference other)
		{
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;

			return this.typeName.Count == other.typeName.Count && this.typeName.SequenceEqual(other.typeName) &&
				   this.typeArguments.Count == other.typeArguments.Count && this.typeArguments.SequenceEqual(other.typeArguments);
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}

		public static bool operator ==(TypeReference x, TypeReference y)
		{
			return ReferenceEquals(x, y) || Equals(x, y);
		}
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
