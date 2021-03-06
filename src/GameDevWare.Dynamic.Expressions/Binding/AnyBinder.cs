/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class AnyBinder
	{
		public static bool TryBindInNewScope(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			bindingContext = bindingContext.CreateNestedContext();
			var result = TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
			bindingContext.CompleteNullPropagation(ref boundExpression);
			return result;
		}
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
					case Constants.EXPRESSION_TYPE_MEMBER_RESOLVE:
					case Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD:
						return MemberBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
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
					case Constants.EXPRESSION_TYPE_TYPE_OF:
						return TypeOfBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CONVERT:
					case Constants.EXPRESSION_TYPE_CONVERT_CHECKED:
					case Constants.EXPRESSION_TYPE_TYPE_IS:
					case Constants.EXPRESSION_TYPE_TYPE_AS:
						return TypeBinaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_DEFAULT:
						return DefaultBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEW:
						return NewBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS:
						return NewArrayBoundsBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_INIT:
						return NewArrayInitBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_MEMBER_INIT:
						return MemberInitBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_LIST_INIT:
						return ListInitBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_ADD:
					case Constants.EXPRESSION_TYPE_ADD_CHECKED:
					case Constants.EXPRESSION_TYPE_SUBTRACT:
					case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED:
					case Constants.EXPRESSION_TYPE_LEFT_SHIFT:
					case Constants.EXPRESSION_TYPE_RIGHT_SHIFT:
					case Constants.EXPRESSION_TYPE_GREATER_THAN:
					case Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_LESS_THAN:
					case Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_POWER:
					case Constants.EXPRESSION_TYPE_DIVIDE:
					case Constants.EXPRESSION_TYPE_MULTIPLY:
					case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED:
					case Constants.EXPRESSION_TYPE_MODULO:
					case Constants.EXPRESSION_TYPE_EQUAL:
					case Constants.EXPRESSION_TYPE_NOT_EQUAL:
					case Constants.EXPRESSION_TYPE_AND:
					case Constants.EXPRESSION_TYPE_OR:
					case Constants.EXPRESSION_TYPE_EXCLUSIVE_OR:
					case Constants.EXPRESSION_TYPE_AND_ALSO:
					case Constants.EXPRESSION_TYPE_OR_ELSE:
					case Constants.EXPRESSION_TYPE_COALESCE:
						return BinaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_NEGATE:
					case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					case Constants.EXPRESSION_TYPE_COMPLEMENT:
					case Constants.EXPRESSION_TYPE_NOT:
					case Constants.EXPRESSION_TYPE_UNARY_PLUS:
					case Constants.EXPRESSION_TYPE_ARRAY_LENGTH:
						return UnaryBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_CONDITION:
						return ConditionBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_PARAMETER:
						return ParameterBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
					case Constants.EXPRESSION_TYPE_QUOTE:
						return QuoteBinder.TryBind(node, bindingContext, expectedType, out boundExpression, out bindingError);
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
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_FAILEDTOBIND, node.GetExpressionType(throwOnError: false) ?? "<unknown>", error.Message), error, node);
				return false;
			}
		}

	}
}
