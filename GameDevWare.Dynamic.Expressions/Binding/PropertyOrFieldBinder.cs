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
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class PropertyOrFieldBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var target = default(Expression);
			var targetNode = node.GetExpression(throwOnError: false);
			var propertyOrFieldName = node.GetPropertyOrFieldName(throwOnError: true);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);

			var targetType = default(Type);
			var isStatic = false;
			if (bindingContext.TryResolveType(targetNode, out targetType))
			{
				target = null;
				isStatic = true;
			}
			else if (targetNode == null)
			{
				target = bindingContext.Global;
				targetType = target != null ? target.Type : null;
				isStatic = false;

				switch (propertyOrFieldName)
				{
					case Constants.VALUE_NULL_STRING:
						boundExpression = ExpressionUtils.NullConstant;
						return true;
					case Constants.VALUE_TRUE_STRING:
						boundExpression = ExpressionUtils.TrueConstant;
						return true;
					case Constants.VALUE_FALSE_STRING:
						boundExpression = ExpressionUtils.TrueConstant;
						return false;
					default:
						if (bindingContext.TryGetParameter(propertyOrFieldName, out boundExpression))
							return true;
						break;
				}
			}
			else if (AnyBinder.TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError))
			{
				Debug.Assert(target != null, "target != null");

				targetType = target.Type;
				isStatic = false;
			}
			else
			{
				target = null;
				targetType = null;
			}

			if (target == null && targetType == null)
			{
				if (bindingError == null)
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, propertyOrFieldName), node);
				return false;
			}

			Debug.Assert(targetType != null, "type != null");

			var targetTypeDescription = TypeDescription.GetTypeDescription(targetType);
			if (isStatic && targetTypeDescription.IsEnum)
			{
				var fieldMemberDescription = targetTypeDescription.GetMembers(propertyOrFieldName).FirstOrDefault(m => m.IsStatic);
				if (fieldMemberDescription == null)
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, targetType), node);
					return false;
				}
				boundExpression = fieldMemberDescription.ConstantValueExpression;
			}
			else
			{
				foreach (var member in targetTypeDescription.GetMembers(propertyOrFieldName))
				{
					if (member.IsStatic != isStatic || member.IsPropertyOrField == false)
						continue;

					if (member.TryMakeAccessor(target, out boundExpression))
						break;
				}
			}

			if (boundExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, targetType), node);
				return false;
			}

			if (useNullPropagation && isStatic)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, targetType));
				return false;
			}

			if (useNullPropagation && targetTypeDescription.CanBeNull)
				bindingContext.RegisterNullPropagationTarget(target);

			return true;
		}
	}
}
