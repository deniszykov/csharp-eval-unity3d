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
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal static class TypeInference
	{
		/// <summary>
		///     Infers type arguments of a generic method from the types of its call arguments, as C# does for
		///     <see cref="System.Linq.Enumerable" /> queries and other generic methods invoked without explicit type arguments.
		/// </summary>
		/// <param name="method">Generic method definition to instantiate. Not null.</param>
		/// <param name="extensionTarget">Receiver of an extension method call, bound to the first parameter. Null for other calls.</param>
		/// <param name="argumentsTree">Arguments of the call, by name or position. Not null.</param>
		/// <param name="bindingContext">Context of the current binding. Not null.</param>
		/// <param name="genericArguments">Inferred type arguments or null.</param>
		/// <returns>True if every type parameter is inferred. Overwise false.</returns>
		public static bool TryInferGenericArguments
		(
			MemberDescription method,
			Expression extensionTarget,
			ArgumentsTree argumentsTree,
			BindingContext bindingContext,
			out Type[] genericArguments
		)
		{
			if (method == null) throw new ArgumentNullException(nameof(method));
			if (argumentsTree == null) throw new ArgumentNullException(nameof(argumentsTree));
			if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

			genericArguments = null;

			var genericParameters = method.GenericArguments;
			if (genericParameters == null || genericParameters.Length == 0)
				return false;

			var argumentOffset = extensionTarget != null ? 1 : 0;
			if (argumentsTree.Count + argumentOffset > method.GetParametersCount())
				return false;

			var bindings = new Dictionary<Type, Type>();
			if (extensionTarget != null)
				Unify(method.GetParameterType(0), extensionTarget.Type, bindings);

			// lambdas are inferred last: their parameter types are only known once the other arguments are unified
			var lambdaArguments = default(List<KeyValuePair<int, SyntaxTreeNode>>);
			foreach (var argumentName in argumentsTree.Keys)
			{
				if (!TryGetParameterIndex(method, argumentName, argumentOffset, out var parameterIndex))
					return false;

				var argumentNode = argumentsTree[argumentName];
				if (string.Equals(argumentNode.GetExpressionType(false), Constants.EXPRESSION_TYPE_LAMBDA, StringComparison.Ordinal))
				{
					if (lambdaArguments == null) lambdaArguments = new List<KeyValuePair<int, SyntaxTreeNode>>();
					lambdaArguments.Add(new KeyValuePair<int, SyntaxTreeNode>(parameterIndex, argumentNode));
					continue;
				}

				if (AnyBinder.TryBindInNewScope(argumentNode, bindingContext, TypeDescription.ObjectType, out var argumentValue, out _))
					Unify(method.GetParameterType(parameterIndex), argumentValue.Type, bindings);
			}

			if (lambdaArguments != null)
			{
				foreach (var lambdaArgument in lambdaArguments)
				{
					InferFromLambda(method.GetParameterType(lambdaArgument.Key), lambdaArgument.Value, bindingContext, bindings);
				}
			}

			var inferredArguments = new Type[genericParameters.Length];
			for (var i = 0; i < inferredArguments.Length; i++)
			{
				if (!bindings.TryGetValue(genericParameters[i], out inferredArguments[i]))
					return false;
			}

			genericArguments = inferredArguments;
			return true;
		}

		private static bool TryGetParameterIndex(MemberDescription method, string argumentName, int argumentOffset, out int parameterIndex)
		{
			var parametersCount = method.GetParametersCount();
			if (int.TryParse(argumentName, NumberStyles.Integer, Constants.DefaultFormatProvider, out parameterIndex))
			{
				parameterIndex += argumentOffset;
				return parameterIndex >= argumentOffset && parameterIndex < parametersCount;
			}

			for (var i = argumentOffset; i < parametersCount; i++)
			{
				if (!string.Equals(method.GetParameterName(i), argumentName, StringComparison.Ordinal)) continue;

				parameterIndex = i;
				return true;
			}

			return false;
		}

		/// <summary>
		///     Infers type parameters which only appear in the result of a lambda argument (ex. TResultT of Select) by binding
		///     the lambda's body against the already inferred types of its parameters.
		/// </summary>
		private static void InferFromLambda(Type parameterType, SyntaxTreeNode lambdaNode, BindingContext bindingContext, Dictionary<Type, Type> bindings)
		{
			if (parameterType == null || !parameterType.GetTypeInfo().ContainsGenericParameters || !IsDelegate(parameterType))
				return;

			if (!TryGetDelegateSignature(parameterType, out var delegateParameterTypes, out var delegateResultType) ||
				!delegateResultType.GetTypeInfo().ContainsGenericParameters)
				return;

			var lambdaParameterTypes = new Type[delegateParameterTypes.Length];
			for (var i = 0; i < lambdaParameterTypes.Length; i++)
			{
				lambdaParameterTypes[i] = Substitute(delegateParameterTypes[i], bindings);
				if (lambdaParameterTypes[i].GetTypeInfo().ContainsGenericParameters)
					return;
			}

			if (TryBindLambdaBody(lambdaNode, lambdaParameterTypes, bindingContext, out var bodyType))
				Unify(delegateResultType, bodyType, bindings);
		}
		private static bool TryBindLambdaBody(SyntaxTreeNode lambdaNode, Type[] lambdaParameterTypes, BindingContext bindingContext, out Type bodyType)
		{
			bodyType = null;

			try
			{
				var argumentNames = LambdaBinder.ExtractArgumentNames(lambdaNode);
				if (argumentNames.Length != lambdaParameterTypes.Length)
					return false;

				var lambdaParameters = new ParameterExpression[argumentNames.Length];
				for (var i = 0; i < lambdaParameters.Length; i++)
				{
					lambdaParameters[i] = Expression.Parameter(lambdaParameterTypes[i], argumentNames[i]);
				}

				var parameters = new List<ParameterExpression>(lambdaParameters.Length + bindingContext.Parameters.Count);
				parameters.AddRange(lambdaParameters);
				foreach (var parameter in bindingContext.Parameters)
				{
					if (Array.IndexOf(argumentNames, parameter.Name) < 0)
						parameters.Add(parameter);
				}

				var nestedBindingContext = bindingContext.CreateNestedContext(parameters.AsReadOnly(), typeof(object));
				if (!AnyBinder.TryBindInNewScope(lambdaNode.GetExpression(true), nestedBindingContext, TypeDescription.ObjectType, out var body, out _))
					return false;

				bodyType = body.Type;
				return bodyType != typeof(void);
			}
			catch (ExpressionParserException)
			{
				return false;
			}
		}

		private static void Unify(Type parameterType, Type argumentType, Dictionary<Type, Type> bindings)
		{
			if (parameterType == null || argumentType == null)
				return;

			var parameterTypeInfo = parameterType.GetTypeInfo();
			if (!parameterTypeInfo.ContainsGenericParameters)
				return;

			if (parameterType.IsGenericParameter)
			{
				if (argumentType != typeof(void) && !argumentType.GetTypeInfo().ContainsGenericParameters && !bindings.ContainsKey(parameterType))
					bindings.Add(parameterType, argumentType);
				return;
			}

			var argumentTypeInfo = argumentType.GetTypeInfo();
			if (parameterTypeInfo.IsArray)
			{
				if (argumentTypeInfo.IsArray)
					Unify(parameterTypeInfo.GetElementType(), argumentTypeInfo.GetElementType(), bindings);
				return;
			}

			if (!parameterTypeInfo.IsGenericType)
				return;

			// an argument matches ex. IEnumerable<T> through its own type, a base type or an interface: Int32[] binds T to Int32
			var genericTypeDefinition = parameterType.GetGenericTypeDefinition();
			var parameterArguments = parameterTypeInfo.GetGenericArguments();
			foreach (var candidateType in GetTypeHierarchy(argumentType))
			{
				var candidateTypeInfo = candidateType.GetTypeInfo();
				if (!candidateTypeInfo.IsGenericType || candidateType.GetGenericTypeDefinition() != genericTypeDefinition)
					continue;

				var candidateArguments = candidateTypeInfo.GetGenericArguments();
				for (var i = 0; i < parameterArguments.Length && i < candidateArguments.Length; i++)
				{
					Unify(parameterArguments[i], candidateArguments[i], bindings);
				}

				return;
			}
		}
		private static Type Substitute(Type type, Dictionary<Type, Type> bindings)
		{
			var typeInfo = type.GetTypeInfo();
			if (!typeInfo.ContainsGenericParameters)
				return type;

			if (type.IsGenericParameter)
				return bindings.TryGetValue(type, out var boundType) ? boundType : type;

			if (typeInfo.IsArray)
			{
				var elementType = Substitute(typeInfo.GetElementType(), bindings);
				return elementType.GetTypeInfo().ContainsGenericParameters ? type : elementType.MakeArrayType();
			}

			if (!typeInfo.IsGenericType)
				return type;

			var typeArguments = typeInfo.GetGenericArguments();
			var substitutedArguments = new Type[typeArguments.Length];
			for (var i = 0; i < substitutedArguments.Length; i++)
			{
				substitutedArguments[i] = Substitute(typeArguments[i], bindings);
				if (substitutedArguments[i].GetTypeInfo().ContainsGenericParameters)
					return type;
			}

			return type.GetGenericTypeDefinition().MakeGenericType(substitutedArguments);
		}

		private static IEnumerable<Type> GetTypeHierarchy(Type type)
		{
			var baseType = type;
			while (baseType != null)
			{
				yield return baseType;
				baseType = baseType.GetTypeInfo().BaseType;
			}

			foreach (var interfaceType in type.GetTypeInfo().GetImplementedInterfaces())
			{
				yield return interfaceType;
			}
		}
		private static bool TryGetDelegateSignature(Type delegateType, out Type[] parameterTypes, out Type resultType)
		{
			parameterTypes = null;
			resultType = null;

			var invokeMethod = delegateType.GetTypeInfo().GetDeclaredMethod(Constants.DELEGATE_INVOKE_NAME);
			if (invokeMethod == null)
				return false;

			var parameters = invokeMethod.GetParameters();
			parameterTypes = new Type[parameters.Length];
			for (var i = 0; i < parameterTypes.Length; i++)
			{
				parameterTypes[i] = parameters[i].ParameterType;
			}

			resultType = invokeMethod.ReturnType;
			return true;
		}
		private static bool IsDelegate(Type type)
		{
			var baseType = type.GetTypeInfo().BaseType;
			while (baseType != null)
			{
				if (baseType == typeof(MulticastDelegate))
					return true;

				baseType = baseType.GetTypeInfo().BaseType;
			}

			return false;
		}
	}
}
