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
using System.Runtime.InteropServices;
using System.Text;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	/// Helper class for rendering parsed C# expressions to it's string representation.
	/// </summary>
	public static class CSharpExpressionRenderer
	{
		/// <summary>
		/// Renders syntax tree into string representation.
		/// </summary>
		/// <param name="node">Syntax tree.</param>
		/// <param name="checkedScope">True to assume all arithmetic and conversion operation is checked for overflows. Overwise false.</param>
		/// <returns>Rendered expression.</returns>
		public static string Render(this SyntaxTreeNode node, bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			var builder = new StringBuilder();
			Render(node, builder, true, checkedScope);

			return builder.ToString();
		}
		/// <summary>
		/// Renders syntax tree into string representation.
		/// </summary>
		/// <param name="expression">Syntax tree.</param>
		/// <param name="checkedScope">True to assume all arithmetic and conversion operation is checked for overflows. Overwise false.</param>
		/// <returns>Rendered expression.</returns>
		public static string Render(this Expression expression, bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var builder = new StringBuilder();
			Render(expression, builder, true, checkedScope);

			return builder.ToString();
		}

		private static void Render(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));

			try
			{
				var expressionType = (string)expressionTypeObj;
				switch (expressionType)
				{
					case "Invoke":
					case "Index": RenderInvokeOrIndex(node, builder, checkedScope); break;
					case "Enclose":
					case "UncheckedScope":
					case "CheckedScope":
					case "Group": RenderGroup(node, builder, checkedScope); break;
					case "Constant": RenderConstant(node, builder); break;
					case "PropertyOrField": RenderPropertyOrField(node, builder, checkedScope); break;
					case "TypeOf": RenderTypeOf(node, builder); break;
					case "Default": RenderDefault(node, builder); break;
					case "NewArrayBounds":
					case "New": RenderNew(node, builder, checkedScope); break;
					case "UnaryPlus":
					case "Negate":
					case "NegateChecked":
					case "Not":
					case "Complement": RenderUnary(node, builder, wrapped, checkedScope); break;
					case "Divide":
					case "Multiply":
					case "MultiplyChecked":
					case "Modulo":
					case "Add":
					case "AddChecked":
					case "Subtract":
					case "SubtractChecked":
					case "LeftShift":
					case "RightShift":
					case "GreaterThan":
					case "GreaterThanOrEqual":
					case "LessThan":
					case "LessThanOrEqual":
					case "Equal":
					case "NotEqual":
					case "And":
					case "Or":
					case "ExclusiveOr":
					case "AndAlso":
					case "OrElse":
					case "Coalesce": RenderBinary(node, builder, wrapped, checkedScope); break;
					case "Condition": RenderCondition(node, builder, wrapped, checkedScope); break;
					case "Convert":
					case "ConvertChecked":
					case "TypeIs":
					case "TypeAs": RenderTypeBinary(node, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_LAMBDA: RenderLambda(node, builder, wrapped, checkedScope); break;
					default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
				}
			}
			catch (InvalidOperationException)
			{
				throw;
			}
			catch (System.Threading.ThreadAbortException)
			{
				throw;
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_RENDERFAILED, expressionTypeObj, exception.Message), exception);
			}
		}
		private static void RenderTypeBinary(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionType = node.GetExpressionType(throwOnError: true);
			var typeName = node.GetTypeName(throwOnError: true);
			var target = node.GetExpression(throwOnError: true);

			Convert.ToString(typeName, Constants.DefaultFormatProvider);

			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_CONVERTCHECKED ? true :
				expressionType == Constants.EXPRESSION_TYPE_CONVERT ? false : checkedScope;

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
				builder.Append("(");
				closeParent = true;
			}

			switch (expressionType)
			{
				case "ConvertChecked":
				case "Convert":
					builder.Append("(");
					RenderTypeName(typeName, builder);
					builder.Append(")");
					Render(target, builder, false, checkedOperation);
					break;
				case "TypeIs":
					Render(target, builder, false, checkedScope);
					builder.Append(" is ");
					RenderTypeName(typeName, builder);
					break;
				case "TypeAs":
					Render(target, builder, false, checkedScope);
					builder.Append(" as ");
					RenderTypeName(typeName, builder);
					break;
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderCondition(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var testObj = default(object);
			if (node.TryGetValue(Constants.TEST_ATTRIBUTE, out testObj) == false || testObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TEST_ATTRIBUTE, node.GetTypeName(throwOnError: true)));

			var ifTrueObj = default(object);
			if (node.TryGetValue(Constants.IFTRUE_ATTRIBUTE, out ifTrueObj) == false || ifTrueObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.IFTRUE_ATTRIBUTE, node.GetTypeName(throwOnError: true)));

			var ifFalseObj = default(object);
			if (node.TryGetValue(Constants.IFFALSE_ATTRIBUTE, out ifFalseObj) == false || ifFalseObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.IFFALSE_ATTRIBUTE, node.GetTypeName(throwOnError: true)));

			var test = (SyntaxTreeNode)testObj;
			var ifTrue = (SyntaxTreeNode)ifTrueObj;
			var ifFalse = (SyntaxTreeNode)ifFalseObj;

			if (!wrapped)
				builder.Append("(");
			Render(test, builder, true, checkedScope);
			builder.Append(" ? ");
			Render(ifTrue, builder, true, checkedScope);
			builder.Append(" : ");
			Render(ifFalse, builder, true, checkedScope);
			if (!wrapped)
				builder.Append(")");
		}
		private static void RenderBinary(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionType = node.GetExpressionType(throwOnError: true);

			var leftObj = default(object);
			if (node.TryGetValue(Constants.LEFT_ATTRIBUTE, out leftObj) == false || leftObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.LEFT_ATTRIBUTE, expressionType));
			var rightObj = default(object);
			if (node.TryGetValue(Constants.RIGHT_ATTRIBUTE, out rightObj) == false || rightObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.RIGHT_ATTRIBUTE, expressionType));

			var left = (SyntaxTreeNode)leftObj;
			var right = (SyntaxTreeNode)rightObj;
			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED || expressionType == Constants.EXPRESSION_TYPE_ADD_CHECKED || expressionType == Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED ? true :
				expressionType == Constants.EXPRESSION_TYPE_MULTIPLY || expressionType == Constants.EXPRESSION_TYPE_ADD || expressionType == Constants.EXPRESSION_TYPE_SUBTRACT ? false : checkedScope;

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
				builder.Append("(");
				closeParent = true;
			}

			Render(left, builder, false, checkedOperation);
			switch (expressionType)
			{
				case "Divide": builder.Append(" / "); break;
				case "MultiplyChecked":
				case "Multiply": builder.Append(" * "); break;
				case "Modulo": builder.Append(" % "); break;
				case "AddChecked":
				case "Add": builder.Append(" + "); break;
				case "SubtractChecked":
				case "Subtract": builder.Append(" - "); break;
				case "LeftShift": builder.Append(" << "); break;
				case "RightShift": builder.Append(" >> "); break;
				case "GreaterThan": builder.Append(" > "); break;
				case "GreaterThanOrEqual": builder.Append(" >= "); break;
				case "LessThan": builder.Append(" < "); break;
				case "LessThanOrEqual": builder.Append(" <= "); break;
				case "Equal": builder.Append(" == "); break;
				case "NotEqual": builder.Append(" != "); break;
				case "And": builder.Append(" & "); break;
				case "Or": builder.Append(" | "); break;
				case "ExclusiveOr": builder.Append(" ^ "); break;
				case "Power": builder.Append(" ** "); break;
				case "AndAlso": builder.Append(" && "); break;
				case "OrElse": builder.Append(" || "); break;
				case "Coalesce": builder.Append(" ?? "); break;
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}
			Render(right, builder, false, checkedOperation);

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderUnary(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var expressionObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType));

			var expression = (SyntaxTreeNode)expressionObj;
			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_NEGATE_CHECKED ? true :
				expressionType == Constants.EXPRESSION_TYPE_NEGATE ? false : checkedScope;

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
				builder.Append("(");
				closeParent = true;
			}

			switch (expressionType)
			{
				case "UnaryPlus":
					builder.Append("+");
					break;
				case "NegateChecked":
				case "Negate":
					builder.Append("-");
					break;
				case "Not":
					builder.Append("!");
					break;
				case "Complement":
					builder.Append("~");
					break;
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}
			Render(expression, builder, false, checkedOperation);

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderNew(SyntaxTreeNode node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionType = node.GetExpressionType(throwOnError: true);
			var typeName = node.GetTypeName(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);

			builder.Append("new ");
			RenderTypeName(typeName, builder);
			if (expressionType == "NewArrayBounds")
				builder.Append("[");
			else
				builder.Append("(");

			RenderArguments(arguments, builder, checkedScope);

			if (expressionType == "NewArrayBounds")
				builder.Append("]");
			else
				builder.Append(")");
		}
		private static void RenderDefault(SyntaxTreeNode node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeName = node.GetTypeName(throwOnError: true);
			builder.Append("default(");
			RenderTypeName(typeName, builder);
			builder.Append(")");
		}
		private static void RenderTypeOf(SyntaxTreeNode node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeName = node.GetTypeName(throwOnError: true);
			builder.Append("typeof(");
			RenderTypeName(typeName, builder);
			builder.Append(")");
		}
		private static void RenderPropertyOrField(SyntaxTreeNode node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var target = node.GetExpression(throwOnError: false);
			var propertyOrFieldName = node.GetPropertyOrFieldName(throwOnError: true);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);
			var arguments = node.GetArguments(throwOnError: false);
			if (target != null)
			{
				Render(target, builder, false, checkedScope);
				if (useNullPropagation)
					builder.Append("?.");
				else
					builder.Append(".");
			}
			builder.Append(propertyOrFieldName);
			if (arguments != null && arguments.Count > 0)
			{
				builder.Append("<");
				for (var i = 0; i < arguments.Count; i++)
				{
					if (i != 0) builder.Append(",");
					var typeArgument = default(SyntaxTreeNode);
					if (arguments.TryGetValue(i, out typeArgument))
						Render(typeArgument, builder, true, checkedScope);
				}
				builder.Append(">");
			}
		}
		private static void RenderConstant(SyntaxTreeNode node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeObj = default(object);
			var valueObj = default(object);
			if (node.TryGetValue(Constants.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TYPE_ATTRIBUTE, node.GetExpressionType(throwOnError: true)));

			if (node.TryGetValue(Constants.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.VALUE_ATTRIBUTE, node.GetExpressionType(throwOnError: true)));

			if (valueObj == null)
			{
				builder.Append("null");
				return;
			}

			var type = Convert.ToString(typeObj, Constants.DefaultFormatProvider);
			var value = Convert.ToString(valueObj, Constants.DefaultFormatProvider);
			switch (type)
			{
				case "System.Char":
				case "Char":
				case "char":
					RenderTextLiteral(value, builder, isChar: true);
					break;
				case "System.String":
				case "String":
				case "string":
					RenderTextLiteral(value, builder, isChar: false);
					break;
				case "UInt16":
				case "System.UInt16":
				case "ushort":
				case "UInt32":
				case "System.UInt32":
				case "uint":
					builder.Append(value);
					builder.Append("u");
					break;
				case "UInt64":
				case "System.UInt64":
				case "ulong":
					builder.Append(value);
					builder.Append("ul");
					break;
				case "Int64":
				case "System.Int64":
				case "long":
					builder.Append(value);
					builder.Append("l");
					break;
				case "Single":
				case "System.Single":
				case "float":
					builder.Append(value);
					builder.Append("f");
					break;
				case "Double":
				case "System.Double":
				case "double":
					builder.Append(value);
					if (value.IndexOf('.') == -1)
						builder.Append("d");
					break;
				case "Decimal":
				case "System.Decimal":
				case "decimal":
					builder.Append(value);
					builder.Append("m");
					break;
				case "Boolean":
				case "System.Boolean":
				case "bool":
					builder.Append(value.ToLowerInvariant());
					break;
				default:
					builder.Append(value);
					break;

			}
		}
		private static void RenderGroup(SyntaxTreeNode node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var expressionObj = default(object);
			if (node.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is SyntaxTreeNode == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE, expressionType));

			var expression = (SyntaxTreeNode)expressionObj;

			if (expressionType == Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE)
				builder.Append("unchecked");
			if (expressionType == Constants.EXPRESSION_TYPE_CHECKED_SCOPE)
				builder.Append("checked");
			builder.Append("(");
			Render(expression, builder, true, checkedScope);
			builder.Append(")");
		}
		private static void RenderInvokeOrIndex(SyntaxTreeNode node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionType = node.GetExpressionType(throwOnError: true);
			var target = node.GetExpression(throwOnError: true);
			var arguments = node.GetArguments(throwOnError: false);
			var useNullPropagation = node.GetUseNullPropagation(throwOnError: false);

			Render(target, builder, false, checkedScope);
			builder.Append(expressionType == "Invoke" ? "(" : (useNullPropagation ? "?[" : "["));
			RenderArguments(arguments, builder, checkedScope);
			builder.Append(expressionType == "Invoke" ? ")" : "]");
		}
		private static void RenderLambda(SyntaxTreeNode node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentException("node");
			if (builder == null) throw new ArgumentException("builder");

			if (!wrapped) builder.Append("(");

			var arguments = node.GetArguments(throwOnError: false);
			var body = node.GetExpression(throwOnError: true);
			if (arguments.Count != 1) builder.Append("(");
			var firstParam = true;
			foreach (var param in arguments.Values)
			{
				if (firstParam == false) builder.Append(", ");
				Render(param, builder, true, checkedScope);
				firstParam = false;
			}
			if (arguments.Count != 1) builder.Append(")");
			builder.Append(" => ");
			Render(body, builder, false, checkedScope);

			if (!wrapped) builder.Append(")");
		}
		private static void RenderArguments(ArgumentsTree arguments, StringBuilder builder, bool checkedScope)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (builder == null) throw new ArgumentNullException("builder");

			var firstArgument = true;
			foreach (var argumentName in arguments.Keys)
			{
				var positionalArguments = new SortedDictionary<int, SyntaxTreeNode>();
				var namedArguments = new SortedDictionary<string, SyntaxTreeNode>();
				var position = default(int);
				if (int.TryParse(argumentName, out position))
					positionalArguments[position] = arguments[argumentName];
				else
					namedArguments[argumentName] = arguments[argumentName];

				foreach (var argument in positionalArguments.Values)
				{
					if (!firstArgument)
						builder.Append(", ");
					Render(argument, builder, true, checkedScope);
					firstArgument = false;
				}
				foreach (var argumentKv in namedArguments)
				{
					if (!firstArgument)
						builder.Append(", ");
					builder.Append(argumentKv.Key).Append(": ");
					Render(argumentKv.Value, builder, true, checkedScope);
					firstArgument = false;
				}
			}
		}
		private static void RenderTypeName(object typeName, StringBuilder builder)
		{
			if (typeName == null) throw new ArgumentNullException("typeName");
			if (builder == null) throw new ArgumentNullException("builder");


			if (typeName is SyntaxTreeNode)
			{
				Render((SyntaxTreeNode)typeName, builder, true, true);
			}
			else
			{
				builder.Append(Convert.ToString(typeName, Constants.DefaultFormatProvider));
			}
		}

		private static void Render(Expression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentNullException("expression");
			if (builder == null) throw new ArgumentNullException("builder");

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
					RenderCall((MethodCallExpression)expression, builder, checkedScope);
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
					builder.Append("(");
					RenderArguments(invocationExpression.Arguments, builder, checkedScope);
					builder.Append(")");
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
					RenderLambda((LambdaExpression)expression, builder, wrapped, checkedScope);
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
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.Type));
			}
		}

		private static void RenderCondition(ConditionalExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			var baseExpression = default(Expression);
			var continuationExpression = default(Expression);
			// try to detect null-propagation operation
			if (Binder.ExtractNullPropagationExpression(expression, out baseExpression, out continuationExpression))
			{
				var callExpression = continuationExpression as MethodCallExpression;
				var memberExpression = continuationExpression as MemberExpression;
				var indexExpression = continuationExpression as BinaryExpression;

				Render(baseExpression, builder, false, checkedScope);
				if (memberExpression != null)
				{
					builder.Append("?.");
					builder.Append(memberExpression.Member.Name);
				}
				else if (callExpression != null && callExpression.NodeType == ExpressionType.Call)
				{
					builder.Append("?.");
					builder.Append(callExpression.Method.Name);
					builder.Append("(");
					RenderArguments(callExpression.Arguments, builder, checkedScope);
					builder.Append(")");
				}
				else if (callExpression != null && callExpression.NodeType == ExpressionType.ArrayIndex)
				{
					builder.Append("?[");
					RenderArguments(callExpression.Arguments, builder, checkedScope);
					builder.Append("]");
				}
				else if (indexExpression != null)
				{
					builder.Append("?[");
					Render(indexExpression.Right, builder, false, checkedScope);
					builder.Append("]");
				}
				else
				{
					throw new InvalidOperationException(string.Format("Unknown null-propagation pattern met: {0}?.{1}.", baseExpression.NodeType, continuationExpression.NodeType));
				}
			}
			else
			{
				var cond = expression;
				if (!wrapped) builder.Append("(");
				Render(cond.Test, builder, true, checkedScope);
				builder.Append(" ? ");
				Render(cond.IfTrue, builder, true, checkedScope);
				builder.Append(" : ");
				Render(cond.IfFalse, builder, true, checkedScope);
				if (!wrapped) builder.Append(")");
			}
		}
		private static void RenderConvert(UnaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression.Type.IsInterface == false && expression.Type.IsAssignableFrom(expression.Operand.Type))
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
				builder.Append("(");
				closeParent = true;
			}

			builder.Append("(");
			RenderType(expression.Type, builder);
			builder.Append(")");
			Render(expression.Operand, builder, false, checkedScope);

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderNewArray(NewArrayExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			if (expression.NodeType == ExpressionType.NewArrayBounds)
			{
				builder.Append("new ");
				RenderType(expression.Type.GetElementType(), builder);
				builder.Append("[");
				var isFirstArgument = true;
				foreach (var argument in expression.Expressions)
				{
					if (isFirstArgument == false) builder.Append(", ");
					Render(argument, builder, false, checkedScope);
					isFirstArgument = false;
				}
				builder.Append("]");
			}
			else
			{
				builder.Append("new ");
				RenderType(expression.Type.GetElementType(), builder);
				builder.Append("[] { ");
				var isFirstInitializer = true;
				foreach (var initializer in expression.Expressions)
				{
					if (isFirstInitializer == false) builder.Append(", ");
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
					if (isFirstBinder == false) builder.Append(", ");

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
					if (isFirstInitializer == false) builder.Append(", ");

					RenderListInitializer(initializer, builder, checkedScope);

					isFirstInitializer = false;
				}
				builder.Append(" }");
			}
		}
		private static void RenderLambda(LambdaExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			builder.Append("new ");
			RenderType(expression.Type, builder);
			builder.Append(" (");

			if (expression.Parameters.Count != 1) builder.Append("(");
			var firstParam = true;
			foreach (var param in expression.Parameters)
			{
				if (firstParam == false) builder.Append(", ");
				builder.Append(param.Name);
				firstParam = false;
			}
			if (expression.Parameters.Count != 1) builder.Append(")");

			builder.Append(" => ");
			Render(expression.Body, builder, false, checkedScope);

			builder.Append(")");
		}
		private static void RenderNew(NewExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var constructorArguments = expression.Arguments;
			if (expression.Members != null && expression.Members.Count > 0)
				constructorArguments = constructorArguments.Take(expression.Constructor.GetParameters().Length).ToList().AsReadOnly();

			builder.Append("new ");
			RenderType(expression.Constructor.DeclaringType, builder);
			builder.Append("(");
			RenderArguments(constructorArguments, builder, checkedScope);
			builder.Append(")");

			if (expression.Members != null && expression.Members.Count > 0)
			{
				var isFirstMember = true;
				var memberIdx = constructorArguments.Count;
				builder.Append(" { ");
				foreach (var memberInit in expression.Members)
				{
					if (isFirstMember == false) builder.Append(", ");

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
			var isStatic = (field != null && field.IsStatic) || (prop != null && prop.GetGetMethod(true) != null && prop.GetGetMethod(true).IsStatic);
			if (expression.Expression != null)
			{
				Render(expression.Expression, builder, false, checkedScope);
				builder.Append(".");
			}
			else if (isStatic && declType != null)
			{
				RenderType(declType, builder);
				builder.Append(".");
			}
			builder.Append(expression.Member.Name);
		}
		private static void RenderCall(MethodCallExpression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var isIndex = expression.NodeType == ExpressionType.ArrayIndex;
			if (expression.Method.IsStatic)
			{
				var methodType = expression.Method.DeclaringType;
				if (methodType != null)
					RenderType(methodType, builder);
			}
			else
			{
				Render(expression.Object, builder, false, checkedScope);
			}

			if (isIndex)
			{
				builder.Append("[");
				RenderArguments(expression.Arguments, builder, checkedScope);
				builder.Append("]");
			}
			else
			{
				var method = expression.Method;
				builder.Append(".");
				builder.Append(method.Name);
				if (method.IsGenericMethod)
				{
					builder.Append("<");
					foreach (var genericArgument in method.GetGenericArguments())
					{
						RenderType(genericArgument, builder);
						builder.Append(',');
					}
					builder.Length--;
					builder.Append(">");
				}
				builder.Append("(");
				RenderArguments(expression.Arguments, builder, checkedScope);
				builder.Append(")");
			}

		}
		private static void RenderArrayIndex(Expression expression, StringBuilder builder, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var binaryExpression = expression as BinaryExpression;
			var methodCallExpression = expression as MethodCallExpression;

			if (binaryExpression != null)
			{
				Render(binaryExpression.Left, builder, false, checkedScope);
				builder.Append("[");
				Render(binaryExpression.Right, builder, false, checkedScope);
				builder.Append("]");
			}
			else if (methodCallExpression != null)
			{
				if (methodCallExpression.Method.IsStatic)
				{
					var methodType = methodCallExpression.Method.DeclaringType;
					if (methodType != null)
					{
						RenderType(methodType, builder);
						builder.Append(".");
					}
				}
				else
				{
					Render(methodCallExpression.Object, builder, false, checkedScope);
				}
				builder.Append("[");
				RenderArguments(methodCallExpression.Arguments, builder, checkedScope);
				builder.Append("]");
			}
			else
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_INVALIDCONSTANTEXPRESSION, expression.NodeType));
			}
		}
		private static void RenderConstant(ConstantExpression expression, StringBuilder builder)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			if (expression.Value == null)
			{
				builder.Append("default(");
				RenderType(expression.Type, builder);
				builder.Append(")");
				return;
			}

			var strValue = Convert.ToString(expression.Value, Constants.DefaultFormatProvider);
			if (expression.Type == typeof(string))
				RenderTextLiteral(strValue, builder, isChar: false);
			else if (expression.Type == typeof(char))
				RenderTextLiteral(strValue, builder, isChar: true);
			else if (expression.Type == typeof(Type))
			{
				builder.Append("typeof(");
				RenderType((Type)expression.Value, builder);
				builder.Append(")");
			}
			else if (expression.Type == typeof(ushort) || expression.Type == typeof(uint))
				builder.Append(strValue).Append("u");
			else if (expression.Type == typeof(ulong))
				builder.Append(strValue).Append("ul");
			else if (expression.Type == typeof(long))
				builder.Append(strValue).Append("l");
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
				builder.Append(strValue).Append("m");
			else if (expression.Type == typeof(bool))
				builder.Append(strValue.ToLowerInvariant());
			else if (expression.Type == typeof(byte) || expression.Type == typeof(sbyte) || expression.Type == typeof(short) || expression.Type == typeof(int))
				builder.Append(strValue);
			else
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_INVALIDCONSTANTEXPRESSION, expression.Type));
		}
		private static void RenderUnary(UnaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.NegateChecked ? true :
						expression.NodeType == ExpressionType.Negate ? false : checkedScope;
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
				builder.Append("(");
				closeParent = true;
			}

			switch (expression.NodeType)
			{
				case ExpressionType.NegateChecked:
				case ExpressionType.Negate:
					builder.Append("-");
					break;
				case ExpressionType.UnaryPlus:
					builder.Append("+");
					break;
				case ExpressionType.Not:
					switch (Type.GetTypeCode(expression.Operand.Type))
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
							builder.Append("~");
							break;
						default:
							builder.Append("~");
							break;
					}
					break;
				default:
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.Type));
			}
			Render(expression.Operand, builder, false, checkedScope);

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderBinary(BinaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.AddChecked || expression.NodeType == ExpressionType.MultiplyChecked || expression.NodeType == ExpressionType.SubtractChecked ? true :
									expression.NodeType == ExpressionType.Add || expression.NodeType == ExpressionType.Multiply || expression.NodeType == ExpressionType.Subtract ? false : checkedScope;

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
				builder.Append("(");
				closeParent = true;
			}

			Render(expression.Left, builder, false, checkedScope);
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
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expression.Type));
			}
			Render(expression.Right, builder, false, checkedScope);

			if (closeParent)
				builder.Append(")");
		}
		private static void RenderArguments(ReadOnlyCollection<Expression> arguments, StringBuilder builder, bool checkedScope)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (builder == null) throw new ArgumentNullException("builder");

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
						if (isFirstBinder == false) builder.Append(", ");
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
						if (isFirstInitializer == false) builder.Append(", ");
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
			{
				Render(initializer.Arguments[0], builder, true, checkedScope);
			}
			else
			{
				var isFirstArgument = true;
				builder.Append("{ ");
				foreach (var argument in initializer.Arguments)
				{
					if (isFirstArgument == false) builder.Append(", ");
					Render(argument, builder, true, checkedScope);

					isFirstArgument = false;
				}
				builder.Append("}");
			}
		}

		private static void RenderType(Type type, StringBuilder builder)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (builder == null) throw new ArgumentNullException("builder");

			var nameStartIndex = builder.Length;
			NameUtils.WriteFullName(type, builder, writeGenericArguments: true);
			NameUtils.RemoveGenericSuffix(builder, nameStartIndex, builder.Length - nameStartIndex);
		}
		private static void RenderTextLiteral(string value, StringBuilder builder, bool isChar)
		{
			if (value == null) throw new ArgumentException("value");
			if (builder == null) throw new ArgumentException("builder");

			if (isChar && value.Length != 1) throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_BIND_INVALIDCHARLITERAL, value));

			if (isChar)
				builder.Append("'");
			else
				builder.Append("\"");

			builder.Append(value);
			for (var i = builder.Length - value.Length; i < builder.Length; i++)
			{
				if (builder[i] == '"')
				{
					builder.Insert(i, '\\');
					i++;
				}
				else if (builder[i] == '\\')
				{
					builder.Insert(i, '\\');
					i++;
				}
				else if (builder[i] == '\0')
				{
					builder[i] = '0';
					builder.Insert(i, '\\');
					i++;
				}
				else if (builder[i] == '\r')
				{
					builder[i] = 'r';
					builder.Insert(i, '\\');
					i++;
				}
				else if (builder[i] == '\n')
				{
					builder[i] = 'n';
					builder.Insert(i, '\\');
					i++;
				}
			}

			if (isChar)
				builder.Append("'");
			else
				builder.Append("\"");
		}
	}
}
