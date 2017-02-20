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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// Binder which is used for binding syntax tree concrete types and members.
	/// </summary>
	public class Binder
	{
		/// <summary>
		/// Default type resolver which is used if none is specified.
		/// </summary>
		public static ITypeResolver DefaultTypeResolver = null;

		private readonly ReadOnlyCollection<ParameterExpression> parameters;
		private readonly TypeDescription resultType;
		private readonly ITypeResolver typeResolver;

		/// <summary>
		/// List of parameters for bound expression.
		/// </summary>
		public ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameters; } }
		/// <summary>
		/// Result type of bound expression.
		/// </summary>
		public Type ResultType { get { return this.resultType; } }

		/// <summary>
		/// Creates new binder for expressions with signature contains <paramref name="parameters"/> and <paramref name="resultType"/>. Optionally specified <paramref name="contextType"/> and <paramref name="typeResolver"/>.
		/// </summary>
		/// <param name="parameters">List of parameter for bound expression.</param>
		/// <param name="resultType">Result type of bound expression.</param>
		/// <param name="contextType">Context type of bound expression.</param>
		/// <param name="typeResolver">Type resolver for bound expression.</param>
		public Binder(IList<ParameterExpression> parameters, Type resultType, Type contextType = null, ITypeResolver typeResolver = null)
		{
			if (resultType == null) throw new ArgumentNullException("resultType");
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (parameters.Any(p => p.Type.IsGenericParameter)) throw new ArgumentNullException("parameters");
			if (typeResolver == null) typeResolver = new KnownTypeResolver(parameters.Select(p => p.Type), DefaultTypeResolver);

			if (parameters is ReadOnlyCollection<ParameterExpression> == false)
				parameters = new ReadOnlyCollection<ParameterExpression>(parameters);

			this.parameters = (ReadOnlyCollection<ParameterExpression>)parameters;
			this.resultType = TypeDescription.GetTypeDescription(resultType);
			this.typeResolver = typeResolver;

		}

		/// <summary>
		/// Binds specified syntax tree to concrete types and optional context.
		/// </summary>
		/// <param name="node">Syntax tree. Not null.</param>
		/// <param name="context">Context expression. Can be null. Usually <see cref="Expression.Constant(object)"/>.</param>
		/// <returns></returns>
		[Obsolete("User Bind() instead.")]
		public Expression Build(SyntaxTreeNode node, Expression context = null)
		{
			if (node == null) throw new ArgumentNullException("node");

			return Bind(node, context);
		}
		/// <summary>
		/// Binds specified syntax tree to concrete types and optional context.
		/// </summary>
		/// <param name="node">Syntax tree. Not null.</param>
		/// <param name="global">Context expression. Can be null. Usually <see cref="Expression.Constant(object)"/>.</param>
		/// <returns></returns>
		public Expression Bind(SyntaxTreeNode node, Expression global = null)
		{
			if (node == null) throw new ArgumentNullException("node");

			var bindingContext = default(BindingContext);
			// lambda binding substitution feature
			if (node.GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA && this.resultType.IsDelegate == false)
			{
				var newParameterNames = LambdaBinder.ExtractArgumentNames(node);
				if (newParameterNames.Length != this.parameters.Count)
					throw new ExpressionParserException(Resources.EXCEPTION_BIND_UNABLEREMAPPARAMETERSCOUNTMISMATCH, node);

				var newParameters = new ParameterExpression[newParameterNames.Length];
				for (var i = 0; i < newParameters.Length; i++)
					newParameters[i] = Expression.Parameter(this.parameters[i].Type, newParameterNames[i]);
				bindingContext = new BindingContext(this.typeResolver, new ReadOnlyCollection<ParameterExpression>(newParameters), this.resultType, global);
			}
			else
			{
				bindingContext = new BindingContext(this.typeResolver, this.parameters, this.resultType, global);
			}
			var expression = default(Expression);
			var bindingError = default(Exception);
			if (AnyBinder.TryBind(node, bindingContext, this.resultType, out expression, out bindingError) == false)
				throw bindingError;

			if (expression.Type != this.resultType)
				expression = Expression.ConvertChecked(expression, this.resultType);

			return expression;
		}
	}
}
