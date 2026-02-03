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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class TypeCache
	{
		private readonly TypeCache parentCache;
		private readonly Dictionary<Type, TypeDescription> types;

		public Dictionary<Type, TypeDescription>.ValueCollection Values => this.types.Values;

		public TypeCache(TypeCache parentCache = null)
		{
			this.parentCache = parentCache;
			this.types = new Dictionary<Type, TypeDescription>();
		}

		public bool TryGetValue(Type type, out TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (this.types.TryGetValue(type, out typeDescription))
				return true;

			if (this.parentCache == null)
				return false;

			lock (this.parentCache)
				return this.parentCache.TryGetValue(type, out typeDescription);
		}
		public bool TryAdd(Type type, ref TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (typeDescription == null) throw new ArgumentNullException(nameof(typeDescription));

			if (this.TryGetValue(type, out var existingValue))
			{
				typeDescription = existingValue;
				return false;
			}

			this.types[type] = typeDescription;
			return true;
		}
		public void Add(Type type, TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (typeDescription == null) throw new ArgumentNullException(nameof(typeDescription));

			if (!this.TryAdd(type, ref typeDescription))
				throw new ArgumentException($"TypeDescription for types '{typeDescription}' is already exists in cache.", nameof(type));
		}
		public TypeDescription GetOrCreateTypeDescription(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (this.TryGetValue(type, out var typeDescription))
				return typeDescription;

			typeDescription = new TypeDescription(type, this);
			return typeDescription;
		}

		public void Merge(TypeCache otherCache)
		{
			if (otherCache == null) throw new ArgumentNullException(nameof(otherCache));

			foreach (var kv in otherCache.types)
			{
				this.types[kv.Key] = kv.Value;
			}
		}

		public override string ToString()
		{
			return this.types.ToString();
		}
	}
}
