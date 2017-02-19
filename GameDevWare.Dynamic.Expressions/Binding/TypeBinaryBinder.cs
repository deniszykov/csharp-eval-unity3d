using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class TypeBinaryBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			try
			{
				var expressionType = node.GetExpressionType(throwOnError: true);
				var expression = node.GetExpression(throwOnError: true);
				var typeName = node.GetTypeName(throwOnError: true);
				var type = default(Type);
				if (bindingContext.TryResolveType(typeName, out type))
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
					return false;
				}

				var target = default(Expression);
				if (AnyBinder.TryBind(expression, bindingContext, null, out target, out bindingError) == false)
					return false;

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
				}
				return true;
			}
			catch (Exception error)
			{
				bindingError = error;
				return false;
			}
		}
	}
}
