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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class Constants
	{
		public const string EXPRESSION_LINE_NUMBER_OLD = "$lineNum";
		public const string EXPRESSION_COLUMN_NUMBER_OLD = "$columnNum";
		public const string EXPRESSION_TOKEN_LENGTH_OLD = "$tokenLength";
		public const string EXPRESSION_ORIGINAL_OLD = "$originalExpression";
		public const string EXPRESSION_ORIGINAL = "_src";
		public const string EXPRESSION_POSITION = "_pos";
		public const string EXPRESSION_TYPE_ATTRIBUTE = "expressionType";
		public const string EXPRESSION_ATTRIBUTE = "expression";
		public const string ARGUMENTS_ATTRIBUTE = "arguments";
		public const string LEFT_ATTRIBUTE = "left";
		public const string RIGHT_ATTRIBUTE = "right";
		public const string TEST_ATTRIBUTE = "test";
		public const string IFTRUE_ATTRIBUTE = "ifTrue";
		public const string IFFALSE_ATTRIBUTE = "ifFalse";
		public const string TYPE_ATTRIBUTE = "type";
		public const string VALUE_ATTRIBUTE = "value";
		public const string PROPERTY_OR_FIELD_NAME_ATTRIBUTE = "propertyOrFieldName";
		public const string USE_NULL_PROPAGATION_ATTRIBUTE = "useNullPropagation";
		public const string METHOD_ATTRIBUTE = "method";
		public const string EXPRESSION_TYPE_PROPERTY_OR_FIELD = "PropertyOrField";
		public const string EXPRESSION_TYPE_CONSTANT = "Constant";
		public const string EXPRESSION_TYPE_CONVERT = "Convert";
		public const string EXPRESSION_TYPE_CONVERTCHECKED = "ConvertChecked";
		public const string EXPRESSION_TYPE_CALL = "Call";
		public const string EXPRESSION_TYPE_GROUP = "Group";
		public const string EXPRESSION_TYPE_INVOKE = "Invoke";
		public const string EXPRESSION_TYPE_LAMBDA = "Lambda";
		public const string EXPRESSION_TYPE_INDEX = "Index";
		public const string EXPRESSION_TYPE_UNCHECKED_SCOPE = "UncheckedScope";
		public const string EXPRESSION_TYPE_CHECKED_SCOPE = "CheckedScope";
		public const string EXPRESSION_TYPE_TYPEOF = "TypeOf";
		public const string EXPRESSION_TYPE_DEFAULT = "Default";
		public const string EXPRESSION_TYPE_NEW = "New";
		public const string EXPRESSION_TYPE_NEW_ARRAY_BOUNDS = "NewArrayBounds";
		public const string EXPRESSION_TYPE_ADD = "Add";
		public const string EXPRESSION_TYPE_ADD_CHECKED = "AddChecked";
		public const string EXPRESSION_TYPE_SUBTRACT = "Subtract";
		public const string EXPRESSION_TYPE_SUBTRACT_CHECKED = "SubtractChecked";
		public const string EXPRESSION_TYPE_LEFTSHIFT = "LeftShift";
		public const string EXPRESSION_TYPE_RIGHTSHIFT = "RightShift";
		public const string EXPRESSION_TYPE_GREATERTHAN = "GreaterThan";
		public const string EXPRESSION_TYPE_GREATERTHAN_OR_EQUAL = "GreaterThanOrEqual";
		public const string EXPRESSION_TYPE_LESSTHAN = "LessThan";
		public const string EXPRESSION_TYPE_LESSTHAN_OR_EQUAL = "LessThanOrEqual";
		public const string EXPRESSION_TYPE_NEGATE = "Negate";
		public const string EXPRESSION_TYPE_NEGATE_CHECKED = "NegateChecked";
		public const string EXPRESSION_TYPE_POWER = "Power";
		public const string EXPRESSION_TYPE_COMPLEMENT = "Complement";
		public const string EXPRESSION_TYPE_DIVIDE = "Divide";
		public const string EXPRESSION_TYPE_MULTIPLY = "Multiply";
		public const string EXPRESSION_TYPE_MULTIPLY_CHECKED = "MultiplyChecked";
		public const string EXPRESSION_TYPE_MODULO = "Modulo";
		public const string EXPRESSION_TYPE_TYPEIS = "TypeIs";
		public const string EXPRESSION_TYPE_TYPEAS = "TypeAs";
		public const string EXPRESSION_TYPE_NOT = "Not";
		public const string EXPRESSION_TYPE_EQUAL = "Equal";
		public const string EXPRESSION_TYPE_NOTEQUAL = "NotEqual";
		public const string EXPRESSION_TYPE_AND = "And";
		public const string EXPRESSION_TYPE_OR = "Or";
		public const string EXPRESSION_TYPE_EXCLUSIVEOR = "ExclusiveOr";
		public const string EXPRESSION_TYPE_ANDALSO = "AndAlso";
		public const string EXPRESSION_TYPE_ORELSE = "OrElse";
		public const string EXPRESSION_TYPE_COALESCE = "Coalesce";
		public const string EXPRESSION_TYPE_CONDITION = "Condition";
		public const string EXPRESSION_TYPE_UNARYPLUS = "UnaryPlus";
		public const string EXPRESSION_TYPE_CHECKEDSUFFIX = "Checked";
		public const string DELEGATE_INVOKE_NAME = "Invoke";
		public const string EXECUTE_PREPARE_NAME = "PrepareFunc";
		public const string VALUE_TRUE_STRING = "true";
		public const string VALUE_FALSE_STRING = "false";
		public const string VALUE_NULL_STRING = "null";

		public const int MAX_ARGUMENTS_COUNT = 100;


		public static readonly object TrueObject = true;
		public static readonly object FalseObject = false;

		public static readonly CultureInfo DefaultFormatProvider = CultureInfo.InvariantCulture;
		public static readonly ParameterExpression[] EmptyParameters = new ParameterExpression[0];

		private static readonly string[] IntToString = Array.ConvertAll(Enumerable.Range(0, MAX_ARGUMENTS_COUNT).ToArray(), i => i.ToString());

		public static string GetIndexAsString(int index)
		{
			if (index < IntToString.Length)
				return IntToString[index];
			else
				return index.ToString(DefaultFormatProvider);
		}
	}
}
