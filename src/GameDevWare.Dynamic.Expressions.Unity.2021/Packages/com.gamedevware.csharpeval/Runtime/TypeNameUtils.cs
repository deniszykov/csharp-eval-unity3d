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

#if NETFRAMEWORK
using TypeInfo = System.Type;
#else
using System.Reflection;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GameDevWare.Dynamic.Expressions.CSharp;

// ReSharper disable CheckForReferenceEqualityInstead.1

namespace GameDevWare.Dynamic.Expressions
{
	internal static class TypeNameUtils
	{
		public struct TypeNestingEnumerator : IEnumerator<TypeInfo>, IEnumerable<TypeInfo>
		{
			private readonly TypeInfo typeInfo;

			public TypeNestingEnumerator(TypeInfo typeInfo)
			{
				this.typeInfo = typeInfo;
				this.Current = null;
			}

			public bool MoveNext()
			{
				if (this.Current == null)
				{
					this.Reset();
					return true;
				}

				if (this.Current.Equals(this.typeInfo)) return false;

				var typeAboveCurrent = this.typeInfo;
				while (typeAboveCurrent != null && !this.Current.Equals(GetDeclaringType(typeAboveCurrent)))
				{
					typeAboveCurrent = GetDeclaringType(typeAboveCurrent);
				}

				this.Current = typeAboveCurrent;
				return typeAboveCurrent != null;
			}
			public void Reset()
			{
				this.Current = this.typeInfo;
				while (GetDeclaringType(this.Current) != null)
				{
					this.Current = GetDeclaringType(this.Current);
				}
			}

			private static TypeInfo GetDeclaringType(TypeInfo type)
			{
				if (type == null) throw new ArgumentNullException(nameof(type));

				var declaringType = type.DeclaringType;

				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (declaringType == null) return null;

				return declaringType.GetTypeInfo();
			}

			public TypeInfo Current { get; private set; }
			object IEnumerator.Current => this.Current;

			public TypeNestingEnumerator GetEnumerator()
			{
				return this;
			}
			IEnumerator<TypeInfo> IEnumerable<TypeInfo>.GetEnumerator()
			{
				return this;
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this;
			}

			public void Dispose()
			{
			}
		}

		private static readonly string[] EmptyNames = ArrayUtils.Empty<string>();

		public static string[] GetTypeNames(this TypeInfo typeInfo)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			var name = typeInfo.Name;
			if (string.IsNullOrEmpty(name))
				return EmptyNames;

			if (typeInfo.Equals(typeof(Array)))
				return new[] { name, name + "`1" };

			if (typeInfo.IsGenericType)
			{
				return new[] {
					typeInfo.GetCSharpName(options: TypeNameFormatOptions.None).ToString(),
					typeInfo.GetCSharpName(options: TypeNameFormatOptions.IncludeGenericSuffix).ToString()
				};
			}

			if (CSharpTypeNameAlias.TryGetAlias(typeInfo, out var alias))
				return new[] { alias, typeInfo.GetCSharpName().ToString() };

			return new[] { typeInfo.GetCSharpName().ToString() };
		}
		public static string[] GetTypeFullNames(this TypeInfo typeInfo)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			var fullName = typeInfo.FullName;
			if (string.IsNullOrEmpty(fullName))
				return EmptyNames;

			if (typeInfo.Equals(typeof(Array)))
				return new[] { fullName, fullName + "`1" };

			if (typeInfo.IsGenericType)
			{
				return new[] {
					typeInfo.GetCSharpFullName(options: TypeNameFormatOptions.None).ToString(),
					typeInfo.GetCSharpFullName(options: TypeNameFormatOptions.IncludeGenericSuffix).ToString()
				};
			}

			return new[] { typeInfo.GetCSharpFullName().ToString() };
		}

		public static StringBuilder GetCSharpFullName
			(this TypeInfo typeInfo, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			if (builder == null) builder = new StringBuilder();

			var nameStartIndex = builder.Length;
			WriteName(typeInfo, builder, options | TypeNameFormatOptions.IncludeNamespace);

			if ((options & TypeNameFormatOptions.IncludeGenericSuffix) == 0) RemoveGenericSuffix(builder, nameStartIndex, builder.Length - nameStartIndex);

			return builder;
		}
		public static StringBuilder GetCSharpName
			(this TypeInfo typeInfo, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			if (builder == null) builder = new StringBuilder();

			var nameStartIndex = builder.Length;
			WriteName(typeInfo, builder, (options & ~TypeNameFormatOptions.IncludeNamespace) | TypeNameFormatOptions.IncludeDeclaringType);

			if ((options & TypeNameFormatOptions.IncludeGenericSuffix) == 0) RemoveGenericSuffix(builder, nameStartIndex, builder.Length - nameStartIndex);

			return builder;
		}
		public static StringBuilder GetCSharpNameOnly
			(this TypeInfo typeInfo, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			if (builder == null) builder = new StringBuilder();

			var nameStartIndex = builder.Length;
			WriteName(typeInfo, builder, options & ~TypeNameFormatOptions.IncludeNamespace);

			if ((options & TypeNameFormatOptions.IncludeGenericSuffix) == 0) RemoveGenericSuffix(builder, nameStartIndex, builder.Length - nameStartIndex);

			return builder;
		}

		public static string RemoveGenericSuffix(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			var markerIndex = name.IndexOf('`');
			var offset = 0;
			if (markerIndex < 0) return name;

			var builder = new StringBuilder(name.Length);

			while (markerIndex >= 0)
			{
				builder.Append(name, offset, markerIndex - offset);
				markerIndex++;
				while (markerIndex < name.Length && char.IsDigit(name[markerIndex]))
				{
					markerIndex++;
				}

				offset = markerIndex;
				markerIndex = name.IndexOf('`', offset);
			}

			return builder.ToString();
		}
		public static StringBuilder RemoveGenericSuffix(StringBuilder builder)
		{
			return RemoveGenericSuffix(builder, 0, builder.Length);
		}
		public static StringBuilder RemoveGenericSuffix(StringBuilder builder, int startIndex, int count)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (startIndex < 0 || startIndex > builder.Length) throw new ArgumentOutOfRangeException(nameof(startIndex));
			if (count < 0 || startIndex + count > builder.Length) throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0 || startIndex == builder.Length) return builder;

			var endIndex = startIndex + count;
			var markerIndex = builder.IndexOf('`', startIndex, count);
			var cutStartIndex = markerIndex;
			while (markerIndex >= 0)
			{
				markerIndex++;
				while (markerIndex < endIndex && char.IsDigit(builder[markerIndex]))
				{
					markerIndex++;
				}

				var cutLength = markerIndex - cutStartIndex;
				builder.Remove(cutStartIndex, cutLength);

				endIndex -= cutLength;
				markerIndex = builder.IndexOf('`', cutStartIndex, endIndex - cutStartIndex);
				cutStartIndex = markerIndex;
			}

			return builder;
		}

		public static TypeNestingEnumerator GetDeclaringTypes(this TypeInfo typeInfo)
		{
			return new TypeNestingEnumerator(typeInfo);
		}

		private static void WriteName(TypeInfo typeInfo, StringBuilder builder, TypeNameFormatOptions options)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if ((options & TypeNameFormatOptions.UseAliases) == TypeNameFormatOptions.UseAliases &&
				CSharpTypeNameAlias.TryGetAlias(typeInfo, out var alias))
			{
				builder.Append(alias);
				return;
			}

			var arrayDepth = 0;
			while (typeInfo.IsArray)
			{
				var elementType = typeInfo.GetElementType();
				Debug.Assert(elementType != null, nameof(elementType) + " != null");
				typeInfo = elementType.GetTypeInfo();
				arrayDepth++;
			}

			var writeGenericArguments = (options & TypeNameFormatOptions.IncludeGenericArguments) == TypeNameFormatOptions.IncludeGenericArguments;
			var namespaceWritten = (options & TypeNameFormatOptions.IncludeNamespace) != TypeNameFormatOptions.IncludeNamespace;
			var writeDeclaringType = (options & TypeNameFormatOptions.IncludeDeclaringType) == TypeNameFormatOptions.IncludeDeclaringType;

			var genericArguments =
				(typeInfo.IsGenericType && writeGenericArguments ? typeInfo.GetGenericArguments() : Type.EmptyTypes).ConvertAll(t => t.GetTypeInfo());
			var genericArgumentOffset = 0;
			foreach (var declaringTypeInfo in new TypeNestingEnumerator(typeInfo))
			{
				if (!namespaceWritten)
				{
					var typeNamespace = declaringTypeInfo.Namespace;
					builder.Append(typeNamespace);
					if (!string.IsNullOrEmpty(typeNamespace))
						builder.Append('.');
					namespaceWritten = true;
				}

				var genericArgumentsCount = (declaringTypeInfo.IsGenericType && writeGenericArguments ? declaringTypeInfo.GetGenericArguments().Length : 0) -
					genericArgumentOffset;
				var partialGenerics = new ArraySegment<TypeInfo>(genericArguments, genericArgumentOffset, genericArgumentsCount);

				if (writeDeclaringType || declaringTypeInfo.Equals(typeInfo))
				{
					WriteNamePart(declaringTypeInfo, builder, partialGenerics, options);

					if (!declaringTypeInfo.Equals(typeInfo))
						builder.Append('.');
				}

				genericArgumentOffset += genericArgumentsCount;
			}

			for (var d = 0; d < arrayDepth; d++) builder.Append("[]");
		}
		private static void WriteNamePart(TypeInfo type, StringBuilder builder, ArraySegment<TypeInfo> genericArguments, TypeNameFormatOptions options)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.Append(type.Name);

			if (genericArguments.Count > 0)
			{
				builder.Append('<');
				for (var i = genericArguments.Offset; i < genericArguments.Offset + genericArguments.Count; i++)
				{
					// ReSharper disable once PossibleNullReferenceException
					if (!genericArguments.Array[i].IsGenericParameter) WriteName(genericArguments.Array[i], builder, options);

					builder.Append(',');
				}

				builder.Length--;
				builder.Append('>');
			}
		}
		private static int IndexOf(this StringBuilder builder, char character, int startIndex, int count)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (startIndex < 0 || startIndex > builder.Length) throw new ArgumentOutOfRangeException(nameof(startIndex));
			if (count < 0 || startIndex + count > builder.Length) throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0 || startIndex == builder.Length) return -1;

			for (int i = startIndex, len = startIndex + count; i < len; i++)
			{
				if (builder[i] == character)
					return i;
			}

			return -1;
		}

#if !NETFRAMEWORK
		public static string[] GetTypeFullNames(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetTypeInfo().GetTypeFullNames();
		}
		public static string[] GetTypeNames(this Type typeInfo)
		{
			if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

			return typeInfo.GetTypeInfo().GetTypeNames();
		}
		public static StringBuilder GetCSharpNameOnly
			(this Type type, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetTypeInfo().GetCSharpNameOnly(builder, options);
		}
		public static StringBuilder GetCSharpName
			(this Type type, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetTypeInfo().GetCSharpName(builder, options);
		}
		public static TypeNestingEnumerator GetDeclaringTypes(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetTypeInfo().GetDeclaringTypes();
		}
		public static StringBuilder GetCSharpFullName
			(this Type type, StringBuilder builder = null, TypeNameFormatOptions options = TypeNameFormatOptions.IncludeGenericSuffix)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return type.GetTypeInfo().GetCSharpFullName(builder, options);
		}
#endif
	}
}
