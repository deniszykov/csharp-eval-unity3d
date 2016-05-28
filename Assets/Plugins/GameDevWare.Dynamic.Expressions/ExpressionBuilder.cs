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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions
{
	public class ExpressionBuilder
	{
		private static readonly CultureInfo Format = CultureInfo.InvariantCulture;
		private static readonly ReadOnlyDictionary<string, object> EmptyArguments = ReadOnlyDictionary<string, object>.Empty;
		private static readonly Dictionary<Type, ReadOnlyCollection<MemberInfo>> InstanceMembersByType = new Dictionary<Type, ReadOnlyCollection<MemberInfo>>();
		private static readonly Dictionary<Type, ReadOnlyCollection<MemberInfo>> StaticMembersByType = new Dictionary<Type, ReadOnlyCollection<MemberInfo>>();
		private static readonly ILookup<string, MethodInfo> ExpressionConstructors;
		private static readonly string[] OperationWithPromotionForBothOperand;
		private static readonly string[] OperationWithPromotionForFirstOperand;
		private static readonly TypeCode[] SignedIntegerTypes;
		private static readonly TypeCode[] UnsignedIntegerTypes;
		private static readonly TypeCode[] Numeric;
		public static ITypeResolutionService DefaultTypeResolutionService = null;

		private readonly ReadOnlyCollection<ParameterExpression> parameters;
		private readonly Type contextType;
		private readonly Type resultType;
		private readonly ITypeResolutionService typeResolutionService;

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
		public ExpressionBuilder(IList<ParameterExpression> parameters, Type resultType, Type contextType = null, ITypeResolutionService typeResolutionService = null)
		{
			if (resultType == null) throw new ArgumentNullException("resultType");
			if (parameters == null) throw new ArgumentNullException("parameters");
			if (typeResolutionService == null) typeResolutionService = DefaultTypeResolutionService ?? new KnownTypeResolutionService(parameters.Select(p => p.Type));

			if (parameters is ReadOnlyCollection<ParameterExpression> == false)
				parameters = new ReadOnlyCollection<ParameterExpression>(parameters);

			this.parameters = (ReadOnlyCollection<ParameterExpression>)parameters;
			this.resultType = resultType;
			this.contextType = contextType;
			this.typeResolutionService = typeResolutionService;

		}

		public Expression Build(ExpressionTree node, Expression context = null)
		{
			return Build(node, context, this.resultType);
		}

		private Expression Build(ExpressionTree node, Expression context, Type expectedType)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE), node);

			try
			{
				var expression = default(Expression);
				var expressionType = (string)expressionTypeObj;
				switch (expressionType)
				{
					case "Invoke": expression = BuildInvoke(node, context); break;
					case "Index": expression = BuildIndex(node, context); break;
					case "Enclose":
					case "UncheckedScope":
					case "CheckedScope":
					case "Group": expression = BuildGroup(node, context); break;
					case "Constant": expression = BuildConstant(node); break;
					case "PropertyOrField": expression = BuildPropertyOrField(node, context); break;
					case "TypeOf": expression = BuildTypeOf(node); break;
					case "Default": expression = BuildDefault(node); break;
					case "New": expression = BuildNew(node, context); break;
					case "NewArrayBounds": expression = BuildNewArrayBounds(node, context); break;
					default: expression = BuildByType(node, context); break;
				}

				if (expectedType != null && expression.Type != expectedType)
					expression = Expression.Convert(expression, expectedType);

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
				throw new ExpressionParserException(string.Format("An error occured while trying to build '{0}' expression: {1}", expressionTypeObj, exception.Message), exception, node);
			}
		}

		private Expression BuildByType(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionType = (string)node[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE];
			if (expressionType == "Complement")
				expressionType = "Not";

			if (ExpressionConstructors.Contains(expressionType) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType), node);

			var argumentNames = new HashSet<string>(node.Keys, StringComparer.Ordinal);
			argumentNames.Remove(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE);
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
						var typeName = default(string);
						if (argument != null && methodParameter.ParameterType == typeof(Type) && TryGetTypeName(argument, out typeName))
							argument = this.typeResolutionService.GetType(typeName);
						else if (argument is ExpressionTree)
							argument = Build((ExpressionTree)argument, context, expectedType: null);
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
					PromoteBothNumerics(method, methodArguments);
				if (Array.BinarySearch(OperationWithPromotionForFirstOperand, expressionType) >= 0)
					PromoteFirstNumeric(method, methodArguments);

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
						(string.Equals(expressionType, "Add", StringComparison.Ordinal) || string.Equals(expressionType, "AddChecked", StringComparison.Ordinal))
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
						(string.Equals(expressionType, "Negate", StringComparison.Ordinal) || string.Equals(expressionType, "NegateChecked", StringComparison.Ordinal))
					)
					{
						var operand = (Expression)methodArguments[0];
						var negativeConst = operand.Type == typeof(float) ? Expression.Constant(-1.0f) : Expression.Constant(-1.0d);

						return Expression.Multiply(operand, negativeConst);
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

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"), node);

			return Build((ExpressionTree)expressionObj, context, expectedType: null);
		}
		private Expression BuildPropertyOrField(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"), node);

			var propertyOrFieldNameObj = default(object);
			if (node.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"), node);

			var propertyOrFieldName = (string)propertyOrFieldNameObj;
			var expression = default(Expression);
			var typeName = default(string);
			var type = default(Type);
			if (expressionObj != null && TryGetTypeName(expressionObj, out typeName) && this.typeResolutionService.TryGetType(typeName, out type))
			{
				expression = null;
			}
			else if (expressionObj == null)
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
				expression = Build((ExpressionTree)expressionObj, context, expectedType: null);
			}

			if (expression == null && type == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName), node);

			if (expression != null)
				type = expression.Type;

			var isStatic = expression == null;

			foreach (var member in GetMembers(type, isStatic))
			{
				if (member is PropertyInfo == false && member is FieldInfo == false)
					continue;
				if (member.Name != propertyOrFieldName)
					continue;

				try
				{
					if (member is PropertyInfo)
						return Expression.Property(expression, member as PropertyInfo);
					else
						return Expression.Field(expression, member as FieldInfo);
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}

			if (expression == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, propertyOrFieldName), node);
			else
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVEMEMBERONTYPE, propertyOrFieldName, type), node);
		}
		private Expression BuildConstant(ExpressionTree node)
		{
			if (node == null) throw new ArgumentNullException("node");

			var typeObj = default(object);
			var valueObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "Constant"), node);
			if (node.TryGetValue(ExpressionTree.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.VALUE_ATTRIBUTE, "Constant"), node);

			var type = this.typeResolutionService.GetType(Convert.ToString(typeObj, Format));
			var value = ChangeType(valueObj, type);
			return Expression.Constant(value);
		}
		private Expression BuildIndex(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Index"), node);

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Index"), node);

			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;
			var expression = Build((ExpressionTree)expressionObj, context, expectedType: null);
			var properties = expression.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			Array.Sort(properties, (x, y) => x.GetIndexParameters().Length.CompareTo(y.GetIndexParameters().Length));
			foreach (var property in properties)
			{
				var indexerParameters = property.GetIndexParameters();
				if (indexerParameters.Length == 0) continue;

				var getMethod = property.GetGetMethod(nonPublic: false);
				var argumentExpressions = default(Expression[]);
				if (getMethod == null || TryBindMethod(indexerParameters, arguments, out argumentExpressions, context) == false)
					continue;

				try
				{
					return Expression.Call(expression, getMethod, argumentExpressions);
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}
			throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDINDEXER, expression.Type), node);
		}
		private Expression BuildCall(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Call"), node);

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Call"), node);

			var methodObj = default(object);
			if (node.TryGetValue(ExpressionTree.METHOD_ATTRIBUTE, out methodObj) == false || methodObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.METHOD_ATTRIBUTE, "Call"), node);

			if (expressionObj == null && context == null)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETORESOLVENAME, methodObj), node);

			var methodName = (string)methodObj;
			var expression = default(Expression);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			var typeName = default(string);
			var type = default(Type);
			var isStatic = true;
			if (TryGetTypeName(expressionObj, out typeName) == false || this.typeResolutionService.TryGetType(typeName, out type) == false)
			{
				expression = Build((ExpressionTree)expressionObj, context, expectedType: null);
				type = expression.Type;
				isStatic = false;
			}

			foreach (var member in GetMembers(type, isStatic))
			{
				var method = member as MethodInfo;
				if (method == null) continue;
				if (method.IsGenericMethod) continue;
				if (methodName != method.Name) continue;

				var methodParameters = method.GetParameters();
				var argumentExpressions = default(Expression[]);
				if (TryBindMethod(methodParameters, arguments, out argumentExpressions, context) == false)
					continue;

				try
				{
					return expression == null ?
						Expression.Call(method, argumentExpressions) : // static call
						Expression.Call(expression, method, argumentExpressions); // instance call
				}
				catch (Exception exception)
				{
					throw new ExpressionParserException(exception.Message, exception, node);
				}
			}
			throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOBINDCALL, methodObj, type), node);
		}
		private Expression BuildInvoke(ExpressionTree node, Expression context)
		{
			if (node == null) throw new ArgumentNullException("node");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "Invoke"), node);

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "Invoke"), node);

			var expressionTree = (ExpressionTree)expressionObj;
			var expressionTreeType = (string)expressionTree[ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE];
			var expression = default(Expression);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			if (expressionTreeType == "PropertyOrField")
			{
				var propertyOrFieldExpressionObj = default(object);
				if (expressionTree.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out propertyOrFieldExpressionObj) && propertyOrFieldExpressionObj != null && propertyOrFieldExpressionObj is ExpressionTree == false)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"), node);
				var propertyOrFieldNameObj = default(object);
				if (expressionTree.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
					throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"), node);

				var methodName = (string)propertyOrFieldNameObj;
				var typeName = default(string);
				var type = default(Type);
				var isStatic = true;
				if (TryGetTypeName(propertyOrFieldExpressionObj, out typeName) == false || this.typeResolutionService.TryGetType(typeName, out type) == false)
				{
					var propertyOrFieldExpression = propertyOrFieldExpressionObj != null ? Build((ExpressionTree)propertyOrFieldExpressionObj, context, expectedType: null) : context;
					if (propertyOrFieldExpression != null)
					{
						type = propertyOrFieldExpression.Type;
						isStatic = false;
					}
				}
				if (type != null && GetMembers(type, isStatic).Any(m => m is MethodInfo && m.Name == methodName))
				{
					var callNode = new Dictionary<string, object>(node);
					callNode[ExpressionTree.METHOD_ATTRIBUTE] = methodName;
					callNode[ExpressionTree.EXPRESSION_ATTRIBUTE] = propertyOrFieldExpressionObj;
					return this.BuildCall(new ExpressionTree(callNode), context);
				}
			}

			expression = Build((ExpressionTree)expressionObj, context, expectedType: null);

			if (typeof(Delegate).IsAssignableFrom(expression.Type) == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNABLETOINVOKENONDELEG, expression.Type), node);

			var method = expression.Type.GetMethod("Invoke");
			var methodParameters = method.GetParameters();
			var argumentExpressions = default(Expression[]);
			if (TryBindMethod(methodParameters, arguments, out argumentExpressions, context) == false)
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
			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "Default"), node);

			var type = this.typeResolutionService.GetType((string)typeObj);

			return DefaultExpression(type);
		}
		private Expression BuildTypeOf(ExpressionTree node)
		{
			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "TypeOf"), node);

			var type = this.typeResolutionService.GetType((string)typeObj);

			return Expression.Constant(type, typeof(Type));
		}
		private Expression BuildNewArrayBounds(ExpressionTree node, Expression context)
		{
			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "New"), node);

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "New"), node);

			var type = this.typeResolutionService.GetType((string)typeObj);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;
			var argumentExpressions = Enumerable.Range(0, arguments.Count).Where(n => arguments.ContainsKey(n.ToString())).Select(n => Build((ExpressionTree)arguments[n.ToString()], context, typeof(int))).ToList();

			return Expression.NewArrayBounds(type, argumentExpressions);
		}
		private Expression BuildNew(ExpressionTree node, Expression context)
		{
			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "New"), node);

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, "New"), node);

			var type = this.typeResolutionService.GetType((string)typeObj);
			var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;
			Array.Sort(constructors, (x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));

			foreach (var constructorInfo in constructors)
			{
				var methodParameters = constructorInfo.GetParameters();
				var argumentExpressions = default(Expression[]);
				if (TryBindMethod(methodParameters, arguments, out argumentExpressions, context) == false)
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

		private bool TryBindMethod(ParameterInfo[] methodParameters, IDictionary<string, object> arguments, out Expression[] callArguments, Expression context)
		{
			callArguments = null;

			// check argument count
			if (arguments.Count > methodParameters.Length)
				return false; // not all arguments are bound to parameters

			var requiredParametersCount = methodParameters.Length - methodParameters.Count(p => p.IsOptional);
			if (arguments.Count < requiredParametersCount)
				return false; // not all required parameters has values

			// bind arguments
			var parametersByName = methodParameters.ToDictionary(p => p.Name);
			var parametersByPos = methodParameters.ToDictionary(p => p.Position);
			var argumentNames = arguments.Keys.ToArray();
			callArguments = new Expression[methodParameters.Length];
			foreach (var argName in argumentNames)
			{
				var parameter = default(ParameterInfo);
				var parameterIndex = 0;
				if (argName.All(char.IsDigit))
				{
					parameterIndex = int.Parse(argName, Format);
					if (parametersByPos.TryGetValue(parameterIndex, out parameter) == false)
						return false; // position out of range

					if (arguments.ContainsKey(parameter.Name))
						return false; // positional intersects named
				}
				else
				{
					if (parametersByName.TryGetValue(argName, out parameter) == false)
						return false; // parameter is not found
					parameterIndex = parameter.Position;
				}

				var argValue = arguments[argName] as Expression;
				if (argValue == null)
				{
					argValue = this.Build((ExpressionTree)arguments[argName], context, expectedType: null);
					// arguments[argName] = argValue // no arguments optimization
				}

				callArguments[parameterIndex] = argValue;

				var expectedType = parameter.ParameterType;
				var actualType = argValue.Type;

				if (expectedType == actualType)
					continue;

				if (TryCastTo(expectedType, ref argValue))
				{
					callArguments[parameterIndex] = argValue;
					continue;
				}

				return false;
			}

			for (var i = 0; i < callArguments.Length; i++)
			{
				if (callArguments[i] != null) continue;
				var parameter = parametersByPos[i];
				if (parameter.IsOptional == false)
					return false; // missing required parameter

				callArguments[i] = Expression.Constant(GetDefaultValue(parameter.ParameterType), parameter.ParameterType);
			}

			return true;
		}
		private static bool TryCastTo(Type expectedType, ref Expression expression)
		{
			var actualType = expression.Type;

			// 1: check if types are convertible
			// 2: check if value is constant and could be converted

			if (IsHeirOf(actualType, expectedType))
			{
				expression = Expression.Convert(expression, expectedType);
				return true;
			}

			// convert to/from enum, nullable
			var nullableUnderlyingType = Nullable.GetUnderlyingType(expectedType);
			if ((expectedType.IsEnum && Enum.GetUnderlyingType(expectedType) == actualType) ||
				(actualType.IsEnum && Enum.GetUnderlyingType(actualType) == expectedType) ||
				(nullableUnderlyingType != null && nullableUnderlyingType == actualType))
			{
				expression = Expression.Convert(expression, expectedType);
				return true;
			}

			// implicit convertion on expectedType
			var implicitConvertion = expectedType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new Type[] { actualType }, null);
			if (implicitConvertion != null && implicitConvertion.ReturnType == expectedType)
			{
				expression = Expression.Convert(expression, expectedType, implicitConvertion);
				return true;
			}

			// implicit convertion on actualType
			implicitConvertion = actualType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new Type[] { actualType }, null);
			if (implicitConvertion != null && implicitConvertion.ReturnType == expectedType)
			{
				expression = Expression.Convert(expression, expectedType, implicitConvertion);
				return true;
			}

			// try to convert value of constant
			var constantValue = default(object);
			var constantType = default(Type);
			if (!TryExposeConstant(expression, out constantValue, out constantType))
				return false;

			if (constantValue == null)
			{
				if (constantType == typeof(object) && !expectedType.IsValueType)
				{
					expression = Expression.Constant(null, expectedType);
					return true;
				}
				else
				{
					return false;
				}
			}

			var expectedTypeCode = Type.GetTypeCode(expectedType);
			var constantTypeCode = Type.GetTypeCode(constantType);
			var convertibleToExpectedType = default(bool);
			switch (expectedTypeCode)
			{
				case TypeCode.Byte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)byte.MinValue, (ulong)byte.MaxValue); break;
				case TypeCode.SByte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)sbyte.MinValue, (ulong)sbyte.MaxValue); break;
				case TypeCode.UInt16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt16.MinValue, UInt16.MaxValue); break;
				case TypeCode.Int16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int16.MinValue, (ulong)Int16.MaxValue); break;
				case TypeCode.UInt32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt32.MinValue, UInt32.MaxValue); break;
				case TypeCode.Int32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int32.MinValue, (ulong)Int32.MaxValue); break;
				case TypeCode.UInt64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt64.MinValue, UInt64.MaxValue); break;
				case TypeCode.Int64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int64.MinValue, (ulong)Int64.MaxValue); break;
				case TypeCode.Char:
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Single: convertibleToExpectedType = Array.BinarySearch(SignedIntegerTypes, constantTypeCode) >= 0 || Array.BinarySearch(UnsignedIntegerTypes, constantTypeCode) >= 0; break;
				default: convertibleToExpectedType = false; break;
			}

			if (convertibleToExpectedType)
			{
				expression = Expression.Constant(Convert.ChangeType(constantValue, expectedTypeCode, Format));
				return true;
			}

			return false;
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
				var signedValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
				if (signedValue >= minValue && signedValue >= 0 && unchecked((ulong)signedValue) <= maxValue)
					return true;
			}
			else if (Array.BinarySearch(UnsignedIntegerTypes, valueTypeCode) >= 0)
			{
				var unsignedValue = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
				if (unsignedValue <= maxValue)
					return true;
			}
			return false;
		}
		private static bool TryExposeConstant(Expression expression, out object constantValue, out Type constantType)
		{
			constantValue = null;
			constantType = null;
			var constantExpression = (expression as ConstantExpression);
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
				return Enum.Parse(toType, Convert.ToString(value, Format));
			else
				return Convert.ChangeType(value, toType, Format);
		}
		private static object GetDefaultValue(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}
		private static void PromoteBothNumerics(MethodInfo method, object[] methodArguments)
		{
			if (method == null) throw new ArgumentNullException("method");
			if (methodArguments == null) throw new ArgumentNullException("methodArguments");

			var left = default(Expression);
			var right = default(Expression);
			var leftIdx = -1;
			var rightIdx = -1;
			foreach (var parameter in method.GetParameters())
			{
				switch (parameter.Name)
				{
					case "left":
					case "ifTrue":
						left = (Expression)methodArguments[parameter.Position];
						leftIdx = parameter.Position;
						break;
					case "right":
					case "ifFalse":
						right = (Expression)methodArguments[parameter.Position];
						rightIdx = parameter.Position;
						break;
				}
			}

			if (left == null || right == null || leftIdx < 0 || rightIdx < 0)
				return;

			if (left.Type.IsEnum)
				left = Expression.Convert(left, Enum.GetUnderlyingType(left.Type));
			if (right.Type.IsEnum)
				right = Expression.Convert(right, Enum.GetUnderlyingType(right.Type));

			//if (Nullable.GetUnderlyingType(left.Type) != null)
			//	left = Expression.Property(left, "Value");
			//if (Nullable.GetUnderlyingType(right.Type) != null)
			//	right = Expression.Property(right, "Value");

			if (left.Type == right.Type)
			{
				var typeCode = Type.GetTypeCode(left.Type);
				if (typeCode < TypeCode.SByte || typeCode > TypeCode.UInt16)
					return;

				// expand smaller integers to int32
				methodArguments[leftIdx] = Expression.Convert(left, typeof(int));
				methodArguments[rightIdx] = Expression.Convert(right, typeof(int));
				return;
			}

			if (left.Type == typeof(object))
				methodArguments[rightIdx] = Expression.Convert(right, typeof(object));
			else if (right.Type == typeof(object))
				methodArguments[leftIdx] = Expression.Convert(left, typeof(object));

			var leftType = Type.GetTypeCode(left.Type);
			var rightType = Type.GetTypeCode(right.Type);
			if (Array.BinarySearch(Numeric, leftType) < 0 || Array.BinarySearch(Numeric, rightType) < 0)
				return;

			if (leftType == TypeCode.Decimal || rightType == TypeCode.Decimal)
			{
				if (leftType == TypeCode.Double || leftType == TypeCode.Single || rightType == TypeCode.Double || rightType == TypeCode.Single)
					return; // will throw exception
				if (leftType == TypeCode.Decimal)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(decimal));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(decimal));
			}
			else if (leftType == TypeCode.Double || rightType == TypeCode.Double)
			{
				if (leftType == TypeCode.Double)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(double));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(double));
			}
			else if (leftType == TypeCode.Single || rightType == TypeCode.Single)
			{
				if (leftType == TypeCode.Single)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(float));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(float));
			}
			else if (leftType == TypeCode.UInt64)
			{
				if (Array.IndexOf(SignedIntegerTypes, rightType) > 0 && TryCastTo(typeof(UInt64), ref right) == false)
					return; // will throw exception

				methodArguments[rightIdx] = right.Type != typeof(ulong) ? Expression.Convert(right, typeof(ulong)) : right;
			}
			else if (rightType == TypeCode.UInt64)
			{
				if (Array.IndexOf(SignedIntegerTypes, leftType) > 0 && TryCastTo(typeof(UInt64), ref left) == false)
					return; // will throw exception

				methodArguments[leftIdx] = left.Type != typeof(ulong) ? Expression.Convert(left, typeof(ulong)) : left;
			}
			else if (leftType == TypeCode.Int64 || rightType == TypeCode.Int64)
			{
				if (leftType == TypeCode.Int64)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(long));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(long));
			}
			else if ((leftType == TypeCode.UInt32 && Array.IndexOf(SignedIntegerTypes, rightType) > 0) ||
				(rightType == TypeCode.UInt32 && Array.IndexOf(SignedIntegerTypes, leftType) > 0))
			{
				methodArguments[rightIdx] = Expression.Convert(right, typeof(long));
				methodArguments[leftIdx] = Expression.Convert(left, typeof(long));
			}
			else if (leftType == TypeCode.UInt32 || rightType == TypeCode.UInt32)
			{
				if (leftType == TypeCode.UInt32)
					methodArguments[rightIdx] = Expression.Convert(right, typeof(uint));
				else
					methodArguments[leftIdx] = Expression.Convert(left, typeof(uint));
			}
			else
			{
				methodArguments[rightIdx] = Expression.Convert(right, typeof(int));
				methodArguments[leftIdx] = Expression.Convert(left, typeof(int));
			}
		}
		private static void PromoteFirstNumeric(MethodInfo method, object[] methodArguments)
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

		private bool TryGetTypeName(object value, out string typeName)
		{
			typeName = default(string);
			if (value is ExpressionTree)
			{
				var typeNameParts = new List<string>();
				var current = (ExpressionTree)value;
				while (current != null)
				{
					var expressionTypeObj = default(object);
					if (current.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE), current);

					var expressionType = (string)expressionTypeObj;
					if (expressionType != "PropertyOrField")
						return false;

					var expressionObj = default(object);
					if (current.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"), current);

					var typeNamePartObj = default(object);
					if (current.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out typeNamePartObj) == false || typeNamePartObj is string == false)
						throw new ExpressionParserException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"), current);

					var typeNamePart = (string)typeNamePartObj;
					typeNameParts.Add(typeNamePart);
					current = expressionObj as ExpressionTree;
				}

				typeNameParts.Reverse();
				typeName = string.Join(".", typeNameParts.ToArray());
				return true;
			}
			else
			{
				typeName = Convert.ToString(value, Format);
				return true;
			}
		}

		public static Expression DefaultExpression(Type forType)
		{
			if (forType == null) throw new ArgumentNullException("forType");

			if (forType.IsValueType)
				return Expression.Constant(Activator.CreateInstance(forType), forType);
			else
				return Expression.Constant(null, forType);
		}
	}
}
