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

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Options of type discovery on specified types.
	/// </summary>
	[Flags]
	public enum TypeDiscoveryOptions
	{
		/// <summary>
		///     Only specified types is known.
		/// </summary>
		Default = 0,
		/// <summary>
		///     All interfaces on specified types are known.
		/// </summary>
		Interfaces = 0x1 << 1,
		/// <summary>
		///     All generic arguments on specified types are known.
		/// </summary>
		GenericArguments = 0x1 << 2,
		/// <summary>
		///     All types from <see cref="ExpressionKnownTypeAttribute" /> on specified types are known.
		/// </summary>
		KnownTypes = 0x1 << 3,
		/// <summary>
		///     All <see cref="Type.DeclaringType" />(recursively) on specified types are known.
		/// </summary>
		DeclaringTypes = 0x1 << 4,

		/// <summary>
		///     All discovery options are active.
		/// </summary>
		All = Interfaces | GenericArguments | KnownTypes | DeclaringTypes
	}
}
