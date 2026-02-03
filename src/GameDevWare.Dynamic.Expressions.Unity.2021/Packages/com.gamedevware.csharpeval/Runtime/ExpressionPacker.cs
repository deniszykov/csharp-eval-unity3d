using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;
using GameDevWare.Dynamic.Expressions.Packing;

namespace GameDevWare.Dynamic.Expressions
{
	public static class ExpressionPacker
	{
		public static Dictionary<string, object> Pack(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return AnyPacker.Pack(expression);
		}
		public static Expression Unpack
			(IDictionary<string, object> packedExpression, ITypeResolver typeResolver = null, Expression global = null, Type expectedType = null)
		{
			if (packedExpression == null) throw new ArgumentNullException(nameof(packedExpression));

			if (typeResolver == null) typeResolver = KnownTypeResolver.Default;
			if (expectedType == null) expectedType = typeof(object);

			var syntaxTree = new SyntaxTreeNode(packedExpression);
			var bindingContext = new BindingContext(typeResolver, Constants.EmptyReadonlyParameters, expectedType, global);

			if (!AnyBinder.TryBindInNewScope(syntaxTree, bindingContext, TypeDescription.GetTypeDescription(expectedType), out var boundExpression,
					out var bindingError))
				throw bindingError;

			return boundExpression;
		}
		public static LambdaExpression UnpackLambda
			(Type delegateType, IDictionary<string, object> packedExpression, ITypeResolver typeResolver = null, Expression global = null)
		{
			if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));
			if (packedExpression == null) throw new ArgumentNullException(nameof(packedExpression));

			var syntaxTree = new SyntaxTreeNode(packedExpression);
			var binder = new Binder(delegateType, typeResolver);
			var unpackedExpression = binder.Bind(syntaxTree, global);
			return unpackedExpression;
		}
		public static Expression<DelegateT> UnpackLambda<DelegateT>
			(IDictionary<string, object> expressionTree, ITypeResolver typeResolver = null, Expression global = null)
		{
			if (expressionTree == null) throw new ArgumentNullException(nameof(expressionTree));

			return (Expression<DelegateT>)UnpackLambda(typeof(DelegateT), expressionTree, typeResolver, global);
		}
	}
}
