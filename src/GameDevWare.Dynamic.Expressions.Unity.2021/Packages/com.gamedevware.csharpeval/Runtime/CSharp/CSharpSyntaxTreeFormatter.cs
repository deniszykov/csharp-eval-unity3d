using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using GameDevWare.Dynamic.Expressions.Properties;

namespace GameDevWare.Dynamic.Expressions.CSharp
{
	/// <summary>
	///     Type extension class for <see cref="SyntaxTreeNode" />. Add expression formatting methods for
	///     <see cref="SyntaxTreeNode" />.
	/// </summary>
	public static class CSharpSyntaxTreeFormatter
	{
		/// <summary>
		///     Renders syntax tree into string representation.
		/// </summary>
		/// <param name="syntaxTree">Syntax tree.</param>
		/// <param name="checkedScope">
		///     True to assume all arithmetic and conversion operation is checked for overflows. Overwise
		///     false.
		/// </param>
		/// <returns>Rendered expression.</returns>
		[Obsolete("Use CSharpExpression.Format() instead. Will be removed in next releases.", true)]
		public static string Render(this SyntaxTreeNode syntaxTree, bool checkedScope = CSharpExpression.DEFAULT_CHECKED_SCOPE)
		{
			return Format(syntaxTree, checkedScope);
		}

		internal static string Format(this SyntaxTreeNode syntaxTree, bool checkedScope = CSharpExpression.DEFAULT_CHECKED_SCOPE)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));

			var builder = new StringBuilder();
			Render(syntaxTree, builder, true, checkedScope);

			return builder.ToString();
		}

		private static void Render(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (!syntaxTree.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out var expressionTypeObj) || !(expressionTypeObj is string))
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));

			try
			{
				var expressionType = (string)expressionTypeObj;
				switch (expressionType)
				{
					case Constants.EXPRESSION_TYPE_ARRAY_LENGTH: RenderArrayLength(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_INVOKE:
					case Constants.EXPRESSION_TYPE_INDEX: RenderInvokeOrIndex(syntaxTree, builder, checkedScope); break;
					case "Enclose":
					case Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_CHECKED_SCOPE:
					case Constants.EXPRESSION_TYPE_GROUP: RenderGroup(syntaxTree, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_CONSTANT: RenderConstant(syntaxTree, builder); break;
					case Constants.EXPRESSION_TYPE_MEMBER_RESOLVE:
					case Constants.EXPRESSION_TYPE_PROPERTY_OR_FIELD: RenderPropertyOrField(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_TYPE_OF: RenderTypeOf(syntaxTree, builder); break;
					case Constants.EXPRESSION_TYPE_DEFAULT: RenderDefault(syntaxTree, builder); break;
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS:
					case Constants.EXPRESSION_TYPE_NEW: RenderNew(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_NEW_ARRAY_INIT: RenderNewArrayInit(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_MEMBER_INIT: RenderMemberInit(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_LIST_INIT: RenderListInit(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_ELEMENT_INIT_BINDING: RenderElementInit(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_ASSIGNMENT_BINDING:
					case Constants.EXPRESSION_TYPE_ASSIGNMENT_BINDING_ALT: RenderAssignmentBinding(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_LIST_BINDING: RenderListBinding(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_MEMBER_BINDING: RenderMemberBinding(syntaxTree, builder, checkedScope); break;
					case Constants.EXPRESSION_TYPE_UNARY_PLUS:
					case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					case Constants.EXPRESSION_TYPE_NEGATE:
					case Constants.EXPRESSION_TYPE_NOT:
					case Constants.EXPRESSION_TYPE_COMPLEMENT: RenderUnary(syntaxTree, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_DIVIDE:
					case Constants.EXPRESSION_TYPE_MULTIPLY:
					case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED:
					case Constants.EXPRESSION_TYPE_MODULO:
					case Constants.EXPRESSION_TYPE_ADD:
					case Constants.EXPRESSION_TYPE_ADD_CHECKED:
					case Constants.EXPRESSION_TYPE_SUBTRACT:
					case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED:
					case Constants.EXPRESSION_TYPE_LEFT_SHIFT:
					case Constants.EXPRESSION_TYPE_RIGHT_SHIFT:
					case Constants.EXPRESSION_TYPE_GREATER_THAN:
					case Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_LESS_THAN:
					case Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL:
					case Constants.EXPRESSION_TYPE_EQUAL:
					case Constants.EXPRESSION_TYPE_NOT_EQUAL:
					case Constants.EXPRESSION_TYPE_AND:
					case Constants.EXPRESSION_TYPE_OR:
					case Constants.EXPRESSION_TYPE_EXCLUSIVE_OR:
					case Constants.EXPRESSION_TYPE_AND_ALSO:
					case Constants.EXPRESSION_TYPE_OR_ELSE:
					case Constants.EXPRESSION_TYPE_POWER:
					case Constants.EXPRESSION_TYPE_COALESCE: RenderBinary(syntaxTree, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_CONDITION: RenderCondition(syntaxTree, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_CONVERT:
					case Constants.EXPRESSION_TYPE_CONVERT_CHECKED:
					case Constants.EXPRESSION_TYPE_TYPE_IS:
					case Constants.EXPRESSION_TYPE_TYPE_AS: RenderTypeBinary(syntaxTree, builder, wrapped, checkedScope); break;
					case Constants.EXPRESSION_TYPE_LAMBDA: RenderLambda(syntaxTree, builder, wrapped, checkedScope); break;
					default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
				}
			}
			catch (InvalidOperationException)
			{
				throw;
			}
#if !NETSTANDARD
			catch (System.Threading.ThreadAbortException)
			{
				throw;
			}
#endif
			catch (Exception exception)
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_RENDERFAILED, expressionTypeObj, exception.Message),
					exception);
			}
		}
		private static void RenderArrayLength(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			Render(syntaxTree.GetExpression(true), builder, false, checkedScope);
			builder.Append(".Length");
		}
		private static void RenderTypeBinary(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var expressionType = syntaxTree.GetExpressionType(true);
			var typeName = syntaxTree.GetTypeName(true);
			var target = syntaxTree.GetExpression(true);

			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_CONVERT_CHECKED ||
				expressionType != Constants.EXPRESSION_TYPE_CONVERT && checkedScope;

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

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_CONVERT:
				case Constants.EXPRESSION_TYPE_CONVERT_CHECKED:
					builder.Append('(');
					RenderTypeName(typeName, builder);
					builder.Append(')');
					Render(target, builder, false, checkedOperation);
					break;
				case Constants.EXPRESSION_TYPE_TYPE_IS:
					Render(target, builder, false, checkedScope);
					builder.Append(" is ");
					RenderTypeName(typeName, builder);
					break;
				case Constants.EXPRESSION_TYPE_TYPE_AS:
					Render(target, builder, false, checkedScope);
					builder.Append(" as ");
					RenderTypeName(typeName, builder);
					break;
				default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderCondition(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (!syntaxTree.TryGetValue(Constants.TEST_ATTRIBUTE, out var testObj) || !(testObj is SyntaxTreeNode testNode))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TEST_ATTRIBUTE,
					syntaxTree.GetTypeName(true)));
			}

			if (!syntaxTree.TryGetValue(Constants.IF_TRUE_ATTRIBUTE, out var ifTrueObj) || !(ifTrueObj is SyntaxTreeNode ifTrue))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.IF_TRUE_ATTRIBUTE,
					syntaxTree.GetTypeName(true)));
			}

			if (!syntaxTree.TryGetValue(Constants.IF_FALSE_ATTRIBUTE, out var ifFalseObj) || !(ifFalseObj is SyntaxTreeNode ifFalse))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.IF_FALSE_ATTRIBUTE,
					syntaxTree.GetTypeName(true)));
			}

			if (!wrapped)
			{
				builder.Append('(');
			}
			Render(testNode, builder, true, checkedScope);
			builder.Append(" ? ");
			Render(ifTrue, builder, true, checkedScope);
			builder.Append(" : ");
			Render(ifFalse, builder, true, checkedScope);
			if (!wrapped)
			{
				builder.Append(')');
			}
		}
		private static void RenderBinary(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var expressionType = syntaxTree.GetExpressionType(true);

			if (!syntaxTree.TryGetValue(Constants.LEFT_ATTRIBUTE, out var leftObj) || !(leftObj is SyntaxTreeNode left))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.LEFT_ATTRIBUTE, expressionType));
			}

			if (!syntaxTree.TryGetValue(Constants.RIGHT_ATTRIBUTE, out var rightObj) || !(rightObj is SyntaxTreeNode right))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.RIGHT_ATTRIBUTE, expressionType));
			}

			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED ||
				expressionType == Constants.EXPRESSION_TYPE_ADD_CHECKED ||
				expressionType == Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED || expressionType != Constants.EXPRESSION_TYPE_MULTIPLY &&
				expressionType != Constants.EXPRESSION_TYPE_ADD &&
				expressionType != Constants.EXPRESSION_TYPE_SUBTRACT && checkedScope;

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

			Render(left, builder, false, checkedOperation);
			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_DIVIDE: builder.Append(" / "); break;
				case Constants.EXPRESSION_TYPE_MULTIPLY:
				case Constants.EXPRESSION_TYPE_MULTIPLY_CHECKED: builder.Append(" * "); break;
				case Constants.EXPRESSION_TYPE_MODULO: builder.Append(" % "); break;
				case Constants.EXPRESSION_TYPE_ADD_CHECKED:
				case Constants.EXPRESSION_TYPE_ADD: builder.Append(" + "); break;
				case Constants.EXPRESSION_TYPE_SUBTRACT:
				case Constants.EXPRESSION_TYPE_SUBTRACT_CHECKED: builder.Append(" - "); break;
				case Constants.EXPRESSION_TYPE_LEFT_SHIFT: builder.Append(" << "); break;
				case Constants.EXPRESSION_TYPE_RIGHT_SHIFT: builder.Append(" >> "); break;
				case Constants.EXPRESSION_TYPE_GREATER_THAN: builder.Append(" > "); break;
				case Constants.EXPRESSION_TYPE_GREATER_THAN_OR_EQUAL: builder.Append(" >= "); break;
				case Constants.EXPRESSION_TYPE_LESS_THAN: builder.Append(" < "); break;
				case Constants.EXPRESSION_TYPE_LESS_THAN_OR_EQUAL: builder.Append(" <= "); break;
				case Constants.EXPRESSION_TYPE_EQUAL: builder.Append(" == "); break;
				case Constants.EXPRESSION_TYPE_NOT_EQUAL: builder.Append(" != "); break;
				case Constants.EXPRESSION_TYPE_AND: builder.Append(" & "); break;
				case Constants.EXPRESSION_TYPE_OR: builder.Append(" | "); break;
				case Constants.EXPRESSION_TYPE_EXCLUSIVE_OR: builder.Append(" ^ "); break;
				case Constants.EXPRESSION_TYPE_POWER: builder.Append(" ** "); break;
				case Constants.EXPRESSION_TYPE_AND_ALSO: builder.Append(" && "); break;
				case Constants.EXPRESSION_TYPE_OR_ELSE: builder.Append(" || "); break;
				case Constants.EXPRESSION_TYPE_COALESCE: builder.Append(" ?? "); break;
				default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}

			Render(right, builder, false, checkedOperation);

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderUnary(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (!syntaxTree.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out var expressionTypeObj) || !(expressionTypeObj is string expressionType))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));
			}

			if (!syntaxTree.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out var expressionObj) || !(expressionObj is SyntaxTreeNode expression))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE,
					expressionType));
			}

			var checkedOperation = expressionType == Constants.EXPRESSION_TYPE_NEGATE_CHECKED || expressionType != Constants.EXPRESSION_TYPE_NEGATE && checkedScope;

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

			switch (expressionType)
			{
				case Constants.EXPRESSION_TYPE_UNARY_PLUS:
					builder.Append('+');
					break;
				case Constants.EXPRESSION_TYPE_NEGATE:
				case Constants.EXPRESSION_TYPE_NEGATE_CHECKED:
					builder.Append('-');
					break;
				case Constants.EXPRESSION_TYPE_NOT:
					builder.Append('!');
					break;
				case Constants.EXPRESSION_TYPE_COMPLEMENT:
					builder.Append('~');
					break;
				default: throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_UNKNOWNEXPRTYPE, expressionType));
			}

			Render(expression, builder, false, checkedOperation);

			if (closeParent)
				builder.Append(')');
		}
		private static void RenderNew(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var expressionType = syntaxTree.GetExpressionType(true);
			var typeName = syntaxTree.GetTypeName(true);
			var arguments = syntaxTree.GetArguments(false);

			builder.Append("new ");
			RenderTypeName(typeName, builder);
			if (expressionType == Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS)
				builder.Append('[');
			else
				builder.Append('(');

			RenderArguments(arguments, builder, checkedScope);

			if (expressionType == Constants.EXPRESSION_TYPE_NEW_ARRAY_BOUNDS)
				builder.Append(']');
			else
				builder.Append(')');
		}
		private static void RenderListInit(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var newExpression = syntaxTree.GetNewExpression(true);
			var initializers = syntaxTree.GetInitializerOrBindingList(Constants.INITIALIZERS_ATTRIBUTE, true);

			Render(newExpression, builder, wrapped: false, checkedScope);
			builder.Append(" { ");

			var isFirst = true;
			foreach (var initializer in initializers)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(initializer, builder, true, checkedScope);
			}

			builder.Append(" }");
		}
		private static void RenderMemberInit(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var newExpression = syntaxTree.GetNewExpression(true);
			var bindings = syntaxTree.GetInitializerOrBindingList(Constants.BINDINGS_ATTRIBUTE, true);

			Render(newExpression, builder, wrapped: false, checkedScope);
			builder.Append(" { ");

			var isFirst = true;
			foreach (var binding in bindings)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(binding, builder, true, checkedScope);
			}

			builder.Append(" }");
		}
		private static void RenderNewArrayInit(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var typeName = syntaxTree.GetTypeName(true);
			var initializers = syntaxTree.GetInitializerOrBindingList(Constants.INITIALIZERS_ATTRIBUTE, true);

			builder.Append("new ");
			RenderTypeName(typeName, builder);
			builder.Append("[] { ");

			var isFirst = true;
			foreach (var initializer in initializers)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(initializer, builder, true, checkedScope);
			}

			builder.Append(" }");
		}
		private static void RenderMemberBinding(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var memberName = syntaxTree.GetName(throwOnError: true);
			var bindings = syntaxTree.GetInitializerOrBindingList(Constants.BINDINGS_ATTRIBUTE, throwOnError: true);

			builder.Append(memberName);
			builder.Append(" = { ");

			var isFirst = true;
			foreach (var binding in bindings)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(binding, builder, true, checkedScope);
			}

			builder.Append(" }");
		}
		private static void RenderListBinding(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var memberName = syntaxTree.GetName(throwOnError: true);
			var initializers = syntaxTree.GetInitializerOrBindingList(Constants.INITIALIZERS_ATTRIBUTE, throwOnError: true);

			builder.Append(memberName);
			builder.Append(" = { ");

			var isFirst = true;
			foreach (var initializer in initializers)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(initializer, builder, true, checkedScope);
			}

			builder.Append(" }");
		}
		private static void RenderAssignmentBinding(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var memberName = syntaxTree.GetName(throwOnError: true);
			var expression = syntaxTree.GetExpression(throwOnError: true);

			builder.Append(memberName);
			builder.Append(" = ");
			Render(expression, builder, true, checkedScope);
		}
		private static void RenderElementInit(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var initializers = syntaxTree.GetInitializerOrBindingList(Constants.INITIALIZERS_ATTRIBUTE, throwOnError: true);

			if (initializers.Count > 1)
			{
				builder.Append("{ ");
			}

			var isFirst = true;
			foreach (var initializer in initializers)
			{
				if (!isFirst)
				{
					builder.Append(", ");
				}
				isFirst = false;

				Render(initializer, builder, true, checkedScope);
			}

			if (initializers.Count > 1)
			{
				builder.Append(" }");
			}
		}
		private static void RenderDefault(SyntaxTreeNode syntaxTree, StringBuilder builder)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var typeName = syntaxTree.GetTypeName(true);
			builder.Append("default(");
			RenderTypeName(typeName, builder);
			builder.Append(')');
		}
		private static void RenderTypeOf(SyntaxTreeNode syntaxTree, StringBuilder builder)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var typeName = syntaxTree.GetTypeName(true);
			builder.Append("typeof(");
			RenderTypeName(typeName, builder);
			builder.Append(')');
		}
		private static void RenderPropertyOrField(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var target = syntaxTree.GetExpression(false);
			var propertyOrFieldName = syntaxTree.GetMemberName(true);
			var useNullPropagation = syntaxTree.GetUseNullPropagation(false);
			var arguments = syntaxTree.GetArguments(false);
			if (target != null)
			{
				Render(target, builder, false, checkedScope);
				if (useNullPropagation)
					builder.Append("?.");
				else
					builder.Append('.');
			}

			builder.Append(propertyOrFieldName);
			if (arguments != null && arguments.Count > 0)
			{
				builder.Append('<');
				for (var i = 0; i < arguments.Count; i++)
				{
					if (i != 0) builder.Append(',');
					if (arguments.TryGetValue(i, out var typeArgument))
						Render(typeArgument, builder, true, checkedScope);
				}

				builder.Append('>');
			}
		}
		private static void RenderConstant(SyntaxTreeNode syntaxTree, StringBuilder builder)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (!syntaxTree.TryGetValue(Constants.TYPE_ATTRIBUTE, out var typeObj) || !(typeObj is string))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.TYPE_ATTRIBUTE,
					syntaxTree.GetExpressionType(true)));
			}

			if (!syntaxTree.TryGetValue(Constants.VALUE_ATTRIBUTE, out var valueObj))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.VALUE_ATTRIBUTE,
					syntaxTree.GetExpressionType(true)));
			}

			if (valueObj == null)
			{
				if (IsObjectType(typeObj))
				{
					builder.Append("null");
					return;
				}

				builder.Append("default(");
				RenderTypeName(typeObj, builder);
				builder.Append(')');
				return;
			}

			var type = Convert.ToString(typeObj, Constants.DefaultFormatProvider);
			var value = Convert.ToString(valueObj, Constants.DefaultFormatProvider) ?? "";
			switch (type)
			{
				case "System.Char":
				case "Char":
				case "char":
					RenderTextLiteral(value, builder, true);
					break;
				case "System.String":
				case "String":
				case "string":
					RenderTextLiteral(value, builder, false);
					break;
				case "UInt16":
				case "System.UInt16":
				case "ushort":
				case "UInt32":
				case "System.UInt32":
				case "uint":
					builder.Append(value);
					builder.Append('u');
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
					builder.Append('l');
					break;
				case "Single":
				case "System.Single":
				case "float":
					builder.Append(value);
					builder.Append('f');
					break;
				case "Double":
				case "System.Double":
				case "double":
					builder.Append(value);
					if (value.IndexOf('.') == -1)
						builder.Append('d');
					break;
				case "Decimal":
				case "System.Decimal":
				case "decimal":
					builder.Append(value);
					builder.Append('m');
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
		private static void RenderGroup(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (!syntaxTree.TryGetValue(Constants.EXPRESSION_TYPE_ATTRIBUTE, out var expressionTypeObj) || !(expressionTypeObj is string expressionType))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_TYPE_ATTRIBUTE));
			}

			if (!syntaxTree.TryGetValue(Constants.EXPRESSION_ATTRIBUTE, out var expressionObj) || !(expressionObj is SyntaxTreeNode expression))
			{
				throw new InvalidOperationException(string.Format(Resources.EXCEPTION_BIND_MISSINGATTRONNODE, Constants.EXPRESSION_ATTRIBUTE,
					expressionType));
			}

			if (expressionType == Constants.EXPRESSION_TYPE_UNCHECKED_SCOPE && checkedScope)
			{
				builder.Append("unchecked");
				wrapped = false;
				checkedScope = false;
			}

			if (expressionType == Constants.EXPRESSION_TYPE_CHECKED_SCOPE && !checkedScope)
			{
				builder.Append("checked");
				wrapped = false;
				checkedScope = true;
			}

			if (!wrapped) builder.Append('(');
			Render(expression, builder, true, checkedScope);
			if (!wrapped) builder.Append(')');
		}
		private static void RenderInvokeOrIndex(SyntaxTreeNode syntaxTree, StringBuilder builder, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var expressionType = syntaxTree.GetExpressionType(true);
			var target = syntaxTree.GetExpression(true);
			var arguments = syntaxTree.GetArguments(false);
			var useNullPropagation = syntaxTree.GetUseNullPropagation(false);

			Render(target, builder, false, checkedScope);
			builder.Append(expressionType == Constants.DELEGATE_INVOKE_NAME ? "(" : useNullPropagation ? "?[" : "[");
			RenderArguments(arguments, builder, checkedScope);
			builder.Append(expressionType == Constants.DELEGATE_INVOKE_NAME ? ")" : "]");
		}
		private static void RenderLambda(SyntaxTreeNode syntaxTree, StringBuilder builder, bool wrapped, bool checkedScope)
		{
			if (syntaxTree == null) throw new ArgumentException("syntaxTree");
			if (builder == null) throw new ArgumentException("builder");

			if (!wrapped) builder.Append('(');

			var arguments = syntaxTree.GetArguments(false);
			var body = syntaxTree.GetExpression(true);
			if (arguments.Count != 1) builder.Append('(');
			var firstParam = true;
			foreach (var param in arguments.Values)
			{
				if (!firstParam) builder.Append(", ");
				Render(param, builder, true, checkedScope);
				firstParam = false;
			}

			if (arguments.Count != 1) builder.Append(')');
			builder.Append(" => ");
			Render(body, builder, false, checkedScope);

			if (!wrapped) builder.Append(')');
		}
		private static void RenderArguments(ArgumentsTree arguments, StringBuilder builder, bool checkedScope)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			var firstArgument = true;
			foreach (var argumentName in arguments.Keys)
			{
				Debug.Assert(argumentName != null, nameof(argumentName) + " != null");

				var positionalArguments = new SortedDictionary<int, SyntaxTreeNode>();
				var namedArguments = new SortedDictionary<string, SyntaxTreeNode>();
				if (int.TryParse(argumentName, out var position))
				{
					positionalArguments[position] = arguments[argumentName];
				}
				else
				{
					namedArguments[argumentName] = arguments[argumentName];
				}

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
			if (typeName == null) throw new ArgumentNullException(nameof(typeName));
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			if (typeName is SyntaxTreeNode name)
				Render(name, builder, true, true);
			else
				builder.Append(Convert.ToString(typeName, Constants.DefaultFormatProvider));
		}

		internal static void RenderTextLiteral(string value, StringBuilder builder, bool isChar)
		{
			if (value == null) throw new ArgumentException("value");
			if (builder == null) throw new ArgumentException("builder");

			if (isChar && value.Length != 1) throw new ArgumentException(string.Format(Resources.EXCEPTION_BIND_INVALIDCHARLITERAL, value));

			if (isChar)
				builder.Append('\'');
			else
				builder.Append('"');

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
				builder.Append('\'');
			else
				builder.Append('"');
		}
		private static bool IsObjectType(object typeObj)
		{
			return string.Equals("object", typeObj as string, StringComparison.Ordinal) ||
				string.Equals("Object", typeObj as string, StringComparison.Ordinal) ||
				string.Equals("System.Object", typeObj as string, StringComparison.Ordinal);
		}
	}
}
