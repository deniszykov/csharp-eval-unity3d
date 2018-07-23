using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class CallPacker
	{
		public static Dictionary<string, object> Pack(MethodCallExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var arguments = expression.Arguments.ToArray();
			var argumentNames = ArrayUtils.ConvertAll(expression.Method.GetParameters(), p => p.Name);

			return new Dictionary<string, object>(4) {
				{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_CALL},
				{Constants.EXPRESSION_ATTRIBUTE, AnyPacker.Pack(expression.Object)},
				{Constants.METHOD_ATTRIBUTE, AnyPacker.Pack(expression.Method)},
				{Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, argumentNames)},
			};
		}
	}
}
