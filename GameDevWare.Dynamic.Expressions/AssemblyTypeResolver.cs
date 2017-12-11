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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// <see cref="ITypeResolver"/> which allows any public (<see cref="Type.IsPublic"/>) type from specified <see cref="Assembly"/>(or multiple <see cref="Assembly"/>.
	/// </summary>
	public sealed class AssemblyTypeResolver : KnownTypeResolver
	{
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
		public static readonly AssemblyTypeResolver UnityEngine = new AssemblyTypeResolver(typeof(UnityEngine.Application).Assembly);
#endif

		/// <summary>
		/// Creates new <see cref="AssemblyTypeResolver"/> from list of assemblies.
		/// </summary>
		/// <param name="assemblies">List of assemblies to add as source of known types.</param>
		public AssemblyTypeResolver(params Assembly[] assemblies)
			: this((IEnumerable<Assembly>)assemblies, null)
		{

		}
		/// <summary>
		/// Creates new <see cref="AssemblyTypeResolver"/> from list of assemblies.
		/// </summary>
		/// <param name="assemblies">List of assemblies to add as source of known types.</param>
		public AssemblyTypeResolver(IEnumerable<Assembly> assemblies)
			: this(assemblies, null)
		{
		}
		/// <summary>
		/// Creates new <see cref="AssemblyTypeResolver"/> from list of assemblies.
		/// </summary>
		/// <param name="otherTypeResolver">Backup type resolver used if current can't find a type.</param>
		/// <param name="assemblies">List of assemblies to add as source of known types.</param>
		public AssemblyTypeResolver(IEnumerable<Assembly> assemblies, ITypeResolver otherTypeResolver)
			: base(GetAssembliesPublicTypes(assemblies ?? Enumerable.Empty<Assembly>()), otherTypeResolver)
		{
			if (assemblies == null) throw new ArgumentNullException("assemblies");
		}

#if NETSTANDARD
		private static IEnumerable<Type> GetAssembliesPublicTypes(IEnumerable<Assembly> assemblies)
		{
			if (assemblies == null) throw new ArgumentNullException("assemblies");

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.DefinedTypes)
				{
					if (type.IsPublic)
						yield return type.AsType();
				}
			}
		}
#else
		private static IEnumerable<Type> GetAssembliesPublicTypes(IEnumerable<Assembly> assemblies)
		{
			if (assemblies == null) throw new ArgumentNullException("assemblies");

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes())
				{
					if (type.IsPublic)
						yield return type;
				}
			}
		}
#endif
	}
}
