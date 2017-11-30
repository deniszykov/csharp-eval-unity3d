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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class TypeBinaryBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var expressionType = node.GetExpressionType(throwOnError: true);
			var targetNode = node.GetExpression(throwOnError: true);
			var typeName = node.GetTypeName(throwOnError: true);
			var type = default(Type);
			if (bindingContext.TryResolveType(typeName, out type) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
				return false;
			}

			var target = default(Expression);
			if (AnyBinder.TryBindInNewScope(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError) == false)
				return false;

			Debug.Assert(target != null, "target != null");

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_TYPEIS:
					boundExpression = Expression.TypeIs(target, type);
					break;
				case Constants.EXPRESSION_TYPE_TYPEAS:
					boundExpression = Expression.TypeAs(target, type);
					break;
				case Constants.EXPRESSION_TYPE_CONVERT:
					boundExpression = Expression.Convert(target, type);
					break;
				case Constants.EXPRESSION_TYPE_CONVERTCHECKED:
					boundExpression = Expression.ConvertChecked(target, type);
					break;
				default:
					boundExpression = null;
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
					return false;
			}
			return true;
		}
	}
}
