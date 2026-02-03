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
	///     Interface for type resolution services.
	/// </summary>
	public interface ITypeResolver
	{
		/// <summary>
		///     Tries to retrieve type by it's name and generic parameters.
		/// </summary>
		/// <param name="typeReference">Type name. Not null. Not <see cref="TypeReference.Empty" /></param>
		/// <param name="foundType">Found type or null.</param>
		/// <returns>True if type is found. Overwise is false.</returns>
		bool TryGetType(TypeReference typeReference, out Type foundType);
		/// <summary>
		///     Checks if specified type is known by current type resolver;
		/// </summary>
		/// <param name="type">Type to lookup. Not null.</param>
		/// <returns>True if type is known by this resolver. Overwise false.</returns>
		bool IsKnownType(Type type);
	}
}
