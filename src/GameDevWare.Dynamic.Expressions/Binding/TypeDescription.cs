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
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class TypeDescription : IComparable<TypeDescription>, IComparable, IEquatable<TypeDescription>, IEquatable<Type>
	{
		private static readonly TypeCache Types;
		public static readonly MemberDescription[] EmptyMembers;
		public static readonly TypeDescription[] EmptyTypes;
		public static readonly TypeDescription ObjectType;
		public static readonly TypeDescription Int32Type;
		public static readonly Expression NullObjectDefaultExpression = Expression.Constant(null, typeof(object));

		private readonly Type type;
		private readonly int hashCode;
		private TypeDescription nullableType;
		private Expression defaultExpression;

		public readonly Dictionary<string, MemberDescription[]> MembersByName;
		public readonly MemberDescription[] ImplicitConvertTo;
		public readonly MemberDescription[] ImplicitConvertFrom;
		public readonly MemberDescription[] ExplicitConvertTo;
		public readonly MemberDescription[] ExplicitConvertFrom;
		public readonly MemberDescription[] Conversions;
		public readonly MemberDescription[] Addition;
		public readonly MemberDescription[] Division;
		public readonly MemberDescription[] Equality;
		public readonly MemberDescription[] GreaterThan;
		public readonly MemberDescription[] GreaterThanOrEqual;
		public readonly MemberDescription[] Inequality;
		public readonly MemberDescription[] LessThan;
		public readonly MemberDescription[] LessThanOrEqual;
		public readonly MemberDescription[] Modulus;
		public readonly MemberDescription[] Multiply;
		public readonly MemberDescription[] Subtraction;
		public readonly MemberDescription[] UnaryNegation;
		public readonly MemberDescription[] UnaryPlus;
		public readonly MemberDescription[] BitwiseAnd;
		public readonly MemberDescription[] BitwiseOr;
		public readonly MemberDescription[] Indexers;
		public readonly MemberDescription[] Constructors;

		public readonly string Name;
		public readonly TypeCode TypeCode;
		public Expression DefaultExpression { get { return this.GetOrCreateDefaultExpression(); } }
		public readonly bool IsNullable;
		public readonly bool CanBeNull;
		public readonly bool IsEnum;
		public readonly bool IsVoid;
		public readonly bool IsNumber;
		public readonly bool IsDelegate;
		public readonly bool IsInterface;
		public readonly bool IsByRefLike;
		public readonly bool IsValueType;
		public readonly bool HasGenericParameters;
		public readonly TypeDescription BaseType;
		public readonly TypeDescription UnderlyingType;
		public readonly TypeDescription[] BaseTypes;
		public readonly TypeDescription[] Interfaces;
		public readonly TypeDescription[] GenericArguments;

		static TypeDescription()
		{
			Types = new TypeCache();
			EmptyMembers = new MemberDescription[0];
			EmptyTypes = new TypeDescription[0];
			ObjectType = GetTypeDescription(typeof(object));
			Int32Type = GetTypeDescription(typeof(int));

			// create type descriptors for build-in types
			ArrayUtils.ConvertAll(new[]
			{
				typeof(char?), typeof(string), typeof(float?), typeof(double?), typeof(decimal?),
				typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?), typeof(int?), typeof(uint?),
				typeof(long?), typeof(ulong?), typeof(Enum), typeof(MulticastDelegate)
			}, GetTypeDescription);
		}
		public TypeDescription(Type type, TypeCache cache)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (cache == null) throw new ArgumentNullException("cache");

			this.type = type;
			this.hashCode = type.GetHashCode();
			this.Name = TypeNameUtils.RemoveGenericSuffix(type.GetCSharpName()).ToString();

			cache.Add(type, this);

			var typeInfo = type.GetTypeInfo();
			var underlyingType = default(Type);
			if (typeInfo.IsEnum)
				underlyingType = Enum.GetUnderlyingType(type);
			else if (typeInfo.IsValueType)
				underlyingType = Nullable.GetUnderlyingType(type);
			else if (typeInfo.IsArray)
				underlyingType = typeInfo.GetElementType();

			this.BaseType = typeInfo.BaseType != null ? cache.GetOrCreateTypeDescription(typeInfo.BaseType) : null;
			this.UnderlyingType = underlyingType != null ? cache.GetOrCreateTypeDescription(underlyingType) : null;
			this.BaseTypes = GetBaseTypes(this, 0);
			this.Interfaces = typeInfo.GetImplementedInterfaces().Select(cache.GetOrCreateTypeDescription).ToArray();
			this.GenericArguments = typeInfo.IsGenericType ? ArrayUtils.ConvertAll(typeInfo.GetGenericArguments(), cache.GetOrCreateTypeDescription) : EmptyTypes;

			this.IsInterface = typeInfo.IsInterface;
			this.IsValueType = typeInfo.IsValueType;
			this.IsNullable = Nullable.GetUnderlyingType(type) != null;
			this.IsNumber = NumberUtils.IsNumber(type);
			this.CanBeNull = this.IsNullable || typeInfo.IsValueType == false;
			this.IsEnum = typeInfo.IsEnum;
			this.IsVoid = type == typeof(void);
			this.IsDelegate = typeof(Delegate).GetTypeInfo().IsAssignableFrom(typeInfo) && type != typeof(Delegate) && type != typeof(MulticastDelegate);
			this.IsByRefLike = HasByRefLikeAttribute(type);
#if NETFRAMEWORK
			this.IsByRefLike = this.IsByRefLike || type == typeof(TypedReference) || type == typeof(ArgIterator) || type == typeof(RuntimeArgumentHandle);
#endif
			this.HasGenericParameters = typeInfo.ContainsGenericParameters;
			this.TypeCode = ReflectionUtils.GetTypeCode(type);

			this.MembersByName = this.GetMembersByName(ref this.Indexers);
			this.Indexers = this.Indexers ?? (MemberDescription[])Enumerable.Empty<MemberDescription>();

			var methods = typeInfo.GetAllMethods().Where(m => m.IsPublic && m.IsStatic).ToList();
			var methodsDescriptions = new MemberDescription[methods.Count];

			// ReSharper disable LocalizableElement
			this.ImplicitConvertTo = this.GetOperators(methods, methodsDescriptions, "op_Implicit", 0);
			this.ImplicitConvertFrom = this.GetOperators(methods, methodsDescriptions, "op_Implicit", -1);
			this.ExplicitConvertTo = this.GetOperators(methods, methodsDescriptions, "op_Explicit", 0);
			this.ExplicitConvertFrom = this.GetOperators(methods, methodsDescriptions, "op_Explicit", -1);
			this.Addition = this.GetOperators(methods, methodsDescriptions, "op_Addition");
			this.Division = this.GetOperators(methods, methodsDescriptions, "op_Division");
			this.Equality = this.GetOperators(methods, methodsDescriptions, "op_Equality");
			this.GreaterThan = this.GetOperators(methods, methodsDescriptions, "op_GreaterThan");
			this.GreaterThanOrEqual = this.GetOperators(methods, methodsDescriptions, "op_GreaterThanOrEqual");
			this.Inequality = this.GetOperators(methods, methodsDescriptions, "op_Inequality");
			this.LessThan = this.GetOperators(methods, methodsDescriptions, "op_LessThan");
			this.LessThanOrEqual = this.GetOperators(methods, methodsDescriptions, "op_LessThanOrEqual");
			this.Modulus = this.GetOperators(methods, methodsDescriptions, "op_Modulus");
			this.Multiply = this.GetOperators(methods, methodsDescriptions, "op_Multiply");
			this.Subtraction = this.GetOperators(methods, methodsDescriptions, "op_Subtraction");
			this.UnaryNegation = this.GetOperators(methods, methodsDescriptions, "op_UnaryNegation");
			this.UnaryPlus = this.GetOperators(methods, methodsDescriptions, "op_UnaryPlus");
			this.BitwiseAnd = this.GetOperators(methods, methodsDescriptions, "op_BitwiseAnd");
			this.BitwiseOr = this.GetOperators(methods, methodsDescriptions, "op_BitwiseOr");
			// ReSharper restore LocalizableElement
			this.Conversions = Combine(this.ImplicitConvertTo, this.ImplicitConvertFrom, this.ExplicitConvertTo, this.ExplicitConvertFrom);
			this.Constructors = type.GetTypeInfo().GetPublicInstanceConstructors()
				.Select(ctr => new MemberDescription(this, ctr))
				.Where(ctr => !ctr.HasByRefLikeParameters).ToArray();

			Array.Sort(this.Conversions);
			Array.Sort(this.Constructors);

			if (this.IsNullable && this.UnderlyingType != null)
				this.UnderlyingType.nullableType = this;
		}

		private MemberDescription[] GetOperators(List<MethodInfo> methods, MemberDescription[] methodsDescriptions, string operatorName, int? compareParameterIndex = null)
		{
			if (methods == null) throw new ArgumentNullException("methods");
			if (methodsDescriptions == null) throw new ArgumentNullException("methodsDescriptions");
			if (operatorName == null) throw new ArgumentNullException("operatorName");

			var operators = default(List<MemberDescription>);
			for (var i = 0; i < methods.Count; i++)
			{
				var method = methods[i];
				if (method.Name != operatorName || method.IsGenericMethod)
				{
					continue;
				}

				if (methodsDescriptions[i] == null)
				{
					methodsDescriptions[i] = new MemberDescription(this, method);
				}

				var methodDescription = methodsDescriptions[i];
				if (compareParameterIndex.HasValue && methodDescription.GetParameterType(compareParameterIndex.Value) != this.type)
				{
					continue;
				}

				if (operators == null)
				{
					operators = new List<MemberDescription>();
				}
				operators.Add(methodDescription);
			}

			return operators != null ? operators.ToArray() : EmptyMembers;
		}
		private Expression GetOrCreateDefaultExpression()
		{
			if (this.defaultExpression != null)
			{
				return this.defaultExpression;
			}
			if (this.IsVoid || this.IsByRefLike || this.type.IsPointer)
			{
				this.defaultExpression = NullObjectDefaultExpression;
			}
			else
			{
				try
				{
					this.defaultExpression = Expression.Constant(this.IsValueType && this.IsNullable == false ? Activator.CreateInstance(this.type) : null, this.type);
				}
				catch
				{
					this.defaultExpression = Expression.Constant(null, this.type);
				}
			}
			return this.defaultExpression;
		}
		private Dictionary<string, MemberDescription[]> GetMembersByName(ref MemberDescription[] indexers)
		{
			var declaredMembers = GetDeclaredMembers(this.type);
			var memberSetsByName = new Dictionary<string, HashSet<MemberDescription>>((this.BaseType != null ? this.BaseType.MembersByName.Count : 0) + declaredMembers.Count);
			foreach (var member in declaredMembers)
			{
				var memberDescription = default(MemberDescription);
				var method = member as MethodInfo;
				var field = member as FieldInfo;
				var property = member as PropertyInfo;
				if (property != null)
				{
					memberDescription = new MemberDescription(this, property);
					if (memberDescription.GetParametersCount() != 0)
					{
						Add(ref indexers, memberDescription);
					}
				}
				else if (field != null)
				{
					memberDescription = new MemberDescription(this, field);
				}
				else if (method != null && method.IsSpecialName == false)
				{
					memberDescription = new MemberDescription(this, method);
				}

				if (memberDescription == null)
					continue;

				var members = default(HashSet<MemberDescription>);
				if (memberSetsByName.TryGetValue(memberDescription.Name, out members) == false)
					memberSetsByName[memberDescription.Name] = members = new HashSet<MemberDescription>();
				members.Add(memberDescription);
			}

			if (this.BaseType != null)
			{
				foreach (var kv in this.BaseType.MembersByName)
				{
					var memberName = kv.Key;
					var memberList = kv.Value;

					var members = default(HashSet<MemberDescription>);
					if (memberSetsByName.TryGetValue(memberName, out members) == false)
						memberSetsByName[memberName] = members = new HashSet<MemberDescription>();

					foreach (var member in memberList)
						members.Add(member);
				}
			}

			var membersByName = new Dictionary<string, MemberDescription[]>(memberSetsByName.Count, StringComparer.Ordinal);
			foreach (var kv in memberSetsByName)
			{
				var membersArray = kv.Value.ToArray();
				Array.Sort(membersArray);
				membersByName.Add(kv.Key, membersArray);
			}
			return membersByName;
		}
		private static List<MemberInfo> GetDeclaredMembers(Type type)
		{
			var typeInfo = type.GetTypeInfo();
			var declaredMembers = new List<MemberInfo>(typeInfo.GetDeclaredMembers());
			if (typeInfo.IsInterface)
			{
				foreach (var interfaceType in typeInfo.GetImplementedInterfaces())
				{
					var interfaceTypeInfo = interfaceType.GetTypeInfo();
					declaredMembers.AddRange(interfaceTypeInfo.GetDeclaredMembers());
				}
			}
			return declaredMembers;
		}
		private static TypeDescription[] GetBaseTypes(TypeDescription type, int depth)
		{
			var baseTypes = default(TypeDescription[]);
			if (type.BaseType != null)
				baseTypes = GetBaseTypes(type.BaseType, depth + 1);
			else
				baseTypes = new TypeDescription[depth + 1];

			baseTypes[depth] = type;
			return baseTypes;
		}
		private static void Add<T>(ref T[] array, T element) where T : class
		{
			if (element == null) throw new ArgumentNullException("element");

			if (array == null)
			{
				array = new T[] { element };
			}
			else
			{
				Array.Resize(ref array, array.Length + 1);
				array[array.Length - 1] = element;
			}
		}
		private static T[] Combine<T>(params T[][] arrays)
		{
			var totalLength = 0;
			foreach (var array in arrays)
				totalLength += array.Length;

			if (arrays.Length == 0)
				return arrays[0];

			var newArray = new T[totalLength];
			var offset = 0;
			foreach (var array in arrays)
			{
				array.CopyTo(newArray, offset);
				offset += array.Length;
			}
			return newArray;
		}

		public static bool HasByRefLikeAttribute(Type type)
		{
			return type.GetTypeInfo().GetCustomAttributes(inherit: true).Any(attribute => IsByRefLikeAttributeType(attribute.GetType()));
		}
		private static bool IsByRefLikeAttributeType(Type attributeType)
		{
			return attributeType.Namespace == "System.Runtime.CompilerServices" && attributeType.Name == "IsByRefLikeAttribute";
		}

		public MemberDescription[] GetMembers(string memberName)
		{
			if (memberName == null) throw new ArgumentNullException("memberName");

			var members = default(MemberDescription[]);
			if (this.MembersByName.TryGetValue(memberName, out members))
				return members;
			else
				return EmptyMembers;
		}

		public TypeDescription GetNullableType()
		{
			if (this.IsNullable)
				return this;
			else if (this.IsValueType == false)
				throw new InvalidOperationException();
			else if (this.nullableType != null)
				return this.nullableType;
			else
				return this.nullableType = GetTypeDescription(typeof(Nullable<>).MakeGenericType(this.type));
		}

		public bool IsAssignableFrom(Type operandType)
		{
			return this.type.GetTypeInfo().IsAssignableFrom(operandType.GetTypeInfo());
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as TypeDescription);
		}
		public override int GetHashCode()
		{
			return this.hashCode;
		}
		public int CompareTo(TypeDescription other)
		{
			if (other == null) return 1;

			return this.hashCode.CompareTo(other.hashCode);
		}
		public int CompareTo(object obj)
		{
			return this.CompareTo(obj as TypeDescription);
		}
		public bool Equals(TypeDescription other)
		{
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;

			return this.type == other.type;
		}
		public bool Equals(Type other)
		{
			if (other == null) return false;
			if (ReferenceEquals(this.type, other)) return true;

			return this.type == other;
		}

		public static bool operator ==(TypeDescription type1, TypeDescription type2)
		{
			if (ReferenceEquals(type1, type2)) return true;
			if (ReferenceEquals(type1, null) || ReferenceEquals(type2, null)) return false;

			return type1.Equals(type2);
		}
		public static bool operator !=(TypeDescription type1, TypeDescription type2)
		{
			return !(type1 == type2);
		}
		public static bool operator ==(TypeDescription type1, Type type2)
		{
			if (!ReferenceEquals(type1, null) && ReferenceEquals(type1.type, type2)) return true;
			if (ReferenceEquals(type1, null) || ReferenceEquals(type2, null)) return false;

			return type1.type == type2;
		}
		public static bool operator !=(Type type1, TypeDescription type2)
		{
			return !(type2 == type1);
		}
		public static bool operator ==(Type type1, TypeDescription type2)
		{
			if (!ReferenceEquals(type2, null) && ReferenceEquals(type2.type, type1)) return true;
			if (ReferenceEquals(type1, null) || ReferenceEquals(type2, null)) return false;

			return type2.type == type1;
		}
		public static bool operator !=(TypeDescription type1, Type type2)
		{
			return !(type1 == type2);
		}

		public static implicit operator Type(TypeDescription typeDescription)
		{
			if (typeDescription == null) return null;
			return typeDescription.type;
		}

		public static TypeDescription GetTypeDescription(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			lock (Types)
			{
				var typeDescription = default(TypeDescription);
				if (Types.TryGetValue(type, out typeDescription))
					return typeDescription;
			}

			var localCache = new TypeCache(Types);
			var newTypeDescription = localCache.GetOrCreateTypeDescription(type);

			lock (Types)
				Types.Merge(localCache);

			TypeConversion.UpdateConversions(localCache.Values);

			return newTypeDescription;
		}

		public override string ToString()
		{
			return this.type.ToString();
		}
	}
}
