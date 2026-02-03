using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Binding;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class ReflectionUtils
	{
		public static bool IsStatic(this MemberInfo memberInfo)
		{
			if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));

			var propertyInfo = memberInfo as PropertyInfo;
			var fieldInfo = memberInfo as FieldInfo;
			var eventInfo = memberInfo as EventInfo;
			var methodInfo = memberInfo as MethodInfo;

			if (propertyInfo != null)
			{
#if NETSTANDARD
				var accessor = propertyInfo.GetMethod ?? propertyInfo.SetMethod;
#else
				var accessor = (propertyInfo.GetGetMethod(nonPublic: true) ?? propertyInfo.GetSetMethod(nonPublic: true));
#endif
				return accessor != null && accessor.IsStatic;
			}

			if (fieldInfo != null) return fieldInfo.IsStatic;

			if (eventInfo != null)
			{
#if NETSTANDARD
				var accessor = eventInfo.AddMethod ?? eventInfo.RemoveMethod;
#else
				var accessor = (eventInfo.GetAddMethod(nonPublic: true) ?? eventInfo.GetRemoveMethod(nonPublic: true));
#endif
				return accessor != null && accessor.IsStatic;
			}

			if (methodInfo != null) return methodInfo.IsStatic;

			return false;
		}
		public static bool IsStatic(this PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));

#if NETSTANDARD
			var accessor = propertyInfo.GetMethod ?? propertyInfo.SetMethod;
#else
			var accessor = (propertyInfo.GetGetMethod(nonPublic: true) ?? propertyInfo.GetSetMethod(nonPublic: true));
#endif
			return accessor != null && accessor.IsStatic;
		}
		public static MethodInfo GetAnyGetter(this PropertyInfo propertyInfo)
		{
#if NETSTANDARD
			var accessor = propertyInfo.GetMethod;
#else
			var accessor = propertyInfo.GetGetMethod(nonPublic: true);
#endif
			return accessor;
		}
		public static MethodInfo GetPublicGetter(this PropertyInfo propertyInfo)
		{
#if NETSTANDARD
			var accessor = propertyInfo.GetMethod;
			if (accessor == null || !accessor.IsPublic)
				return null;
#else
			var accessor = propertyInfo.GetGetMethod(nonPublic: false);
#endif
			return accessor;
		}
		public static MethodInfo GetPublicSetter(this PropertyInfo propertyInfo)
		{
#if NETSTANDARD
			var accessor = propertyInfo.SetMethod;
			if (accessor == null || !accessor.IsPublic)
				return null;
#else
			var accessor = propertyInfo.GetSetMethod(nonPublic: false);
#endif
			return accessor;
		}
		public static MethodInfo GetAnySetter(this PropertyInfo propertyInfo)
		{
#if NETSTANDARD
			var accessor = propertyInfo.SetMethod;
#else
			var accessor = propertyInfo.GetSetMethod(nonPublic: true);
#endif
			return accessor;
		}
		public static bool IsIndexer(this MethodInfo methodInfo)
		{
			if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));

			var type = methodInfo.DeclaringType;
			while (type != null && type != typeof(object))
			{
				var properties = methodInfo.DeclaringType?
#if NETSTANDARD
					.GetTypeInfo()
#endif
					.GetDeclaredProperties() ?? Array.Empty<PropertyInfo>();

				foreach (var property in properties)
				{
					if (property.GetAnyGetter() == methodInfo ||
						property.GetAnySetter() == methodInfo)
						return true;
				}

				type = type
#if NETSTANDARD
					.GetTypeInfo()
#endif
					.BaseType;
			}

			return false;
		}

#if NETSTANDARD
		public static IEnumerable<FieldInfo> GetDeclaredFields(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.DeclaredFields;
		}
		public static IEnumerable<PropertyInfo> GetDeclaredProperties(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.DeclaredProperties;
		}
		public static IEnumerable<ConstructorInfo> GetPublicInstanceConstructors(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic);
		}
		public static IEnumerable<Type> GetImplementedInterfaces(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.ImplementedInterfaces;
		}
		public static IEnumerable<MethodInfo> GetDeclaredMethods(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.DeclaredMethods;
		}
		public static IEnumerable<MethodInfo> GetAllMethods(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			do
			{
				foreach (var method in type.DeclaredMethods)
				{
					yield return method;
				}

				type = type.BaseType == null || type.BaseType == typeof(object) ? null : type.BaseType.GetTypeInfo();
			} while (type != null);
		}
		public static IEnumerable<TypeInfo> GetAllNestedTypes(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			do
			{
				foreach (var nestedType in type.DeclaredNestedTypes)
				{
					yield return nestedType;
				}

				type = type.BaseType == null || type.BaseType == typeof(object) ? null : type.BaseType.GetTypeInfo();
			} while (type != null);
		}
		public static IEnumerable<MemberInfo> GetDeclaredMembers(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.DeclaredMembers;
		}

		public static Type[] GetGenericArguments(this TypeInfo type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GenericTypeArguments;
		}
		public static TypeCode GetTypeCode(Type type)
		{
			if (type == null) return TypeCode.Empty;

			if (type.GetTypeInfo().IsEnum) type = Enum.GetUnderlyingType(type);

			if (type == typeof(bool)) return TypeCode.Boolean;
			if (type == typeof(char)) return TypeCode.Char;
			if (type == typeof(sbyte)) return TypeCode.SByte;
			if (type == typeof(byte)) return TypeCode.Byte;
			if (type == typeof(short)) return TypeCode.Int16;
			if (type == typeof(ushort)) return TypeCode.UInt16;
			if (type == typeof(int)) return TypeCode.Int32;
			if (type == typeof(uint)) return TypeCode.UInt32;
			if (type == typeof(long)) return TypeCode.Int64;
			if (type == typeof(ulong)) return TypeCode.UInt64;
			if (type == typeof(float)) return TypeCode.Single;
			if (type == typeof(double)) return TypeCode.Double;
			if (type == typeof(decimal)) return TypeCode.Decimal;
			if (type == typeof(DateTime)) return TypeCode.DateTime;
			if (type == typeof(string)) return TypeCode.String;

			return TypeCode.Object;
		}
#else
		public static Type GetTypeInfo(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type;
		}
		public static IEnumerable<FieldInfo> GetDeclaredFields(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}
		public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}
		public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}
		public static IEnumerable<MethodInfo> GetAllMethods(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		}
		public static TypeCode GetTypeCode(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return Type.GetTypeCode(type);
		}
		public static IEnumerable<Type> GetAllNestedTypes(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
		}
		public static MethodInfo GetDeclaredMethod(this Type type, string name)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (name == null) throw new ArgumentNullException("name");

			return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
		}
		public static IEnumerable<ConstructorInfo> GetPublicInstanceConstructors(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
		}
		public static IEnumerable<Type> GetImplementedInterfaces(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetInterfaces();
		}
		public static IEnumerable<MemberInfo> GetDeclaredMembers(this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}
#endif
		public static MethodInfo FindConversion(this MemberDescription[] conversionOperators, Type fromType, Type toType)
		{
			if (conversionOperators == null) throw new ArgumentNullException(nameof(conversionOperators));
			if (fromType == null) throw new ArgumentNullException(nameof(fromType));
			if (toType == null) throw new ArgumentNullException(nameof(toType));

			foreach (var conversionOperator in conversionOperators)
			{
				if (conversionOperator.ResultType != toType)
					continue;
				if (conversionOperator.GetParametersCount() != 1 || conversionOperator.GetParameterType(0) != fromType)
					continue;

				return conversionOperator;
			}

			return null;
		}
	}
}
