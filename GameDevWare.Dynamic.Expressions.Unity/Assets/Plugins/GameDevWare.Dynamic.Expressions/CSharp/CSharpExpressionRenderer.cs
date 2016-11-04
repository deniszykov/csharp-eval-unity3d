using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	public static class CSharpExpressionRenderer
	{
		private static readonly IFormatProvider Format = CultureInfo.InvariantCulture;
		private static readonly ReadOnlyDictionary<string, object> EmptyArguments = ReadOnlyDictionary<string, object>.Empty;

		public static string Render(this ExpressionTree node, bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			var builder = new StringBuilder();
			Render(node, builder, true, checkedScope);

			return builder.ToString();
		}
		public static string Render(this Expression expression, bool checkedScope = CSharpExpression.DefaultCheckedScope)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			var builder = new StringBuilder();
			Render(expression, builder, true, checkedScope);

			return builder.ToString();
		}

		private static void Render(ExpressionTree node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));

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
					default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType));
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
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_RENDERFAILED, expressionTypeObj, exception.Message), exception);
			}
		}
		private static void RenderTypeBinary(ExpressionTree node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, expressionType));

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, expressionType));

			var expression = (ExpressionTree)expressionObj;
			var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
			var checkedOperation = expressionType == "ConvertChecked" ? true :
				expressionType == "Convert" ? false : checkedScope;

			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

			switch (expressionType)
			{
				case "ConvertChecked":
				case "Convert":
					builder.Append("(").Append(type).Append(")");
					Render(expression, builder, false, checkedOperation);
					break;
				case "TypeIs":
					Render(expression, builder, false, checkedScope);
					builder.Append(" is ").Append(type);
					break;
				case "TypeAs":
					Render(expression, builder, false, checkedScope);
					builder.Append(" as ").Append(type);
					break;
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType));
			}

			if (!wrapped || checkedOperation != checkedScope)
				builder.Append(")");
		}
		private static void RenderCondition(ExpressionTree node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var testObj = default(object);
			if (node.TryGetValue(ExpressionTree.TEST_ATTRIBUTE, out testObj) == false || testObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TEST_ATTRIBUTE, "Condition"));

			var ifTrueObj = default(object);
			if (node.TryGetValue(ExpressionTree.IFTRUE_ATTRIBUTE, out ifTrueObj) == false || ifTrueObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.IFTRUE_ATTRIBUTE, "Condition"));

			var ifFalseObj = default(object);
			if (node.TryGetValue(ExpressionTree.IFFALSE_ATTRIBUTE, out ifFalseObj) == false || ifFalseObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.IFFALSE_ATTRIBUTE, "Condition"));

			var test = (ExpressionTree)testObj;
			var ifTrue = (ExpressionTree)ifTrueObj;
			var ifFalse = (ExpressionTree)ifFalseObj;

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
		private static void RenderBinary(ExpressionTree node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var leftObj = default(object);
			if (node.TryGetValue(ExpressionTree.LEFT_ATTRIBUTE, out leftObj) == false || leftObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.LEFT_ATTRIBUTE, expressionType));
			var rightObj = default(object);
			if (node.TryGetValue(ExpressionTree.RIGHT_ATTRIBUTE, out rightObj) == false || rightObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.RIGHT_ATTRIBUTE, expressionType));

			var left = (ExpressionTree)leftObj;
			var right = (ExpressionTree)rightObj;
			var checkedOperation = expressionType == "MultiplyChecked" || expressionType == "AddChecked" || expressionType == "SubtractChecked" ? true :
				expressionType == "Multiply" || expressionType == "Add" || expressionType == "Subtract" ? false : checkedScope;

			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

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
				case "AndAlso": builder.Append(" && "); break;
				case "OrElse": builder.Append(" || "); break;
				case "Coalesce": builder.Append(" ?? "); break;
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType));
			}
			Render(right, builder, false, checkedOperation);

			if (!wrapped || checkedOperation != checkedScope)
				builder.Append(")");
		}
		private static void RenderUnary(ExpressionTree node, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, expressionType));

			var expression = (ExpressionTree)expressionObj;
			var checkedOperation = expressionType == "NegateChecked" ? true :
				expressionType == "Negate" ? false : checkedScope;

			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

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
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expressionType));
			}
			Render(expression, builder, false, checkedOperation);

			if (!wrapped || checkedOperation != checkedScope)
				builder.Append(")");
		}
		private static void RenderNew(ExpressionTree node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, expressionType));

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, expressionType));

			var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			builder.Append("new ").Append(type);
			if (expressionType == "NewArrayBounds")
				builder.Append("[");
			else
				builder.Append("(");

			RenderArguments(arguments, builder, checkedScope);

			if (expressionType == "NewArrayBounds")
				builder.Append(")");
			else
				builder.Append("]");
		}
		private static void RenderDefault(ExpressionTree node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "Default"));

			var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
			builder.Append("default(").Append(type).Append(")");
		}
		private static void RenderTypeOf(ExpressionTree node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "TypeOf"));

			var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
			builder.Append("typeof(").Append(type).Append(")");
		}
		private static void RenderPropertyOrField(ExpressionTree node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) && expressionObj != null && expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, "PropertyOrField"));

			var propertyOrFieldNameObj = default(object);
			if (node.TryGetValue(ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, out propertyOrFieldNameObj) == false || propertyOrFieldNameObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.PROPERTY_OR_FIELD_NAME_ATTRIBUTE, "PropertyOrField"));

			var nullPropagationObj = default(object);
			if (node.TryGetValue(ExpressionTree.USE_NULL_PROPAGATION_ATTRIBUTE, out nullPropagationObj) == false)
				nullPropagationObj = "false";

			var propertyOrFieldName = (string)propertyOrFieldNameObj;
			var expression = (ExpressionTree)expressionObj;
			var useNullPropagation = Convert.ToBoolean(nullPropagationObj, Format);

			if (expression != null)
			{
				Render(expression, builder, false, checkedScope);
				if (useNullPropagation)
					builder.Append("?.");
				else
					builder.Append(".");
			}
			builder.Append(propertyOrFieldName);
		}
		private static void RenderConstant(ExpressionTree node, StringBuilder builder)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var typeObj = default(object);
			var valueObj = default(object);
			if (node.TryGetValue(ExpressionTree.TYPE_ATTRIBUTE, out typeObj) == false || typeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.TYPE_ATTRIBUTE, "Constant"));

			if (node.TryGetValue(ExpressionTree.VALUE_ATTRIBUTE, out valueObj) == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.VALUE_ATTRIBUTE, "Constant"));

			if (valueObj == null)
			{
				builder.Append("null");
				return;
			}

			var type = Convert.ToString(typeObj, CultureInfo.InvariantCulture);
			var value = Convert.ToString(valueObj, CultureInfo.InvariantCulture);
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
		private static void RenderGroup(ExpressionTree node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, expressionType));

			var expression = (ExpressionTree)expressionObj;

			if (expressionType == "UncheckedScope")
				builder.Append("unchecked");
			if (expressionType == "CheckedScope")
				builder.Append("checked");
			builder.Append("(");
			Render(expression, builder, true, checkedScope);
			builder.Append(")");
		}
		private static void RenderInvokeOrIndex(ExpressionTree node, StringBuilder builder, bool checkedScope)
		{
			if (node == null) throw new ArgumentNullException("node");
			if (builder == null) throw new ArgumentNullException("builder");

			var expressionTypeObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE, out expressionTypeObj) == false || expressionTypeObj is string == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_TYPE_ATTRIBUTE));
			var expressionType = (string)expressionTypeObj;

			var expressionObj = default(object);
			if (node.TryGetValue(ExpressionTree.EXPRESSION_ATTRIBUTE, out expressionObj) == false || expressionObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.EXPRESSION_ATTRIBUTE, expressionType));

			var argumentsObj = default(object);
			if (node.TryGetValue(ExpressionTree.ARGUMENTS_ATTRIBUTE, out argumentsObj) && argumentsObj != null && argumentsObj is ExpressionTree == false)
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_MISSINGATTRONNODE, ExpressionTree.ARGUMENTS_ATTRIBUTE, expressionType));

			var useNullPropagation = false;
			var useNullPropagationObj = default(object);
			if (node.TryGetValue(ExpressionTree.USE_NULL_PROPAGATION_ATTRIBUTE, out useNullPropagationObj) && useNullPropagationObj != null)
				useNullPropagation = Convert.ToBoolean(useNullPropagationObj, Format);

			var expression = (ExpressionTree)expressionObj;
			var arguments = (ExpressionTree)argumentsObj ?? EmptyArguments;

			Render(expression, builder, false, checkedScope);
			builder.Append(expressionType == "Invoke" ? "(" : (useNullPropagation ? "?[" : "["));
			RenderArguments(arguments, builder, checkedScope);
			builder.Append(expressionType == "Invoke" ? ")" : "]");
		}
		private static void RenderArguments(ReadOnlyDictionary<string, object> arguments, StringBuilder builder, bool checkedScope)
		{
			if (arguments == null) throw new ArgumentNullException("arguments");
			if (builder == null) throw new ArgumentNullException("builder");

			var firstArgument = true;
			foreach (var argumentName in arguments.Keys)
			{
				var positionalArguments = new SortedDictionary<int, ExpressionTree>();
				var namedArguments = new SortedDictionary<string, ExpressionTree>();
				var position = default(int);
				if (int.TryParse(argumentName, out position))
					positionalArguments[position] = (ExpressionTree)arguments[argumentName];
				else
					namedArguments[argumentName] = (ExpressionTree)arguments[argumentName];

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
					RenderBinary((BinaryExpression)expression, builder, wrapped, checkedScope);
					break;
				case ExpressionType.Power:
					builder.Append("System.Math.Pow(");
					Render(((BinaryExpression)expression).Left, builder, true, checkedScope);
					builder.Append(", ");
					Render(((BinaryExpression)expression).Right, builder, true, checkedScope);
					builder.Append(")");
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
					var cond = (ConditionalExpression)expression;
					if (!wrapped) builder.Append("(");
					Render(cond.Test, builder, true, checkedScope);
					builder.Append(" ? ");
					Render(cond.IfTrue, builder, true, checkedScope);
					builder.Append(" : ");
					Render(cond.IfFalse, builder, true, checkedScope);
					if (!wrapped) builder.Append(")");
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
				default: throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expression.Type));
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

			var checkedOperation = expression.NodeType == ExpressionType.ConvertChecked;
			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

			builder.Append("(");
			RenderType(expression.Type, builder);
			builder.Append(")");
			Render(expression.Operand, builder, true, checkedScope);

			if (!wrapped || checkedOperation != checkedScope)
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

			if (!wrapped) builder.Append("(");

			var firstParam = true;
			foreach (var param in expression.Parameters)
			{
				if (firstParam == false) builder.Append(", ");
				builder.Append(param.Name);
				firstParam = false;
			}
			builder.Append(" => ");
			Render(expression.Body, builder, false, checkedScope);

			if (!wrapped) builder.Append(")");
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

			if (expression.Method.IsStatic)
			{
				var methodType = expression.Method.DeclaringType;
				if (methodType != null)
				{
					RenderType(methodType, builder);
					builder.Append(".");
				}

			}
			else
			{
				Render(expression.Object, builder, false, checkedScope);
				builder.Append(".");
			}
			builder.Append(expression.Method.Name);
			builder.Append("(");
			RenderArguments(expression.Arguments, builder, checkedScope);
			builder.Append(")");
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
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_INVALIDCONSTANTEXPRESSION, expression.NodeType));
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

			var strValue = Convert.ToString(expression.Value, CultureInfo.InvariantCulture);
			if (expression.Type == typeof(string))
				RenderTextLiteral(strValue, builder, isChar: false);
			else if (expression.Type == typeof(char))
				RenderTextLiteral(strValue, builder, isChar: true);
			else if (expression.Type == typeof(Type))
				builder.Append("typeof(").Append(strValue).Append(")");
			else if (expression.Type == typeof(ushort) || expression.Type == typeof(uint))
				builder.Append(strValue).Append("u");
			else if (expression.Type == typeof(ulong))
				builder.Append(strValue).Append("ul");
			else if (expression.Type == typeof(long))
				builder.Append(strValue).Append("l");
			else if (expression.Type == typeof(float) || expression.Type == typeof(double))
			{
				var is32Bit = expression.Type == typeof(float);
				var doubleValue = Convert.ToDouble(expression.Value, CultureInfo.InvariantCulture);

				if (double.IsPositiveInfinity(doubleValue))
					builder.Append(is32Bit ? "System.Single.PositiveInfinity" : "System.Double.PositiveInfinity");
				if (double.IsNegativeInfinity(doubleValue))
					builder.Append(is32Bit ? "System.Single.NegativeInfinity" : "System.Double.NegativeInfinity");
				if (double.IsNaN(doubleValue))
					builder.Append(is32Bit ? "System.Single.NaN" : "System.Double.NaN");
				else
					builder.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture));
				builder.Append(is32Bit ? "f" : "d");
			}
			else if (expression.Type == typeof(decimal))
				builder.Append(strValue).Append("m");
			else if (expression.Type == typeof(bool))
				builder.Append(strValue.ToLowerInvariant());
			else if (expression.Type == typeof(byte) || expression.Type == typeof(sbyte) || expression.Type == typeof(short) || expression.Type == typeof(int))
				builder.Append(strValue);
			else
				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_INVALIDCONSTANTEXPRESSION, expression.Type));
		}
		private static void RenderUnary(UnaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.NegateChecked ? true :
						expression.NodeType == ExpressionType.Negate ? false : checkedScope;

			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

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
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expression.Type));
			}
			Render(expression.Operand, builder, false, checkedScope);

			if (!wrapped || checkedOperation != checkedScope)
				builder.Append(")");
		}
		private static void RenderBinary(BinaryExpression expression, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (expression == null) throw new ArgumentException("expression");
			if (builder == null) throw new ArgumentException("builder");

			var checkedOperation = expression.NodeType == ExpressionType.AddChecked || expression.NodeType == ExpressionType.MultiplyChecked || expression.NodeType == ExpressionType.SubtractChecked ? true :
									expression.NodeType == ExpressionType.Add || expression.NodeType == ExpressionType.Multiply || expression.NodeType == ExpressionType.Subtract ? false : checkedScope;

			if (checkedOperation && !checkedScope)
				builder.Append("checked(");
			else if (!checkedOperation && checkedScope)
				builder.Append("unchecked(");
			else if (!wrapped)
				builder.Append("(");

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
				default:
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_BUILD_UNKNOWNEXPRTYPE, expression.Type));
			}
			Render(expression.Right, builder, false, checkedScope);

			if (!wrapped || checkedOperation != checkedScope)
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

		private static void RenderType(Type methodType, StringBuilder builder)
		{
			if (methodType == null) throw new ArgumentNullException("methodType");
			if (builder == null) throw new ArgumentNullException("builder");

			builder.Append(methodType.FullName.Replace("+", "."));
		}
		private static void RenderTextLiteral(string value, StringBuilder builder, bool isChar)
		{
			if (value == null) throw new ArgumentException("value");
			if (builder == null) throw new ArgumentException("builder");

			if (isChar && value.Length != 1) throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_BUILD_INVALIDCHARLITERAL, value));

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
