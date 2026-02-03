using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class MemberInitBinder
	{
		public static bool TryBind
			(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));
			if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));

			boundExpression = null;
			bindingError = null;

			var newNode = node.GetNewExpression(true);
			if (!AnyBinder.TryBind(newNode, bindingContext, TypeDescription.ObjectType, out var newExpression, out bindingError) ||
				!(newExpression is NewExpression))
			{
				if (bindingError == null)
					bindingError = new ExpressionParserException(Resources.EXCEPTION_BIND_FAILEDTOBINDNEWEXPRESSION, node);
				return false;
			}

			if (!TryGetBindings(newExpression.Type, node, bindingContext, out var bindings, out bindingError)) return false;

			boundExpression = Expression.MemberInit((NewExpression)newExpression, bindings);
			return true;
		}
		private static bool TryGetBindings
			(Type newExpressionType, SyntaxTreeNode node, BindingContext bindingContext, out MemberBinding[] bindings, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

			var bindingNodes = node.EnumerateBindings(true).ToList();
			bindingError = null;

			bindings = new MemberBinding[bindingNodes.Count];
			var index = 0;
			foreach (var bindingNode in bindingNodes)
			{
				if (!TryGetBinding(newExpressionType, bindingNode, bindingContext, out bindings[index], out bindingError))
				{
					bindingError = new ExpressionParserException(Resources.EXCEPTION_BIND_FAILEDTOBINDMEMBERBINDINGS, node);
					return false;
				}

				index++;
			}

			return true;
		}
		private static bool TryGetBinding(Type newExpressionType, object bindingNode, BindingContext bindingContext, out MemberBinding memberBinding, out Exception bindingError)
		{
			bindingError = null;
			memberBinding = null;
			if (!(bindingNode is SyntaxTreeNode bindingNodeTree))
			{
				return false;
			}

			var bindingType = bindingNodeTree.GetExpressionType(true);
			var member = default(MemberDescription);
			var memberOrNameObj = (object)bindingNodeTree.GetMember(false);
			if (memberOrNameObj != null)
				bindingContext.TryResolveMember(memberOrNameObj, out member);
			else
			{
				memberOrNameObj = bindingNodeTree.GetName(true);
				var memberName = memberOrNameObj.ToString();
				var typeDescription = TypeDescription.GetTypeDescription(newExpressionType);
				member = typeDescription.GetMembers(memberName).FirstOrDefault(memberDesc => !memberDesc.IsStatic);
			}

			if (member == null)
			{
				bindingError = new ExpressionParserException(string.Format(Resources.EXCEPTION_BIND_UNABLETOBINDMEMBER, memberOrNameObj, newExpressionType),
					bindingNodeTree);
				return false;
			}

			var memberValueType = TypeDescription.GetTypeDescription(member.ResultType);

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (bindingType)
			{
				case Constants.EXPRESSION_TYPE_ASSIGNMENT_BINDING:
					var expressionNode = bindingNodeTree.GetExpression(true);
					if (!AnyBinder.TryBindInNewScope(expressionNode, bindingContext, memberValueType, out var expression,
							out bindingError))
					{
						return false; // file to bind member's value
					}

					if (member.IsMethod)
						memberBinding = Expression.Bind((MethodInfo)member, expression);
					else
						memberBinding = Expression.Bind((MemberInfo)member, expression);
					return true;
				case Constants.EXPRESSION_TYPE_MEMBER_BINDING:
					if (!TryGetBindings(member.ResultType, bindingNodeTree, bindingContext, out var bindings,
							out bindingError))
					{
						return false; // failed to resolve bindings
					}

					if (member.IsMethod)
						memberBinding = Expression.MemberBind((MethodInfo)member, bindings);
					else
						memberBinding = Expression.MemberBind((MemberInfo)member, bindings);
					return true;
				case Constants.EXPRESSION_TYPE_LIST_BINDING:
					if (!ListInitBinder.TryGetListInitializers(member.ResultType, bindingNodeTree, bindingContext, out var initializers, out bindingError))
					{
						return false; // failed to resolve list initializers
					}

					if (member.IsMethod)
						memberBinding = Expression.ListBind((MethodInfo)member, initializers);
					else
						memberBinding = Expression.ListBind((MemberInfo)member, initializers);
					return true;
			}

			return false;
		}
	}
}
