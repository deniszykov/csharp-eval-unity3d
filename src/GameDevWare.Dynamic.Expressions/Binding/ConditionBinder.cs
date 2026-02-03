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
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			bindingError = null;
			boundExpression = null;

			var test = node.GetTestExpression(true);
			var ifTrue = node.GetIfTrueExpression(true);
			var ifFalse = node.GetIfFalseExpression(true);

			if (!AnyBinder.TryBindInNewScope(test, bindingContext, TypeDescription.GetTypeDescription(typeof(bool)), out var testExpression, out bindingError))
				return false;
			if (!AnyBinder.TryBindInNewScope(ifTrue, bindingContext, TypeDescription.ObjectType, out var ifTrueBranch, out bindingError))
				return false;
			if (!AnyBinder.TryBindInNewScope(ifFalse, bindingContext, TypeDescription.ObjectType, out var ifFalseBranch, out bindingError))
				return false;

			Debug.Assert(testExpression != null, "testExpression != null");
			Debug.Assert(ifTrueBranch != null, "ifTrueBranch != null");
			Debug.Assert(ifFalseBranch != null, "ifFalseBranch != null");

			if (ExpressionUtils.TryPromoteBinaryOperation(ref ifTrueBranch, ref ifFalseBranch, ExpressionType.Conditional, out boundExpression))
			{
				return true;
			}

			if (ifTrueBranch.Type != ifFalseBranch.Type)
			{
				ExpressionUtils.TryCoerceType(ref ifTrueBranch, ifFalseBranch.Type, out _);
			}

			boundExpression = Expression.Condition(testExpression, ifTrueBranch, ifFalseBranch);

			return true;
		}
	}
}
