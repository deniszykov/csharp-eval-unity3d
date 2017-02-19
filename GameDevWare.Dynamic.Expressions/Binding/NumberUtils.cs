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

			return IsNumber(Type.GetTypeCode(type));
		}
		public static bool IsNumber(TypeCode type)
		{
			return Array.BinarySearch(NumberTypes, (int) type) >= 0;
		}

		public static bool IsSignedInteger(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return IsSignedInteger(Type.GetTypeCode(type));
		}
		public static bool IsSignedInteger(TypeCode type)
		{
			return Array.BinarySearch(SignedIntegerTypes, (int)type) >= 0;
		}

		public static bool IsUnsignedInteger(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return IsUnsignedInteger(Type.GetTypeCode(type));
		}
		public static bool IsUnsignedInteger(TypeCode type)
		{
			return Array.BinarySearch(UnsignedIntegerTypes, (int)type) >= 0;
		}
	}
}
