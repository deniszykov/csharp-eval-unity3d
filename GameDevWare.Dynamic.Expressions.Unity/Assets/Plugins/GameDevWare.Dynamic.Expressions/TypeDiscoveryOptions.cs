using System;

namespace GameDevWare.Dynamic.Expressions
{
	[Flags]
	public enum TypeDiscoveryOptions
	{
		Default = 0,
		Interfaces = 0x1 << 1,
		GenericArguments = 0x1 << 2,
		KnownTypes = 0x1 << 3,
		DeclaringTypes = 0x1 << 4,

		All = Interfaces | GenericArguments | KnownTypes | DeclaringTypes
	}
}
