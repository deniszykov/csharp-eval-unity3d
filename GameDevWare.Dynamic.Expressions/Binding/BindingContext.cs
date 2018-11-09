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
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Binding
{
	internal sealed class BindingContext
	{
		private readonly Expression global;
		private readonly ReadOnlyCollection<ParameterExpression> parameters;
		private List<Expression> nullPropagationTargets;
		private readonly Type resultType;
		private readonly ITypeResolver typeResolver;

		public ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameters; } }
		public Type ResultType { get { return this.resultType; } }
		public Expression Global { get { return this.global; } }

		public BindingContext(ITypeResolver typeResolver, ReadOnlyCollection<ParameterExpression> parameters, Type resultType, Expression global)
		{
			if (typeResolver == null) throw new ArgumentNullException("typeResolver");
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (resultType == null) throw new ArgumentNullException("resultType");

			this.typeResolver = typeResolver;
			this.parameters = parameters;
			this.resultType = resultType;
			this.global = global;
		}

		public bool TryResolveType(object typeName, out Type type)
		{
			type = default(Type);
			if (typeName == null)
				return false;

			var typeReference = default(TypeReference);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				return false;

			return type != null;
		}
		public bool TryResolveMember(object memberName, out MemberDescription member)
		{
			member = null;
			if (memberName is SyntaxTreeNode == false)
			{
				return false;
			}

			var memberNode = (SyntaxTreeNode)memberName;
			var typeName = memberNode.GetTypeName(throwOnError: false);
			var type = default(Type);
			if (typeName == null || this.TryResolveType(typeName, out type) == false)
			{
				return false;
			}

			var name = memberNode.GetName(throwOnError: false);
			var nameRef = default(TypeReference);
			if (name == null || TryGetTypeReference(name, out nameRef) == false)
			{
				return false;
			}

			var genericArguments = default(Type[]);
			if (nameRef.IsGenericType)
			{
				genericArguments = new Type[nameRef.TypeArguments.Count];
				for (var i = 0; i < genericArguments.Length; i++)
				{
					var typeArgument = nameRef.TypeArguments[i];
					if (this.TryResolveType(typeArgument, out genericArguments[i]) == false)
					{
						return false;
					}
				}
			}

			var typeDescription = TypeDescription.GetTypeDescription(type);
			var argumentNames = memberNode.GetArgumentNames(throwOnError: false);
			var members = nameRef.Name == ".ctor" ? typeDescription.Constructors : typeDescription.GetMembers(nameRef.Name);
			foreach (var declaredMember in members)
			{
				if ((declaredMember.IsMethod || declaredMember.IsConstructor) && argumentNames != null)
				{
					var paramsCount = declaredMember.GetParametersCount();
					if (paramsCount != argumentNames.Count)
					{
						continue;
					}
					else if (paramsCount > 0)
					{
						for (var i = 0; i < paramsCount; i++)
						{
							var parameter = declaredMember.GetParameter(i);
							var parameterIndex = Constants.GetIndexAsString(parameter.Position);

							var argumentName = default(string);
							if (argumentNames.TryGetValue(parameterIndex, out argumentName) == false)
							{
								break;
							}

							if (string.Equals(argumentName, parameter.Name, StringComparison.OrdinalIgnoreCase) == false &&
								string.Equals(argumentName, parameterIndex, StringComparison.OrdinalIgnoreCase) == false)
							{
								break;
							}

							if (i == paramsCount - 1)
							{
								member = declaredMember;
								return TryMakeGenericMethod(ref member, genericArguments);
							}
						}
					}
					else
					{
						member = declaredMember;
						return TryMakeGenericMethod(ref member, genericArguments);
					}
				}
				else if (nameRef.TypeArguments.Count == declaredMember.GenericArgumentsCount)
				{
					member = declaredMember;
					return TryMakeGenericMethod(ref member, genericArguments);
				}
			}
			return false;
		}
		public bool TryGetParameter(string parameterName, out Expression parameter)
		{
			if (parameterName == null) throw new ArgumentNullException("parameterName");

			// ReSharper disable once ForCanBeConvertedToForeach
			for (var i = 0; i < this.parameters.Count; i++)
			{
				parameter = this.parameters[i];
				if (string.Equals(((ParameterExpression)parameter).Name, parameterName, StringComparison.Ordinal))
					return true;
			}
			parameter = null;
			return false;
		}
		public bool IsKnownType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return this.typeResolver.IsKnownType(type);
		}

		public static bool TryGetTypeReference(object value, out TypeReference typeReference)
		{
			if (value == null) throw new ArgumentNullException("value");

			typeReference = default(TypeReference);

			if (value is TypeReference)
			{
				typeReference = (TypeReference)value;
				return true;
			}
			else if (value is SyntaxTreeNode)
			{
				var parts = new List<SyntaxTreeNode>(10);
				var current = (SyntaxTreeNode)value;
				while (current != null)
				{
					var expressionType = current.GetExpressionType(throwOnError: false);
					if (expressionType != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD &&
						expressionType != Constants.EXPRESSION_TYPE_TYPE_REFERENCE)
					{
						return false;
					}

					parts.Add(current);
					current = current.GetExpression(throwOnError: false);
				}

				var typeNameParts = default(List<string>);
				var typeArguments = default(List<TypeReference>);
				for (var p = 0; p < parts.Count; p++)
				{
					var part = parts[parts.Count - 1 - p]; // reverse order
					var arguments = part.GetTypeArguments(throwOnError: false);
					var typeNamePart = part.GetMemberName(throwOnError: true);
					if (typeNameParts == null) typeNameParts = new List<string>();
					var typeArgumentsCount = 0;
					if (arguments != null && arguments.Count > 0)
					{
						if (typeArguments == null) typeArguments = new List<TypeReference>(10);

						for (var i = 0; i < arguments.Count; i++)
						{
							var typeArgumentObj = default(object);
							var typeArgumentTypeReference = default(TypeReference);
							var key = Constants.GetIndexAsString(i);
							if (arguments.TryGetValue(key, out typeArgumentObj) == false || typeArgumentObj == null)
							{
								return false;
							}

							if (TryGetTypeReference(typeArgumentObj, out typeArgumentTypeReference) == false)
							{
								return false;
							}

							typeArguments.Add(typeArgumentTypeReference);
							typeArgumentsCount++;
						}
						typeNamePart = typeNamePart + "`" + Constants.GetIndexAsString(typeArgumentsCount);
					}
					typeNameParts.Add(typeNamePart);
				}

				if (typeNameParts == null || typeNameParts.Count == 0 || (typeNameParts.Count == 1 && string.IsNullOrEmpty(typeNameParts[0])))
				{
					typeReference = TypeReference.Empty;
				}
				else
				{
					typeReference = new TypeReference(typeNameParts, typeArguments ?? TypeReference.EmptyTypeArguments);
				}
				return true;
			}
			else
			{
				var typeName = Convert.ToString(value, Constants.DefaultFormatProvider);
				if (string.IsNullOrEmpty(typeName))
				{
					typeReference = TypeReference.Empty;
				}
				else
				{
					typeReference = new TypeReference(new[] { typeName }, TypeReference.EmptyTypeArguments);
				}
				return true;
			}
		}
		public static bool TryGetMethodReference(object value, out TypeReference methodReference)
		{
			if (value == null) throw new ArgumentNullException("value");

			methodReference = default(TypeReference);

			if (value is TypeReference)
			{
				methodReference = (TypeReference)value;
				return true;
			}
			else if (value is SyntaxTreeNode)
			{
				var typeArguments = default(List<TypeReference>);
				var methodNameTree = (SyntaxTreeNode)value;

				var arguments = methodNameTree.GetTypeArguments(throwOnError: false);
				var methodName = methodNameTree.GetMemberName(throwOnError: true);
				if (arguments != null && arguments.Count > 0)
				{
					typeArguments = new List<TypeReference>(10);

					for (var i = 0; i < arguments.Count; i++)
					{

						var typeArgumentObj = default(object);
						var typeArgumentTypeReference = default(TypeReference);
						var key = Constants.GetIndexAsString(i);
						if (arguments.TryGetValue(key, out typeArgumentObj) == false || typeArgumentObj == null)
						{
							return false; // cant get argument 
						}

						if (TryGetTypeReference(typeArgumentObj, out typeArgumentTypeReference) == false)
						{
							return false; // type resolution failed
						}

						if (ReferenceEquals(typeArgumentTypeReference, TypeReference.Empty))
						{
							return false; // no open generic methods are allowed
						}

						typeArguments.Add(typeArgumentTypeReference);
					}
				}

				methodReference = new TypeReference(new[] { methodName }, typeArguments ?? TypeReference.EmptyTypeArguments);
				return true;
			}
			else
			{
				methodReference = new TypeReference(new[] { Convert.ToString(value, Constants.DefaultFormatProvider) }, TypeReference.EmptyTypeArguments);
				return true;
			}
		}

		public BindingContext CreateNestedContext()
		{
			return new BindingContext(this.typeResolver, this.parameters, this.resultType, this.global);
		}
		public BindingContext CreateNestedContext(ReadOnlyCollection<ParameterExpression> newParameters, Type resultType)
		{
			if (newParameters == null) throw new ArgumentNullException("newParameters");
			if (resultType == null) throw new ArgumentNullException("resultType");

			return new BindingContext(this.typeResolver, newParameters, resultType, this.global);
		}

		public void RegisterNullPropagationTarget(Expression target)
		{
			if (target == null) throw new ArgumentNullException("target");

			if (this.nullPropagationTargets == null)
				this.nullPropagationTargets = new List<Expression>();
			this.nullPropagationTargets.Add(target);
		}
		public void CompleteNullPropagation(ref Expression expression)
		{
			if (expression == null || this.nullPropagationTargets == null || this.nullPropagationTargets.Count == 0)
				return;

			expression = ExpressionUtils.MakeNullPropagationExpression(this.nullPropagationTargets, expression);
		}
		private static bool TryMakeGenericMethod(ref MemberDescription methodDescription, Type[] typeArguments)
		{
			if (methodDescription == null) throw new ArgumentNullException("methodDescription");

			try
			{
				if (typeArguments != null)
					methodDescription = methodDescription.MakeGenericMethod(typeArguments);

				Debug.Assert(methodDescription != null, "methodDescription != null");

				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
