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
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	public class ExpressionBuilder
	{
		private static readonly Dictionary<Type, ReadOnlyCollection<MemberInfo>> InstanceMembersByType = new Dictionary<Type, ReadOnlyCollection<MemberInfo>>();
		private static readonly Dictionary<Type, ReadOnlyCollection<MemberInfo>> StaticMembersByType = new Dictionary<Type, ReadOnlyCollection<MemberInfo>>();
		private static readonly ILookup<string, MethodInfo> ExpressionConstructors;
		private static readonly string[] OperationWithPromotionForBothOperand;
		private static readonly string[] OperationWithPromotionForFirstOperand;
		private static readonly TypeCode[] SignedIntegerTypes;
		private static readonly TypeCode[] UnsignedIntegerTypes;
		private static readonly TypeCode[] Numeric;
		public static ITypeResolver DefaultTypeResolver = null;

		private readonly ReadOnlyCollection<ParameterExpression> parameters;
		private readonly Type contextType;
		private readonly Type resultType;
		private readonly ITypeResolver typeResolver;

		public ReadOnlyCollection<ParameterExpression> Parameters { get { return this.parameters; } }
		public Type ResultType { get { return this.resultType; } }
		public Type ContextType { get { return this.contextType; } }

		static ExpressionBuilder()
		{
			ExpressionConstructors = typeof(Expression)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(m => typeof(Expression).IsAssignableFrom(m.ReturnType))
				.ToLookup(m => m.Name);

			OperationWithPromotionForBothOperand = new[]
			{
				"Add", "AddChecked", "And", "Coalesce", "Condition", "Divide", "ExclusiveOr", "Equal",  "GreaterThan", "GreaterThanOrEqual",
				"LessThan", "LessThanOrEqual", "Modulo", "Multiply", "MultiplyChecked", "NotEqual", "Or", "Subtract", "SubtractChecked"
			};
			Array.Sort(OperationWithPromotionForBothOperand);
			OperationWithPromotionForFirstOperand = new[]
			{
				"LeftShift", "RightShift", "Negate", "Complement"
			};
			Array.Sort(OperationWithPromotionForFirstOperand);

			SignedIntegerTypes = new[] { TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64 };
			UnsignedIntegerTypes = new[] { TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64 };
			Numeric = new[]
			{
				TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64,
				TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64,
				TypeCode.Single, TypeCode.Double, TypeCode.Decimal,
			};
			Array.Sort(Numeric);
			Array.Sort(SignedIntegerTypes);
			Array.Sort(UnsignedIntegerTypes);
		}
		public ExpressionBuilder(IList<ParameterExpression> parameters, Type resultType, Type contextType = null, ITypeResolver typeResolver = null)
		{
			if (resultType == null) throw new ArgumentNullException("resultType");
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (typeResolver == null) typeResolver = new KnownTypeResolver(parameters.Select(p => p.Type), DefaultTypeResolver);

			if (parameters is ReadOnlyCollection<ParameterExpression> == false)
				parameters = new ReadOnlyCollection<ParameterExpression>(parameters);

			this.parameters = (ReadOnlyCollection<ParameterExpression>)parameters;
			this.resultType = resultType;
			this.contextType = contextType;
			this.typeResolver = typeResolver;

		}

		public Expression Build(ExpressionTree node, Expression context = null)
		{
			// lambda binding substitution feature
			if (node.GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA && typeof(Delegate).IsAssignableFrom(resultType) == false)
			{
				var callParameters = new Expression[this.parameters.Count];
				var lambdaTypeArgs = new Type[this.parameters.Count + 1];
				for (var i = 0; i < this.parameters.Count; i++)
				{
					lambdaTypeArgs[i] = this.parameters[i].Type;
					callParameters[i] = this.parameters[i];
				}
				lambdaTypeArgs[lambdaTypeArgs.Length - 1] = this.resultType;
				var lambdaExpr = (LambdaExpression)this.BuildLambda(node, Expression.GetFuncType(lambdaTypeArgs), context);
				var substitution = new Dictionary<Expression, Expression>();
				for (var i = 0; i < this.parameters.Count; i++)
					substitution[lambdaExpr.Parameters[i]] = this.parameters[i];
				return ExpressionSubstitutor.Visit(lambdaExpr.Body, substitution);
			}

			return Build(node, context, this.resultType, this.resultType);
		}
		private Expression Build(ExpressionTree node, Expression context, Type expectedType, Type typeHint)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionType = node.GetExpressionType(throwOnError: true);
			try
			{
				var expression = default(Expression);
				switch (expressionType)
				{
					case Constants.EXPRESSION_TYPE_INVOKE: expression = BuildInvoke(node, context); break;
					case Constants.EXPRESSION_TYPE_LAMBDA: expression = BuildLambda(node, typeHint, context); break;
					case Constants.EXPRESSION_TYPE_INDEX: expression = BuildIndex(node, context); break;
					case "Enclose":
					case Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_CHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_GROUP: expression = BuildGroup(node, context); break;
					case Constants.EXPRESSION_TYPE_CONSTANT: expression = BuildConstant(node); break;
					case Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD: expression = BuildPropertyOrField(node, context); break;
					case Constants.EXPRESSION_TYPE_TYPEOF: expression = BuildTypeOf(node); break;
					case Constants.EXPRESSION_TYPE_DEFAULT: expression = BuildDefault(node); break;
					case Constants.EXPRESSION_TYPE_NEW: expression = BuildNew(node, context); break;
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS: expression = BuildNewArrayBounds(node, context); break;
					default: expression = BuildByType(node, context); break;
				}

				if (expectedType != null && expression.Type != expectedType)
					expression = Expression.ConvertChecked(expression, expectedType);

				return expression;
			}
			catch (ExpressionParserException)
			{
				throw;
			}
			catch (System.Threading.ThreadAbortException)
			{
				throw;
			}
			catch (Exception exception)
			{
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_BUILDFAILED, expressionType, exception.Message), exception, node);
			}
		}

		private Expression BuildByType(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionType = (string)node[Constants.EXPRESSION_TYPE_ATTRIBUTE];
			if (expressionType == Constants.EXPRESSION_TYPE_COMPLEMENT)
				expressionType = Constants.EXPRESSION_TYPE_NOT;

			if (ExpressionConstructors.Contains(expressionType) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType), node);

			var argumentNames = new HashSet<string>(node.Keys, StringComparer.Ordinal);
			argumentNames.Remove(Constants.EXPRESSION_TYPE_ATTRIBUTE);
			argumentNames.RemoveWhere(e => e.StartsWith("$", StringComparison.Ordinal));
			foreach (var method in ExpressionConstructors[expressionType].OrderBy(m => m.GetParameters().Length))
			{
				var parameterNames = new HashSet<string>(method.GetParameters().Select(p => p.Name), StringComparer.Ordinal);
				if (argumentNames.IsSubsetOf(parameterNames) == false)
					continue;

				var methodParameters = method.GetParameters();
				var methodArguments = new object[methodParameters.Length];
				var index = 0;
				foreach (var methodParameter in methodParameters)
				{
					var argument = default(object);
					if (node.TryGetValue(methodParameter.Name, out argument))
					{
						if (argument != null && methodParameter.ParameterType == typeof(Type))
						{
							var typeReference = default(TypeReference);
							var type = default(Type);
							if (TryGetTypeReference(argument, out typeReference) && this.typeResolver.TryGetType(typeReference, out type))
								argument = type;
							else
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? argument), node);
						}
						else if (argument is ExpressionTree)
							argument = Build((ExpressionTree)argument, context, expectedType: null, typeHint: methodParameter.ParameterType);
						else if (argument != null)
							argument = ChangeType(argument, methodParameter.ParameterType);
						else if (methodParameter.ParameterType.IsValueType)
							argument = GetDefaultValue(methodParameter.ParameterType);

						methodArguments[index] = argument;
					}
					else
					{
						methodArguments[index] = GetDefaultValue(methodParameter.ParameterType);
					}

					index++;
				}

				if (Array.BinarySearch(OperationWithPromotionForBothOperand, expressionType) >= 0)
					PromoteBothArguments(method, methodArguments);
				if (Array.BinarySearch(OperationWithPromotionForFirstOperand, expressionType) >= 0)
					PromoteFirstArgument(method, methodArguments);

				try
				{
					if
					(
						methodArguments.Length == 2 &&
						methodArguments[0] is Expression &&
						methodArguments[1] is Expression &&
						(
							((Expression)methodArguments[0]).Type == typeof(string) ||
							((Expression)methodArguments[1]).Type == typeof(string)
						) &&
						(string.Equals(expressionType, Constants.EXPRESSION_TYPE_ADD, StringComparison.Ordinal) || string.Equals(expressionType, Constants.EXPRESSION_TYPE_ADD_CHECKED, StringComparison.Ordinal))
					)
					{
						var concatArguments = new Expression[]
						{
							Expression.Convert((Expression) methodArguments[0], typeof (object)),
							Expression.Convert((Expression) methodArguments[1], typeof (object))
						};
						return Expression.Call(typeof(string), "Concat", Type.EmptyTypes, concatArguments);
					}
					// fixing bug in mono expression compiler: Negate on float or double = exception
					else if
					(
						methodArguments.Length == 1 &&
						methodArguments[0] is Expression &&
						(
							((Expression)methodArguments[0]).Type == typeof(float) ||
							((Expression)methodArguments[0]).Type == typeof(double)
						) &&
						(string.Equals(expressionType, Constants.EXPRESSION_TYPE_NEGATE, StringComparison.Ordinal) || string.Equals(expressionType, Constants.EXPRESSION_TYPE_NEGATE_CHECKED, StringComparison.Ordinal))
					)
					{
						var operand = (Expression)methodArguments[0];
						var negativeConst = operand.Type == typeof(float) ? Expression.Constant(-1.0f) : Expression.Constant(-1.0d);

						return Expression.Multiply(operand, negativeConst);
					}
					// fix Power variants
					else if
					(
						methodArguments.Length == 2 &&
						methodArguments[0] is Expression &&
						methodArguments[1] is Expression &&
						(
							((Expression)methodArguments[0]).Type != typeof(double) ||
							((Expression)methodArguments[1]).Type != typeof(double)
						) &&
						string.Equals(expressionType, Constants.EXPRESSION_TYPE_POWER, StringComparison.Ordinal)
					)
					{
						return Expression.ConvertChecked
						(
							expression: Expression.Power
							(
								left: Expression.ConvertChecked((Expression)methodArguments[0], typeof(double)),
								right: Expression.ConvertChecked((Expression)methodArguments[1], typeof(double))
							),
							type: ((Expression)methodArguments[0]).Type
						);
					}
					else
					{
						return (Expression)method.Invoke(null, methodArguments);
					}
				}
				catch (TargetInvocationException te)
				{
					throw new ExpressionParserException(te.InnerException.Message, te.InnerException, node);
				}
			}
			throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOCREATEEXPRWITHPARAMS, expressionType, string.Join(", ", argumentNames.ToArray())), node);
		}
		private Expression BuildGroup(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expression = node.GetExpression(throwOnError: true);

			return Build(expression, context, expectedType: null, typeHint: null);
		}
		private Expression BuildConstant(ExpressionTree node)
		{
			if (node == null) throw new ArgumentNullException("node");

			var valueObj = node.GetValue(throwOnError: true);
			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			var value = ChangeType(valueObj, type);
			return Expression.Constant(value);
		}
		private Expression BuildPropertyOrField(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

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
				expression = Build(target, context, expectedType: null, typeHint: null);
			}

			if (expression == null && type == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName), node);

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

			if (expression == null && memberAccessExpression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName), node);
			else if (memberAccessExpression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type), node);

			if (useNullPropagation && expression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, type));

			if (useNullPropagation)
				return MakeNullPropagationExpression(expression, memberAccessExpression);
			else
				return memberAccessExpression;
		}
		private Expression BuildIndex(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);
			var arguments = node.GetArguments(throwOnError: true);
			var target = node.GetExpression(throwOnError: true);
			var expression = Build(target, context, expectedType: null, typeHint: null);
			var indexExpression = default(Expression);
			if (expression.Type.IsArray)
			{
				var indexingExpressions = new Expression[arguments.Count];
				for (var i = 0; i < indexingExpressions.Length; i++)
				{
					var argName = Constants.GetIndexAsString(i);
					var argument = default(ExpressionTree);
					if (arguments.TryGetValue(argName, out argument))
						indexingExpressions[i] = Build(argument, context, expectedType: typeof(int), typeHint: typeof(int));
				}

				try
				{
					if (indexingExpressions.Length == 1 && indexingExpressions[0] != null)
						indexExpression = Expression.ArrayIndex(expression, indexingExpressions[0]);
					else if (indexingExpressions.Length > 1 && Array.TrueForAll(indexingExpressions, a => a != null))
						indexExpression = Expression.ArrayIndex(expression, indexingExpressions);
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}
			else
			{
				var properties = expression.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				Array.Sort(properties, (x, y) => x.GetIndexParameters().Length.CompareTo(y.GetIndexParameters().Length));
				foreach (var property in properties)
				{
					var indexerParameters = property.GetIndexParameters();
					if (indexerParameters.Length == 0) continue;

					var getMethod = property.GetGetMethod(nonPublic: false);
					var argumentExpressions = default(Expression[]);
					if (getMethod == null || TryBindMethod(indexerParameters, arguments, context, out argumentExpressions) <= 0)
						continue;

					try
					{
						indexExpression = Expression.Call(expression, getMethod, argumentExpressions);
					}
					catch (Exception exception)
					{
						throw new ExpressionParserException(exception.Message, exception, node);
					}
				}
			}
			if (indexExpression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDINDEXER, expression.Type), node);

			if (useNullPropagation)
				return MakeNullPropagationExpression(expression, indexExpression);
			else
				return indexExpression;
		}
		private Expression BuildCall(ExpressionTree node, ExpressionTree target, bool useNullPropagation, ReadOnlyDictionary<string, ExpressionTree> arguments, TypeReference methodRef, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (target == null) throw new ArgumentNullException("target");
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (methodRef == null) throw new ArgumentNullException("methodRef");

			if (target == null && context == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, methodRef.Name), node);

			var expression = default(Expression);
			var typeReference = default(TypeReference);
			var type = default(Type);
			var isStatic = true;
			if (TryGetTypeReference(target, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
			{
				expression = Build(target, context, expectedType: null, typeHint: null);
				type = expression.Type;
				isStatic = false;
			}

			var quality = 0.0f;
			var callExpression = default(MethodCallExpression);
			var isGenericRequired = methodRef.TypeArguments.Count > 0;
			var genericArguments = default(Type[]);
			if (isGenericRequired)
			{
				genericArguments = new Type[methodRef.TypeArguments.Count];
				for (var i = 0; i < genericArguments.Length; i++)
				{
					var typeArgument = methodRef.TypeArguments[i];
					if (this.typeResolver.TryGetType(typeArgument, out genericArguments[i]) == false)
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeArgument), node);
				}
			}

			foreach (var member in GetMembers(type, isStatic))
			{
				var method = member as MethodInfo;
				if (method == null) continue;
				if (methodRef.Name != method.Name) continue;
				if (method.IsGenericMethod != isGenericRequired) continue;
				if (isGenericRequired && method.GetGenericArguments().Length != methodRef.TypeArguments.Count) continue;

				if (method.IsGenericMethod && genericArguments != null)
				{
					if (!method.IsGenericMethodDefinition) method = method.GetGenericMethodDefinition();
					try
					{
						method = method.MakeGenericMethod(genericArguments);
					}
					catch (ArgumentException) { continue; /* An element of typeArguments does not satisfy the constraints specified for the corresponding type parameter of the current generic method definition. */  }
				}

				var methodParameters = method.GetParameters();
				var argumentExpressions = default(Expression[]);
				var methodQuality = TryBindMethod(methodParameters, arguments, context, out argumentExpressions);
				if (float.IsNaN(methodQuality) || methodQuality <= quality)
					continue;

				try
				{
					callExpression = expression == null ?
						Expression.Call(method, argumentExpressions) : // static call
						Expression.Call(expression, method, argumentExpressions); // instance call
					quality = methodQuality;
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}

			if (callExpression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDCALL, methodRef.Name, type), node);

			if (useNullPropagation && expression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOAPPLYNULLCONDITIONALOPERATORONTYPEREF, type));

			if (useNullPropagation)
				return MakeNullPropagationExpression(expression, callExpression);
			else
				return callExpression;
		}
		private Expression BuildInvoke(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var target = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var targetExpressionType = target.GetExpressionType(throwOnError: true);
			var expression = default(Expression);

			if (targetExpressionType == Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
			{
				var propertyOrFieldTarget = target.GetExpression(throwOnError: false);
				var useNullPropagation = target.GetUseNullPropagation(throwOnError: true);

				var typeReference = default(TypeReference);
				var type = default(Type);
				var isStatic = true;
				if (propertyOrFieldTarget == null || TryGetTypeReference(propertyOrFieldTarget, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				{
					try
					{
						var propertyOrFieldExpression = propertyOrFieldTarget != null ? Build(propertyOrFieldTarget, context, expectedType: null, typeHint: null) : context;
						if (propertyOrFieldExpression != null)
						{
							type = propertyOrFieldExpression.Type;
							isStatic = false;
						}
					}
					catch (ExpressionParserException)
					{
						if (typeReference != null) // throw better error message about wrong type reference
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference), node);
						throw;

					}
				}
				var methodRef = default(TypeReference);
				if (type != null && TryGetMethodReference(target, out methodRef) && GetMembers(type, isStatic).Any(m => m is MethodInfo && m.Name == methodRef.Name))
					return this.BuildCall(node, propertyOrFieldTarget, useNullPropagation, arguments, methodRef, context);
			}

			expression = Build(target, context, expectedType: null, typeHint: null);

			if (typeof(Delegate).IsAssignableFrom(expression.Type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOINVOKENONDELEG, expression.Type), node);

			var method = expression.Type.GetMethod(Constants.DELEGATE_INVOKE_NAME);
			if (method == null) throw new MissingMethodException(expression.Type.FullName, Constants.DELEGATE_INVOKE_NAME);
			var methodParameters = method.GetParameters();
			var argumentExpressions = default(Expression[]);
			if (TryBindMethod(methodParameters, arguments, context, out argumentExpressions) <= 0)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDDELEG, expression.Type, string.Join(", ", Array.ConvertAll(methodParameters, p => p.ParameterType.Name))), node);

			try
			{
				return Expression.Invoke(expression, argumentExpressions);
			}
			catch (Exception exception)
			{
				throw new ExpressionParserException(exception.Message, exception, node);
			}
		}
		private Expression BuildDefault(ExpressionTree node)
		{
			if (node == null) throw new ArgumentNullException("node");

			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			return DefaultExpression(type);
		}
		private Expression BuildTypeOf(ExpressionTree node)
		{
			if (node == null) throw new ArgumentNullException("node");

			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			return Expression.Constant(type, typeof(Type));
		}
		private Expression BuildNewArrayBounds(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			var arguments = node.GetArguments(throwOnError: true);
			var argumentExpressions = new Expression[arguments.Count];
			for (var i = 0; i < arguments.Count; i++)
			{
				var key = Constants.GetIndexAsString(i);
				var argument = default(ExpressionTree);
				if (arguments.TryGetValue(key, out argument) == false)
					throw new ExpressionParserException(Properties.Resources.EXCEPTION_BOUNDEXPR_ARGSDOESNTMATCHPARAMS, node);
				argumentExpressions[i] = Build(argument, context, typeof(int), typeHint: typeof(int));
			}

			return Expression.NewArrayBounds(type, argumentExpressions);
		}
		private Expression BuildNew(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var arguments = node.GetArguments(throwOnError: false);
			var typeName = node.GetTypeName(throwOnError: true);
			var typeReference = default(TypeReference);
			var type = default(Type);
			if (TryGetTypeReference(typeName, out typeReference) == false || this.typeResolver.TryGetType(typeReference, out type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVETYPE, typeReference ?? typeName), node);

			var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			Array.Sort(constructors, (x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));

			// feature: lambda building via new Func()
			var lambdaArgument = default(ExpressionTree);
			if (typeof(Delegate).IsAssignableFrom(type) && arguments.Count == 1 && (lambdaArgument = arguments.Values.Single()).GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_LAMBDA)
				return BuildLambda(lambdaArgument, type, context);

			foreach (var constructorInfo in constructors)
			{
				var methodParameters = constructorInfo.GetParameters();
				var argumentExpressions = default(Expression[]);
				if (TryBindMethod(methodParameters, arguments, context, out argumentExpressions) <= 0)
					continue;

				try
				{
					return Expression.New(constructorInfo, argumentExpressions);
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}
			throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDCONSTRUCTOR, type), node);
		}
		private Expression BuildLambda(ExpressionTree node, Type lambdaType, Expression context)
		{
			if (lambdaType == null || typeof(Delegate).IsAssignableFrom(lambdaType) == false || lambdaType.ContainsGenericParameters)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_VALIDDELEGATETYPEISEXPECTED, lambdaType != null ? lambdaType.ToString() : "<null>"));

			var expressionType = node.GetExpressionType(throwOnError: true);
			var expression = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var lambdaInvokeMethod = lambdaType.GetMethod(Constants.DELEGATE_INVOKE_NAME, BindingFlags.Public | BindingFlags.Instance);
			if (lambdaInvokeMethod == null) throw new MissingMethodException(lambdaType.FullName, Constants.DELEGATE_INVOKE_NAME);
			var lambdaInvokeMethodParameters = lambdaInvokeMethod.GetParameters();
			if (lambdaInvokeMethodParameters.Length != arguments.Count)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_INVALIDLAMBDAARGUMENTS, lambdaType));
			var argumentNames = new string[arguments.Count];
			for (var i = 0; i < argumentNames.Length; i++)
			{
				var argumentNameTree = default(ExpressionTree);
				if (arguments.TryGetValue(Constants.GetIndexAsString(i), out argumentNameTree) == false || argumentNameTree == null || argumentNameTree.GetExpressionType(throwOnError: true) != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType), node);
				argumentNames[i] = argumentNameTree.GetPropertyOrFieldName(throwOnError: true);
			}
			var lambdaParameters = new ParameterExpression[lambdaInvokeMethodParameters.Length];
			for (var i = 0; i < lambdaInvokeMethodParameters.Length; i++)
				lambdaParameters[i] = Expression.Parameter(lambdaInvokeMethodParameters[i].ParameterType, argumentNames[i]);

			var builderParameters = new List<ParameterExpression>(lambdaParameters.Length + this.parameters.Count);
			// add all lambda's parameters
			builderParameters.AddRange(lambdaParameters);
			// add closure parameters
			foreach (var parameterExpr in this.parameters)
				if (Array.IndexOf(argumentNames, parameterExpr.Name) < 0)
					builderParameters.Add(parameterExpr);
			// create builder and bind body
			var builder = new ExpressionBuilder(builderParameters, lambdaInvokeMethod.ReturnType, this.contextType, this.typeResolver);
			var body = builder.Build(expression, context, lambdaInvokeMethod.ReturnType, typeHint: lambdaInvokeMethod.ReturnType);

			return Expression.Lambda(lambdaType, body, lambdaParameters);
		}

		private float TryBindMethod(ParameterInfo[] methodParameters, ReadOnlyDictionary<string, ExpressionTree> arguments, Expression context, out Expression[] callArguments)
		{
			callArguments = null;

			// check argument count
			if (arguments.Count > methodParameters.Length)
				return 0; // not all arguments are bound to parameters

			var requiredParametersCount = methodParameters.Length - methodParameters.Count(p => p.IsOptional);
			if (arguments.Count < requiredParametersCount)
				return 0; // not all required parameters has values

			// bind arguments
			var parametersByName = methodParameters.ToDictionary(p => p.Name);
			var parametersByPos = methodParameters.ToDictionary(p => p.Position);
			var argumentNames = arguments.Keys.ToArray();
			var parametersQuality = new float[methodParameters.Length];

			callArguments = new Expression[methodParameters.Length];
			foreach (var argName in argumentNames)
			{
				var parameter = default(ParameterInfo);
				var parameterIndex = 0;
				if (argName.All(char.IsDigit))
				{
					parameterIndex = int.Parse(argName, Constants.DefaultFormatProvider);
					if (parametersByPos.TryGetValue(parameterIndex, out parameter) == false)
						return 0; // position out of range

					if (arguments.ContainsKey(parameter.Name))
						return 0; // positional intersects named
				}
				else
				{
					if (parametersByName.TryGetValue(argName, out parameter) == false)
						return 0; // parameter is not found
					parameterIndex = parameter.Position;
				}

				var expectedType = parameter.ParameterType;
				var argValue = this.Build(arguments[argName], context, expectedType: null, typeHint: expectedType);
				var quality = TryCastTo(expectedType, ref argValue);

				if (quality > 0)
				{
					parametersQuality[parameterIndex] = quality; // casted
					callArguments[parameterIndex] = argValue;
					continue;
				}

				return 0;
			}

			for (var i = 0; i < callArguments.Length; i++)
			{
				if (callArguments[i] != null) continue;
				var parameter = parametersByPos[i];
				if (parameter.IsOptional == false)
					return 0; // missing required parameter

				callArguments[i] = Expression.Constant(GetDefaultValue(parameter.ParameterType), parameter.ParameterType);
			}

			if (parametersQuality.Length == 0)
				return 1;

			var qualitySum = 0.0f;
			foreach (var value in parametersQuality)
				qualitySum += value;

			return qualitySum / parametersQuality.Length;
		}
		private static float TryCastTo(Type expectedType, ref Expression expression)
		{
			var actualType = expression.Type;

			if (actualType == expectedType)
				return 1.0f;

			// 1: check if types are convertible
			// 2: check if value is constant and could be converted

			if (IsHeirOf(actualType, expectedType))
			{
				expression = Expression.Convert(expression, expectedType);
				return 0.9f; // same type hierarchy
			}

			// convert to/from enum, nullable
			var nullableUnderlyingType = Nullable.GetUnderlyingType(expectedType);
			if ((expectedType.IsEnum && Enum.GetUnderlyingType(expectedType) == actualType) ||
				(actualType.IsEnum && Enum.GetUnderlyingType(actualType) == expectedType) ||
				(nullableUnderlyingType != null && nullableUnderlyingType == actualType))
			{
				expression = Expression.Convert(expression, expectedType);
				return 0.9f; // same type hierarchy
			}

			// implicit convertion on expectedType
			var implicitConvertion = expectedType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new[] { actualType }, null);
			if (implicitConvertion != null && implicitConvertion.ReturnType == expectedType)
			{
				expression = Expression.Convert(expression, expectedType, implicitConvertion);
				return 0.5f; // converted with operator
			}

			// implicit convertion on actualType
			implicitConvertion = actualType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new[] { actualType }, null);
			if (implicitConvertion != null && implicitConvertion.ReturnType == expectedType)
			{
				expression = Expression.Convert(expression, expectedType, implicitConvertion);
				return 0.5f; // converted with operator
			}

			// try to convert value of constant
			var constantValue = default(object);
			var constantType = default(Type);
			if (!TryExposeConstant(expression, out constantValue, out constantType))
				return 0.0f;

			if (constantValue == null)
			{
				if (constantType == typeof(object) && !expectedType.IsValueType)
				{
					expression = Expression.Constant(null, expectedType);
					return 1.0f; // exact type (null)
				}
				else
				{
					return 0.0f;
				}
			}

			var expectedTypeCode = Type.GetTypeCode(expectedType);
			var constantTypeCode = Type.GetTypeCode(constantType);
			var convertibleToExpectedType = default(bool);
			// ReSharper disable RedundantCast
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (expectedTypeCode)
			{
				case TypeCode.Byte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)byte.MinValue, (ulong)byte.MaxValue); break;
				case TypeCode.SByte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)sbyte.MinValue, (ulong)sbyte.MaxValue); break;
				case TypeCode.UInt16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)ushort.MinValue, ushort.MaxValue); break;
				case TypeCode.Int16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)short.MinValue, (ulong)short.MaxValue); break;
				case TypeCode.UInt32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)uint.MinValue, uint.MaxValue); break;
				case TypeCode.Int32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)int.MinValue, (ulong)int.MaxValue); break;
				case TypeCode.UInt64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)ulong.MinValue, ulong.MaxValue); break;
				case TypeCode.Int64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)long.MinValue, (ulong)long.MaxValue); break;
				case TypeCode.Char:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Single: convertibleToExpectedType = Array.BinarySearch(SignedIntegerTypes, constantTypeCode) >= 0 || Array.BinarySearch(UnsignedIntegerTypes, constantTypeCode) >= 0; break;
				default: convertibleToExpectedType = false; break;
			}
			// ReSharper restore RedundantCast

			if (convertibleToExpectedType)
			{
				expression = Expression.Constant(Convert.ChangeType(constantValue, expectedTypeCode, Constants.DefaultFormatProvider));
				return 0.7f; // converted in-place
			}

			return 0.0f;
		}
		private static ReadOnlyCollection<MemberInfo> GetMembers(Type type, bool isStatic)
		{
			var members = default(ReadOnlyCollection<MemberInfo>);
			if (isStatic)
			{
				lock (StaticMembersByType)
					if (StaticMembersByType.TryGetValue(type, out members))
						return members;
			}
			else
			{
				lock (InstanceMembersByType)
					if (InstanceMembersByType.TryGetValue(type, out members))
						return members;
			}

			var bindingFlags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public;

			var methods = default(List<MethodInfo>);
			var properties = new List<PropertyInfo>();
			var field = new List<FieldInfo>();
			if (type.IsInterface)
			{
				methods = new List<MethodInfo>();
				properties = new List<PropertyInfo>();
				field = new List<FieldInfo>();
				methods.AddRange(type.GetMethods(bindingFlags));
				properties.AddRange(type.GetProperties(bindingFlags));
				foreach (var @interface in type.GetInterfaces())
				{
					methods.AddRange(@interface.GetMethods(bindingFlags));
					properties.AddRange(@interface.GetProperties(bindingFlags));
				}
			}
			else
			{
				methods = new List<MethodInfo>(type.GetMethods(bindingFlags));
				properties = new List<PropertyInfo>(type.GetProperties(bindingFlags));
				field = new List<FieldInfo>(type.GetFields(bindingFlags));
			}
			methods.Sort((x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));

			var membersList = new List<MemberInfo>(methods.Count + properties.Count + field.Count);
			membersList.AddRange(methods.Cast<MemberInfo>());
			membersList.AddRange(properties.Cast<MemberInfo>());
			membersList.AddRange(field.Cast<MemberInfo>());
			members = new ReadOnlyCollection<MemberInfo>(membersList);

			if (isStatic)
				lock (StaticMembersByType) StaticMembersByType[type] = members;
			else
				lock (InstanceMembersByType) InstanceMembersByType[type] = members;

			return members;
		}

		private static bool IsHeirOf(Type actualType, Type expectedType)
		{
			if (actualType == null) throw new ArgumentNullException("actualType");
			if (expectedType == null) throw new ArgumentNullException("expectedType");

			if (expectedType == typeof(object))
				return true;

			if (expectedType == actualType)
				return true; // is required type

			if (expectedType.IsInterface && Array.IndexOf(actualType.GetInterfaces(), expectedType) != -1)
				return true; // has required interface

			var baseType = actualType;
			while (baseType != null)
			{
				if (baseType == expectedType)
					return true; // inherits from expected type

				baseType = baseType.BaseType == baseType ? null : baseType.BaseType;
			}

			return false;
		}
		private static bool IsInRange(object value, TypeCode valueTypeCode, long minValue, ulong maxValue)
		{
			if (Array.BinarySearch(SignedIntegerTypes, valueTypeCode) >= 0)
			{
				var signedValue = Convert.ToInt64(value, Constants.DefaultFormatProvider);
				if (signedValue >= minValue && signedValue >= 0 && unchecked((ulong)signedValue) <= maxValue)
					return true;
			}
			else if (Array.BinarySearch(UnsignedIntegerTypes, valueTypeCode) >= 0)
			{
				var unsignedValue = Convert.ToUInt64(value, Constants.DefaultFormatProvider);
				if (unsignedValue <= maxValue)
					return true;
			}
			return false;
		}

		private static bool IsNullableType(Type type)
		{
			if (type.IsValueType == false)
				return true;
			else if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return true;
			else
				return type.IsValueType == false;
		}
		private static bool TryExposeConstant(Expression expression, out object constantValue, out Type constantType)
		{
			// unwrap conversions
			var convertExpression = expression as UnaryExpression;
			while (convertExpression != null && (convertExpression.NodeType == ExpressionType.Convert || convertExpression.NodeType == ExpressionType.ConvertChecked))
			{
				expression = convertExpression.Operand;
				convertExpression = expression as UnaryExpression;
			}

			constantValue = null;
			constantType = null;
			var constantExpression = expression as ConstantExpression;
			if (constantExpression == null)
				return false;

			constantType = constantExpression.Type;
			constantValue = constantExpression.Value;

			var constantNullableUnderlyingType = constantExpression.Type.IsValueType ? Nullable.GetUnderlyingType(constantExpression.Type) : null;
			if (constantNullableUnderlyingType != null)
				constantType = constantNullableUnderlyingType;

			return true;
		}

		private static object ChangeType(object value, Type toType)
		{
			if (toType == null) throw new ArgumentNullException("toType");

			if (toType.IsEnum)
				return Enum.Parse(toType, Convert.ToString(value, Constants.DefaultFormatProvider));
			else
				return Convert.ChangeType(value, toType, Constants.DefaultFormatProvider);
		}
		private static object GetDefaultValue(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var underlyingNullableType = Nullable.GetUnderlyingType(type);
			if (underlyingNullableType != null)
				return null;
			else if (type.IsValueType)
				return Activator.CreateInstance(type);
			else
				return null;
		}
		private static void PromoteBothArguments(MethodInfo method, object[] methodArguments)
		{
			if (method == null) throw new ArgumentNullException("method");
			if (methodArguments == null) throw new ArgumentNullException("methodArguments");


			var originalLeft = default(Expression);
			var originalRight = default(Expression);
			var leftIdx = -1;
			var rightIdx = -1;
			foreach (var parameter in method.GetParameters())
			{
				switch (parameter.Name)
				{
					case "left":
					case "ifTrue":
						originalLeft = (Expression)methodArguments[parameter.Position];
						leftIdx = parameter.Position;
						break;
					case "right":
					case "ifFalse":
						originalRight = (Expression)methodArguments[parameter.Position];
						rightIdx = parameter.Position;
						break;
				}
			}

			if (originalLeft == null || originalRight == null || leftIdx < 0 || rightIdx < 0)
				return;

			var left = originalLeft;
			var right = originalRight;
			var leftType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
			var rightType = Nullable.GetUnderlyingType(right.Type) ?? right.Type;
			var liftToNullable = leftType != left.Type || rightType != right.Type;

			if (liftToNullable && leftType == left.Type)
				left = ConvertToNullable(left);
			if (liftToNullable && rightType != right.Type)
				right = ConvertToNullable(right);

			if (leftType.IsEnum)
			{
				leftType = Enum.GetUnderlyingType(leftType);
				methodArguments[leftIdx] = left = Expression.Convert(left, liftToNullable ? typeof(Nullable<>).MakeGenericType(leftType) : leftType);
			}
			if (rightType.IsEnum)
			{
				rightType = Enum.GetUnderlyingType(rightType);
				methodArguments[rightIdx] = right = Expression.Convert(right, liftToNullable ? rightType = typeof(Nullable<>).MakeGenericType(rightType) : rightType);
			}

			if (leftType == rightType)
			{
				var typeCode = Type.GetTypeCode(leftType);
				if (typeCode < TypeCode.SByte || typeCode > TypeCode.UInt16)
					return;

				// expand smaller integers to int32
				methodArguments[leftIdx] = left = Expression.Convert(left, liftToNullable ? typeof(int?) : typeof(int));
				methodArguments[rightIdx] = right = Expression.Convert(right, liftToNullable ? typeof(int?) : typeof(int));
				return;
			}

			if (leftType == typeof(object))
			{
				methodArguments[rightIdx] = right = Expression.Convert(right, typeof(object));
				return;
			}
			else if (rightType == typeof(object))
			{
				methodArguments[leftIdx] = left = Expression.Convert(left, typeof(object));
				return;
			}

			var leftTypeCode = Type.GetTypeCode(leftType);
			var rightTypeCode = Type.GetTypeCode(rightType);
			if (Array.BinarySearch(Numeric, leftTypeCode) < 0 || Array.BinarySearch(Numeric, rightTypeCode) < 0)
				return;

			if (leftTypeCode == TypeCode.Decimal || rightTypeCode == TypeCode.Decimal)
			{
				if (leftTypeCode == TypeCode.Double || leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Single)
					return; // will throw exception
				if (leftTypeCode == TypeCode.Decimal)
					right = Expression.Convert(right, liftToNullable ? typeof(decimal?) : typeof(decimal));
				else
					left = Expression.Convert(left, liftToNullable ? typeof(decimal?) : typeof(decimal));
			}
			else if (leftTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Double)
			{
				if (leftTypeCode == TypeCode.Double)
					right = Expression.Convert(right, liftToNullable ? typeof(double?) : typeof(double));
				else
					left = Expression.Convert(left, liftToNullable ? typeof(double?) : typeof(double));
			}
			else if (leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Single)
			{
				if (leftTypeCode == TypeCode.Single)
					right = Expression.Convert(right, liftToNullable ? typeof(float?) : typeof(float));
				else
					left = Expression.Convert(left, liftToNullable ? typeof(float?) : typeof(float));
			}
			else if (leftTypeCode == TypeCode.UInt64)
			{
				if (Array.IndexOf(SignedIntegerTypes, rightTypeCode) > 0 && TryCastTo(typeof(ulong), ref right) <= 0)
					return; // will throw exception

				var expectedRightType = liftToNullable ? typeof(ulong?) : typeof(ulong);
				right = right.Type != expectedRightType ? Expression.Convert(right, expectedRightType) : right;
			}
			else if (rightTypeCode == TypeCode.UInt64)
			{
				if (Array.IndexOf(SignedIntegerTypes, leftTypeCode) > 0 && TryCastTo(typeof(ulong), ref left) <= 0)
					return; // will throw exception

				var expectedLeftType = liftToNullable ? typeof(ulong?) : typeof(ulong);
				left = left.Type != expectedLeftType ? Expression.Convert(left, expectedLeftType) : left;
			}
			else if (leftTypeCode == TypeCode.Int64 || rightTypeCode == TypeCode.Int64)
			{
				if (leftTypeCode == TypeCode.Int64)
					right = Expression.Convert(right, liftToNullable ? typeof(long?) : typeof(long));
				else
					left = Expression.Convert(left, liftToNullable ? typeof(long?) : typeof(long));
			}
			else if ((leftTypeCode == TypeCode.UInt32 && Array.IndexOf(SignedIntegerTypes, rightTypeCode) > 0) ||
				(rightTypeCode == TypeCode.UInt32 && Array.IndexOf(SignedIntegerTypes, leftTypeCode) > 0))
			{
				right = Expression.Convert(right, liftToNullable ? typeof(long?) : typeof(long));
				left = Expression.Convert(left, liftToNullable ? typeof(long?) : typeof(long));
			}
			else if (leftTypeCode == TypeCode.UInt32 || rightTypeCode == TypeCode.UInt32)
			{
				if (leftTypeCode == TypeCode.UInt32)
					right = Expression.Convert(right, liftToNullable ? typeof(uint?) : typeof(uint));
				else
					left = Expression.Convert(left, liftToNullable ? typeof(uint?) : typeof(uint));
			}
			else
			{
				right = Expression.Convert(right, liftToNullable ? typeof(int?) : typeof(int));
				left = Expression.Convert(left, liftToNullable ? typeof(int?) : typeof(int));
			}

			methodArguments[leftIdx] = left;
			methodArguments[rightIdx] = right;
		}
		private static void PromoteFirstArgument(MethodInfo method, object[] methodArguments)
		{
			if (method == null) throw new ArgumentNullException("method");
			if (methodArguments == null) throw new ArgumentNullException("methodArguments");

			var first = default(Expression);
			var firstIdx = -1;
			foreach (var parameter in method.GetParameters())
			{
				if (parameter.Name != "expression" && parameter.Name != "left")
					continue;

				first = (Expression)methodArguments[parameter.Position];
				firstIdx = parameter.Position;
				break;
			}

			if (first == null || firstIdx < 0)
				return;

			if (first.Type.IsEnum)
				first = Expression.Convert(first, Enum.GetUnderlyingType(first.Type));

			var typeCode = Type.GetTypeCode(first.Type);
			if (typeCode >= TypeCode.SByte && typeCode <= TypeCode.UInt16)
			{
				methodArguments[firstIdx] = Expression.Convert(first, typeof(int));
			}
			else if (typeCode == TypeCode.UInt32 && method.Name == "Not")
			{
				methodArguments[firstIdx] = Expression.Convert(first, typeof(long));
			}

		}

		private static bool TryGetTypeReference(object value, out TypeReference typeReference)
		{
			typeReference = default(TypeReference);

			if (value is ExpressionTree)
			{
				var parts = new List<ExpressionTree>(10);
				var current = (ExpressionTree)value;
				while (current != null)
				{
					var expressionType = current.GetExpressionType(throwOnError: true);
					if (expressionType != Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD)
						return false;

					parts.Add(current);
					current = current.GetExpression(throwOnError: false);
				}

				var typeNameParts = default(List<string>);
				var typeArguments = default(List<TypeReference>);
				for (var p = 0; p < parts.Count; p++)
				{
					var part = parts[parts.Count - 1 - p]; // reverse order
					var arguments = part.GetArguments(throwOnError: false);
					var typeNamePart = part.GetPropertyOrFieldName(throwOnError: true);
					if (typeNameParts == null) typeNameParts = new List<string>();
					var typeArgumentsCount = 0;
					if (arguments != null && arguments.Count > 0)
					{
						if (typeArguments == null) typeArguments = new List<TypeReference>(10);

						for (var i = 0; i < arguments.Count; i++)
						{
							var typeArgument = default(ExpressionTree);
							var typeArgumentTypeReference = default(TypeReference);
							var key = Constants.GetIndexAsString(i);
							if (arguments.TryGetValue(key, out typeArgument) == false || typeArgument == null)
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGORWRONGARGUMENT, key), part);

							if (typeArgument.GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD && typeArgument.GetPropertyOrFieldName(throwOnError: true) == string.Empty)
								typeArgumentTypeReference = TypeReference.Empty;
							else if (TryGetTypeReference(typeArgument, out typeArgumentTypeReference) == false)
								throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGORWRONGARGUMENT, key), part);

							typeArguments.Add(typeArgumentTypeReference);
							typeArgumentsCount++;
						}
						typeNamePart = typeNamePart + "`" + Constants.GetIndexAsString(typeArgumentsCount);
					}
					typeNameParts.Add(typeNamePart);
				}

				typeReference = new TypeReference(typeNameParts, typeArguments ?? TypeReference.EmptyGenericArguments);
				return true;
			}
			else
			{
				typeReference = new TypeReference(new[] { Convert.ToString(value, Constants.DefaultFormatProvider) }, TypeReference.EmptyGenericArguments);
				return true;
			}
		}
		private static bool TryGetMethodReference(object value, out TypeReference methodReference)
		{
			methodReference = default(TypeReference);

			if (value is ExpressionTree)
			{
				var typeArguments = default(List<TypeReference>);
				var methodNameTree = (ExpressionTree)value;

				var arguments = methodNameTree.GetArguments(throwOnError: false);
				var methodName = methodNameTree.GetPropertyOrFieldName(throwOnError: true);
				if (arguments != null && arguments.Count > 0)
				{
					typeArguments = new List<TypeReference>(10);

					for (var i = 0; i < arguments.Count; i++)
					{
						var typeArgument = default(ExpressionTree);
						var typeArgumentTypeReference = default(TypeReference);
						var key = Constants.GetIndexAsString(i);
						if (arguments.TryGetValue(key, out typeArgument) == false || typeArgument == null)
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGORWRONGARGUMENT, key), methodNameTree);

						var isEmptyTypeArgument = typeArgument.GetExpressionType(throwOnError: true) == Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD && typeArgument.GetPropertyOrFieldName(throwOnError: true) == string.Empty;
						if (isEmptyTypeArgument || TryGetTypeReference(typeArgument, out typeArgumentTypeReference) == false)
							throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGORWRONGARGUMENT, key), methodNameTree);

						typeArguments.Add(typeArgumentTypeReference);
					}
				}

				methodReference = new TypeReference(new[] { methodName }, typeArguments ?? TypeReference.EmptyGenericArguments);
				return true;
			}
			else
			{
				methodReference = new TypeReference(new[] { Convert.ToString(value, Constants.DefaultFormatProvider) }, TypeReference.EmptyGenericArguments);
				return true;
			}
		}

		private static Expression DefaultExpression(Type forType)
		{
			if (forType == null) throw new ArgumentNullException("forType");

			return Expression.Constant(GetDefaultValue(forType), forType);
		}
		private static Expression ConvertToNullable(Expression notNullableExpression)
		{
			if (notNullableExpression == null) throw new ArgumentNullException("notNullableExpression");

			if (notNullableExpression.Type.IsValueType && Nullable.GetUnderlyingType(notNullableExpression.Type) == null)
				return Expression.Convert(notNullableExpression, typeof(Nullable<>).MakeGenericType(notNullableExpression.Type));
			else
				return notNullableExpression;
		}
		private static Expression MakeNullPropagationExpression(Expression testExpression, Expression notNullExpression)
		{
			if (!IsNullableType(testExpression.Type)) // no need in null propagation
				return notNullExpression;

			var resultType = !IsNullableType(notNullExpression.Type) ? typeof(Nullable<>).MakeGenericType(notNullExpression.Type) : notNullExpression.Type;
			if (resultType != notNullExpression.Type)
				notNullExpression = Expression.Convert(notNullExpression, resultType);

			return Expression.Condition
			(
				test: Expression.NotEqual(testExpression, DefaultExpression(testExpression.Type)),
				ifTrue: notNullExpression,
				ifFalse: DefaultExpression(notNullExpression.Type)
			);
		}

		internal static bool ExtractNullPropagationExpression(ConditionalExpression conditionalExpression, out Expression baseExpression, out Expression continuationExpression)
		{
			if (conditionalExpression == null) throw new ArgumentNullException("conditionalExpression");

			var testAsNotEqual = conditionalExpression.Test as BinaryExpression;
			var testAsNotEqualRightConst = testAsNotEqual != null ? testAsNotEqual.Right as ConstantExpression : null;
			var ifFalseConst = conditionalExpression.IfFalse as ConstantExpression;
			var ifTrueUnwrapped = conditionalExpression.IfTrue.NodeType == ExpressionType.Convert ? ((UnaryExpression)conditionalExpression.IfTrue).Operand : conditionalExpression.IfTrue;
			var ifTrueCall = ifTrueUnwrapped as MethodCallExpression;
			var ifTrueMember = ifTrueUnwrapped as MemberExpression;
			var ifTrueIndex = ifTrueUnwrapped as BinaryExpression;

			// try to detect null-propagation operation
			if (testAsNotEqual != null && testAsNotEqualRightConst != null && testAsNotEqualRightConst.Value == null &&
				ifFalseConst != null && ifFalseConst.Value == null &&
				(
					(ifTrueCall != null && ReferenceEquals(ifTrueCall.Object, testAsNotEqual.Left)) ||
					(ifTrueMember != null && ReferenceEquals(ifTrueMember.Expression, testAsNotEqual.Left)) ||
					(ifTrueIndex != null && ReferenceEquals(ifTrueIndex.Left, testAsNotEqual.Left))
				)
			)
			{
				baseExpression = testAsNotEqual.Left;
				continuationExpression = ifTrueUnwrapped;

				return true;
			}
			else
			{
				baseExpression = null;
				continuationExpression = null;

				return false;
			}
		}
	}
}
