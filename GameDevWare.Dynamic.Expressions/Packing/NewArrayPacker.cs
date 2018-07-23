using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class NewArrayPacker
	{
		public static Dictionary<string, object> Pack(NewArrayExpression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var arguments = expression.Expressions.ToArray();
			var elementType = expression.Type.GetTypeInfo().GetElementType();
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (expression.NodeType)
			{
				case ExpressionType.NewArrayBounds:
					return new Dictionary<string, object>(3) {
						{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS},
						{Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(elementType)},
						{Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, names: null)},
					};
				case ExpressionType.NewArrayInit:
					return new Dictionary<string, object>(3) {
						{Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_NEW_ARRAY_INIT},
						{Constants.TYPE_ATTRIBUTE, AnyPacker.Pack(elementType)},
						{Constants.ARGUMENTS_ATTRIBUTE, AnyPacker.Pack(arguments, names: null)},
					};
				default: throw new InvalidOperationException("Invalid expression type for this packer.");
			}
		}
	}
}
