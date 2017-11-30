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
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class BinaryBinder
	{
		private static readonly Func<object, object, string> StringConcat = string.Concat;

		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var expressionType = node.GetExpressionType(throwOnError: true);
			var left = node.GetLeftExpression(throwOnError: true);
			var right = node.GetRightExpression(throwOnError: true);
			var leftOperand = default(Expression);
			var rightOperand = default(Expression);

			if (AnyBinder.TryBindInNewScope(left, bindingContext, TypeDescription.ObjectType, out leftOperand, out bindingError) == false)
				return false;
			if (AnyBinder.TryBindInNewScope(right, bindingContext, TypeDescription.ObjectType, out rightOperand, out bindingError) == false)
				return false;

			Debug.Assert(leftOperand != null, "leftOperand != null");
			Debug.Assert(rightOperand != null, "rightOperand != null");

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_ADD:
					if (leftOperand.Type == typeof(string) || rightOperand.Type == typeof(string))
					{
						boundExpression = Expression.Call
						(
							StringConcat.GetMethodInfo(),
							Expression.Convert(leftOperand, typeof(object)),
							Expression.Convert(rightOperand, typeof(object))
						);
						break;
					}
					else
					{
						if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Add, out boundExpression) == false)
							boundExpression = Expression.Add(leftOperand, rightOperand);
						break;
					}
				case Constants.EXPRESSION_TYPE_ADD_CHECKED:
					if (leftOperand.Type == typeof(string) || rightOperand.Type == typeof(string))
					{
						boundExpression = Expression.Call
						(
							StringConcat.GetMethodInfo(),
							Expression.Convert(leftOperand, typeof(object)),
							Expression.Convert(rightOperand, typeof(object))
						);
						break;
					}
					else
					{
						if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.AddChecked, out boundExpression) == false)
							boundExpression = Expression.AddChecked(leftOperand, rightOperand);
						break;
					}
				case Constants.EXPRESSION_TYPE_SUBTRACT:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Subtract, out boundExpression) == false)
						boundExpression = Expression.Subtract(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.SubtractChecked, out boundExpression) == false)
						boundExpression = Expression.SubtractChecked(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LEFTSHIFT:
					ExpressionUtils.TryPromoteUnaryOperation(ref leftOperand, ExpressionType.LeftShift, out boundExpression);
					ExpressionUtils.TryPromoteUnaryOperation(ref rightOperand, ExpressionType.LeftShift, out boundExpression);
					boundExpression = Expression.LeftShift(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_RIGHTSHIFT:
					ExpressionUtils.TryPromoteUnaryOperation(ref leftOperand, ExpressionType.RightShift, out boundExpression);
					ExpressionUtils.TryPromoteUnaryOperation(ref rightOperand, ExpressionType.RightShift, out boundExpression);
					boundExpression = Expression.RightShift(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_GREATERTHAN:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.GreaterThan, out boundExpression) == false)
						boundExpression = Expression.GreaterThan(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_GREATERTHAN_OR_EQUAL:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.GreaterThanOrEqual, out boundExpression) == false)
						boundExpression = Expression.GreaterThanOrEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LESSTHAN:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.LessThan, out boundExpression) == false)
						boundExpression = Expression.LessThan(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LESSTHAN_OR_EQUAL:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.LessThanOrEqual, out boundExpression) == false)
						boundExpression = Expression.LessThanOrEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_POWER:
					var resultType = TypeDescription.GetTypeDescription(leftOperand.Type);
					var resultTypeUnwrap = resultType.IsNullable ? resultType.UnderlyingType : resultType;
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Power, out boundExpression) == false)
					{
						var operandsType = TypeDescription.GetTypeDescription(leftOperand.Type);
						var operandTypeUnwrap = operandsType.IsNullable ? operandsType.UnderlyingType : operandsType;
						var promoteToNullable = resultType.IsNullable || operandsType.IsNullable;
						if (operandTypeUnwrap != typeof(double) && leftOperand.Type == rightOperand.Type)
						{
							leftOperand = Expression.ConvertChecked(leftOperand, promoteToNullable ? typeof(double?) : typeof(double));
							rightOperand = Expression.ConvertChecked(rightOperand, promoteToNullable ? typeof(double?) : typeof(double));
						}
						boundExpression = Expression.Power(leftOperand, rightOperand);

						if (resultType != typeof(double))
							boundExpression = Expression.ConvertChecked(boundExpression, promoteToNullable ? resultTypeUnwrap.GetNullableType() : resultTypeUnwrap);
					}
					break;
				case Constants.EXPRESSION_TYPE_DIVIDE:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Divide, out boundExpression) == false)
						boundExpression = Expression.Divide(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MULTIPLY:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Multiply, out boundExpression) == false)
						boundExpression = Expression.Multiply(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.MultiplyChecked, out boundExpression) == false)
						boundExpression = Expression.MultiplyChecked(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MODULO:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Modulo, out boundExpression) == false)
						boundExpression = Expression.Modulo(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_EQUAL:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Equal, out boundExpression) == false)
						boundExpression = Expression.Equal(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_NOTEQUAL:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.NotEqual, out boundExpression) == false)
						boundExpression = Expression.NotEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_AND:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.And, out boundExpression) == false)
						boundExpression = Expression.And(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_OR:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Or, out boundExpression) == false)
						boundExpression = Expression.Or(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_EXCLUSIVEOR:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.ExclusiveOr, out boundExpression) == false)
						boundExpression = Expression.ExclusiveOr(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_ANDALSO:
					boundExpression = Expression.AndAlso(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_ORELSE:
					boundExpression = Expression.OrElse(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_COALESCE:
					if (ExpressionUtils.TryPromoteBinaryOperation(ref leftOperand, ref rightOperand, ExpressionType.Coalesce, out boundExpression) == false)
						boundExpression = Expression.Coalesce(leftOperand, rightOperand);

					break;

				default:
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
					return false;
			}
			return true;
		}
	}
}
