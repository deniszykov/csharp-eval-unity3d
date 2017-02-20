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

			var type = default(Type);
			var isStatic = false;
			if (bindingContext.TryResolveType(targetNode, out type))
			{
				target = null;
				isStatic = true;
			}
			else if (targetNode == null)
			{
				target = bindingContext.Global;
				type = target != null ? target.Type : null;
				isStatic = false;

				switch (propertyOrFieldName)
				{
					case Constants.VALUE_NULL_STRING:
						boundExpression = expectedType.CanBeNull ? expectedType.DefaultExpression : TypeDescription.ObjectType.DefaultExpression;
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
				type = target.Type;
				isStatic = false;
			}
			else
			{
				target = null;
				type = null;
			}

			if (target == null && type == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, propertyOrFieldName), node);
				return false;
			}

			Debug.Assert(type != null, "type != null");

			var typeDescription = TypeDescription.GetTypeDescription(type);
			if (isStatic && type.IsEnum)
			{
				var fieldMemberDescription = typeDescription.GetMembers(propertyOrFieldName).FirstOrDefault(m => m.IsStatic);
				if (fieldMemberDescription == null)
				{
					bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type), node);
					return false;
				}
				boundExpression = fieldMemberDescription.ConstantValueExpression;
			}
			else
			{
				foreach (var member in typeDescription.GetMembers(propertyOrFieldName))
				{
					if (member.IsStatic != isStatic || member.IsPropertyOrField == false)
						continue;

					if (member.TryMakeAccessor(target, out boundExpression))
						break;
				}
			}

			if (boundExpression == null)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type), node);
				return false;
			}

			if (useNullPropagation && isStatic)
			{
				bindingError = new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, type));
				return false;
			}

			if (useNullPropagation)
				boundExpression = ExpressionUtils.MakeNullPropagationExpression(target, boundExpression);

			return true;
		}
	}
}
