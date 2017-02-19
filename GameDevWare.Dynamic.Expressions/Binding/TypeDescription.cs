using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class TypeDescription : IComparable<TypeDescription>, IComparable, IEquatable<TypeDescription>, IEquatable<Type>
	{
		public static readonly MemberDescription[] EmptyMembers = new MemberDescription[0];
		public static readonly TypeDescription[] EmptyTypes = new TypeDescription[0];

		private readonly Type type;
		private readonly int hashCode;

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
		public readonly Expression DefaultExpression;
		public readonly bool IsNullable;
		public readonly bool IsEnum;
		public readonly bool IsDelegate;
		public readonly TypeDescription BaseType;
		public readonly TypeDescription UnderlyingType;
		public readonly TypeDescription[] BaseTypes;
		public readonly TypeDescription[] Interfaces;
		public readonly TypeDescription[] GenericArguments;

		public TypeDescription(Type type, TypeDescription baseType, TypeDescription underlyingType, TypeDescription[] interfaces, TypeDescription[] genericArguments)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (baseType == null) throw new ArgumentNullException("baseType");
			if (interfaces == null) throw new ArgumentNullException("interfaces");
			if (underlyingType == null) throw new ArgumentNullException("underlyingType");
			if (genericArguments == null) throw new ArgumentNullException("genericArguments");
			if (type == null) throw new ArgumentNullException("type");

			this.hashCode = type.GetHashCode();
			this.type = type;
			this.Name = NameUtils.RemoveGenericSuffix(NameUtils.WriteName(type)).ToString();
			this.BaseType = baseType;
			this.UnderlyingType = underlyingType;
			this.BaseTypes = GetBaseTypes(this, 0);
			this.Interfaces = interfaces;
			this.GenericArguments = genericArguments;
			this.MembersByName = GetMembersByName(ref this.Indexers);
			this.IsNullable = Nullable.GetUnderlyingType(type) != null;
			this.IsEnum = type.IsEnum;
			this.IsDelegate = typeof(Delegate).IsAssignableFrom(type) && type != typeof(Delegate) && type != typeof(MulticastDelegate);
			this.DefaultExpression = Expression.Constant(type.IsValueType && this.IsNullable == false ? Activator.CreateInstance(type) : null);

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			var methodsDescriptions = new MemberDescription[methods.Length];

			// ReSharper disable LocalizableElement
			this.ImplicitConvertTo = GetOperators(methods, methodsDescriptions, "op_Implicit", 0);
			this.ImplicitConvertFrom = GetOperators(methods, methodsDescriptions, "op_Implicit", -1);
			this.ExplicitConvertTo = GetOperators(methods, methodsDescriptions, "op_Explicit", 0);
			this.ExplicitConvertFrom = GetOperators(methods, methodsDescriptions, "op_Explicit", -1);
			this.Addition = GetOperators(methods, methodsDescriptions, "op_Addition");
			this.Division = GetOperators(methods, methodsDescriptions, "op_Division");
			this.Equality = GetOperators(methods, methodsDescriptions, "op_Equality");
			this.GreaterThan = GetOperators(methods, methodsDescriptions, "op_GreaterThan");
			this.GreaterThanOrEqual = GetOperators(methods, methodsDescriptions, "op_GreaterThanOrEqual");
			this.Inequality = GetOperators(methods, methodsDescriptions, "op_Inequality");
			this.LessThan = GetOperators(methods, methodsDescriptions, "op_LessThan");
			this.LessThanOrEqual = GetOperators(methods, methodsDescriptions, "op_LessThanOrEqual");
			this.Modulus = GetOperators(methods, methodsDescriptions, "op_Modulus");
			this.Multiply = GetOperators(methods, methodsDescriptions, "op_Multiply");
			this.Subtraction = GetOperators(methods, methodsDescriptions, "op_Subtraction");
			this.UnaryNegation = GetOperators(methods, methodsDescriptions, "op_UnaryNegation");
			this.UnaryPlus = GetOperators(methods, methodsDescriptions, "op_UnaryPlus");
			this.BitwiseAnd = GetOperators(methods, methodsDescriptions, "op_BitwiseAnd");
			this.BitwiseOr = GetOperators(methods, methodsDescriptions, "op_BitwiseOr");
			// ReSharper restore LocalizableElement
			this.Conversions = Combine(this.ImplicitConvertTo, this.ImplicitConvertFrom, this.ExplicitConvertTo, this.ExplicitConvertFrom);
			this.Constructors = Array.ConvertAll(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance), ctr => new MemberDescription(this, ctr));
		}

		private MemberDescription[] GetOperators(MethodInfo[] methods, MemberDescription[] methodsDescriptions, string operatorName, int? compareParameterIndex = null)
		{
			if (methods == null) throw new ArgumentNullException("methods");
			if (methodsDescriptions == null) throw new ArgumentNullException("methodsDescriptions");
			if (operatorName == null) throw new ArgumentNullException("operatorName");

			var operators = default(List<MemberDescription>);
			for (var i = 0; i < methods.Length; i++)
			{
				var method = methods[i];
				if (method.Name != operatorName) continue;

				if (methodsDescriptions[i] == null)
					methodsDescriptions[i] = new MemberDescription(this, method);

				var methodDescription = methodsDescriptions[i];
				if (compareParameterIndex.HasValue && methodDescription.GetParameter(compareParameterIndex.Value).ParameterType != this.type) continue;

				if (operators == null) operators = new List<MemberDescription>();
				operators.Add(new MemberDescription(this, method));
			}

			return operators != null ? operators.ToArray() : EmptyMembers;
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
					var indexParameters = property.GetIndexParameters();
					if (indexParameters.Length == 0)
						memberDescription = new MemberDescription(this, property);
					else if (indexers == null)
						indexers = new[] { new MemberDescription(this, property) };
					else
						Add(ref indexers, new MemberDescription(this, property));
				}
				else if (field != null)
					memberDescription = new MemberDescription(this, field);
				else if (method != null && method.IsSpecialName == false)
					memberDescription = new MemberDescription(this, method);

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

			var membersByName = new Dictionary<string, MemberDescription[]>(memberSetsByName.Count);
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
			var declaredMembers = new List<MemberInfo>(type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
			if (type.IsInterface)
			{
				foreach (var @interface in type.GetInterfaces())
					declaredMembers.AddRange(@interface.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
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
			if (array == null) throw new ArgumentNullException("array");
			if (element == null) throw new ArgumentNullException("element");

			Array.Resize(ref array, array.Length + 1);
			array[array.Length - 1] = element;
		}
		private T[] Combine<T>(params T[][] arrays)
		{
			var length = 0;
			for (var i = 0; i < arrays.Length; i++)
				length += arrays[i].Length;
			if (arrays.Length == 0)
				return arrays[0];

			var newArray = new T[length];
			var offset = 0;
			for (var i = 0; i < arrays.Length; i++)
			{
				arrays[i].CopyTo(newArray, offset);
				offset = arrays[i].Length;
			}
			return newArray;
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
			if (type1 == null || type2 == null) return false;

			return type1.Equals(type2);
		}
		public static bool operator !=(TypeDescription type1, TypeDescription type2)
		{
			return !(type1 == type2);
		}
		public static bool operator ==(TypeDescription type1, Type type2)
		{
			if (type1 != null && ReferenceEquals(type1.type, type2)) return true;
			if (type1 == null || type2 == null) return false;

			return type1.type == type2;
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

		public override string ToString()
		{
			return this.type.ToString();
		}

	}
}
