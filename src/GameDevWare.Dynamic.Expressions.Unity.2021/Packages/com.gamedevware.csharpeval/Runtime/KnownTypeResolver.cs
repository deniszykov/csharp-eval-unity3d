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
	/// <summary>
	///     <see cref="ITypeResolver" /> which allows access to specified number of types.
	/// </summary>
	public class KnownTypeResolver : ITypeResolver, IExtensionMethodResolver
	{
		private static readonly string ArrayFullName;

		private static readonly string ArrayName;
		private static readonly HashSet<Type> BuildInTypes;
		/// <summary>
		///     Default instance of <see cref="KnownTypeResolver" /> which knows about <see cref="Math" />, <see cref="Array" />
		///     and primitive types.
		/// </summary>
		public static readonly KnownTypeResolver Default;
		private readonly HashSet<Type> knownTypes;

		private readonly Dictionary<string, List<Type>> knownTypesByFullName;
		private readonly Dictionary<string, List<Type>> knownTypesByName;
		private readonly ITypeResolver otherTypeResolver;

		private readonly Dictionary<Type, Dictionary<string, MethodInfo[]>> extensionMethodsByTargetType;
		private List<MethodInfo> declaredExtensionMethods;

		static KnownTypeResolver()
		{
			BuildInTypes = new HashSet<Type> {
				typeof(object),
				typeof(bool),
				typeof(char),
				typeof(sbyte),
				typeof(byte),
				typeof(short),
				typeof(ushort),
				typeof(int),
				typeof(uint),
				typeof(long),
				typeof(ulong),
				typeof(float),
				typeof(double),
				typeof(decimal),
				typeof(DateTime),
				typeof(TimeSpan),
				typeof(string),
				typeof(Math),
				typeof(Array),
				typeof(Enumerable),
				typeof(Nullable<>),
				typeof(Func<>),
				typeof(Func<,>),
				typeof(Func<,,>),
				typeof(Func<,,,>),
				typeof(Func<,,,,>),
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_2021_3_OR_NEWER
				typeof(UnityEngine.Mathf),
				typeof(UnityEngine.Quaternion),
				typeof(UnityEngine.Vector4),
				typeof(UnityEngine.Vector3),
				typeof(UnityEngine.Vector2),
				typeof(UnityEngine.Color),
				typeof(UnityEngine.Color32),
				typeof(UnityEngine.Matrix4x4),
				typeof(UnityEngine.Plane),
#endif
			};
			Default = new KnownTypeResolver();
			ArrayName = nameof(Array) + "`1";
			ArrayFullName = typeof(Array).FullName + "`1";
		}
		/// <summary>
		///     Creates new <see cref="KnownTypeResolver" /> from specified list of types and
		///     <see cref="TypeDiscoveryOptions.All" />.
		/// </summary>
		/// <param name="knownTypes">List of known types.</param>
		public KnownTypeResolver(params Type[] knownTypes)
			: this((IEnumerable<Type>)knownTypes)
		{
		}
		/// <summary>
		///     Creates new <see cref="KnownTypeResolver" /> from specified list of types and
		///     <see cref="TypeDiscoveryOptions.All" />.
		/// </summary>
		/// <param name="knownTypes">List of known types.</param>
		public KnownTypeResolver(IEnumerable<Type> knownTypes) : this(knownTypes, null, TypeDiscoveryOptions.All)
		{
		}

		/// <summary>
		///     Creates new <see cref="KnownTypeResolver" /> from specified list of types and
		///     <see cref="TypeDiscoveryOptions.All" />.
		/// </summary>
		/// <param name="knownTypes">List of known types.</param>
		/// <param name="otherTypeResolver">Backup type resolver used if current can't find a type.</param>
		public KnownTypeResolver(IEnumerable<Type> knownTypes, ITypeResolver otherTypeResolver) : this(knownTypes, otherTypeResolver, TypeDiscoveryOptions.All)
		{
		}
		/// <summary>
		///     Creates new <see cref="KnownTypeResolver" /> from specified list of types and specified
		///     <see cref="TypeDiscoveryOptions" />.
		/// </summary>
		/// <param name="knownTypes">List of known types.</param>
		/// <param name="otherTypeResolver">Backup type resolver used if current can't find a type.</param>
		/// <param name="options">Types discovery options.</param>
		public KnownTypeResolver(IEnumerable<Type> knownTypes, ITypeResolver otherTypeResolver, TypeDiscoveryOptions options)
		{
			if (knownTypes == null) knownTypes = Type.EmptyTypes;

			this.knownTypesByFullName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
			this.knownTypesByName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
			this.extensionMethodsByTargetType = new Dictionary<Type, Dictionary<string, MethodInfo[]>>();
			this.knownTypes = GetKnownTypes(knownTypes, null, options);

			foreach (var type in this.knownTypes)
			{
				foreach (var name in type.GetTypeNames())
				{
					if (!this.knownTypesByName.TryGetValue(name, out var typeList))
						this.knownTypesByName.Add(name, typeList = new List<Type>());
					typeList.Add(type);
				}

				foreach (var fullName in type.GetTypeFullNames())
				{
					if (!this.knownTypesByFullName.TryGetValue(fullName, out var typeList))
						this.knownTypesByFullName.Add(fullName, typeList = new List<Type>());
					typeList.Add(type);
				}
			}

			this.otherTypeResolver = otherTypeResolver;
		}

		private static HashSet<Type> GetKnownTypes(IEnumerable<Type> types, HashSet<Type> collection, TypeDiscoveryOptions options)
		{
			var foundTypes = collection ?? new HashSet<Type>(BuildInTypes);

			foreach (var knownType in types)
			{
				var genericArguments = Type.EmptyTypes;
				var type = knownType;
				var typeInfo = type.GetTypeInfo();
				if (type.HasElementType)
				{
					type = type.GetElementType() ?? type;
					typeInfo = type.GetTypeInfo();
				}

				if (typeInfo.IsGenericType)
				{
					genericArguments = typeInfo.GetGenericArguments();
					type = type.GetGenericTypeDefinition();
				}

				if (type.IsGenericParameter) continue;

				var alreadyAdded = !foundTypes.Add(type);

				if (typeInfo.IsGenericType && (options & TypeDiscoveryOptions.GenericArguments) != 0)
					GetKnownTypes(genericArguments, foundTypes, options);

				if (alreadyAdded)
					continue;

				if ((options & TypeDiscoveryOptions.Interfaces) != 0)
					GetKnownTypes(typeInfo.GetImplementedInterfaces(), foundTypes, options);

				if ((options & TypeDiscoveryOptions.KnownTypes) != 0)
					GetKnownTypes(typeInfo.GetCustomAttributes(typeof(ExpressionKnownTypeAttribute), true).Cast<ExpressionKnownTypeAttribute>().Select(a => a.Type),
						foundTypes, options);

				if ((options & TypeDiscoveryOptions.DeclaringTypes) != 0)
				{
					var declaringTypes = default(List<Type>);
					var declaringType = type.DeclaringType;

					// ReSharper disable HeuristicUnreachableCode
					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					while (declaringType != null)
					{
						if (declaringTypes == null) declaringTypes = new List<Type>(10);
						declaringTypes.Add(declaringType);
						declaringType = declaringType.DeclaringType;
					}

					if (declaringTypes != null)
						GetKnownTypes(declaringTypes, foundTypes, options);

					// ReSharper restore ConditionIsAlwaysTrueOrFalse
					// ReSharper restore HeuristicUnreachableCode
				}
			}

			return foundTypes;
		}

		/// <summary>
		///     Converts type resolver to string for debug purpose.
		/// </summary>
		public override string ToString()
		{
			return this.GetType().Name +
				": " +
				string.Join(", ", this.knownTypesByName.Keys.ToArray()) +
				(this.otherTypeResolver != null ? " -> " + this.otherTypeResolver : string.Empty);
		}

		/// <summary>
		///     Tries to retrieve type by it's name and generic parameters.
		/// </summary>
		/// <param name="typeReference">Type name. Not null. Not <see cref="TypeReference.Empty" /></param>
		/// <param name="foundType">Found type or null.</param>
		/// <returns>True if type is found. Overwise is false.</returns>
		public bool TryGetType(TypeReference typeReference, out Type foundType)
		{
			foundType = null;

			var matches = 0;
			var genericTypeRequired = typeReference.TypeArguments.Count > 0;
			if (this.knownTypesByFullName.TryGetValue(typeReference.FullName, out var typesToCheck))
			{
				foreach (var type in typesToCheck)
				{
					var typeInfo = type.GetTypeInfo();
					if (genericTypeRequired != typeInfo.IsGenericType) continue;
					if (genericTypeRequired && typeInfo.GetGenericArguments().Length != typeReference.TypeArguments.Count) continue;

					if (foundType != type) // could be same type
						matches++;
					foundType = type;
				}
			}

			if (this.knownTypesByName.TryGetValue(typeReference.FullName, out typesToCheck))
			{
				foreach (var type in typesToCheck)
				{
					var typeInfo = type.GetTypeInfo();
					if (genericTypeRequired != typeInfo.IsGenericType) continue;
					if (genericTypeRequired && typeInfo.GetGenericArguments().Length != typeReference.TypeArguments.Count) continue;

					if (foundType != type) // could be same type
						matches++;
					foundType = type;
				}
			}

			if (foundType == null &&
				(string.Equals(typeReference.FullName, ArrayFullName, StringComparison.Ordinal) ||
					string.Equals(typeReference.Name, ArrayName, StringComparison.Ordinal)))
			{
				foundType = typeof(Array);
				matches = 1;
			}

			if (foundType == null && this.otherTypeResolver != null)
			{
				this.otherTypeResolver.TryGetType(typeReference, out foundType);
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
					if (!this.TryGetType(typeReference.TypeArguments[0], out var elementType))
						return false;

					foundType = typeReference.Rank > 1 ? elementType.MakeArrayType(typeReference.Rank) : elementType.MakeArrayType();
					return true;
				}

				if (typeReference.TypeArguments.Count == 0) return true;

				foundType = null;
			}
			else if (foundType != null && typeReference.TypeArguments.Count > 0)
			{
				var genericParameters = default(Type[]);
				var foundTypeInfo = foundType.GetTypeInfo();
				if (foundTypeInfo.IsGenericType &&
					(genericParameters = foundTypeInfo.GetGenericArguments()).Length ==
					typeReference.TypeArguments
						.Count) // BUG: will not match for nested generic types like MyType<T>.MyInnerType<T1> because it will only count one generic argument from last part of type reference
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

					if (!foundTypeInfo.IsGenericTypeDefinition)
						foundType = foundType.GetGenericTypeDefinition();

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
		/// <summary>
		///     Checks if specified type is known by current type resolver;
		/// </summary>
		/// <param name="type">Type to lookup. Not null.</param>
		/// <returns>True if type is known by this resolver. Overwise false.</returns>
		public bool IsKnownType(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return this.knownTypes.Contains(type) || (this.otherTypeResolver != null && this.otherTypeResolver.IsKnownType(type));
		}

		/// <summary>
		///     Tries to retrieve extension methods which could accept specified type as their receiver.
		/// </summary>
		/// <param name="targetType">Type of a value the method is invoked on. Not null.</param>
		/// <param name="methodName">Name of the invoked method. Not null.</param>
		/// <param name="extensionMethods">Found methods or null.</param>
		/// <returns>True if at least one method is found. Overwise is false.</returns>
		public bool TryGetExtensionMethods(Type targetType, string methodName, out MethodInfo[] extensionMethods)
		{
			if (targetType == null) throw new ArgumentNullException(nameof(targetType));
			if (methodName == null) throw new ArgumentNullException(nameof(methodName));

			lock (this.extensionMethodsByTargetType)
			{
				if (!this.extensionMethodsByTargetType.TryGetValue(targetType, out var methodsByName))
					this.extensionMethodsByTargetType[targetType] = methodsByName = this.CreateExtensionMethods(targetType);

				if (methodsByName.TryGetValue(methodName, out extensionMethods))
					return true;
			}

			if (this.otherTypeResolver is IExtensionMethodResolver otherExtensionMethodResolver)
				return otherExtensionMethodResolver.TryGetExtensionMethods(targetType, methodName, out extensionMethods);

			extensionMethods = null;
			return false;
		}

		private Dictionary<string, MethodInfo[]> CreateExtensionMethods(Type targetType)
		{
			var methodListsByName = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
			foreach (var method in this.GetDeclaredExtensionMethods())
			{
				if (!IsPossibleReceiver(method.GetParameters()[0].ParameterType, targetType)) continue;

				if (!methodListsByName.TryGetValue(method.Name, out var methodList))
					methodListsByName.Add(method.Name, methodList = new List<MethodInfo>());
				methodList.Add(method);
			}

			var methodsByName = new Dictionary<string, MethodInfo[]>(methodListsByName.Count, StringComparer.Ordinal);
			foreach (var kv in methodListsByName)
			{
				methodsByName.Add(kv.Key, kv.Value.ToArray());
			}

			return methodsByName;
		}
		private List<MethodInfo> GetDeclaredExtensionMethods()
		{
			if (this.declaredExtensionMethods != null)
				return this.declaredExtensionMethods;

			var extensionMethods = new List<MethodInfo>();
			foreach (var type in this.knownTypes)
			{
				var typeInfo = type.GetTypeInfo();
				// only a non-generic static class could declare extension methods
				if (!typeInfo.IsClass || !typeInfo.IsAbstract || !typeInfo.IsSealed || typeInfo.IsGenericType || !IsExtension(typeInfo))
					continue;

				foreach (var method in typeInfo.GetDeclaredMethods())
				{
					if (method.IsPublic && method.IsStatic && method.GetParameters().Length > 0 && IsExtension(method))
						extensionMethods.Add(method);
				}
			}

			return this.declaredExtensionMethods = extensionMethods;
		}

		/// <summary>
		///     Checks if specified type could be passed as the receiver of an extension method declaring
		///     <paramref name="receiverType" /> as its first parameter. An open receiver is matched by shape because its
		///     type arguments are only bound during overload resolution.
		/// </summary>
		private static bool IsPossibleReceiver(Type receiverType, Type targetType)
		{
			var receiverTypeInfo = receiverType.GetTypeInfo();
			if (!receiverTypeInfo.ContainsGenericParameters)
				return receiverTypeInfo.IsAssignableFrom(targetType.GetTypeInfo());

			if (receiverType.IsGenericParameter)
				return true;

			if (receiverTypeInfo.IsArray)
				return targetType.GetTypeInfo().IsArray;

			if (!receiverTypeInfo.IsGenericType)
				return false;

			var genericTypeDefinition = receiverType.GetGenericTypeDefinition();
			var baseType = targetType;
			while (baseType != null)
			{
				var baseTypeInfo = baseType.GetTypeInfo();
				if (baseTypeInfo.IsGenericType && baseType.GetGenericTypeDefinition() == genericTypeDefinition)
					return true;

				baseType = baseTypeInfo.BaseType;
			}

			foreach (var interfaceType in targetType.GetTypeInfo().GetImplementedInterfaces())
			{
				var interfaceTypeInfo = interfaceType.GetTypeInfo();
				if (interfaceTypeInfo.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericTypeDefinition)
					return true;
			}

			return false;
		}
		private static bool IsExtension(MemberInfo member)
		{
			foreach (var attribute in member.GetCustomAttributes(true))
			{
				var attributeType = attribute.GetType();
				if (attributeType.Namespace == "System.Runtime.CompilerServices" && attributeType.Name == "ExtensionAttribute")
					return true;
			}

			return false;
		}
	}
}
