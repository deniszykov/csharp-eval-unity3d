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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class InvokeBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			if (TryBindMethodCall(node, bindingContext, expectedType, out boundExpression, out bindingError))
				return true;

			var targetNode = node.GetExpression(true);
			var arguments = node.GetArguments(false);
			if (!AnyBinder.TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out var target, out bindingError))
				return false;

			Debug.Assert(target != null, "target != null");

			var typeDescription = TypeDescription.GetTypeDescription(target.Type);
			if (!typeDescription.IsDelegate)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOINVOKENONDELEG, target.Type), node);
				return false;
			}

			var methodDescription = typeDescription.GetMembers(Constants.DELEGATE_INVOKE_NAME).FirstOrDefault(m => m.IsMethod && !m.IsStatic);
			if (methodDescription == null)
			{
				throw new MissingMethodException(string.Format(Resources.EXCEPTION_BIND_MISSINGMETHOD, target.Type.FullName,
					Constants.DELEGATE_INVOKE_NAME));
			}

			if (!methodDescription.TryMakeCall(target, arguments, bindingContext, out boundExpression, out _))
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDDELEG, target.Type, methodDescription),
					node);
				return false;
			}

			return true;
		}
		public static bool TryBindMethodCall
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			// bindingError could return null from this method
			bindingError = null;
			boundExpression = null;

			var methodNameNode = node.GetExpression(true);
			var methodNameNodeType = methodNameNode.GetExpressionType(true);
			if (methodNameNodeType != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD &&
				methodNameNodeType != Constants.EXPRESSION_TYPE_MEMBER_RESOLVE)
				return false;

			var methodTargetNode = methodNameNode.GetExpression(false);
			var methodTarget = default(Expression);

			var type = default(Type);
			var isStatic = false;
			if (methodTargetNode == null && bindingContext.Global != null)
			{
				methodTarget = bindingContext.Global;
				type = methodTarget.Type;
				isStatic = false;
			}
			else if (methodTargetNode == null)
			{
				var methodName = methodNameNode.GetMemberName(false);
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName ?? "<unknown>"),
					node);
				return false;
			}
			else if (BindingContext.TryGetTypeReference(methodTargetNode, out var typeReference) && bindingContext.TryResolveType(typeReference, out type))
				isStatic = true;
			else if (AnyBinder.TryBind(methodTargetNode, bindingContext, TypeDescription.ObjectType, out methodTarget, out bindingError))
			{
				Debug.Assert(methodTarget != null, "methodTarget != null");

				isStatic = false;
				type = methodTarget.Type;
			}
			else
			{
				if (typeReference != null && bindingError == null)
					bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeReference), node);
				return false;
			}

			if (type == null || !BindingContext.TryGetMethodReference(methodNameNode, out var methodRef))
				return false;

			var typeDescription = TypeDescription.GetTypeDescription(type);
			foreach (var member in typeDescription.GetMembers(methodRef.Name))
			{
				if (!member.IsMethod || member.IsStatic != isStatic) continue;

				var callNode = new SyntaxTreeNode(new Dictionary<string, object> {
					{ Constants.EXPRESSION_ATTRIBUTE, methodTarget ?? (object)type },
					{ Constants.ARGUMENTS_ATTRIBUTE, node.GetValueOrDefault(Constants.ARGUMENTS_ATTRIBUTE, default(object)) },
					{ Constants.METHOD_ATTRIBUTE, methodRef },
					{ Constants.USE_NULL_PROPAGATION_ATTRIBUTE, methodNameNode.GetValueOrDefault(Constants.USE_NULL_PROPAGATION_ATTRIBUTE, default(object)) },
					{ Constants.EXPRESSION_POSITION, methodNameNode.GetPositionOrDefault(false) }
				});

				return CallBinder.TryBind(callNode, bindingContext, expectedType, out boundExpression, out bindingError);
			}

			return false;
		}
	}
}
