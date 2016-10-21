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

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public static class ListExtentions
	{
		public static T Dequeue<T>(this List<T> list)
		{
			if (list == null) throw new ArgumentNullException("list");
			if (list.Count == 0) throw new ArgumentException(Properties.Resources.EXCEPTION_LIST_LISTISEMPTY, "list");

			var first = list[0];
			list.RemoveAt(0);
			return first;
		}
		public static T Pop<T>(this List<T> list)
		{
			if (list == null) throw new ArgumentNullException("list");
			if (list.Count == 0) throw new ArgumentException(Properties.Resources.EXCEPTION_LIST_LISTISEMPTY, "list");

			var value = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			return value;
		}
	}
}
