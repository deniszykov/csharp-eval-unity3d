using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GameDevWare.Dynamic.Expressions.Packing
{
	internal static class AnyPacker
	{
		public static Dictionary<string, object> Pack(Expression expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			switch (expression.NodeType)
			{
				case ExpressionType.Not:
				case ExpressionType.NegateChecked:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
					return UnaryPacker.Pack((UnaryExpression)expression);
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Power:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Multiply:
				case ExpressionType.Modulo:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Equal:
				case ExpressionType.Divide:
				case ExpressionType.Coalesce:
				case ExpressionType.AndAlso:
				case ExpressionType.And:
				case ExpressionType.AddChecked:
				case ExpressionType.Add:
					return BinaryPacker.Pack((BinaryExpression)expression);
				case ExpressionType.Conditional:
					return ConditionPacker.Pack((ConditionalExpression)expression);
				case ExpressionType.ArrayLength:
					return ArrayLengthPacker.Pack((UnaryExpression)expression);
				case ExpressionType.ArrayIndex:
					return ArrayIndexPacker.Pack(expression);
				case ExpressionType.Call:
					return CallPacker.Pack((MethodCallExpression)expression);
				case ExpressionType.Constant:
					return ConstantPacker.Pack((ConstantExpression)expression);
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					return ConvertPacker.Pack((UnaryExpression)expression);
				case ExpressionType.Invoke:
					return InvokePacker.Pack((InvocationExpression)expression);
				case ExpressionType.Lambda:
					return LambdaPacker.Pack((LambdaExpression)expression);
				case ExpressionType.ListInit:
					return ListInitPacker.Pack((ListInitExpression)expression);
				case ExpressionType.MemberAccess:
					return MemberAccessPacker.Pack((MemberExpression)expression);
				case ExpressionType.MemberInit:
					return MemberInitPacker.Pack((MemberInitExpression)expression);
				case ExpressionType.New:
					return NewPacker.Pack((NewExpression)expression);
				case ExpressionType.NewArrayBounds:
				case ExpressionType.NewArrayInit:
					return NewArrayPacker.Pack((NewArrayExpression)expression);
				case ExpressionType.Parameter:
					return ParameterPacker.Pack((ParameterExpression)expression);
				case ExpressionType.Quote:
					return QuotePacker.Pack((UnaryExpression)expression);
				case ExpressionType.TypeAs:
					return TypeAsPacker.Pack((UnaryExpression)expression);
				case ExpressionType.TypeIs:
					return TypeIsPacker.Pack((TypeBinaryExpression)expression);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		internal static object Pack(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type.IsArray)
			{
				var typeArguments = new[] { type.GetTypeInfo().GetElementType() };
				var methodNameTree = new Dictionary<string, object>(3) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_TYPE_REFERENCE },
					{ Constants.NAME_ATTRIBUTE, typeof(Array).GetCSharpFullName().ToString() },
					{ Constants.ARGUMENTS_ATTRIBUTE, Pack(typeArguments) }
				};
				return methodNameTree;
			}
			else if (type.GetTypeInfo().IsGenericType)
			{
				var typeArguments = type.GetTypeInfo().GetGenericArguments();
				var methodNameTree = new Dictionary<string, object>(3) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_TYPE_REFERENCE },
					{ Constants.NAME_ATTRIBUTE, type.GetCSharpFullName(options: TypeNameFormatOptions.None).ToString() },
					{ Constants.ARGUMENTS_ATTRIBUTE, Pack(typeArguments) }
				};
				return methodNameTree;
			}
			else
			{
				return type.GetCSharpFullName().ToString();
			}
		}
		internal static object Pack(MemberInfo member)
		{
			if (member.DeclaringType == null)
				return null;

			var memberName = (object)TypeNameUtils.RemoveGenericSuffix(member.Name);
			if (member is MethodBase)
			{
				var methodBase = (MethodBase)member;
				if (methodBase.IsGenericMethod)
				{
					var typeArguments = methodBase.GetGenericArguments();
					var methodNameTree = new Dictionary<string, object>(3) {
						{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_REFERENCE },
						{ Constants.NAME_ATTRIBUTE, memberName },
						{ Constants.ARGUMENTS_ATTRIBUTE, Pack(typeArguments) }
					};
					memberName = methodNameTree;
				}

				var parameters = methodBase.GetParameters();
				var arguments = new Dictionary<string, object>(parameters.Length);
				foreach (var parameterInfo in parameters)
				{
					var key = Constants.GetIndexAsString(parameterInfo.Position);
					var value = parameterInfo.Name ?? key;
					arguments[key] = value;
				}

				return new Dictionary<string, object>(4) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_REFERENCE },
					{ Constants.TYPE_ATTRIBUTE, Pack(methodBase.DeclaringType) },
					{ Constants.NAME_ATTRIBUTE, memberName },
					{ Constants.ARGUMENTS_ATTRIBUTE, arguments }
				};
			}
			else
			{
				return new Dictionary<string, object>(3) {
					{ Constants.EXPRESSION_TYPE_ATTRIBUTE, Constants.EXPRESSION_TYPE_MEMBER_REFERENCE },
					{ Constants.TYPE_ATTRIBUTE, Pack(member.DeclaringType) },
					{ Constants.NAME_ATTRIBUTE, memberName },
				};
			}
		}
		internal static object Pack(Expression[] arguments, string[] names)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (names != null && names.Length != arguments.Length) throw new ArgumentOutOfRangeException("names");

			var argumentsNode = new Dictionary<string, object>(arguments.Length);
			for (var i = 0; i < arguments.Length; i++)
			{
				var name = (names != null ? names[i] : null) ?? Constants.GetIndexAsString(i);
				argumentsNode[name] = Pack(arguments[i]);
			}
			return argumentsNode;
		}
		private static object Pack(Type[] typeArguments)
		{
			if (typeArguments == null) throw new ArgumentNullException("typeArguments");

			var argumentsNode = new Dictionary<string, object>(typeArguments.Length);
			for (var i = 0; i < typeArguments.Length; i++)
			{
				var key = Constants.GetIndexAsString(i);
				argumentsNode[key] = Pack(typeArguments[i]);
			}
			return argumentsNode;
		}
	}
}
