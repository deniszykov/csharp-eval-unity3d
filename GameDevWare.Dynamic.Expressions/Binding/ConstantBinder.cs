using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class ConstantBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;

			try
			{
				var valueObj = node.GetValue(throwOnError: true);
				var typeName = node.GetTypeName(throwOnError: true);
				var type = default(Type);
				if (bindingContext.TryResolveType(typeName, out type))
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeName), node);
					return false;
				}

				var value = ChangeType(valueObj, type);
				boundExpression = Expression.Constant(value);
				return true;
			}
			catch (Exception error)
			{
				bindingError = error;
				return false;
			}
		}

		public static object ChangeType(object value, Type toType)
		{
			if (toType == null) throw new ArgumentNullException("toType");

			if (toType.IsEnum)
				return Enum.Parse(toType, Convert.ToString(value, Constants.DefaultFormatProvider));
			else
				return Convert.ChangeType(value, toType, Constants.DefaultFormatProvider);
		}
	}
}
