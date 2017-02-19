using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class PropertyOrFieldBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;
			return false;
			/*
			var expression = default(Expression);
			var target = node.GetExpression(throwOnError: false);
			var propertyOrFieldName = node.GetPropertyOrFieldName(throwOnError: true);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);

			var typeReference = default(TypeReference);
			var type = default(Type);
			if (target != null && TryGetTypeReference(target, out typeReference) && this.typeResolver.TryGetType(typeReference, out type))
			{
				expression = null;
			}
			else if (target == null)
			{
				var paramExpression = default(Expression);
				if (propertyOrFieldName == "null")
					return Expression.Constant(null, typeof(object));
				else if (propertyOrFieldName == "true")
					return Expression.Constant(true, typeof(bool));
				else if (propertyOrFieldName == "false")
					return Expression.Constant(false, typeof(bool));
				else if ((paramExpression = parameters.FirstOrDefault(p => p.Name == propertyOrFieldName)) != null)
					return paramExpression;
				else if (context != null)
					expression = context;
			}
			else
			{
				expression = Build(target, context, typeHint: null);
			}

			if (expression == null && type == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVENAME, propertyOrFieldName), node);

			if (expression != null)
				type = expression.Type;

			var isStatic = expression == null;
			var memberAccessExpression = default(Expression);
			if (isStatic && type.IsEnum)
			{
				memberAccessExpression = Expression.Constant(Enum.Parse(type, propertyOrFieldName, ignoreCase: false), type);
			}
			else
			{
				foreach (var member in GetMembers(type, isStatic))
				{
					if (member is PropertyInfo == false && member is FieldInfo == false)
						continue;
					if (member.Name != propertyOrFieldName)
						continue;

					try
					{
						if (member is PropertyInfo)
						{
							memberAccessExpression = Expression.Property(expression, member as PropertyInfo);
							break;
						}
						else
						{
							memberAccessExpression = Expression.Field(expression, member as FieldInfo);
							break;
						}
					}
					catch (Exception exception)
					{
						throw new ExpressionParserException(exception.Message, exception, node);
					}
				}
			}

			if (memberAccessExpression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type), node);

			if (useNullPropagation && isStatic)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, type));

			if (useNullPropagation)
				return MakeNullPropagationExpression(expression, memberAccessExpression);
			else
				return memberAccessExpression;
				*/
		}
	}
}
