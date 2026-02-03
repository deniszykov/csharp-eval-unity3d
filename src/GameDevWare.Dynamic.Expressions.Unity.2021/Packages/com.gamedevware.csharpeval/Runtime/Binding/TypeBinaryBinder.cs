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
using System.Diagnostics;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class TypeBinaryBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var expressionType = node.GetExpressionType(true);
			var targetNode = node.GetExpression(true);
			var typeName = node.GetTypeName(true);
			if (!bindingContext.TryResolveType(typeName, out var type))
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			if (!AnyBinder.TryBindInNewScope(targetNode, bindingContext, TypeDescription.ObjectType, out var target, out bindingError))
			{
				return false;
			}

			Debug.Assert(target != null, "target != null");

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_TYPE_IS:
					boundExpression = Expression.TypeIs(target, type);
					break;
				case Constants.EXPRESSION_TYPE_TYPE_AS:
					boundExpression = Expression.TypeAs(target, type);
					break;
				case Constants.EXPRESSION_TYPE_CONVERT:
					boundExpression = Expression.Convert(target, type);
					break;
				case Constants.EXPRESSION_TYPE_CONVERT_CHECKED:
					boundExpression = Expression.ConvertChecked(target, type);
					break;
				default:
					boundExpression = null;
					bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
					return false;
			}

			return true;
		}
	}
}
