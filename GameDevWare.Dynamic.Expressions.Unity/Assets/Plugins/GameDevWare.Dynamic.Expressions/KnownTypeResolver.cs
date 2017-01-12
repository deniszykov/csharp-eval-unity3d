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
using System.Linq;
using System.Text;

namespace GameDevWare.Dynamic.Expressions
{
	public class KnownTypeResolver : ITypeResolver
	{
		public static readonly HashSet<Type> BuildInTypes;
		public static readonly KnownTypeResolver Default;

		private static readonly string ArrayName;
		private static readonly string ArrayFullName;

		private readonly Dictionary<string, List<Type>> knownTypesByFullName;
		private readonly Dictionary<string, List<Type>> knownTypesByName;
		private readonly HashSet<Type> knownTypes;
		private readonly ITypeResolver otherTypeResolver;

		static KnownTypeResolver()
		{
			BuildInTypes = new HashSet<Type>
			{
				typeof (object),
				typeof (bool),
				typeof (char),
				typeof (sbyte),
				typeof (byte),
				typeof (short),
				typeof (ushort),
				typeof (int),
				typeof (uint),
				typeof (long),
				typeof (ulong),
				typeof (float),
				typeof (double),
				typeof (decimal),
				typeof (DateTime),
				typeof (TimeSpan),
				typeof (string),
				typeof (Math),
				typeof (Array),
				typeof (Nullable<>),
				typeof (Func<>),
				typeof (Func<,>),
				typeof (Func<,,>),
				typeof (Func<,,,>),
				typeof (Func<,,,,>),
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
				typeof(UnityEngine.Mathf),
				typeof(UnityEngine.Quaternion),
				typeof(UnityEngine.Vector4),
				typeof(UnityEngine.Vector3),
				typeof(UnityEngine.Vector3),
				typeof(UnityEngine.Color),
				typeof(UnityEngine.Color32),
				typeof(UnityEngine.Matrix4x4),
				typeof(UnityEngine.Plane),
#endif
			};
			Default = new KnownTypeResolver();
			ArrayName = typeof(Array).Name + "`1";
			ArrayFullName = typeof(Array).FullName + "`1";
		}
		public KnownTypeResolver(params Type[] knownTypes)
			: this((IEnumerable<Type>)knownTypes)
		{

		}
		public KnownTypeResolver(IEnumerable<Type> knownTypes, ITypeResolver otherTypeResolver = null, TypeDiscoveryOptions options = TypeDiscoveryOptions.All)
		{
			if (knownTypes == null) knownTypes = Type.EmptyTypes;

			this.knownTypesByFullName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
			this.knownTypesByName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
			this.knownTypes = GetKnownTypes(knownTypes, null, options);

			foreach (var type in this.knownTypes)
			{
				foreach (var name in GetTypeNames(type))
				{
					var typeList = default(List<Type>);
					if (this.knownTypesByName.TryGetValue(name, out typeList) == false)
						this.knownTypesByName.Add(name, typeList = new List<Type>());
					typeList.Add(type);
				}

				foreach (var fullName in GetTypeFullNames(type))
				{
					var typeList = default(List<Type>);
					if (this.knownTypesByFullName.TryGetValue(fullName, out typeList) == false)
						this.knownTypesByFullName.Add(fullName, typeList = new List<Type>());
					typeList.Add(type);
				}
			}

			this.otherTypeResolver = otherTypeResolver;
		}

		public bool TryGetType(TypeReference typeReference, out Type foundType)
		{
			foundType = default(Type);

			var matches = 0;
			var genericTypeRequired = typeReference.TypeArguments.Count > 0;
			var typesToCheck = default(List<Type>);
			if (this.knownTypesByFullName.TryGetValue(typeReference.FullName, out typesToCheck))
			{
				foreach (var type in typesToCheck)
				{
					if (genericTypeRequired != type.IsGenericType) continue;
					if (genericTypeRequired && type.GetGenericArguments().Length != typeReference.TypeArguments.Count) continue;

					if (foundType != type)  // could be same type
						matches++;
					foundType = type;
				}
			}
			if (this.knownTypesByName.TryGetValue(typeReference.Name, out typesToCheck) || this.knownTypesByName.TryGetValue(typeReference.FullName, out typesToCheck))
			{
				foreach (var type in typesToCheck)
				{
					if (genericTypeRequired != type.IsGenericType) continue;
					if (genericTypeRequired && type.GetGenericArguments().Length != typeReference.TypeArguments.Count) continue;

					if (foundType != type) // could be same type
						matches++;
					foundType = type;
				}
			}

			if (foundType == null && (string.Equals(typeReference.FullName, ArrayFullName, StringComparison.Ordinal) || string.Equals(typeReference.Name, ArrayName, StringComparison.Ordinal)))
			{
				foundType = typeof(Array);
				matches = 1;
			}

			if (foundType == null && otherTypeResolver != null)
			{
				otherTypeResolver.TryGetType(typeReference, out foundType);
				matches = foundType != null ? 1 : 0;
			}

			if (matches != 1)
			{
				foundType = null;
				return false;
			}

			if (foundType == typeof(Array))
			{
				if (typeReference.TypeArguments.Count == 1)
				{
					var elementType = default(Type);
					if (this.TryGetType(typeReference.TypeArguments[0], out elementType) == false)
						return false;

					foundType = elementType.MakeArrayType();
					return true;
				}

				foundType = null;
			}
			else if (foundType != null && typeReference.TypeArguments.Count > 0)
			{
				var genericParameters = default(Type[]);
				if (foundType.IsGenericType && (genericParameters = foundType.GetGenericArguments()).Length == typeReference.TypeArguments.Count)
				{
					var typeArguments = new Type[genericParameters.Length];
					var allArgumentBound = true;
					var isOpenType = true;
					for (var i = 0; i < typeArguments.Length; i++)
					{
						var genericArgumentTypeReference = typeReference.TypeArguments[i];
						if (genericArgumentTypeReference == TypeReference.Empty)
							typeArguments[i] = genericParameters[i];
						else if (this.TryGetType(genericArgumentTypeReference, out typeArguments[i]))
							isOpenType = false;
						else
							allArgumentBound = false;
					}

					if (!foundType.IsGenericTypeDefinition)
						foundType = foundType.GetGenericTypeDefinition() ?? foundType;

					if (allArgumentBound)
					{
						foundType = isOpenType ? foundType : foundType.MakeGenericType(typeArguments);
						return true;
					}
				}
				foundType = null;
			}

			return foundType != null;
		}

		private static HashSet<Type> GetKnownTypes(IEnumerable<Type> types, HashSet<Type> collection = null, TypeDiscoveryOptions options = TypeDiscoveryOptions.All)
		{
			var foundTypes = collection ?? new HashSet<Type>(BuildInTypes);

			foreach (var knownType in types)
			{
				var genericArguments = Type.EmptyTypes;
				var type = knownType;
				if (type.HasElementType) type = type.GetElementType() ?? type;
				if (type.IsGenericType)
				{
					genericArguments = type.GetGenericArguments();
					type = type.GetGenericTypeDefinition() ?? type;
				}

				var alreadyAdded = foundTypes.Add(type) == false;

				if (type.IsGenericType && (options & TypeDiscoveryOptions.GenericArguments) != 0)
					GetKnownTypes(genericArguments, foundTypes);

				if (alreadyAdded)
					continue;

				if ((options & TypeDiscoveryOptions.Interfaces) != 0)
					GetKnownTypes(type.GetInterfaces(), foundTypes, options);

				if ((options & TypeDiscoveryOptions.KnownTypes) != 0)
					GetKnownTypes(type.GetCustomAttributes(typeof(ExpressionKnownTypeAttribute), true).Cast<ExpressionKnownTypeAttribute>().Select(a => a.Type), foundTypes, options);

				if ((options & TypeDiscoveryOptions.DeclaringTypes) != 0)
				{
					var declaringTypes = default(List<Type>);
					var declaringType = type.DeclaringType;
					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					while (declaringType != null)
					{
						if (declaringTypes == null) declaringTypes = new List<Type>(10);
						declaringTypes.Add(declaringType);
						declaringType = declaringType.DeclaringType;
					}
					// ReSharper restore ConditionIsAlwaysTrueOrFalse
					if (declaringTypes != null)
						GetKnownTypes(declaringTypes, foundTypes, options);
				}
			}

			return foundTypes;
		}

		private static string[] GetTypeNames(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type == typeof(Array))
				return new[] { type.Name, type.Name + "`1" };
			else if (type.IsNested && type.IsGenericType)
				return new[] { WriteName(type).ToString(), RemoveGenericSuffix(WriteName(type).ToString()) };
			else if (type.IsNested)
				return new[] { WriteName(type).ToString() };
			else
				return new[] { type.Name };
		}
		private static string[] GetTypeFullNames(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type == typeof(Array))
				return new[] { type.FullName, type.FullName + "`1" };
			else if (type.IsNested && type.IsGenericType)
				return new[] { WriteFullName(type).ToString(), RemoveGenericSuffix(WriteFullName(type).ToString()) };
			else if (type.IsNested)
				return new[] { WriteFullName(type).ToString() };
			else if (type.IsGenericType)
				return new[] { type.FullName, RemoveGenericSuffix(type.FullName) };
			else
				return new[] { type.FullName };
		}
		private static StringBuilder WriteFullName(Type type, StringBuilder builder = null)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (builder == null) builder = new StringBuilder();

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (type.DeclaringType != null)
				WriteFullName(type.DeclaringType, builder);

			if (string.IsNullOrEmpty(type.Namespace) == false && type.IsNested == false)
			{
				if (builder.Length > 0)
					builder.Append(".");
				builder.Append(type.Namespace);
			}

			if (builder.Length > 0)
				builder.Append(".");
			builder.Append(type.Name);

			return builder;
		}
		private static StringBuilder WriteName(Type type, StringBuilder builder = null)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (builder == null) builder = new StringBuilder();

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (type.DeclaringType != null)
				WriteName(type.DeclaringType, builder);

			if (builder.Length > 0)
				builder.Append(".");

			builder.Append(type.Name);

			return builder;
		}
		private static string RemoveGenericSuffix(string name)
		{
			if (name == null) throw new ArgumentNullException("name");

			var markerIndex = name.IndexOf('`');
			var offset = 0;
			if (markerIndex < 0) return name;

			var builder = new StringBuilder(name.Length);

			while (markerIndex >= 0)
			{
				builder.Append(name, offset, markerIndex - offset);
				markerIndex++;
				while (markerIndex < name.Length && char.IsDigit(name[markerIndex]))
					markerIndex++;
				offset = markerIndex;
				markerIndex = name.IndexOf('`', offset);
			}

			return builder.ToString();
		}

		public override string ToString()
		{
			return this.GetType().Name + ": " + string.Join(", ", this.knownTypesByName.Keys.ToArray()) + (this.otherTypeResolver != null ? " -> " + this.otherTypeResolver : string.Empty);
		}
	}
}
