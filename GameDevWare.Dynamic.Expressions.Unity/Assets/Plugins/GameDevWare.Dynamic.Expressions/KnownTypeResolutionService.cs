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
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	public class KnownTypeResolutionService : ITypeResolutionService
	{
		public static readonly HashSet<Type> BuildInTypes;
		public static readonly KnownTypeResolutionService Default;

		private readonly Dictionary<string, Type[]> knownTypesByFullName;
		private readonly Dictionary<string, Type[]> knownTypesByName;
		private readonly ITypeResolutionService otherTypeResolutionService;

		static KnownTypeResolutionService()
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
			Default = new KnownTypeResolutionService();
		}
		public KnownTypeResolutionService(params Type[] knownTypes)
			: this((IEnumerable<Type>)knownTypes)
		{

		}
		public KnownTypeResolutionService(IEnumerable<Type> knownTypes) : this(knownTypes, null)
		{
		}
		public KnownTypeResolutionService(IEnumerable<Type> knownTypes, ITypeResolutionService otherTypeResolutionService)
		{
			if (knownTypes == null) knownTypes = Type.EmptyTypes;

			var allTypes = GetKnownTypes(knownTypes);
			this.knownTypesByFullName = allTypes
				.ToLookup(t => t.FullName)
				.ToDictionary(kv => kv.Key, kv => kv.ToArray());
			this.knownTypesByName = allTypes
				.ToLookup(t => t.Name)
				.ToDictionary(kv => kv.Key, kv => kv.ToArray());
			this.otherTypeResolutionService = otherTypeResolutionService;
		}

		public Type GetType(string name)
		{
			return this.GetTypeInternal(name, throwOnError: true);
		}
		public bool TryGetType(string name, out Type type)
		{
			type = GetTypeInternal(name, throwOnError: false);
			return type != null;
		}

		private Type GetTypeInternal(string name, bool throwOnError)
		{
			var arrayDepth = 0;
			var end = name.Length;
			while (end > 2 && string.CompareOrdinal(name, end - 2, "[]", 0, 2) == 0)
			{
				arrayDepth++;
				end -= 2;
			}

			if (arrayDepth != 0)
				name = name.Substring(0, name.Length - arrayDepth * 2);

			var foundTypes = default(Type[]);
			if (knownTypesByFullName.TryGetValue(name, out foundTypes) == false)
				knownTypesByName.TryGetValue(name, out foundTypes);

			if (foundTypes == null)
				foundTypes = Type.EmptyTypes;

			if (foundTypes.Length == 0 && throwOnError)
				throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, name, string.Join(", ", this.knownTypesByFullName.Keys.ToArray())), "name");
			else if (foundTypes.Length > 1 && throwOnError)
				throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPEMULTIPLE, name, string.Join(", ", Array.ConvertAll(foundTypes, t => t.FullName))), "name");

			var foundType = foundTypes.FirstOrDefault();
			while (foundType != null && arrayDepth-- > 0)
				foundType = foundType.MakeArrayType();

			if (foundType == null && otherTypeResolutionService != null)
			{
				if (throwOnError)
					foundType = otherTypeResolutionService.GetType(name);
				else
					otherTypeResolutionService.TryGetType(name, out foundType);
			}

			return foundType;
		}

		private HashSet<Type> GetKnownTypes(IEnumerable<Type> types)
		{
			var foundTypes = new HashSet<Type>(BuildInTypes);

			foreach (var type in types)
			{
				if (foundTypes.Add(type) == false)
					continue;

				foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
					foundTypes.Add(property.PropertyType);

				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
					foundTypes.Add(field.FieldType);

				foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					foundTypes.Add(method.ReturnType);

					foreach (var parameter in method.GetParameters())
						foundTypes.Add(parameter.ParameterType);
				}

				foreach (ExpressionKnownTypeAttribute knownTypeAttribute in type.GetCustomAttributes(typeof(ExpressionKnownTypeAttribute), true))
					foundTypes.Add(knownTypeAttribute.Type);
			}

			foundTypes.RemoveWhere(t => t.IsGenericType);

			return foundTypes;
		}

		public override string ToString()
		{
			return this.GetType().Name + ": " + string.Join(", ", this.knownTypesByName.Keys.ToArray()) + (this.otherTypeResolutionService != null ? " -> " + this.otherTypeResolutionService : string.Empty);
		}
	}
}
