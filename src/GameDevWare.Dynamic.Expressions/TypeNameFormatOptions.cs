using System;

namespace GameDevWare.Dynamic.Expressions
{
	[Flags]
	internal enum TypeNameFormatOptions
	{
		None,
		// ReSharper disable once ShiftExpressionRealShiftCountIsZero
		IncludeDeclaringType = 0x1 << 0,
		IncludeNamespace = 0x1 << 1 | IncludeDeclaringType,
		IncludeGenericArguments = 0x1 << 2,
		IncludeGenericSuffix = 0x1 << 3,
		UseAliases = 0x1 << 4,

	}
}
