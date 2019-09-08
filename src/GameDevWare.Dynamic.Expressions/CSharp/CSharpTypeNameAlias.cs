using System;
using System.Collections.Generic;
using System.Linq;
#if NETFRAMEWORK
using TypeInfo = System.Type;
#else
using System.Reflection;
#endif

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	internal static class CSharpTypeNameAlias
	{
		private static readonly Dictionary<string, string> TypeNameByAlias;
		private static readonly Dictionary<string, string> AliasByTypeName;

		static CSharpTypeNameAlias()
		{
			TypeNameByAlias = new Dictionary<string, string>
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
			AliasByTypeName = TypeNameByAlias.ToDictionary(kv => kv.Value, kv => kv.Key);
		}

		public static bool TryGetTypeName(string alias, out string typeName)
		{
			if (alias == null) throw new ArgumentNullException("alias");

			return TypeNameByAlias.TryGetValue(alias, out typeName);
		}
		public static bool TryGetAlias(string typeName, out string alias)
		{
			if (typeName == null) throw new ArgumentNullException("typeName");

			return AliasByTypeName.TryGetValue(typeName, out alias);
		}
		public static bool TryGetAlias(TypeInfo typeInfo, out string alias)
		{
			if (typeInfo == null) throw new ArgumentNullException("typeInfo");

			return TryGetAlias(typeInfo.FullName, out alias);
		}
	}
}
