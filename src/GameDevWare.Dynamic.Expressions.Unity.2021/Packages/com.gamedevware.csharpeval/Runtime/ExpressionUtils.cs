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
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class ExpressionUtils
	{
		public static readonly Expression FalseConstant = Expression.Constant(false);
		public static readonly Expression NegativeDouble = Expression.Constant(-1.0d);
		public static readonly Expression NegativeSingle = Expression.Constant(-1.0f);
		public static readonly Expression NullConstant = Expression.Constant(null, typeof(object));
		public static readonly Expression TrueConstant = Expression.Constant(true);

		public static bool TryPromoteBinaryOperation(ref Expression leftOperand, ref Expression rightOperand, ExpressionType type, out Expression operation)
		{
			if (leftOperand == null) throw new ArgumentNullException(nameof(leftOperand));
			if (rightOperand == null) throw new ArgumentNullException(nameof(rightOperand));

			operation = null;

			var leftType = TypeDescription.GetTypeDescription(leftOperand.Type);
			var rightType = TypeDescription.GetTypeDescription(rightOperand.Type);

			var leftTypeUnwrap = leftType.IsNullable ? leftType.UnderlyingType : leftType;
			var rightTypeUnwrap = rightType.IsNullable ? rightType.UnderlyingType : rightType;

			// enum + enum
			if (leftTypeUnwrap.IsEnum || rightTypeUnwrap.IsEnum)
				return TryPromoteEnumBinaryOperation(ref leftOperand, leftType, ref rightOperand, rightType, type, out operation);

			// number + number
			if (leftTypeUnwrap.IsNumber && rightTypeUnwrap.IsNumber)
				return TryPromoteNumberBinaryOperation(ref leftOperand, leftType, ref rightOperand, rightType, type, out operation);

			// null + nullable
			if (IsNull(leftOperand) && rightType.CanBeNull)
			{
				leftType = rightType;
				leftOperand = rightType.DefaultExpression;
			}

			// nullable + null
			else if (IsNull(rightOperand) && leftType.CanBeNull)
			{
				rightType = leftType;
				rightOperand = leftType.DefaultExpression;
			}

			// [not]nullable + [not]nullable
			else if (leftType.IsNullable != rightType.IsNullable)
			{
				leftOperand = ConvertToNullable(leftOperand, leftType);
				if (type != ExpressionType.Coalesce)
					rightOperand = ConvertToNullable(rightOperand, rightType);
			}

			return false;
		}
		private static bool TryPromoteNumberBinaryOperation
		(
			ref Expression leftOperand,
			TypeDescription leftType,
			ref Expression rightOperand,
			TypeDescription rightType,
			ExpressionType type,
			out Expression operation)
		{
			if (leftOperand == null) throw new ArgumentNullException(nameof(leftOperand));
			if (leftType == null) throw new ArgumentNullException(nameof(leftType));
			if (rightOperand == null) throw new ArgumentNullException(nameof(rightOperand));
			if (rightType == null) throw new ArgumentNullException(nameof(rightType));

			operation = null;

			var leftTypeUnwrap = leftType.IsNullable ? leftType.UnderlyingType : leftType;
			var rightTypeUnwrap = rightType.IsNullable ? rightType.UnderlyingType : rightType;
			var leftTypeCode = leftTypeUnwrap.TypeCode;
			var rightTypeCode = rightTypeUnwrap.TypeCode;
			var promoteLeftToNullable = leftType.IsNullable || rightType.IsNullable;
			var promoteRightToNullable = rightType.IsNullable || (type != ExpressionType.Coalesce && promoteLeftToNullable);

			if (leftTypeUnwrap == rightTypeUnwrap)
			{
				if (leftTypeCode >= TypeCode.SByte && leftTypeCode <= TypeCode.UInt16)
				{
					// expand smaller integers to int32
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? TypeDescription.Int32Type.GetNullableType() : TypeDescription.Int32Type);
					rightOperand = Expression.Convert(rightOperand,
						promoteRightToNullable ? TypeDescription.Int32Type.GetNullableType() : TypeDescription.Int32Type);
					return false;
				}
			}
			else if (leftTypeCode == TypeCode.Decimal || rightTypeCode == TypeCode.Decimal)
			{
				if (leftTypeCode == TypeCode.Double || leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Single)
					return false; // will throw exception

				if (leftTypeCode == TypeCode.Decimal)
					rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(decimal?) : typeof(decimal));
				else
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(decimal?) : typeof(decimal));
			}
			else if (leftTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Double)
			{
				if (leftTypeCode == TypeCode.Double)
					rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(double?) : typeof(double));
				else
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(double?) : typeof(double));
			}
			else if (leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Single)
			{
				if (leftTypeCode == TypeCode.Single)
					rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(float?) : typeof(float));
				else
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(float?) : typeof(float));
			}
			else if (leftTypeCode == TypeCode.UInt64)
			{
				var rightOperandTmp = rightOperand;
				var expectedRightType = promoteRightToNullable ? typeof(ulong?) : typeof(ulong);
				if (NumberUtils.IsSignedInteger(rightTypeCode) && !TryCoerceType(ref rightOperandTmp, expectedRightType, out _))
					return false; // will throw exception

				rightOperand = rightOperandTmp;
				rightOperand = rightOperand.Type != expectedRightType ? Expression.Convert(rightOperand, expectedRightType) : rightOperand;
			}
			else if (rightTypeCode == TypeCode.UInt64)
			{
				var leftOperandTmp = leftOperand;
				var expectedLeftType = promoteLeftToNullable ? typeof(ulong?) : typeof(ulong);
				if (NumberUtils.IsSignedInteger(leftTypeCode) && !TryCoerceType(ref leftOperandTmp, expectedLeftType, out _))
					return false; // will throw exception

				leftOperand = leftOperandTmp;
				leftOperand = leftOperand.Type != expectedLeftType ? Expression.Convert(leftOperand, expectedLeftType) : leftOperand;
			}
			else if (leftTypeCode == TypeCode.Int64 || rightTypeCode == TypeCode.Int64)
			{
				if (leftTypeCode == TypeCode.Int64)
					rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(long?) : typeof(long));
				else
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(long?) : typeof(long));
			}
			else if ((leftTypeCode == TypeCode.UInt32 && NumberUtils.IsSignedInteger(rightTypeCode)) ||
					(rightTypeCode == TypeCode.UInt32 && NumberUtils.IsSignedInteger(leftTypeCode)))
			{
				rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(long?) : typeof(long));
				leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(long?) : typeof(long));
			}
			else if (leftTypeCode == TypeCode.UInt32 || rightTypeCode == TypeCode.UInt32)
			{
				if (leftTypeCode == TypeCode.UInt32)
					rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(uint?) : typeof(uint));
				else
					leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(uint?) : typeof(uint));
			}
			else
			{
				rightOperand = Expression.Convert(rightOperand, promoteRightToNullable ? typeof(int?) : typeof(int));
				leftOperand = Expression.Convert(leftOperand, promoteLeftToNullable ? typeof(int?) : typeof(int));
			}

			if (promoteLeftToNullable) leftOperand = ConvertToNullable(leftOperand, null);
			if (promoteRightToNullable) rightOperand = ConvertToNullable(rightOperand, null);

			return false;
		}
		private static bool TryPromoteEnumBinaryOperation
		(
			ref Expression leftOperand,
			TypeDescription leftType,
			ref Expression rightOperand,
			TypeDescription rightType,
			ExpressionType type,
			out Expression operation)
		{
			if (leftOperand == null) throw new ArgumentNullException(nameof(leftOperand));
			if (leftType == null) throw new ArgumentNullException(nameof(leftType));
			if (rightOperand == null) throw new ArgumentNullException(nameof(rightOperand));
			if (rightType == null) throw new ArgumentNullException(nameof(rightType));

			operation = null;

			var leftTypeUnwrap = leftType.IsNullable ? leftType.UnderlyingType : leftType;
			var rightTypeUnwrap = rightType.IsNullable ? rightType.UnderlyingType : rightType;
			var promoteToNullable = leftType.IsNullable != rightType.IsNullable;

			// enum + number
			if (leftTypeUnwrap.IsEnum &&
				rightTypeUnwrap.IsNumber &&
				(type == ExpressionType.Add || type == ExpressionType.AddChecked || type == ExpressionType.Subtract || type == ExpressionType.SubtractChecked))
			{
				var integerType = leftTypeUnwrap.UnderlyingType;
				leftOperand = Expression.Convert(leftOperand, promoteToNullable ? integerType.GetNullableType() : integerType);
				if (promoteToNullable)
					rightOperand = ConvertToNullable(rightOperand, leftType);

				// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
				switch (type)
				{
					case ExpressionType.Add: operation = Expression.Add(leftOperand, rightOperand); break;
					case ExpressionType.AddChecked: operation = Expression.AddChecked(leftOperand, rightOperand); break;
					case ExpressionType.Subtract: operation = Expression.Subtract(leftOperand, rightOperand); break;
					case ExpressionType.SubtractChecked: operation = Expression.SubtractChecked(leftOperand, rightOperand); break;
					default: throw new InvalidOperationException("Only subtraction and addition with numbers are promoted.");
				}

				operation = Expression.Convert(operation, promoteToNullable ? leftTypeUnwrap.GetNullableType() : leftTypeUnwrap);
				return true;
			}

			// number + enum

			if (rightTypeUnwrap.IsEnum &&
				leftTypeUnwrap.IsNumber &&
				(type == ExpressionType.Add || type == ExpressionType.AddChecked || type == ExpressionType.Subtract || type == ExpressionType.SubtractChecked))
			{
				var integerType = rightTypeUnwrap.UnderlyingType;
				rightOperand = Expression.ConvertChecked(rightOperand, promoteToNullable ? integerType.GetNullableType() : integerType);
				if (promoteToNullable)
					leftOperand = ConvertToNullable(leftOperand, rightType);

				operation = Expression.MakeBinary(type, leftOperand, rightOperand);
				operation = Expression.Convert(operation, promoteToNullable ? rightTypeUnwrap.GetNullableType() : rightTypeUnwrap);
				return true;
			}

			// null + nullable-enum

			if (IsNull(leftOperand) && rightType.CanBeNull)
			{
				leftType = rightType;
				leftOperand = rightType.DefaultExpression;
			}

			// nullable-enum + null
			else if (IsNull(rightOperand) && leftType.CanBeNull)
			{
				rightType = leftType;
				rightOperand = leftType.DefaultExpression;
			}

			// enum OP enum
			else if (rightTypeUnwrap == leftTypeUnwrap &&
					(type == ExpressionType.And ||
						type == ExpressionType.Or ||
						type == ExpressionType.ExclusiveOr ||
						type == ExpressionType.GreaterThan ||
						type == ExpressionType.GreaterThanOrEqual ||
						type == ExpressionType.LessThan ||
						type == ExpressionType.LessThanOrEqual))
			{
				var integerType = rightTypeUnwrap.UnderlyingType;
				rightOperand = Expression.ConvertChecked(rightOperand, promoteToNullable ? integerType.GetNullableType() : integerType);
				leftOperand = Expression.Convert(leftOperand, promoteToNullable ? integerType.GetNullableType() : integerType);

				operation = Expression.MakeBinary(type, leftOperand, rightOperand);
				return true;
			}

			// [not]nullable + [not]nullable
			else if (promoteToNullable)
			{
				leftOperand = ConvertToNullable(leftOperand, leftType);
				if (type != ExpressionType.Coalesce)
					rightOperand = ConvertToNullable(rightOperand, rightType);
			}

			return false;
		}
		public static bool TryPromoteUnaryOperation(ref Expression operand, ExpressionType type, out Expression operation)
		{
			if (operand == null) throw new ArgumentNullException(nameof(operand));

			operation = null;
			var operandType = TypeDescription.GetTypeDescription(operand.Type);
			var operandTypeUnwrap = operandType.IsNullable ? operandType.UnderlyingType : operandType;
			var promoteToNullable = operandType.IsNullable;

			if (operandTypeUnwrap.IsEnum)
				CoerceType(ref operand, promoteToNullable ? operandTypeUnwrap.UnderlyingType.GetNullableType() : operandTypeUnwrap.UnderlyingType);
			else if (operandTypeUnwrap.TypeCode >= TypeCode.SByte && operandTypeUnwrap.TypeCode <= TypeCode.UInt16)
				CoerceType(ref operand, promoteToNullable ? typeof(int?) : typeof(int));
			else if (operandTypeUnwrap.TypeCode == TypeCode.UInt32 && type == ExpressionType.Not)
				CoerceType(ref operand, promoteToNullable ? typeof(long?) : typeof(long));

			return false;
		}

		public static bool IsNull(Expression expression, bool unwrapConversions = true)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			if (ReferenceEquals(expression, NullConstant))
				return true;

			if (!TryExposeConstant(expression, out var constantExpression))
				return false;

			if (ReferenceEquals(constantExpression, NullConstant))
				return true;

			return constantExpression.Value == null && constantExpression.Type == typeof(object);
		}
		public static void CoerceType(ref Expression expression, Type toType)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (toType == null) throw new ArgumentNullException(nameof(toType));

			if (!TryCoerceType(ref expression, toType, out var quality) || quality <= TypeConversion.QUALITY_NO_CONVERSION)
				throw new InvalidOperationException($"Failed to change type of expression '{expression}' to '{toType}'.");
		}
		public static bool TryCoerceType(ref Expression expression, Type toType, out float quality)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (toType == null) throw new ArgumentNullException(nameof(toType));

			if (expression.Type == toType)
			{
				quality = TypeConversion.QUALITY_SAME_TYPE;
				return true;
			}

			var actualType = TypeDescription.GetTypeDescription(expression.Type);
			var targetType = TypeDescription.GetTypeDescription(toType);

			if (TryConvertInPlace(ref expression, targetType, out quality) || TryFindConversion(ref expression, actualType, targetType, out quality)) return true;

			quality = TypeConversion.QUALITY_NO_CONVERSION;
			return false;
		}
		private static bool TryFindConversion(ref Expression expression, TypeDescription actualType, TypeDescription targetType, out float quality)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (actualType == null) throw new ArgumentNullException(nameof(actualType));
			if (targetType == null) throw new ArgumentNullException(nameof(targetType));

			quality = TypeConversion.QUALITY_NO_CONVERSION;

			var actualTypeUnwrap = actualType.IsNullable ? actualType.UnderlyingType : actualType;
			var targetTypeUnwrap = targetType.IsNullable ? targetType.UnderlyingType : targetType;

			// converting null to nullable-or-reference
			if (targetType.CanBeNull && IsNull(expression))
			{
				expression = targetType.DefaultExpression;
				quality = TypeConversion.QUALITY_SAME_TYPE; // exact type (null)
				return true;
			}

			// converting value to nullable value e.g. T to T?
			if (targetTypeUnwrap == actualType)
			{
				expression = Expression.Convert(expression, targetType);
				quality = TypeConversion.QUALITY_IN_PLACE_CONVERSION;
				return true;
			}

			if (!TypeConversion.TryGetTypeConversion(actualTypeUnwrap, targetTypeUnwrap, out var conversion) ||
				conversion.Quality <= TypeConversion.QUALITY_NO_CONVERSION)
				return false;

			// implicit convertion on expectedType
			if (conversion.Implicit != null && conversion.Implicit.TryMakeConversion(expression, out expression, true))
			{
				quality = TypeConversion.QUALITY_IMPLICIT_CONVERSION;
				return true;
			}

			expression = Expression.Convert(expression, targetType);
			quality = conversion.Quality;
			return true;
		}
		private static bool TryConvertInPlace(ref Expression expression, TypeDescription targetType, out float quality)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));
			if (targetType == null) throw new ArgumentNullException(nameof(targetType));

			quality = TypeConversion.QUALITY_NO_CONVERSION;
			var targetTypeUnwrap = targetType.IsNullable ? targetType.UnderlyingType : targetType;

			// try to convert value of constant
			if (!TryExposeConstant(expression, out var constantExpression))
				return false;

			var constantValue = constantExpression.Value;
			var constantType = TypeDescription.GetTypeDescription(constantExpression.Type);
			var constantTypeUnwrap = constantType.IsNullable ? constantType.UnderlyingType : constantType;
			if (constantValue == null && constantType.DefaultExpression != constantExpression && targetType.CanBeNull)
			{
				quality = TypeConversion.QUALITY_SAME_TYPE;
				expression = Expression.Constant(null, targetType);
				return true;
			}

			if (constantValue == null) return false;

			var constantTypeCode = constantTypeUnwrap.TypeCode;
			var convertibleToExpectedType = false;

			// ReSharper disable RedundantCast
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (targetTypeUnwrap.TypeCode)
			{
				case TypeCode.Byte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, byte.MinValue, byte.MaxValue); break;
				case TypeCode.SByte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, sbyte.MinValue, (ulong)sbyte.MaxValue); break;
				case TypeCode.Char:
				case TypeCode.UInt16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, ushort.MinValue, ushort.MaxValue); break;
				case TypeCode.Int16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, short.MinValue, (ulong)short.MaxValue); break;
				case TypeCode.UInt32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, uint.MinValue, uint.MaxValue); break;
				case TypeCode.Int32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, int.MinValue, int.MaxValue); break;
				case TypeCode.UInt64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)ulong.MinValue, ulong.MaxValue); break;
				case TypeCode.Int64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, long.MinValue, long.MaxValue); break;
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Single:
					convertibleToExpectedType = NumberUtils.IsSignedInteger(constantTypeCode) || NumberUtils.IsUnsignedInteger(constantTypeCode); break;
				default: convertibleToExpectedType = false; break;
			}

			// ReSharper restore RedundantCast

			if (!convertibleToExpectedType)
				return false;

			var newValue = Convert.ChangeType(constantValue, targetTypeUnwrap.TypeCode, Constants.DefaultFormatProvider);
			expression = Expression.Constant(newValue, targetType);
			quality = TypeConversion.QUALITY_IN_PLACE_CONVERSION; // converted in-place
			return true;
		}
		private static Expression ConvertToNullable(Expression notNullableExpression, TypeDescription typeDescription)
		{
			if (notNullableExpression == null) throw new ArgumentNullException(nameof(notNullableExpression));
			if (typeDescription != null && notNullableExpression.Type != typeDescription)
				throw new ArgumentException("Wrong type description.", nameof(typeDescription));

			if (typeDescription == null) typeDescription = TypeDescription.GetTypeDescription(notNullableExpression.Type);

			if (!typeDescription.CanBeNull)
				return Expression.Convert(notNullableExpression, typeDescription.GetNullableType());

			return notNullableExpression;
		}

		public static Expression MakeNullPropagationExpression(List<Expression> nullTestExpressions, Expression ifNotNullExpression)
		{
			if (nullTestExpressions == null) throw new ArgumentNullException(nameof(nullTestExpressions));
			if (ifNotNullExpression == null) throw new ArgumentNullException(nameof(ifNotNullExpression));

			var notNullTestTreeExpression = default(Expression);
			foreach (var nullTestExpression in nullTestExpressions)
			{
				var testTypeDescription = TypeDescription.GetTypeDescription(nullTestExpression.Type);
				var notEqualDefault = Expression.NotEqual(nullTestExpression, testTypeDescription.DefaultExpression);
				if (notNullTestTreeExpression == null)
					notNullTestTreeExpression = notEqualDefault;
				else
					notNullTestTreeExpression = Expression.AndAlso(notNullTestTreeExpression, notEqualDefault);
			}

			if (notNullTestTreeExpression == null)
				notNullTestTreeExpression = TrueConstant;

			var ifNotNullTypeDescription = TypeDescription.GetTypeDescription(ifNotNullExpression.Type);
			var resultType = !ifNotNullTypeDescription.CanBeNull ?
				TypeDescription.GetTypeDescription(typeof(Nullable<>).MakeGenericType(ifNotNullExpression.Type)) : ifNotNullTypeDescription;
			if (resultType != ifNotNullExpression.Type)
				ifNotNullExpression = Expression.Convert(ifNotNullExpression, resultType);

			return Expression.Condition
			(
				notNullTestTreeExpression,
				ifNotNullExpression,
				resultType.DefaultExpression
			);
		}
		public static bool ExtractNullPropagationExpression
			(ConditionalExpression conditionalExpression, out List<Expression> nullTestExpressions, out Expression ifNotNullExpression)
		{
			if (conditionalExpression == null) throw new ArgumentNullException(nameof(conditionalExpression));

			nullTestExpressions = null;
			ifNotNullExpression = null;

			if (!TryExtractTestTargets(conditionalExpression.Test, ref nullTestExpressions))
				return false;

			if (nullTestExpressions == null || nullTestExpressions.Count == 0)
				return false;

			var ifFalseConst = conditionalExpression.IfFalse as ConstantExpression;
			var ifTrueUnwrapped = conditionalExpression.IfTrue.NodeType == ExpressionType.Convert ? ((UnaryExpression)conditionalExpression.IfTrue).Operand :
				conditionalExpression.IfTrue;

			// try to detect null-propagation operation
			if (ifFalseConst == null || ifFalseConst.Value != null || !ExpressionLookupVisitor.Lookup(conditionalExpression.IfTrue, nullTestExpressions))
				return false;

			ifNotNullExpression = ifTrueUnwrapped;
			return true;
		}
		private static bool TryExtractTestTargets(Expression testExpression, ref List<Expression> nullTestExpressions)
		{
			if (testExpression == null) throw new ArgumentNullException(nameof(testExpression));

			if (testExpression.NodeType == ExpressionType.NotEqual)
			{
				var notEqual = (BinaryExpression)testExpression;
				var rightConst = notEqual.Right as ConstantExpression;
				var rightConstValue = rightConst?.Value;
				if (notEqual.Left.Type != notEqual.Right.Type || rightConst == null || rightConstValue != null)
					return false;

				if (nullTestExpressions == null) nullTestExpressions = new List<Expression>();
				nullTestExpressions.Add(notEqual.Left);
				return true;
			}

			if (testExpression.NodeType == ExpressionType.AndAlso)
			{
				var andAlsoExpression = (BinaryExpression)testExpression;
				return TryExtractTestTargets(andAlsoExpression.Left, ref nullTestExpressions) &&
					TryExtractTestTargets(andAlsoExpression.Right, ref nullTestExpressions);
			}

			return false;
		}

		private static bool TryExposeConstant(Expression expression, out ConstantExpression constantExpression)
		{
			// unwrap conversions
			var convertExpression = expression as UnaryExpression;
			while (convertExpression != null &&
					(convertExpression.NodeType == ExpressionType.Convert || convertExpression.NodeType == ExpressionType.ConvertChecked))
			{
				expression = convertExpression.Operand;
				convertExpression = expression as UnaryExpression;
			}

			constantExpression = null;
			constantExpression = expression as ConstantExpression;
			return constantExpression != null;
		}
		private static bool IsInRange(object value, TypeCode valueTypeCode, long minValue, ulong maxValue)
		{
			if (NumberUtils.IsSignedInteger(valueTypeCode))
			{
				var signedValue = Convert.ToInt64(value, Constants.DefaultFormatProvider);
				if (signedValue >= minValue && signedValue >= 0 && unchecked((ulong)signedValue) <= maxValue)
					return true;
			}
			else if (NumberUtils.IsUnsignedInteger(valueTypeCode))
			{
				var unsignedValue = Convert.ToUInt64(value, Constants.DefaultFormatProvider);
				if (unsignedValue <= maxValue)
					return true;
			}

			return false;
		}
	}
}
