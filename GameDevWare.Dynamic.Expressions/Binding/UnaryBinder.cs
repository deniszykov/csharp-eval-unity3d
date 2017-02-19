using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class UnaryBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			var expressionType = node.GetExpressionType(throwOnError: true);
			var expression = node.GetExpression(throwOnError: true);
			var operand = default(Expression);

			if (AnyBinder.TryBind(expression, bindingContext, null, out operand, out bindingError) == false)
				return false;

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_NEGATE:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.Negate);
					// fixing b_u_g in mono expression compiler: Negate on float or double = exception
					if (operand.Type == typeof(double) || operand.Type == typeof(float))
						boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? Expression.Constant(-1.0f) : Expression.Constant(-1.0d));
					else
						boundExpression = Expression.Negate(operand);
					break;
				case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.NegateChecked);
					// fixing b_u_g in mono expression compiler: Negate on float or double = exception
					if (operand.Type == typeof(double) || operand.Type == typeof(float))
						boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? Expression.Constant(-1.0f) : Expression.Constant(-1.0d));
					else
						boundExpression = Expression.NegateChecked(operand);
					break;
				case Constants.EXPRESSION_TYPE_COMPLEMENT:
				case Constants.EXPRESSION_TYPE_NOT:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.Not);
					boundExpression = Expression.Not(operand);
					break;
				case Constants.EXPRESSION_TYPE_UNARYPLUS:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.UnaryPlus);
					boundExpression = Expression.UnaryPlus(operand);
					break;
				default:
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
					return false;
			}
			return true;

		}
	}
}
