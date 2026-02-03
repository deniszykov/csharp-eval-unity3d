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
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class ConstantBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var valueObj = node.GetValue(true);
			var typeName = node.GetTypeName(true);
			if (!bindingContext.TryResolveType(typeName, out var type))
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var typeDescription = TypeDescription.GetTypeDescription(type);
			if (valueObj == null && (typeDescription.IsNullable || !typeDescription.IsValueType))
				boundExpression = Expression.Constant(null, type);
			else
			{
				var value = ChangeType(valueObj, type);
				boundExpression = Expression.Constant(value, type);
			}

			return true;
		}

		public static object ChangeType(object value, Type toType)
		{
			if (toType == null) throw new ArgumentNullException(nameof(toType));

			if (toType.GetTypeInfo().IsEnum)
				return Enum.Parse(toType, Convert.ToString(value, Constants.DefaultFormatProvider));

			return Convert.ChangeType(value, toType, Constants.DefaultFormatProvider);
		}
	}
}
