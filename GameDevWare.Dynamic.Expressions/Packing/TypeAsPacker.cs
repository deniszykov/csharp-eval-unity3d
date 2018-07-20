using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class TypeAsPacker
	{
		public static Dictionary<string, object> Pack(UnaryExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			return new Dictionary<string, object>(3) {
				{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_TYPE_AS},
				{Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Operand)},
				{Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(expression.Type)}
			};
		}
	}
}