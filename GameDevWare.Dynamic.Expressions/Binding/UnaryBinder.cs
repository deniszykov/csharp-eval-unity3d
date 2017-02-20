using System;
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

			if (AnyBinder.TryBind(operandNode, bindingContext, TypeDescription.ObjectType, out operand, out bindingError) == false)
				return false;

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_NEGATE:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.Negate);
					// fixing b_u_g in mono expression compiler: Negate on float or double = exception
					if (operand.Type == typeof(double) || operand.Type == typeof(float))
						boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? ExpressionUtils.NegativeSingle : ExpressionUtils.NegativeDouble);
					else
						boundExpression = Expression.Negate(operand);
					break;
				case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					ExpressionUtils.PromoteOperand(ref operand, ExpressionType.NegateChecked);
					// fixing b_u_g in mono expression compiler: Negate on float or double = exception
					if (operand.Type == typeof(double) || operand.Type == typeof(float))
						boundExpression = Expression.Multiply(operand, operand.Type == typeof(float) ? ExpressionUtils.NegativeSingle : ExpressionUtils.NegativeDouble);
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
