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
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Optional companion of <see cref="ITypeResolver" /> which exposes extension methods declared by known types.
	///     A type resolver which doesn't implement it makes no extension method available to expressions.
	/// </summary>
	public interface IExtensionMethodResolver
	{
		/// <summary>
		///     Tries to retrieve extension methods which could accept specified type as their receiver.
		/// </summary>
		/// <param name="targetType">Type of a value the method is invoked on. Not null.</param>
		/// <param name="methodName">Name of the invoked method. Not null.</param>
		/// <param name="extensionMethods">Found methods or null.</param>
		/// <returns>True if at least one method is found. Overwise is false.</returns>
		bool TryGetExtensionMethods(Type targetType, string methodName, out MethodInfo[] extensionMethods);
	}
}
