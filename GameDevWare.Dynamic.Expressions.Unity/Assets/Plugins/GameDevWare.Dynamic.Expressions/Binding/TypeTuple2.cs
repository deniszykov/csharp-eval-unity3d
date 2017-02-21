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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal struct TypeTuple2 : IEquatable<TypeTuple2>
	{
		private readonly int hashCode;

		public readonly Type Type1;
		public readonly Type Type2;

		public TypeTuple2(Type type1, Type type2)
		{
			if (type1 == null) throw new ArgumentNullException("type1");
			if (type2 == null) throw new ArgumentNullException("type2");

			this.Type1 = type1;
			this.Type2 = type2;

			unchecked
			{
				this.hashCode = 17;
				this.hashCode = this.hashCode * 23 + this.Type1.GetHashCode();
				this.hashCode = this.hashCode * 23 + this.Type2.GetHashCode();
			}
		}

		public bool Equals(TypeTuple2 other)
		{
			return this.Type1 == other.Type1 && this.Type2 == other.Type2;
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		public override bool Equals(object obj)
		{
			if (obj is TypeTuple2)
				return Equals((TypeTuple2)obj);
			return false;
		}

		public override string ToString()
		{
			if (this.Type1 != null && this.Type2 != null)
				return this.Type1.Name + "/" + this.Type2.Name;
			else
				return "empty";
		}
	}
}
