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
	///     Marker for <see cref="KnownTypeResolver" /> to discover additional types with specified type (on which attribute is
	///     placed).
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ExpressionKnownTypeAttribute : Attribute
	{
		/// <summary>
		///     Additional type to discover.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		///     Creates new <see cref="ExpressionKnownTypeAttribute" /> with specified type.
		/// </summary>
		/// <param name="type">Additional type to discover. Not null. </param>
		public ExpressionKnownTypeAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			this.Type = type;
		}
	}
}
