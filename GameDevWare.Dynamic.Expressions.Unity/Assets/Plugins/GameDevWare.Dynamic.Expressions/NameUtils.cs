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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class NameUtils
	{
		private static readonly string[] EmptyNames = new string[0];

		public static string[] GetTypeNames(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var name = type.Name;
			if (string.IsNullOrEmpty(name))
				return EmptyNames;

			if (type == typeof(Array))
				return new[] { name, name + "`1" };
			else if (type.IsGenericType)
				return new[] { WriteName(type).ToString(), RemoveGenericSuffix(WriteName(type)).ToString() };
			else
				return new[] { WriteName(type).ToString() };
		}
		public static string[] GetTypeFullNames(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var fullName = type.FullName;
			if (string.IsNullOrEmpty(fullName))
				return EmptyNames;

			if (type == typeof(Array))
				return new[] { fullName, fullName + "`1" };
			else if (type.IsGenericType)
				return new[] { WriteFullName(type).ToString(), RemoveGenericSuffix(WriteFullName(type)).ToString() };
			else
				return new[] { WriteFullName(type).ToString() };
		}

		public static StringBuilder WriteFullName(Type type, StringBuilder builder = null, bool writeGenericArguments = false)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (builder == null) builder = new StringBuilder();

			var genericArguments = type.IsGenericType && writeGenericArguments ? type.GetGenericArguments() : Type.EmptyTypes;
			var genericArgumentOffset = 0;
			foreach (var declaringType in new TypeNestingEnumerator(type))
			{
				if (string.IsNullOrEmpty(declaringType.Namespace) == false && !declaringType.IsNested)
				{
					builder.Append(type.Namespace);
					builder.Append('.');
				}

				var genericArgumentsCount = declaringType.IsGenericType && writeGenericArguments ? declaringType.GetGenericArguments().Length - genericArgumentOffset : 0;

				WriteNameInternal(declaringType, new ArraySegment<Type>(genericArguments, genericArgumentOffset, genericArgumentsCount), builder);

				if (declaringType != type)
					builder.Append('.');

				genericArgumentOffset += genericArgumentsCount;
			}

			return builder;
		}
		public static StringBuilder WriteName(Type type, StringBuilder builder = null, bool writeGenericArguments = false)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (builder == null) builder = new StringBuilder();

			var genericArguments = type.IsGenericType && writeGenericArguments ? type.GetGenericArguments() : Type.EmptyTypes;
			var genericArgumentOffset = 0;
			foreach (var declaringType in new TypeNestingEnumerator(type))
			{
				var genericArgumentsCount = declaringType.IsGenericType && writeGenericArguments ? declaringType.GetGenericArguments().Length : 0;

				WriteNameInternal(declaringType, new ArraySegment<Type>(genericArguments, genericArgumentOffset, genericArgumentsCount), builder);

				if (declaringType != type)
					builder.Append('.');

				genericArgumentOffset += genericArgumentsCount;
			}

			return builder;
		}

		public static string RemoveGenericSuffix(string name)
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
				while (markerIndex < name.Length && Char.IsDigit(name[markerIndex]))
					markerIndex++;
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
			if (builder == null) throw new ArgumentNullException("builder");
			if (startIndex < 0 || startIndex > builder.Length) throw new ArgumentOutOfRangeException("startIndex");
			if (count < 0 || startIndex + count > builder.Length) throw new ArgumentOutOfRangeException("count");

			if (count == 0 || startIndex == builder.Length) return builder;

			var endIndex = startIndex + count;
			var markerIndex = builder.IndexOf('`', startIndex, count);
			var cutStartIndex = markerIndex;
			while (markerIndex >= 0)
			{
				markerIndex++;
				while (markerIndex < endIndex && char.IsDigit(builder[markerIndex]))
					markerIndex++;

				var cutLength = markerIndex - cutStartIndex;
				builder.Remove(cutStartIndex, cutLength);

				endIndex -= cutLength;
				markerIndex = builder.IndexOf('`', cutStartIndex, endIndex - cutStartIndex);
				cutStartIndex = markerIndex;
			}

			return builder;
		}

		private static int IndexOf(this StringBuilder builder, char character, int startIndex, int count)
		{
			if (builder == null) throw new ArgumentNullException("builder");
			if (startIndex < 0 || startIndex > builder.Length) throw new ArgumentOutOfRangeException("startIndex");
			if (count < 0 || startIndex + count > builder.Length) throw new ArgumentOutOfRangeException("count");

			if (count == 0 || startIndex == builder.Length) return -1;

			for (int i = startIndex, len = startIndex + count; i < len; i++)
			{
				if (builder[i] == character)
					return i;
			}
			return -1;
		}
		private static Type GetDeclaringType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.DeclaringType;
		}

		private static void WriteNameInternal(Type type, ArraySegment<Type> genericArguments, StringBuilder builder)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (builder == null) throw new ArgumentNullException("builder");

			builder.Append(type.Name);

			if (genericArguments.Count > 0)
			{
				builder.Append("<");
				for (var i = genericArguments.Offset; i < genericArguments.Offset + genericArguments.Count; i++)
				{
					if (!genericArguments.Array[i].IsGenericParameter)
						WriteFullName(genericArguments.Array[i], builder, writeGenericArguments: true);
					builder.Append(',');
				}
				builder.Length--;
				builder.Append(">");
			}
		}

		public struct TypeNestingEnumerator : IEnumerator<Type>, IEnumerable<Type>
		{
			private readonly Type type;
			private Type current;

			public TypeNestingEnumerator(Type type)
			{
				this.type = type;
				this.current = null;
			}

			public bool MoveNext()
			{
				if (this.current == null)
				{
					this.Reset();
					return true;
				}
				else if (this.current == this.type)
				{
					return false;
				}

				var typeAboveCurrent = this.type;
				while (typeAboveCurrent != null && GetDeclaringType(typeAboveCurrent) != this.current)
					typeAboveCurrent = GetDeclaringType(typeAboveCurrent);

				this.current = typeAboveCurrent;
				return typeAboveCurrent != null;
			}
			public void Reset()
			{
				this.current = type;
				while (GetDeclaringType(this.current) != null)
					current = GetDeclaringType(this.current);
			}

			public Type Current { get { return this.current; } }
			object IEnumerator.Current { get { return this.current; } }

			public TypeNestingEnumerator GetEnumerator()
			{
				return this;
			}
			IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
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

	}
}
