using System;
using System.Collections.Generic;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class TypeCache
	{
		private readonly Dictionary<Type, TypeDescription> types;
		private readonly TypeCache parentCache;

		public TypeCache(TypeCache parentCache = null)
		{
			this.parentCache = parentCache;
			this.types = new Dictionary<Type, TypeDescription>();
		}

		public bool TryGetValue(Type type, out TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (this.types.TryGetValue(type, out typeDescription))
				return true;

			if (this.parentCache == null)
				return false;

			lock (this.parentCache)
				return this.parentCache.TryGetValue(type, out typeDescription);
		}
		public bool TryAdd(Type type, ref TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (typeDescription == null) throw new ArgumentNullException("typeDescription");

			var existingValue = default(TypeDescription);
			if (this.TryGetValue(type, out existingValue))
			{
				typeDescription = existingValue;
				return false;
			}

			this.types[type] = typeDescription;
			return true;
		}
		public void Add(Type type, TypeDescription typeDescription)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (typeDescription == null) throw new ArgumentNullException("typeDescription");

			if (this.TryAdd(type, ref typeDescription) == false)
				throw new ArgumentException(string.Format("TypeDescription for types '{0}' is already exists in cache.", typeDescription), "type");
		}
		public TypeDescription GetOrCreateTypeDescription(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var typeDescription = default(TypeDescription);
			if (this.TryGetValue(type, out typeDescription))
				return typeDescription;

			typeDescription = new TypeDescription(type, this);
			return typeDescription;
		}

		public void Merge(TypeCache otherCache)
		{
			if (otherCache == null) throw new ArgumentNullException("otherCache");

			foreach (var kv in otherCache.types)
				this.types[kv.Key] = kv.Value;
		}

		public override string ToString()
		{
			return this.types.ToString();
		}
	}
}
