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
using System.Diagnostics;
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
		private readonly Type lambdaType;
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
		/// Creates new binder for expressions with <paramref name="lambdaType"/> signature. Optionally specifying <paramref name="typeResolver"/> to reference additional types during binding.
		/// </summary>
		/// <param name="lambdaType">Signature of bound expression.</param>
		/// <param name="typeResolver">Type resolver used for type resolution during binding process.
		/// When not specified then new instance of <see cref="KnownTypeResolver"/> is created using <paramref name="lambdaType"/> parameter types and result type.</param>
		public Binder(Type lambdaType, ITypeResolver typeResolver = null)
		{
			if (lambdaType == null) throw new ArgumentNullException("lambdaType");

			var lambdaTypeDescription = TypeDescription.GetTypeDescription(lambdaType);
			if (lambdaTypeDescription.IsDelegate == false || lambdaTypeDescription.HasGenericParameters)
				throw new ArgumentException(string.Format(Resources.EXCEPTION_BIND_VALIDDELEGATETYPEISEXPECTED, lambdaType), "lambdaType");

			var invokeMethod = lambdaTypeDescription.GetMembers(Constants.DELEGATE_INVOKE_NAME).FirstOrDefault(m => !m.IsStatic && m.IsMethod);
			if (invokeMethod == null)
				throw new MissingMethodException(lambdaType.ToString(), Constants.DELEGATE_INVOKE_NAME);

			var parametersArray = new ParameterExpression[invokeMethod.GetParametersCount()];
			for (var i = 0; i < invokeMethod.GetParametersCount(); i++)
				parametersArray[i] = Expression.Parameter(invokeMethod.GetParameterType(i), invokeMethod.GetParameterName(i));

			this.lambdaType = lambdaType;
			this.parameters = new ReadOnlyCollection<ParameterExpression>(parametersArray);
			this.resultType = TypeDescription.GetTypeDescription(invokeMethod.ResultType);
			this.typeResolver = typeResolver ?? new KnownTypeResolver(GetTypes(this.resultType, this.parameters), DefaultTypeResolver);

		}

		/// <summary>
		/// Creates new binder for expressions with signature contains <paramref name="parameters"/>(up to 4) and <paramref name="resultType"/>. Optionally specifying <paramref name="typeResolver"/> to reference additional types during binding.
		/// </summary>
		/// <param name="parameters">List of parameter for bound expression. Maximum number of parameters is 4.</param>
		/// <param name="resultType">Result type of bound expression.</param>
		/// <param name="typeResolver">Type resolver used for type resolution during binding process.
		/// When not specified then new instance of <see cref="KnownTypeResolver"/> is created using <paramref name="lambdaType"/> parameter types and result type.</param>
		public Binder(IList<ParameterExpression> parameters, Type resultType, ITypeResolver typeResolver = null)
		{
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (resultType == null) throw new ArgumentNullException("resultType");
			if (resultType.IsGenericParameter) throw new ArgumentException("A value can't be generic parameter type.", "resultType");
			if (parameters.Count > 4) throw new ArgumentOutOfRangeException("parameters");
			if (parameters.Any(p => p == null || p.Type.IsGenericParameter)) throw new ArgumentException("Collection can't contain nulls or generic parameter types.", "parameters");

			if (parameters is ReadOnlyCollection<ParameterExpression> == false)
				parameters = new ReadOnlyCollection<ParameterExpression>(parameters);

			var funcParams = new Type[parameters.Count + 1];
			for (var i = 0; i < parameters.Count; i++)
				funcParams[i] = parameters[i].Type;
			funcParams[funcParams.Length - 1] = resultType;

			this.lambdaType = Expression.GetFuncType(funcParams);
			this.parameters = (ReadOnlyCollection<ParameterExpression>)parameters;
			this.resultType = TypeDescription.GetTypeDescription(resultType);
			this.typeResolver = typeResolver ?? new KnownTypeResolver(GetTypes(resultType, parameters), DefaultTypeResolver);

		}

		/// <summary>
		/// Binds specified syntax tree to concrete types and optional context.
		/// </summary>
		/// <param name="node">Syntax tree. Not null.</param>
		/// <param name="global">Context expression. Can be null. Usually <see cref="Expression.Constant(object)"/>.</param>
		/// <returns></returns>
		public LambdaExpression Bind(SyntaxTreeNode node, Expression global = null)
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
				node = node.GetExpression(throwOnError: true);
			}
			else
			{
				bindingContext = new BindingContext(this.typeResolver, this.parameters, this.resultType, global);
			}
			var body = default(Expression);
			var bindingError = default(Exception);
			if (AnyBinder.TryBind(node, bindingContext, this.resultType, out body, out bindingError) == false)
				throw bindingError;

			Debug.Assert(body != null, "body != null");

			bindingContext.CompleteNullPropagation(ref body);

			if (body.Type != this.resultType)
				body = Expression.ConvertChecked(body, this.resultType);

			return Expression.Lambda(lambdaType, body, bindingContext.Parameters);
		}

		private static Type[] GetTypes(Type resultType, IList<ParameterExpression> parameters)
		{
			if (resultType == null) throw new ArgumentNullException("resultType");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var types = new Type[parameters.Count + 1];
			for (var i = 0; i < parameters.Count; i++)
				types[i] = parameters[i].Type;
			types[types.Length - 1] = resultType;
			return types;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("Binder: {0}, ({1}) -> {2}", this.lambdaType, string.Join(", ", parameters.Select(p => p.Name).ToArray()), this.resultType);
		}
	}
}
