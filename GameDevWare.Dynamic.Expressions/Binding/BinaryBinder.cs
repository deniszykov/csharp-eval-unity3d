using System;
using System.Linq.Expressions;

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

			if (AnyBinder.TryBind(left, bindingContext, TypeDescription.ObjectType, out leftOperand, out bindingError) == false)
				return false;
			if (AnyBinder.TryBind(right, bindingContext, TypeDescription.ObjectType, out rightOperand, out bindingError) == false)
				return false;

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_ADD:
					if (leftOperand.Type == typeof(string) || rightOperand.Type == typeof(string))
					{
						boundExpression = Expression.Call
						(
							StringConcat.Method,
							Expression.Convert(leftOperand, typeof(object)),
							Expression.Convert(rightOperand, typeof(object))
						);
						break;
					}
					else
					{
						ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Add);
						boundExpression = Expression.Add(leftOperand, rightOperand);
						break;
					}
				case Constants.EXPRESSION_TYPE_ADD_CHECKED:
					if (leftOperand.Type == typeof(string) || rightOperand.Type == typeof(string))
					{
						boundExpression = Expression.Call
						(
							StringConcat.Method,
							Expression.Convert(leftOperand, typeof(object)),
							Expression.Convert(rightOperand, typeof(object))
						);
						break;
					}
					else
					{
						ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.AddChecked);
						boundExpression = Expression.AddChecked(leftOperand, rightOperand);
						break;
					}
				case Constants.EXPRESSION_TYPE_SUBTRACT:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Subtract);
					boundExpression = Expression.Subtract(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.SubtractChecked);
					boundExpression = Expression.SubtractChecked(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LEFTSHIFT:
					ExpressionUtils.PromoteOperand(ref leftOperand, ExpressionType.LeftShift);
					boundExpression = Expression.LeftShift(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_RIGHTSHIFT:
					ExpressionUtils.PromoteOperand(ref leftOperand, ExpressionType.RightShift);
					boundExpression = Expression.RightShift(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_GREATERTHAN:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.GreaterThan);
					boundExpression = Expression.GreaterThan(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_GREATERTHAN_OR_EQUAL:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.GreaterThanOrEqual);
					boundExpression = Expression.GreaterThanOrEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LESSTHAN:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.LessThan);
					boundExpression = Expression.LessThan(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_LESSTHAN_OR_EQUAL:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.LessThanOrEqual);
					boundExpression = Expression.LessThanOrEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_POWER:
					var leftType = leftOperand.Type;
					if (leftOperand.Type != typeof(double))
						leftOperand = Expression.ConvertChecked(leftOperand, typeof(double));
					if (rightOperand.Type != typeof(double))
						rightOperand = Expression.ConvertChecked(rightOperand, typeof(double));

					boundExpression = Expression.Power(leftOperand, rightOperand);

					if (boundExpression.Type != leftType)
						boundExpression = Expression.ConvertChecked(boundExpression, leftType);
					break;
				case Constants.EXPRESSION_TYPE_DIVIDE:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Divide);
					boundExpression = Expression.Divide(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MULTIPLY:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Multiply);
					boundExpression = Expression.Multiply(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.MultiplyChecked);
					boundExpression = Expression.MultiplyChecked(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_MODULO:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Modulo);
					boundExpression = Expression.Modulo(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_EQUAL:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Equal);
					boundExpression = Expression.Equal(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_NOTEQUAL:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.NotEqual);
					boundExpression = Expression.NotEqual(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_AND:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.And);
					boundExpression = Expression.And(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_OR:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Or);
					boundExpression = Expression.Or(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_EXCLUSIVEOR:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.ExclusiveOr);
					boundExpression = Expression.ExclusiveOr(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_ANDALSO:
					boundExpression = Expression.AndAlso(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_ORELSE:
					boundExpression = Expression.OrElse(leftOperand, rightOperand);
					break;
				case Constants.EXPRESSION_TYPE_COALESCE:
					ExpressionUtils.PromoteBothOperands(ref leftOperand, ref rightOperand, ExpressionType.Coalesce);
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
