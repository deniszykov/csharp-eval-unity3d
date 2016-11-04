using System;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExpressionBuilderTests
	{

		[Theory]
		[InlineData("10", 10)]
		[InlineData("10U", 10U)]
		[InlineData("10L", 10L)]
		[InlineData("10UL", 10UL)]
		[InlineData("10u", 10U)]
		[InlineData("10l", 10L)]
		[InlineData("10uL", 10UL)]
		[InlineData("10Ul", 10UL)]
		[InlineData("10D", 10D)]
		[InlineData("10d", 10D)]
		[InlineData("10f", 10F)]
		[InlineData("10F", 10F)]
		[InlineData("\"a\"", "a")]
		[InlineData("'a'", 'a')]
		[InlineData("true", true)]
		[InlineData("false", false)]
		[InlineData("null", null)]
		public void ConstantsTest(string expression, object expected)
		{
			var actual = CSharpExpression.Evaluate<object>(expression);

			if (expected != null)
				Assert.IsType(expected.GetType(), actual);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("2 * 2", 2 * 2)]
		[InlineData("2 + 2", 2 + 2)]
		[InlineData("2 * 2 + 3", 2 * 2 + 3)]
		[InlineData("2 + 2 * 3", 2 + 2 * 3)]
		[InlineData("2 + 4 / 2", 2 + 4 / 2)]
		[InlineData("2 + 4 / 2 * 3", 2 + 4 / 2 * 3)]
		[InlineData("2 * (2 + 3)", 2 * (2 + 3))]
		[InlineData("2 * (2 + 3) << 1 - 1", 2 * (2 + 3) << 1 - 1)]
		[InlineData("2 * (2 + 3) << 1 + 1 ^ 7", 2 * (2 + 3) << 1 + 1 ^ 7)]
		[InlineData("2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10", 2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10)]
		public void IntArithmeticTests(string expression, int expected)
		{
			var actual = CSharpExpression.Evaluate<int>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + Int32.Parse(\"10\"))", checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + 10))]
		[InlineData("checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + Int32.Parse(\"10\") + Math.Pow(10.0, 2))", checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + 10 + 100.0))]
		[InlineData("checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + Int32.Parse(\"10\") + Math.Pow(10.0, 2) + Math.E)", checked((2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + 10 + 100.0 + Math.E))]
		public void ComplexExpressionTests(string expression, double expected)
		{
			var actual = CSharpExpression.Evaluate<double>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("\"\" + \"a\"", "a")]
		[InlineData("\"\" + \"\"", "")]
		[InlineData("\"a\" + \"b\"", "ab")]
		[InlineData("\"a\" + \"b\"+ \"c\"", "abc")]
		[InlineData("\"\" + \"\"+ \"\"", "")]
		[InlineData("\"\" + \"a\"+ \"\"", "a")]
		[InlineData("\"\\r\" + \"\\n\"", "\r\n")]
		[InlineData("\"\\\\\" + \"\\\\\"", @"\\")]
		[InlineData("\"\\\\\" + \"\\\\\\\\\"", @"\\\")]
		[InlineData("\"a\\r\" + \"\\nb\"", "a\r\nb")]
		[InlineData("\"\\x038\" + \"\\u0112\"+ \"\\112\"", "8\u0112p")]
		[InlineData("\"a\" + 1", "a1")] // string concatenation with object
		[InlineData("1 + \"a\"", "1a")] // string concatenation with object
		[InlineData("\"1\" + 'a'", "1a")]
		[InlineData("\"1\" + '\t'", "1\t")]
		public void StringConcatenationTest(string expression, string expected)
		{
			var actual = CSharpExpression.Evaluate<string>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("Math.Max(1.0,2.0)", 2.0)]
		[InlineData("Math.Pow(2,2)", 4.0)]
		[InlineData("System.Math.Pow(2,2)", 4.0)]
		[InlineData("Math.E", Math.E)]
		public void MathTest(string expression, double expected)
		{
			var actual = CSharpExpression.Evaluate<double>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("default(Math)?.ToString()", null)]
		[InlineData("Math.E?.ToString()", "2.71828182845905")]
		[InlineData("default(Math[])?[0]", null)]
		public void NullResolveTest(string expression, object expected)
		{
			var actual = CSharpExpression.Evaluate<object>(expression);

			if (expected == null)
				Assert.Null(actual);
			else
				Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("typeof(Int32)", typeof(int))]
		[InlineData("typeof(System.Int32)", typeof(int))]
		[InlineData("typeof(short)", typeof(short))]
		[InlineData("typeof(Math)", typeof(Math))]
		public void TypeOfTest(string expression, Type expected)
		{
			var actual = CSharpExpression.Evaluate<Type>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("default(Int32)", default(int))]
		[InlineData("default(System.Int32)", default(int))]
		[InlineData("default(short)", default(short))]
		[InlineData("default(Math)", default(string))]
		public void DefaultTest(string expression, object expected)
		{
			var actual = CSharpExpression.Evaluate<object>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("1 is Int32", true)]
		[InlineData("1 is System.Int32", true)]
		[InlineData("1 is Int16", false)]
		[InlineData("1 is short", false)]
		public void IsTest(string expression, bool expected)
		{
			var actual = CSharpExpression.Evaluate<bool>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("arg1 as String", "", "")]
		[InlineData("arg1 as System.String", 1, null)]
		[InlineData("arg1 as string", "", "")]
		[InlineData("arg1 as String", 1, null)]
		[InlineData("arg1 as string", 1, null)]
		public void AsTest(string expression, object arg1, object expected)
		{
			var actual = CSharpExpression.Evaluate<object, string>(expression, arg1, arg1Name: "arg1");
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("new String('a', 3)", "aaa")]
		[InlineData("new System.String('a', 0)", "")]
		[InlineData("new string('a', 3)", "aaa")]
		[InlineData("\"a\" + new string('b', 1) + \"c\" + (\"d\" + 'e')", "abcde")]
		public void NewTest(string expression, string expected)
		{
			var actual = CSharpExpression.Evaluate<string>(expression);

			Assert.NotNull(actual);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("new String[0]", 0)]
		[InlineData("new String[4]", 4)]
		[InlineData("new string[5]", 5)]
		public void NewArrayTest(string expression, int expectedLength)
		{
			var actual = CSharpExpression.Evaluate<string[]>(expression);

			Assert.NotNull(actual);
			Assert.Equal(expectedLength, actual.Length);
		}

		[Theory]
		[InlineData("(Byte)1 + (byte)1", (byte)1 + (byte)1, null)] // int8-16 type promotion to int32
		[InlineData("(Byte)1 + 1", (byte)1 + 1, null)] // one argument type promotion to int32
		[InlineData("1m + 1", 2, typeof(decimal))]
		[InlineData("1.0 + 1", 1.0 + 1, null)]
		[InlineData("1.0f + 1", 1.0f + 1, null)]
		[InlineData("1ul + 1u", 1ul + 1u, null)]
		[InlineData("1ul + 1", 1ul + 1, null)]
		[InlineData("1l + 1", 1L + 1, null)]
		[InlineData("1u + 1", 1u + 1, null)]
		[InlineData("1u + -1", 1u + -1, null)]
		[InlineData("1u + (Byte)1", 1u + (byte)1, null)]
		[InlineData("1 + 1", 1 + 1, null)]
		public void NumericPromotionTest(string expression, object expected, Type expectedType)
		{
			expectedType = expectedType ?? expected.GetType();
			if (expected.GetType() != expectedType)
				expected = Convert.ChangeType(expected, expectedType);

			var actual = ExpressionUtils.Evaluate(expression, new Type[] { expectedType }, forceAot: false);

			Assert.IsType(expectedType, actual);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("a + b", 1, 2, 1 + 2)]
		[InlineData("a * b", 1, 2, 1 * 2)]
		[InlineData("a - b", 1, 2, 1 - 2)]
		[InlineData("a / b", 1, 2, 1 / 2)]
		[InlineData("a % b", 1, 2, 1 % 2)]
		[InlineData("a & b", 1, 2, 1 & 2)]
		[InlineData("a | b", 1, 2, 1 | 2)]
		[InlineData("a ^ b", 1, 2, 1 ^ 2)]
		[InlineData("a << b", 1, 2, 1 << 2)]
		[InlineData("a >> b", 1, 2, 1 >> 2)]
		[InlineData("a + b", 1, null, null)]
		[InlineData("a * b", 1, null, null)]
		[InlineData("a - b", 1, null, null)]
		[InlineData("a / b", 1, null, null)]
		[InlineData("a % b", 1, null, null)]
		[InlineData("a & b", 1, null, null)]
		[InlineData("a | b", 1, null, null)]
		[InlineData("a ^ b", 1, null, null)]
		[InlineData("a << b", 1, null, null)]
		[InlineData("a >> b", 1, null, null)]
		[InlineData("+b", 1, null, null)]
		[InlineData("-b", 1, null, null)]
		[InlineData("~b", 1, null, null)]
		[InlineData("~b", 1, null, null)]
		public void LiftedNullableArithmeticTest(string expression, int? arg1, int? arg2, int? expected)
		{
			var actual = CSharpExpression.Parse<int?, int?, int?>(expression, arg1Name: "a", arg2Name: "b").Compile().Invoke(arg1, arg2);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("a < b", 1, 2, true)]
		[InlineData("a < b", 1, null, false)]
		[InlineData("a > b", 1, null, false)]
		[InlineData("a == b", 1, null, false)]
		[InlineData("a >= b", 1, null, false)]
		[InlineData("a <= b", 1, null, false)]
		[InlineData("null == b", 1, null, true)] // this is special case
		[InlineData("null == a", 1, null, false)] // this is special case
		[InlineData("a != b", 1, null, true)] // this is special case
		[InlineData("a != b", null, null, false)] // this is special case
		public void LiftedNullableEquationTest(string expression, int? arg1, int? arg2, bool expected)
		{
			var actual = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").Compile().Invoke(arg1, arg2);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("(Int32)a", 1, typeof(int))]
		[InlineData("(System.Int32)a", 1, typeof(int))]
		[InlineData("(Double)a", 1, typeof(double))]
		[InlineData("(Byte)a", 1, typeof(byte))]
		[InlineData("(int)a", 1, typeof(int))]
		public void LiftedNullableConversionTest(string expression, int? arg1, Type expectedType)
		{
			var actual = CSharpExpression.Parse<int?, object>(expression, arg1Name: "a").Compile().Invoke(arg1);

			if (expectedType != null)
			{
				Assert.NotNull(actual);
				Assert.IsType(expectedType, actual);
			}
			else
			{
				Assert.Null(actual);
			}
		}
	}
}
