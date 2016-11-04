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
	public sealed class AssemblyTypeResolutionService : KnownTypeResolutionService
	{
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
		public static readonly AssemblyTypeResolutionService UnityEngine = new AssemblyTypeResolutionService(typeof(UnityEngine.Application).Assembly);
#endif

		public AssemblyTypeResolutionService(IEnumerable<Assembly> assemblies)
			: base((assemblies ?? Enumerable.Empty<Assembly>()).SelectMany(a => a.GetTypes()).Where(t => t.IsPublic))
		{
			if (assemblies == null) throw new ArgumentNullException("assemblies");
		}

		public AssemblyTypeResolutionService(params Assembly[] assemblies)
			: this((IEnumerable<Assembly>)assemblies)
		{

		}
	}
}
