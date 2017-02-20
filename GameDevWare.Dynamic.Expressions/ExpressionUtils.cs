using System;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Binding;

namespace GameDevWare.Dynamic.Expressions
{
	internal static class ExpressionUtils
	{
		public static readonly Expression TrueConstant = Expression.Constant(true);
		public static readonly Expression FalseConstant = Expression.Constant(false);
		public static readonly Expression NegativeSingle = Expression.Constant(-1.0f);
		public static readonly Expression NegativeDouble = Expression.Constant(-1.0d);

		public static void PromoteBothOperands(ref Expression left, ref Expression right, ExpressionType type)
		{
			if (left == null) throw new ArgumentNullException("left");
			if (right == null) throw new ArgumentNullException("right");

			var newLeft = left;
			var newRight = right;
			var leftType = Nullable.GetUnderlyingType(newLeft.Type) ?? newLeft.Type;
			var rightType = Nullable.GetUnderlyingType(newRight.Type) ?? newRight.Type;
			var liftToNullable = leftType != newLeft.Type || rightType != newRight.Type;

			if (liftToNullable && leftType == newLeft.Type)
				newLeft = ConvertToNullable(newLeft);
			if (liftToNullable && rightType != newRight.Type)
				newRight = ConvertToNullable(newRight);

			if (leftType.IsEnum)
			{
				leftType = Enum.GetUnderlyingType(leftType);
				left = newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(Nullable<>).MakeGenericType(leftType) : leftType);
			}
			if (rightType.IsEnum)
			{
				rightType = Enum.GetUnderlyingType(rightType);
				right = newRight = Expression.Convert(newRight, liftToNullable ? rightType = typeof(Nullable<>).MakeGenericType(rightType) : rightType);
			}

			if (leftType == rightType)
			{
				var typeCode = Type.GetTypeCode(leftType);
				if (typeCode < TypeCode.SByte || typeCode > TypeCode.UInt16)
					return;

				// expand smaller integers to int32
				left = newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(int?) : typeof(int));
				right = newRight = Expression.Convert(newRight, liftToNullable ? typeof(int?) : typeof(int));
				return;
			}

			if (leftType == typeof(object))
			{
				right = newRight = Expression.Convert(newRight, typeof(object));
				return;
			}
			else if (rightType == typeof(object))
			{
				left = newLeft = Expression.Convert(newLeft, typeof(object));
				return;
			}

			var leftTypeCode = Type.GetTypeCode(leftType);
			var rightTypeCode = Type.GetTypeCode(rightType);
			if (NumberUtils.IsNumber(leftTypeCode) == false || NumberUtils.IsNumber(rightTypeCode) == false)
				return;

			if (leftTypeCode == TypeCode.Decimal || rightTypeCode == TypeCode.Decimal)
			{
				if (leftTypeCode == TypeCode.Double || leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Single)
					return; // will throw exception
				if (leftTypeCode == TypeCode.Decimal)
					newRight = Expression.Convert(newRight, liftToNullable ? typeof(decimal?) : typeof(decimal));
				else
					newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(decimal?) : typeof(decimal));
			}
			else if (leftTypeCode == TypeCode.Double || rightTypeCode == TypeCode.Double)
			{
				if (leftTypeCode == TypeCode.Double)
					newRight = Expression.Convert(newRight, liftToNullable ? typeof(double?) : typeof(double));
				else
					newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(double?) : typeof(double));
			}
			else if (leftTypeCode == TypeCode.Single || rightTypeCode == TypeCode.Single)
			{
				if (leftTypeCode == TypeCode.Single)
					newRight = Expression.Convert(newRight, liftToNullable ? typeof(float?) : typeof(float));
				else
					newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(float?) : typeof(float));
			}
			else if (leftTypeCode == TypeCode.UInt64)
			{
				if (NumberUtils.IsSignedInteger(rightTypeCode) && TryMorphType(ref newRight, typeof(ulong)) <= 0)
					return; // will throw exception

				var expectedRightType = liftToNullable ? typeof(ulong?) : typeof(ulong);
				newRight = newRight.Type != expectedRightType ? Expression.Convert(newRight, expectedRightType) : newRight;
			}
			else if (rightTypeCode == TypeCode.UInt64)
			{
				if (NumberUtils.IsSignedInteger(leftTypeCode) && TryMorphType(ref newLeft, typeof(ulong)) <= 0)
					return; // will throw exception

				var expectedLeftType = liftToNullable ? typeof(ulong?) : typeof(ulong);
				newLeft = newLeft.Type != expectedLeftType ? Expression.Convert(newLeft, expectedLeftType) : newLeft;
			}
			else if (leftTypeCode == TypeCode.Int64 || rightTypeCode == TypeCode.Int64)
			{
				if (leftTypeCode == TypeCode.Int64)
					newRight = Expression.Convert(newRight, liftToNullable ? typeof(long?) : typeof(long));
				else
					newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(long?) : typeof(long));
			}
			else if ((leftTypeCode == TypeCode.UInt32 && NumberUtils.IsSignedInteger(rightTypeCode)) ||
				(rightTypeCode == TypeCode.UInt32 && NumberUtils.IsSignedInteger(leftTypeCode)))
			{
				newRight = Expression.Convert(newRight, liftToNullable ? typeof(long?) : typeof(long));
				newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(long?) : typeof(long));
			}
			else if (leftTypeCode == TypeCode.UInt32 || rightTypeCode == TypeCode.UInt32)
			{
				if (leftTypeCode == TypeCode.UInt32)
					newRight = Expression.Convert(newRight, liftToNullable ? typeof(uint?) : typeof(uint));
				else
					newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(uint?) : typeof(uint));
			}
			else
			{
				newRight = Expression.Convert(newRight, liftToNullable ? typeof(int?) : typeof(int));
				newLeft = Expression.Convert(newLeft, liftToNullable ? typeof(int?) : typeof(int));
			}

			left = newLeft;
			right = newRight;
		}
		public static void PromoteOperand(ref Expression operand, ExpressionType type)
		{
			if (operand == null) throw new ArgumentNullException("operand");

			var newOperand = operand;

			if (newOperand.Type.IsEnum)
				newOperand = Expression.Convert(newOperand, Enum.GetUnderlyingType(newOperand.Type));

			var typeCode = Type.GetTypeCode(newOperand.Type);
			if (typeCode >= TypeCode.SByte && typeCode <= TypeCode.UInt16)
			{
				operand = Expression.Convert(newOperand, typeof(int));
			}
			else if (typeCode == TypeCode.UInt32 && type == ExpressionType.Not)
			{
				operand = Expression.Convert(newOperand, typeof(long));
			}

		}

		public static Expression ConvertToNullable(Expression notNullableExpression)
		{
			if (notNullableExpression == null) throw new ArgumentNullException("notNullableExpression");

			if (notNullableExpression.Type.IsValueType && Nullable.GetUnderlyingType(notNullableExpression.Type) == null)
				return Expression.Convert(notNullableExpression, typeof(Nullable<>).MakeGenericType(notNullableExpression.Type));
			else
				return notNullableExpression;
		}
		public static float TryMorphType(ref Expression expression, Type toType)
		{
			if (expression == null) throw new ArgumentNullException("expression");
			if (toType == null) throw new ArgumentNullException("toType");

			var actualType = expression.Type;

			if (actualType == toType)
				return TypeConversion.QUALITY_SAME_TYPE;

			var conversion = default(TypeConversion);
			if (TypeConversion.TryGetTypeConversion(actualType, toType, out conversion) == false)
				return TypeConversion.QUALITY_NO_CONVERSION;

			// 1: check if types are convertible
			// 2: check if value is constant and could be converted
			if (conversion.IsNatural)
			{
				expression = Expression.Convert(expression, toType);
				return conversion.Quality; // same type hierarchy
			}

			// implicit convertion on expectedType
			if (conversion.Implicit != null && conversion.Implicit.TryMakeConversion(expression, out expression, checkedConversion: true))
				return TypeConversion.QUALITY_IMPLICIT_CONVERSION;

			// try to convert value of constant
			var constantValue = default(object);
			var constantType = default(Type);
			if (!TryExposeConstant(expression, out constantValue, out constantType))
				return 0.0f;

			if (constantValue == null)
			{
				if (constantType == typeof(object) && !toType.IsValueType)
				{
					expression = Expression.Constant(null, toType);
					return TypeConversion.QUALITY_SAME_TYPE; // exact type (null)
				}
				else
				{
					return TypeConversion.QUALITY_NO_CONVERSION;
				}
			}

			var expectedTypeCode = Type.GetTypeCode(toType);
			var constantTypeCode = Type.GetTypeCode(constantType);
			var convertibleToExpectedType = default(bool);
			// ReSharper disable RedundantCast
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (expectedTypeCode)
			{
				case TypeCode.Byte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Byte.MinValue, (ulong)Byte.MaxValue); break;
				case TypeCode.SByte: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)SByte.MinValue, (ulong)SByte.MaxValue); break;
				case TypeCode.Char:
				case TypeCode.UInt16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt16.MinValue, UInt16.MaxValue); break;
				case TypeCode.Int16: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int16.MinValue, (ulong)Int16.MaxValue); break;
				case TypeCode.UInt32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt32.MinValue, UInt32.MaxValue); break;
				case TypeCode.Int32: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int32.MinValue, (ulong)Int32.MaxValue); break;
				case TypeCode.UInt64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)UInt64.MinValue, UInt64.MaxValue); break;
				case TypeCode.Int64: convertibleToExpectedType = IsInRange(constantValue, constantTypeCode, (long)Int64.MinValue, (ulong)Int64.MaxValue); break;
				case TypeCode.Double:
				case TypeCode.Decimal:
				case TypeCode.Single: convertibleToExpectedType = NumberUtils.IsSignedInteger(constantTypeCode) || NumberUtils.IsUnsignedInteger(constantTypeCode); break;
				default: convertibleToExpectedType = false; break;
			}
			// ReSharper restore RedundantCast

			if (convertibleToExpectedType)
			{
				expression = Expression.Constant(Convert.ChangeType(constantValue, expectedTypeCode, Constants.DefaultFormatProvider));
				return TypeConversion.QUALITY_IN_PLACE_CONVERSION; // converted in-place
			}

			return TypeConversion.QUALITY_NO_CONVERSION;
		}

		public static Expression MakeNullPropagationExpression(Expression testExpression, Expression notNullExpression)
		{
			if (testExpression == null) throw new ArgumentNullException("testExpression");
			if (notNullExpression == null) throw new ArgumentNullException("notNullExpression");

			var testTypeDescription = TypeDescription.GetTypeDescription(testExpression.Type);
			var notNullTypeDescription = TypeDescription.GetTypeDescription(notNullExpression.Type);
			if (testTypeDescription.IsNullable == false) // no need in null propagation
				return notNullExpression;

			var resultType = notNullTypeDescription.IsNullable == false ? TypeDescription.GetTypeDescription(typeof(Nullable<>).MakeGenericType(notNullExpression.Type)) : notNullTypeDescription;
			if (resultType != notNullExpression.Type)
				notNullExpression = Expression.Convert(notNullExpression, resultType);

			return Expression.Condition
			(
				test: Expression.NotEqual(testExpression, testTypeDescription.DefaultExpression),
				ifTrue: notNullExpression,
				ifFalse: resultType.DefaultExpression
			);
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
