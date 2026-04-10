using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;
using GameDevWare.Dynamic.Expressions.Packing;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	///     Provides methods for packing <see cref="Expression" /> trees into a serializable format and unpacking them back.
	/// </summary>
	public static class ExpressionPacker
	{
		/// <summary>
		///     Packs the specified <see cref="Expression" /> into a dictionary-based serializable format.
		/// </summary>
		/// <param name="expression">The expression to pack. Cannot be null.</param>
		/// <returns>A dictionary representation of the packed expression.</returns>
		public static Dictionary<string, object> Pack(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			return AnyPacker.Pack(expression);
		}
		/// <summary>
		///     Unpacks a packed expression from its dictionary-based format back into an <see cref="Expression" />.
		/// </summary>
		/// <param name="packedExpression">The dictionary containing the packed expression. Cannot be null.</param>
		/// <param name="typeResolver">The type resolver to use for resolving types in the expression. Defaults to <see cref="KnownTypeResolver.Default"/>.</param>
		/// <param name="global">The global object expression to use as a context. Can be null.</param>
		/// <param name="expectedType">The expected result type of the expression. Defaults to <see cref="object"/>.</param>
		/// <returns>The unpacked <see cref="Expression" />.</returns>
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
		/// <summary>
		///     Unpacks a packed expression into a <see cref="LambdaExpression" /> with the specified delegate type.
		/// </summary>
		/// <param name="delegateType">The type of the delegate to create. Cannot be null.</param>
		/// <param name="packedExpression">The dictionary containing the packed expression. Cannot be null.</param>
		/// <param name="typeResolver">The type resolver to use for resolving types. Can be null.</param>
		/// <param name="global">The global object expression to use as a context. Can be null.</param>
		/// <returns>The unpacked <see cref="LambdaExpression" />.</returns>
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
		/// <summary>
		///     Unpacks a packed expression into an <see cref="Expression{TDelegate}" /> of the specified delegate type.
		/// </summary>
		/// <typeparam name="DelegateT">The type of the delegate.</typeparam>
		/// <param name="expressionTree">The dictionary containing the packed expression. Cannot be null.</param>
		/// <param name="typeResolver">The type resolver to use for resolving types. Can be null.</param>
		/// <param name="global">The global object expression to use as a context. Can be null.</param>
		/// <returns>The unpacked <see cref="Expression{TDelegate}" />.</returns>
		public static Expression<DelegateT> UnpackLambda<DelegateT>
			(IDictionary<string, object> expressionTree, ITypeResolver typeResolver = null, Expression global = null)
		{
			if (expressionTree == null) throw new ArgumentNullException(nameof(expressionTree));

			return (Expression<DelegateT>)UnpackLambda(typeof(DelegateT), expressionTree, typeResolver, global);
		}
	}
}
