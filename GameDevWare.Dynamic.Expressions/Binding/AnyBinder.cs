using System;
using System.CodeDom;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class AnyBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			try
			{
				var expressionType = node.GetExpressionType(throwOnError: true);
				switch (expressionType)
				{
					case Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD:
						return PropertyOrFieldBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CONSTANT:
						return ConstantBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CALL:
						return CallBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case "Enclose":
					case Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_CHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_GROUP:
						return GroupBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_INVOKE:
						return InvokeBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_LAMBDA:
						return LambdaBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_INDEX:
						return IndexBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_TYPEOF:
						return TypeOfBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CONVERT:
					case Constants.EXPRESSION_TYPE_CONVERTCHECKED:
					case Constants.EXPRESSION_TYPE_TYPEIS:
					case Constants.EXPRESSION_TYPE_TYPEAS:
						return TypeBinaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_DEFAULT:
						return DefaultBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEW:
						return NewBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS:
						return NewArrayBoundsBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_ADD:
					case Constants.EXPRESSION_TYPE_ADD_CHECKED:
					case Constants.EXPRESSION_TYPE_SUBTRACT:
					case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED:
					case Constants.EXPRESSION_TYPE_LEFTSHIFT:
					case Constants.EXPRESSION_TYPE_RIGHTSHIFT:
					case Constants.EXPRESSION_TYPE_GREATERTHAN:
					case Constants.EXPRESSION_TYPE_GREATERTHAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_LESSTHAN:
					case Constants.EXPRESSION_TYPE_LESSTHAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_POWER:
					case Constants.EXPRESSION_TYPE_DIVIDE:
					case Constants.EXPRESSION_TYPE_MULTIPLY:
					case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED:
					case Constants.EXPRESSION_TYPE_MODULO:
					case Constants.EXPRESSION_TYPE_EQUAL:
					case Constants.EXPRESSION_TYPE_NOTEQUAL:
					case Constants.EXPRESSION_TYPE_AND:
					case Constants.EXPRESSION_TYPE_OR:
					case Constants.EXPRESSION_TYPE_EXCLUSIVEOR:
					case Constants.EXPRESSION_TYPE_ANDALSO:
					case Constants.EXPRESSION_TYPE_ORELSE:
					case Constants.EXPRESSION_TYPE_COALESCE:
						return BinaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEGATE:
					case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					case Constants.EXPRESSION_TYPE_COMPLEMENT:
					case Constants.EXPRESSION_TYPE_NOT:
					case Constants.EXPRESSION_TYPE_UNARYPLUS:
						return UnaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CONDITION:
						return ConditionBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					default:
						boundExpression = null;
						bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType), node);
						return false;
				}
			}
			catch (ExpressionParserException error)
			{
				boundExpression = null;
				bindingError = error;
				return false;
			}
			catch (Exception error)
			{
				boundExpression = null;
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_FAILEDTOBIND, node.GetExpressionType(throwOnError: false) ?? "<unknown>", error.Message), node);
				return false;
			}
		}

	}
}
