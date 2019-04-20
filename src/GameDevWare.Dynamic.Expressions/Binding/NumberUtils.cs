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
namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class NumberUtils
	{
		private static readonly int[] SignedIntegerTypes;
		private static readonly int[] UnsignedIntegerTypes;
		private static readonly int[] NumberTypes;

		static NumberUtils()
		{
			SignedIntegerTypes = new[] { (int)TypeCode.SByte, (int)TypeCode.Int16, (int)TypeCode.Int32, (int)TypeCode.Int64 };
			UnsignedIntegerTypes = new[] { (int)TypeCode.Byte, (int)TypeCode.UInt16, (int)TypeCode.UInt32, (int)TypeCode.UInt64 };
			NumberTypes = new[]
			{
				(int)TypeCode.SByte, (int)TypeCode.Int16, (int)TypeCode.Int32, (int)TypeCode.Int64,
				(int)TypeCode.Byte, (int)TypeCode.UInt16, (int)TypeCode.UInt32, (int)TypeCode.UInt64,
				(int)TypeCode.Single, (int)TypeCode.Double, (int)TypeCode.Decimal,
			};
			Array.Sort(NumberTypes);
			Array.Sort(SignedIntegerTypes);
			Array.Sort(UnsignedIntegerTypes);
		}

		public static bool IsNumber(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return IsNumber(ReflectionUtils.GetTypeCode(type));
		}
		public static bool IsNumber(TypeCode type)
		{
			return Array.BinarySearch(NumberTypes, (int) type) >= 0;
		}

		public static bool IsSignedInteger(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return IsSignedInteger(ReflectionUtils.GetTypeCode(type));
		}
		public static bool IsSignedInteger(TypeCode type)
		{
			return Array.BinarySearch(SignedIntegerTypes, (int)type) >= 0;
		}

		public static bool IsUnsignedInteger(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return IsUnsignedInteger(ReflectionUtils.GetTypeCode(type));
		}
		public static bool IsUnsignedInteger(TypeCode type)
		{
			return Array.BinarySearch(UnsignedIntegerTypes, (int)type) >= 0;
		}
	}
}
