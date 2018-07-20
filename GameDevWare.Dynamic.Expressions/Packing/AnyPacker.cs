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

			if (type.GetTypeInfo().IsGenericType)
			{
				var typeArguments = AnyPacker.Pack(type.GetTypeInfo().GetGenericArguments());
				var methodNameTree = new Dictionary<string, object>(2) {
					{Constants.NAME_ATTRIBUTE, NameUtils.RemoveGenericSuffix(NameUtils.WriteFullName(type))},
					{Constants.ARGUMENTS_ATTRIBUTE, typeArguments }
				};
				return methodNameTree;
			}
			else
			{
				return NameUtils.WriteFullName(type);
			}
		}
		internal static object Pack(MethodInfo method)
		{
			if (method.DeclaringType == null)
				return null;

			var parameters = method.GetParameters();
			var methodName = (object)NameUtils.RemoveGenericSuffix(method.Name);
			if (method.IsGenericMethod)
			{
				var typeArguments = AnyPacker.Pack(method.GetGenericArguments());
				var methodNameTree = new Dictionary<string, object>(2) {
					{Constants.NAME_ATTRIBUTE, methodName},
					{Constants.ARGUMENTS_ATTRIBUTE, typeArguments }
				};
				methodName = methodNameTree;
			}
			var arguments = new Dictionary<string, object>(parameters.Length);
			foreach (var parameterInfo in parameters)
			{
				var key = parameterInfo.Name;
				if (string.IsNullOrEmpty(key))
				{
					key = parameterInfo.Position.ToString();
				}
				arguments[key] = parameterInfo.Position;
			}

			return new Dictionary<string, object>(3) {
				{Constants.TYPE_ATTRIBUTE, Pack(method.DeclaringType)},
				{Constants.NAME_ATTRIBUTE, methodName},
				{Constants.ARGUMENTS_ATTRIBUTE, arguments}
			};
		}
		internal static object Pack(Expression[] arguments, string[] names)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (names != null && names.Length != arguments.Length) throw new ArgumentOutOfRangeException("names");

			var argumentsNode = new Dictionary<string, object>(arguments.Length);
			for (var i = 0; i < arguments.Length; i++)
			{
				var name = (names != null) ? names[i] : Constants.GetIndexAsString(i);
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
				var name = Constants.GetIndexAsString(i);
				argumentsNode[name] = Pack(typeArguments[i]);
			}
			return argumentsNode;
		}
	}

	internal static class NewArrayPacker
	{
		public static Dictionary<string, object> Pack(NewArrayExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class NewPacker
	{
		public static Dictionary<string, object> Pack(NewExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class MemberInitPacker
	{
		public static Dictionary<string, object> Pack(MemberInitExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class MemberAccessPacker
	{
		public static Dictionary<string, object> Pack(MemberExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class ListInitPacker
	{
		public static Dictionary<string, object> Pack(ListInitExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class LambdaPacker
	{
		public static Dictionary<string, object> Pack(LambdaExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class InvokePacker
	{
		public static Dictionary<string, object> Pack(InvocationExpression expression)
		{
			throw new NotImplementedException();
		}
	}

	internal static class CallPacker
	{
		public static Dictionary<string, object> Pack(MethodCallExpression expression)
		{
			throw new NotImplementedException();
		}
	}
}
