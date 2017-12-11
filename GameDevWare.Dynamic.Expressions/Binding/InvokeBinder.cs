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

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class InvokeBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			if (TryBindMethodCall(node, bindingContext, expectedType, out boundExpression, out bindingError))
				return true;

			var targetNode = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var target = default(Expression);
			if (AnyBinder.TryBind(targetNode, bindingContext, TypeDescription.ObjectType, out target, out bindingError) == false)
				return false;

			Debug.Assert(target != null, "target != null");

			var typeDescription = TypeDescription.GetTypeDescription(target.Type);
			if (typeDescription.IsDelegate == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOINVOKENONDELEG, target.Type), node);
				return false;
			}

			var methodDescription = typeDescription.GetMembers(Constants.DELEGATE_INVOKE_NAME).FirstOrDefault(m => m.IsMethod && !m.IsStatic);
			if (methodDescription == null) throw new MissingMethodException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGMETHOD, target.Type.FullName, Constants.DELEGATE_INVOKE_NAME));

			var expressionQuality = 0.0f;
			if (methodDescription.TryMakeCall(target, arguments, bindingContext, out boundExpression, out expressionQuality) == false)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDDELEG, target.Type, methodDescription), node);
				return false;
			}

			return true;

		}
		public static bool TryBindMethodCall(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			// bindingError could return null from this method
			bindingError = null;
			boundExpression = null;

			var methodNameNode = node.GetExpression(throwOnError: true);
			var methodNameNodeType = methodNameNode.GetExpressionType(throwOnError: true);
			if (methodNameNodeType != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
			{
				return false;
			}
			var methodTargetNode = methodNameNode.GetExpression(throwOnError: false);
			var methodTarget = default(Expression);

			var type = default(Type);
			var typeReference = default(TypeReference);
			var isStatic = false;
			if (methodTargetNode == null && bindingContext.Global != null)
			{
				methodTarget = bindingContext.Global;
				type = methodTarget.Type;
				isStatic = false;
			}
			else if (methodTargetNode == null)
			{
				var methodName = methodNameNode.GetPropertyOrFieldName(throwOnError: false);
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, methodName ?? "<unknown>"), node);
				return false;
			}
			else if (BindingContext.TryGetTypeReference(methodTargetNode, out typeReference) && bindingContext.TryResolveType(typeReference, out type))
			{
				isStatic = true;
			}
			else if (AnyBinder.TryBind(methodTargetNode, bindingContext, TypeDescription.ObjectType, out methodTarget, out bindingError))
			{
				Debug.Assert(methodTarget != null, "methodTarget != null");

				isStatic = false;
				type = methodTarget.Type;
			}
			else
			{
				if (typeReference != null && bindingError == null)
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeReference), node);
				return false;
			}

			var methodRef = default(TypeReference);
			if (type == null || BindingContext.TryGetMethodReference(methodNameNode, out methodRef) == false)
				return false;

			var typeDescription = TypeDescription.GetTypeDescription(type);
			foreach (var member in typeDescription.GetMembers(methodRef.Name))
			{
				if (member.IsMethod == false || member.IsStatic != isStatic) continue;

				var callNode = new SyntaxTreeNode(new Dictionary<string, object>
				{
					{ Constants.EXPRESSION_ATTRIBUTE, methodTarget ?? (object)type },
					{ Constants.ARGUMENTS_ATTRIBUTE, node.GetValueOrDefault(Constants.ARGUMENTS_ATTRIBUTE, default(object)) },
					{ Constants.METHOD_ATTRIBUTE, methodRef },
					{ Constants.USE_NULL_PROPAGATION_ATTRIBUTE, methodNameNode.GetValueOrDefault(Constants.USE_NULL_PROPAGATION_ATTRIBUTE, default(object)) },

					{ Constants.EXPRESSION_POSITION, methodNameNode.GetPosition(throwOnError: false) }
				});

				return CallBinder.TryBind(callNode, bindingContext, expectedType, out boundExpression, out bindingError);
			}

			return false;
		}
	}
}
