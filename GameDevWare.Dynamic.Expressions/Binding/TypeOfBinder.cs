using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class TypeOfBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			try
			{
				var typeName = node.GetTypeName(throwOnError: true);
				var type = default(Type);
				if (bindingContext.TryResolveType(typeName, out type))
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
					return false;
				}

				boundExpression = Expression.Constant(type, typeof(Type));
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
