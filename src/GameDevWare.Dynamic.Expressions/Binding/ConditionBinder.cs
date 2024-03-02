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
using System.Diagnostics;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class ConditionBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			bindingError = null;
			boundExpression = null;

			var test = node.GetTestExpression(throwOnError: true);
			var ifTrue = node.GetIfTrueExpression(throwOnError: true);
			var ifFalse = node.GetIfFalseExpression(throwOnError: true);
			var testExpression = default(Expression);
			var ifTrueBranch = default(Expression);
			var ifFalseBranch = default(Expression);

			if (AnyBinder.TryBindInNewScope(test, bindingContext, TypeDescription.GetTypeDescription(typeof(bool)), out testExpression, out bindingError) == false)
				return false;
			if (AnyBinder.TryBindInNewScope(ifTrue, bindingContext, TypeDescription.ObjectType, out ifTrueBranch, out bindingError) == false)
				return false;
			if (AnyBinder.TryBindInNewScope(ifFalse, bindingContext, TypeDescription.ObjectType, out ifFalseBranch, out bindingError) == false)
				return false;

			Debug.Assert(testExpression != null, "testExpression != null");
			Debug.Assert(ifTrueBranch != null, "ifTrueBranch != null");
			Debug.Assert(ifFalseBranch != null, "ifFalseBranch != null");

			if (ExpressionUtils.TryPromoteBinaryOperation(ref ifTrueBranch, ref ifFalseBranch, ExpressionType.Conditional, out boundExpression) == false)
			{
				if (ifTrueBranch.Type != ifFalseBranch.Type)
				{
					float quality;
					ExpressionUtils.TryCoerceType(ref ifTrueBranch, ifFalseBranch.Type, out quality);
				}

				boundExpression = Expression.Condition(testExpression, ifTrueBranch, ifFalseBranch);
			}
			return true;
		}
	}
}
