using System;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal struct TypeTuple : IEquatable<TypeTuple>
	{
		private readonly int hashCode;

		public readonly Type[] Types;

		public TypeTuple(params Type[] types)
		{
			if (types == null) throw new ArgumentNullException("types");

			this.Types = types;

			unchecked
			{
				this.hashCode = 17;
				foreach (var type in types)
				{
					if (type == null) throw new ArgumentException("One of array's element is null.", "types");

					this.hashCode = this.hashCode * 23 + type.GetHashCode();
				}
			}
		}

		public bool Equals(TypeTuple other)
		{
			if (this.Types == other.Types) return true;
			if (this.Types == null || other.Types == null) return false;
			if (this.Types.Length != other.Types.Length) return false;

			for (var i = 0; i < this.Types.Length; i++)
				if (this.Types[i] != other.Types[i])
					return false;
			return true;
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		public override bool Equals(object obj)
		{
			if (obj is TypeTuple)
				return Equals((TypeTuple)obj);
			return false;
		}

		public override string ToString()
		{
			if (this.Types != null)
			{
				var sb = new System.Text.StringBuilder();
				foreach (var type in this.Types)
					sb.Append(type.Name).Append(", ");
				if (sb.Length > 2)
					sb.Length -= 2;
				return sb.ToString();
			}
			else
				return "empty";
		}
	}
}
