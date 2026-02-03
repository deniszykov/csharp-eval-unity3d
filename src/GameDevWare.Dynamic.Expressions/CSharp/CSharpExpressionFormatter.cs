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
using System.Text;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	///     Type extension class for <see cref="Expression" />. Add expression formatting methods for <see cref="Expression" />
	///     .
	/// </summary>
	public static class CSharpExpressionFormatter
	{
		/// <summary>
		///     Renders syntax tree into string representation.
		/// </summary>
		/// <param name="expression">Syntax tree.</param>
		/// <param name="checkedScope">
		///     True to assume all arithmetic and conversion operation is checked for overflows. Overwise
		///     false.
		/// </param>
		/// <returns>Rendered expression.</returns>
		[Obsolete("Use CSharpExpression.Format() instead. Will be removed in next releases.", true)]
		public static string Render(this Expression expression, bool checkedScope = CSharpExpression.DEFAULT_CHECKED_SCOPE)
		{
			return Format(expression, checkedScope);
		}

		internal static string Format(this Expression expression, bool checkedScope = CSharpExpression.DEFAULT_CHECKED_SCOPE)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var builder = new StringBuilder();
			Render(expression, builder, true, checkedScope);

			return builder.ToString();
		}

		private static void Render(Expression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (expression.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AddChecked:
				case ExpressionType.Add:
				case ExpressionType.AndAlso:
				case ExpressionType.Coalesce:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Power:
					RenderBinary((BinaryExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
					RenderUnary((UnaryExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.ArrayLength:
					Render(((UnaryExpression)expression).Operand, builder, false, checkedScope);
					builder.Append(".Length");
					break;
				case ExpressionType.ArrayIndex:
					RenderArrayIndex(expression, builder, checkedScope);
					break;
				case ExpressionType.Call:
					RenderCall((MethodCallExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.Conditional:
					RenderCondition((ConditionalExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
					RenderConvert((UnaryExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.Invoke:
					var invocationExpression = (InvocationExpression)expression;
					Render(invocationExpression.Expression, builder, false, checkedScope);
					builder.Append('(');
					RenderArguments(invocationExpression.Arguments, builder, checkedScope);
					builder.Append(')');
					break;
				case ExpressionType.Constant:
					RenderConstant((ConstantExpression)expression, builder);
					break;
				case ExpressionType.Parameter:
					var param = (ParameterExpression)expression;
					builder.Append(param.Name);
					break;
				case ExpressionType.Quote:
					Render(((UnaryExpression)expression).Operand, builder, true, checkedScope);
					break;
				case ExpressionType.MemberAccess:
					RenderMemberAccess((MemberExpression)expression, builder, checkedScope);
					break;
				case ExpressionType.TypeAs:
					var typeAsExpression = (UnaryExpression)expression;
					Render(typeAsExpression.Operand, builder, false, checkedScope);
					builder.Append(" as ");
					RenderType(typeAsExpression.Type, builder);
					break;
				case ExpressionType.TypeIs:
					var typeIsExpression = (TypeBinaryExpression)expression;
					Render(typeIsExpression.Expression, builder, false, checkedScope);
					builder.Append(" is ");
					RenderType(typeIsExpression.TypeOperand, builder);
					break;
				case ExpressionType.Lambda:
					RenderLambda((LambdaExpression)expression, builder, checkedScope);
					break;
				case ExpressionType.New:
					RenderNew((NewExpression)expression, builder, checkedScope);
					break;
				case ExpressionType.ListInit:
					RenderListInit((ListInitExpression)expression, builder, checkedScope);
					break;
				case ExpressionType.MemberInit:
					RenderMemberInit((MemberInitExpression)expression, builder, checkedScope);
					break;
				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					RenderNewArray((NewArrayExpression)expression, builder, checkedScope);
					break;
				default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.NodeType));
			}
		}

		private static void RenderCondition(ConditionalExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (!wrapped) builder.Append('(');

			// try to detect null-propagation operation
			if (ExpressionUtils.ExtractNullPropagationExpression(expression, out var nullTestExpressions, out var continuationExpression))
				RenderNullPropagationExpression(continuationExpression, builder, nullTestExpressions, checkedScope);
			else
			{
				var cond = expression;
				Render(cond.Test, builder, true, checkedScope);
				builder.Append(" ? ");
				Render(cond.IfTrue, builder, true, checkedScope);
				builder.Append(" : ");
				Render(cond.IfFalse, builder, true, checkedScope);
			}

			if (!wrapped) builder.Append(')');
		}
		private static void RenderNullPropagationExpression
			(Expression expression, StringBuilder builder, List<Expression> nullPropagationExpressions, bool checkedScope)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (nullPropagationExpressions == null) throw new ArgumentNullException(nameof(nullPropagationExpressions));

			var callExpression = expression as MethodCallExpression;
			var memberExpression = expression as MemberExpression;
			var indexExpression = expression as BinaryExpression;
			var isIndexer = (callExpression != null && callExpression.NodeType == ExpressionType.ArrayIndex) ||
				(callExpression != null && callExpression.Method.IsIndexer());

			if (memberExpression != null)
			{
				if (memberExpression.Member.IsStatic())
				{
					var methodType = memberExpression.Member.DeclaringType;
					if (methodType != null)
						RenderType(methodType, builder);
					builder.Append('.');
				}
				else
				{
					RenderNullPropagationExpression(memberExpression.Expression, builder, nullPropagationExpressions, checkedScope);
					builder.Append(nullPropagationExpressions.Contains(memberExpression.Expression) ? "?." : ".");
				}

				builder.Append(memberExpression.Member.Name);
			}
			else if (callExpression != null && !isIndexer)
			{
				if (callExpression.Method.IsStatic)
				{
					var methodType = callExpression.Method.DeclaringType;
					if (methodType != null)
						RenderType(methodType, builder);
					builder.Append('.');
				}
				else
				{
					RenderNullPropagationExpression(callExpression.Object, builder, nullPropagationExpressions, checkedScope);
					builder.Append(nullPropagationExpressions.Contains(callExpression.Object) ? "?." : ".");
				}

				builder.Append(callExpression.Method.Name);
				builder.Append('(');
				RenderArguments(callExpression.Arguments, builder, checkedScope);
				builder.Append(')');
			}
			else if (callExpression != null)
			{
				RenderNullPropagationExpression(callExpression.Object, builder, nullPropagationExpressions, checkedScope);

				builder.Append(nullPropagationExpressions.Contains(callExpression.Object) ? "?[" : "[");
				RenderArguments(callExpression.Arguments, builder, checkedScope);
				builder.Append(']');
			}
			else if (indexExpression != null)
			{
				RenderNullPropagationExpression(indexExpression.Left, builder, nullPropagationExpressions, checkedScope);

				builder.Append(nullPropagationExpressions.Contains(indexExpression.Left) ? "?[" : "[");
				Render(indexExpression.Right, builder, false, checkedScope);
				builder.Append(']');
			}
			else
				Render(expression, builder, false, checkedScope);
		}
		private static void RenderConvert(UnaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (!expression.Type.GetTypeInfo().IsInterface && expression.Type.GetTypeInfo().IsAssignableFrom(expression.Operand.Type.GetTypeInfo()))
			{
				// implicit convertion is not rendered
				Render(expression.Operand, builder, true, checkedScope);
				return;
			}

			var closeParent = false;
			var checkedOperation = expression.NodeType == ExpressionType.ConvertChecked;
			if (checkedOperation && !checkedScope)
			{
				builder.Append("checked(");
				checkedScope = true;
				closeParent = true;
			}
			else if (!checkedOperation && checkedScope)
			{
				builder.Append("unchecked(");
				checkedScope = false;
				closeParent = true;
			}
			else if (!wrapped)
			{
				builder.Append('(');
				closeParent = true;
			}

			builder.Append('(');
			RenderType(expression.Type, builder);
			builder.Append(')');
			Render(expression.Operand, builder, false, checkedScope);

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderNewArray(NewArrayExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			if (expression.NodeType == ExpressionType.NewArrayBounds)
			{
				builder.Append("new ");
				RenderType(expression.Type.GetElementType(), builder);
				builder.Append('[');
				var isFirstArgument = true;
				foreach (var argument in expression.Expressions)
				{
					if (!isFirstArgument) builder.Append(", ");
					Render(argument, builder, false, checkedScope);
					isFirstArgument = false;
				}

				builder.Append(']');
			}
			else
			{
				builder.Append("new ");
				RenderType(expression.Type.GetElementType(), builder);
				builder.Append("[] { ");
				var isFirstInitializer = true;
				foreach (var initializer in expression.Expressions)
				{
					if (!isFirstInitializer) builder.Append(", ");
					Render(initializer, builder, false, checkedScope);
					isFirstInitializer = false;
				}

				builder.Append(" }");
			}
		}
		private static void RenderMemberInit(MemberInitExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			RenderNew(expression.NewExpression, builder, checkedScope);
			if (expression.Bindings.Count > 0)
			{
				builder.Append(" { ");
				var isFirstBinder = true;
				foreach (var memberBinding in expression.Bindings)
				{
					if (!isFirstBinder) builder.Append(", ");

					RenderMemberBinding(memberBinding, builder, checkedScope);

					isFirstBinder = false;
				}

				builder.Append(" }");
			}
		}
		private static void RenderListInit(ListInitExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			RenderNew(expression.NewExpression, builder, checkedScope);
			if (expression.Initializers.Count > 0)
			{
				builder.Append(" { ");
				var isFirstInitializer = true;
				foreach (var initializer in expression.Initializers)
				{
					if (!isFirstInitializer) builder.Append(", ");

					RenderListInitializer(initializer, builder, checkedScope);

					isFirstInitializer = false;
				}

				builder.Append(" }");
			}
		}
		private static void RenderLambda(LambdaExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			builder.Append("new ");
			RenderType(expression.Type, builder);
			builder.Append(" (");

			if (expression.Parameters.Count != 1) builder.Append('(');
			var firstParam = true;
			foreach (var param in expression.Parameters)
			{
				if (!firstParam) builder.Append(", ");
				builder.Append(param.Name);
				firstParam = false;
			}

			if (expression.Parameters.Count != 1) builder.Append(')');

			builder.Append(" => ");
			Render(expression.Body, builder, false, checkedScope);

			builder.Append(')');
		}
		private static void RenderNew(NewExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var constructorArguments = expression.Arguments;
			if (expression.Members != null && expression.Members.Count > 0)
				constructorArguments = constructorArguments.Take(expression.Constructor?.GetParameters().Length ?? 0).ToList().AsReadOnly();

			builder.Append("new ");
			RenderType(expression.Constructor?.DeclaringType ?? typeof(object), builder);
			builder.Append('(');
			RenderArguments(constructorArguments, builder, checkedScope);
			builder.Append(')');

			if (expression.Members != null && expression.Members.Count > 0)
			{
				var isFirstMember = true;
				var memberIdx = constructorArguments.Count;
				builder.Append(" { ");
				foreach (var memberInit in expression.Members)
				{
					if (!isFirstMember) builder.Append(", ");

					builder.Append(memberInit.Name).Append(" = ");
					Render(expression.Arguments[memberIdx], builder, true, checkedScope);

					isFirstMember = false;
					memberIdx++;
				}

				builder.Append(" }");
			}
		}
		private static void RenderMemberAccess(MemberExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var prop = expression.Member as PropertyInfo;
			var field = expression.Member as FieldInfo;
			var declType = expression.Member.DeclaringType;
			var isStatic = (field != null && field.IsStatic) || (prop != null && prop.IsStatic());
			if (expression.Expression != null)
			{
				Render(expression.Expression, builder, false, checkedScope);
				builder.Append('.');
			}
			else if (isStatic && declType != null)
			{
				RenderType(declType, builder);
				builder.Append('.');
			}

			builder.Append(expression.Member.Name);
		}
		private static void RenderCall(MethodCallExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var isIndex = expression.NodeType == ExpressionType.ArrayIndex;
			if (expression.Method.IsStatic)
			{
				if (expression.Method.DeclaringType == typeof(string) &&
					expression.Method.Name == "Concat" &&
					expression.Arguments.All(a => a.Type == typeof(string) || a.Type == typeof(object)))
				{
					if (wrapped) builder.Append('(');
					for (var i = 0; i < expression.Arguments.Count; i++)
					{
						Render(expression.Arguments[i], builder, true, checkedScope);
						if (i != expression.Arguments.Count - 1)
							builder.Append(" + ");
					}

					if (wrapped) builder.Append(')');
					return;
				}

				var methodType = expression.Method.DeclaringType;
				if (methodType != null)
					RenderType(methodType, builder);
			}
			else
				Render(expression.Object, builder, false, checkedScope);

			if (isIndex)
			{
				builder.Append('[');
				RenderArguments(expression.Arguments, builder, checkedScope);
				builder.Append(']');
			}
			else
			{
				var method = expression.Method;
				builder.Append('.');
				builder.Append(method.Name);
				if (method.IsGenericMethod)
				{
					builder.Append('<');
					foreach (var genericArgument in method.GetGenericArguments())
					{
						RenderType(genericArgument, builder);
						builder.Append(',');
					}

					builder.Length--;
					builder.Append('>');
				}

				builder.Append('(');
				RenderArguments(expression.Arguments, builder, checkedScope);
				builder.Append(')');
			}
		}
		private static void RenderArrayIndex(Expression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			if (expression is BinaryExpression binaryExpression)
			{
				Render(binaryExpression.Left, builder, false, checkedScope);
				builder.Append('[');
				Render(binaryExpression.Right, builder, false, checkedScope);
				builder.Append(']');
			}
			else if (expression is MethodCallExpression methodCallExpression)
			{
				if (methodCallExpression.Method.IsStatic)
				{
					var methodType = methodCallExpression.Method.DeclaringType;
					if (methodType != null)
					{
						RenderType(methodType, builder);
						builder.Append('.');
					}
				}
				else
					Render(methodCallExpression.Object, builder, false, checkedScope);

				builder.Append('[');
				RenderArguments(methodCallExpression.Arguments, builder, checkedScope);
				builder.Append(']');
			}
			else
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_INVALIDCONSTANTEXPRESSION, expression.NodeType));
		}
		private static void RenderConstant(ConstantExpression expression, StringBuilder builder)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			if (expression.Value == null)
			{
				if (expression.Type == typeof(object))
				{
					builder.Append("null");
					return;
				}

				builder.Append("default(");
				RenderType(expression.Type, builder);
				builder.Append(')');
				return;
			}

			var strValue = Convert.ToString(expression.Value, Constants.DefaultFormatProvider) ?? string.Empty;
			if (expression.Type == typeof(string))
				CSharpSyntaxTreeFormatter.RenderTextLiteral(strValue, builder, false);
			else if (expression.Type == typeof(char))
				CSharpSyntaxTreeFormatter.RenderTextLiteral(strValue, builder, true);
			else if (expression.Type == typeof(Type))
			{
				builder.Append("typeof(");
				RenderType((Type)expression.Value, builder);
				builder.Append(')');
			}
			else if (expression.Type == typeof(ushort) || expression.Type == typeof(uint))
				builder.Append(strValue).Append('u');
			else if (expression.Type == typeof(ulong))
				builder.Append(strValue).Append("ul");
			else if (expression.Type == typeof(long))
				builder.Append(strValue).Append('l');
			else if (expression.Type == typeof(float) || expression.Type == typeof(double))
			{
				var is32Bit = expression.Type == typeof(float);
				var doubleValue = Convert.ToDouble(expression.Value, Constants.DefaultFormatProvider);

				if (double.IsPositiveInfinity(doubleValue))
					builder.Append(is32Bit ? "System.Single.PositiveInfinity" : "System.Double.PositiveInfinity");
				if (double.IsNegativeInfinity(doubleValue))
					builder.Append(is32Bit ? "System.Single.NegativeInfinity" : "System.Double.NegativeInfinity");
				if (double.IsNaN(doubleValue))
					builder.Append(is32Bit ? "System.Single.NaN" : "System.Double.NaN");
				else
					builder.Append(doubleValue.ToString("R", Constants.DefaultFormatProvider));
				builder.Append(is32Bit ? "f" : "d");
			}
			else if (expression.Type == typeof(decimal))
				builder.Append(strValue).Append('m');
			else if (expression.Type == typeof(bool))
				builder.Append(strValue.ToLowerInvariant());
			else if (expression.Type == typeof(byte) || expression.Type == typeof(sbyte) || expression.Type == typeof(short) || expression.Type == typeof(int))
				builder.Append(strValue);
			else
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_INVALIDCONSTANTEXPRESSION, expression.Type));
		}
		private static void RenderUnary(UnaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.NegateChecked || expression.NodeType != ExpressionType.Negate && checkedScope;
			var closeParent = false;
			if (checkedOperation && !checkedScope)
			{
				builder.Append("checked(");
				checkedScope = true;
				closeParent = true;
			}
			else if (!checkedOperation && checkedScope)
			{
				builder.Append("unchecked(");
				checkedScope = false;
				closeParent = true;
			}
			else if (!wrapped)
			{
				builder.Append('(');
				closeParent = true;
			}

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (expression.NodeType)
			{
				case ExpressionType.NegateChecked:
				case ExpressionType.Negate:
					builder.Append('-');
					break;
				case ExpressionType.UnaryPlus:
					builder.Append('+');
					break;
				case ExpressionType.Not:
					// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
					switch (ReflectionUtils.GetTypeCode(expression.Operand.Type))
					{
						case TypeCode.Char:
						case TypeCode.SByte:
						case TypeCode.Byte:
						case TypeCode.Int16:
						case TypeCode.UInt16:
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
						default:
							builder.Append('~');
							break;
					}

					break;
				default:
					throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.NodeType));
			}

			Render(expression.Operand, builder, false, checkedScope);

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderBinary(BinaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.AddChecked ||
				expression.NodeType == ExpressionType.MultiplyChecked ||
				expression.NodeType == ExpressionType.SubtractChecked ||
				(expression.NodeType != ExpressionType.Add &&
					expression.NodeType != ExpressionType.Multiply &&
					expression.NodeType != ExpressionType.Subtract &&
					checkedScope);

			var closeParent = false;
			if (checkedOperation && !checkedScope)
			{
				builder.Append("checked(");
				checkedScope = true;
				closeParent = true;
			}
			else if (!checkedOperation && checkedScope)
			{
				builder.Append("unchecked(");
				checkedScope = false;
				closeParent = true;
			}
			else if (!wrapped)
			{
				builder.Append('(');
				closeParent = true;
			}

			Render(expression.Left, builder, false, checkedScope);

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (expression.NodeType)
			{
				case ExpressionType.And:
					builder.Append(" & ");
					break;
				case ExpressionType.AndAlso:
					builder.Append(" && ");
					break;
				case ExpressionType.AddChecked:
				case ExpressionType.Add:
					builder.Append(" + ");
					break;
				case ExpressionType.Coalesce:
					builder.Append(" ?? ");
					break;
				case ExpressionType.Divide:
					builder.Append(" / ");
					break;
				case ExpressionType.Equal:
					builder.Append(" == ");
					break;
				case ExpressionType.ExclusiveOr:
					builder.Append(" ^ ");
					break;
				case ExpressionType.GreaterThan:
					builder.Append(" > ");
					break;
				case ExpressionType.GreaterThanOrEqual:
					builder.Append(" >= ");
					break;
				case ExpressionType.LeftShift:
					builder.Append(" << ");
					break;
				case ExpressionType.LessThan:
					builder.Append(" < ");
					break;
				case ExpressionType.LessThanOrEqual:
					builder.Append(" <= ");
					break;
				case ExpressionType.Modulo:
					builder.Append(" % ");
					break;
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
					builder.Append(" * ");
					break;
				case ExpressionType.NotEqual:
					builder.Append(" != ");
					break;
				case ExpressionType.Or:
					builder.Append(" | ");
					break;
				case ExpressionType.OrElse:
					builder.Append(" || ");
					break;
				case ExpressionType.RightShift:
					builder.Append(" >> ");
					break;
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					builder.Append(" - ");
					break;
				case ExpressionType.Power:
					builder.Append(" ** ");
					break;
				default:
					throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.NodeType));
			}

			Render(expression.Right, builder, false, checkedScope);

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderArguments(ReadOnlyCollection<Expression> arguments, StringBuilder builder, bool checkedScope)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var firstArgument = true;
			foreach (var argument in arguments)
			{
				if (!firstArgument)
					builder.Append(", ");
				Render(argument, builder, true, checkedScope);
				firstArgument = false;
			}
		}
		private static void RenderMemberBinding(MemberBinding memberBinding, StringBuilder builder, bool checkedScope)
		{
			if (memberBinding == null) throw new ArgumentException("memberBinding");
			if (builder == null) throw new ArgumentException("builder");

			builder.Append(memberBinding.Member.Name)
				.Append(" = ");

			switch (memberBinding.BindingType)
			{
				case MemberBindingType.Assignment:
					Render(((MemberAssignment)memberBinding).Expression, builder, true, checkedScope);
					break;
				case MemberBindingType.MemberBinding:
					builder.Append("{ ");
					var isFirstBinder = true;
					foreach (var subMemberBinding in ((MemberMemberBinding)memberBinding).Bindings)
					{
						if (!isFirstBinder) builder.Append(", ");
						RenderMemberBinding(subMemberBinding, builder, checkedScope);
						isFirstBinder = false;
					}

					builder.Append("} ");
					break;
				case MemberBindingType.ListBinding:
					builder.Append(" { ");
					var isFirstInitializer = true;
					foreach (var initializer in ((MemberListBinding)memberBinding).Initializers)
					{
						if (!isFirstInitializer) builder.Append(", ");
						RenderListInitializer(initializer, builder, checkedScope);
						isFirstInitializer = false;
					}

					builder.Append(" }");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		private static void RenderListInitializer(ElementInit initializer, StringBuilder builder, bool checkedScope)
		{
			if (initializer == null) throw new ArgumentException("initializer");
			if (builder == null) throw new ArgumentException("builder");

			if (initializer.Arguments.Count == 1)
				Render(initializer.Arguments[0], builder, true, checkedScope);
			else
			{
				var isFirstArgument = true;
				builder.Append("{ ");
				foreach (var argument in initializer.Arguments)
				{
					if (!isFirstArgument) builder.Append(", ");
					Render(argument, builder, true, checkedScope);

					isFirstArgument = false;
				}

				builder.Append('}');
			}
		}

		private static void RenderType(Type type, StringBuilder builder)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			type.GetCSharpFullName(builder, TypeNameFormatOptions.UseAliases | TypeNameFormatOptions.IncludeGenericArguments);
		}
	}
}
