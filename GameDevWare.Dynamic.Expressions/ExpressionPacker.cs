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
			if (expression == null) throw new ArgumentNullException("expression");

			return AnyPacker.Pack(expression);
		}
		public static Expression Unpack(Dictionary<string, object> packedExpression, ITypeResolver typeResolver = null, Expression global = null, Type expectedType = null)
		{
			if (packedExpression == null) throw new ArgumentNullException("packedExpression");
			if(typeResolver == null) typeResolver = KnownTypeResolver.Default;
			if (expectedType == null) expectedType = typeof(object);

			var syntaxTree = new SyntaxTreeNode(packedExpression);
			var bindingContext = new BindingContext(typeResolver,  Constants.EmptyReadonlyParameters, expectedType, global);
			var boundExpression = default(Expression);
			var bindingError = default(Exception);

			if (AnyBinder.TryBindInNewScope(syntaxTree, bindingContext, TypeDescription.GetTypeDescription(expectedType), out boundExpression, out bindingError) == false)
				throw bindingError;

			return boundExpression;
		}
		public static LambdaExpression UnpackLambda(Type delegateType, Dictionary<string, object> packedExpression, ITypeResolver typeResolver = null, Expression global = null)
		{
			if (delegateType == null) throw new ArgumentNullException("delegateType");
			if (packedExpression == null) throw new ArgumentNullException("packedExpression");

			var syntaxTree = new SyntaxTreeNode(packedExpression);
			var binder = new Binder(delegateType, typeResolver);
			var unpackedExpression = binder.Bind(syntaxTree, global);
			return unpackedExpression;
		}
		public static Expression<DelegateT> UnpackLambda<DelegateT>(Dictionary<string, object> expressionTree, ITypeResolver typeResolver = null, Expression global = null)
		{
			if (expressionTree == null) throw new ArgumentNullException("expressionTree");

			return (Expression<DelegateT>)UnpackLambda(typeof(DelegateT), expressionTree, typeResolver, global);
		}
	}
}
