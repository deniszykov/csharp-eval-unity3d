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
	internal static class UnaryBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var expressionType = node.GetExpressionType(throwOnError: true);
			var operandNode = node.GetExpression(throwOnError: true);
			var operand = default(Expression);
			var methodName = node.GetMethodName(throwOnError: false);
			var methodMember = default(MemberDescription);

			if (AnyBinder.TryBindInNewScope(operandNode, bindingContext, TypeDescription.ObjectType, out operand, out bindingError) == false)
				return false;

			if (methodName != null)
			{
				bindingContext.TryResolveMember(methodName, out methodMember);
			}

			Debug.Assert(operand != null, "operand != null");

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_NEGATE:
					if (ExpressionUtils.TryPromoteUnaryOperation(ref operand, ExpressionType.Negate, out boundExpression) == false)
					{
						// fixing b_u_g in mono expression compiler: Negate on float or double = exception
						if (operand.Type == typeof(double) || operand.Type == typeof(float))
							boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? ExpressionUtils.NegativeSingle : ExpressionUtils.NegativeDouble);
						else
							boundExpression = Expression.Negate(operand, methodMember);
					}
					break;
				case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					if (ExpressionUtils.TryPromoteUnaryOperation(ref operand, ExpressionType.NegateChecked, out boundExpression) == false)
					{
						// fixing b_u_g in mono expression compiler: Negate on float or double = exception
						if (operand.Type == typeof(double) || operand.Type == typeof(float))
							boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? ExpressionUtils.NegativeSingle : ExpressionUtils.NegativeDouble);
						else
							boundExpression = Expression.NegateChecked(operand, methodMember);
					}
					break;
				case Constants.EXPRESSION_TYPE_COMPLEMENT:
				case Constants.EXPRESSION_TYPE_NOT:
					if (ExpressionUtils.TryPromoteUnaryOperation(ref operand, ExpressionType.Not, out boundExpression) == false)
						boundExpression = Expression.Not(operand, methodMember);
					break;
				case Constants.EXPRESSION_TYPE_UNARY_PLUS:
					if (ExpressionUtils.TryPromoteUnaryOperation(ref operand, ExpressionType.UnaryPlus, out boundExpression) == false)
						boundExpression = Expression.UnaryPlus(operand, methodMember);
					break;
				case Constants.EXPRESSION_TYPE_ARRAY_LENGTH:
					boundExpression = Expression.ArrayLength(operand);
					break;
				default:
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
					return false;
			}
			return true;
		}
	}
}
