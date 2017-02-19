using System;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class InvokeBinder
	{
		public static bool TryBind(SyntaxTreeNode node, BindingContext bindingContext, TypeDescription expectedType, out Expression boundExpression, out Exception bindingError)
		{
			boundExpression = null;
			bindingError = null;
			return false;

			/*
			var target = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var targetExpressionType = target.GetExpressionType(throwOnError: true);
			var expression = default(Expression);

			if (targetExpressionType == Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
			{
				var propertyOrFieldTarget = target.GetExpression(throwOnError: false);
				var useNullPropagation = target.GetUseNullPropagation(throwOnError: false);

				var typeReference = default(TypeReference);
				var type = default(Type);
				var isStatic = true;
				if (propertyOrFieldTarget == null || TryGetTypeReference(propertyOrFieldTarget, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				{
					try
					{
						var propertyOrFieldExpression = propertyOrFieldTarget != null ? Build(propertyOrFieldTarget, context, typeHint: null) : context;
						if (propertyOrFieldExpression != null)
						{
							type = propertyOrFieldExpression.Type;
							isStatic = false;
						}
					}
					catch (ExpressionParserException)
					{
						if (typeReference != null) // throw better error message about wrong type reference
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETORESOLVETYPE, typeReference), node);
						throw;

					}
				}
				var methodRef = default(TypeReference);
				if (type != null && TryGetMethodReference(target, out methodRef) && GetMembers(type, isStatic).Any(m => m is MethodInfo && m.Name == methodRef.Name))
					return this.BuildCall(node, propertyOrFieldTarget, useNullPropagation, arguments, methodRef, context);
			}

			expression = Build(target, context, typeHint: null);

			if (typeof(Delegate).IsAssignableFrom(expression.Type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOINVOKENONDELEG, expression.Type), node);

			var method = expression.Type.GetMethod(Constants.DELEGATE_INVOKE_NAME);
			if (method == null) throw new MissingMethodException(expression.Type.FullName, Constants.DELEGATE_INVOKE_NAME);
			var methodParameters = method.GetParameters();
			var argumentExpressions = default(Expression[]);
			if (TryBindMethod(methodParameters, arguments, context, out argumentExpressions) <= 0)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BIND_UNABLETOBINDDELEG, expression.Type, string.Join(", ", Array.ConvertAll(methodParameters, p => p.ParameterType.Name))), node);

			try
			{
				return Expression.Invoke(expression, argumentExpressions);
			}
			catch (Exception exception)
			{
				throw new ExpressionParserException(exception.Message, exception, node);
			}*/


		}
	}
}
