using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class MemberInitBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			boundExpression = null;
			bindingError = null;

			var newNode = node.GetNewExpression(throwOnError: true);
			var newExpression = default(Expression);
			if (AnyBinder.TryBind(newNode, bindingContext, TypeDescription.ObjectType, out newExpression, out bindingError) == false ||
				newExpression is NewExpression == false)
			{
				if (bindingError == null)
					bindingError = new ExpressionParserException(Properties.Resources.EXCEPTION_BIND_FAILEDTOBINDNEWEXPRESSION, node);
				return false;
			}

			var bindings = default(MemberBinding[]);
			if (TryGetBindings(node, bindingContext, out bindings, out bindingError) == false)
			{
				return false;
			}

			boundExpression = Expression.MemberInit((NewExpression)newExpression, bindings);
			return true;
		}
		private static bool TryGetBindings(SyntaxTreeNode node, BindingContext bindingContext, out MemberBinding[] bindings, out Exception bindingError)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (bindingContext == null) throw new ArgumentNullException("bindingContext");

			var bindingNodes = node.GetBindings(throwOnError: true);
			bindingError = null;

			bindings = new MemberBinding[bindingNodes.Count];
			for (var i = 0; i < bindings.Length; i++)
			{
				if (TryGetBinding(bindingNodes[Constants.GetIndexAsString(i)], bindingContext, out bindings[i], out bindingError))
					continue;

				bindingError = bindingError ?? new ExpressionParserException(Properties.Resources.EXCEPTION_BIND_FAILEDTOBINDMEMBERBINDINGS, node);
				return false;
			}

			return true;
		}
		private static bool TryGetBinding(object bindingNode, BindingContext bindingContext, out MemberBinding memberBinding, out Exception bindingError)
		{
			bindingError = null;
			memberBinding = null;
			var bindingNodeTree = bindingNode as SyntaxTreeNode;
			if (bindingNodeTree == null)
			{
				return false;
			}

			var bindingType = (string)bindingNodeTree.GetTypeName(throwOnError: true);
			var memberObj = bindingNodeTree.GetMember(throwOnError: true);
			var member = default(MemberDescription);
			if (bindingContext.TryResolveMember(memberObj, out member) == false)
			{
				return false;
			}
			var memberValueType = TypeDescription.GetTypeDescription(member.ResultType);

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (bindingType)
			{
				case "Assignment":
					var expressionNode = bindingNodeTree.GetExpression(throwOnError: true);
					var expression = default(Expression);
					if (AnyBinder.TryBindInNewScope(expressionNode, bindingContext, memberValueType, out expression, out bindingError) == false)
					{
						return false; // file to bind member's value
					}

					if (member.IsMethod)
						memberBinding = Expression.Bind((MethodInfo)member, expression);
					else
						memberBinding = Expression.Bind((MemberInfo)member, expression);
					return true;
				case "MemberBinding":
					var bindings = default(MemberBinding[]);
					if (TryGetBindings(bindingNodeTree, bindingContext, out bindings, out bindingError) == false)
					{
						return false; // failed to resolve bindings
					}
					if (member.IsMethod)
						memberBinding = Expression.MemberBind((MethodInfo)member, bindings);
					else
						memberBinding = Expression.MemberBind((MemberInfo)member, bindings);
					return true;
				case "ListBinding":
					var initializers = default(ElementInit[]);
					if (ListInitBinder.TryGetListInitializers(bindingNodeTree, bindingContext, out initializers, out bindingError) == false)
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
