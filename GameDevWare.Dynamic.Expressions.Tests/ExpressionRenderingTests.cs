using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExpressionRenderingTests
	{
		public class TestClass : IEnumerable
		{
			public static int StaticIntField = 100500;
			public static int StaticIntProperty { get { return StaticIntField; } set { StaticIntField = value; } }

			public int IntField = 100500 * 2;
			public int IntProperty { get { return IntField; } set { IntField = value; } }
			public ExpressionExecutionTests.TestClass TestClassField;
			public ExpressionExecutionTests.TestClass TestClassProperty { get { return this.TestClassField; } set { TestClassField = value; } }
			public List<int> ListField = new List<int>();
			public List<int> ListProperty { get { return this.ListField; } set { this.ListField = value; } }

			public void Add(int value)
			{
				this.ListField.Add(value);
			}
			public void Add(int value1, int value2)
			{
				this.ListField.Add(value1);
				this.ListField.Add(value2);
			}

			public IEnumerator GetEnumerator()
			{
				return this.ListField.GetEnumerator();
			}
		}

		private readonly ITestOutputHelper output;
		public ExpressionRenderingTests(ITestOutputHelper output)
		{
			this.output = output;
		}

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
			expression = CSharpExpression.Parse<object>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Evaluate<object>(expression);


			if (expected != null)
				Assert.IsType(expected.GetType(), actual);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("2 * 2", 2 * 2)]
		[InlineData("2 ** 2", 2 * 2)]
		[InlineData("2 + 2", 2 + 2)]
		[InlineData("2 * 2 + 3", 2 * 2 + 3)]
		[InlineData("2 + 2 * 3", 2 + 2 * 3)]
		[InlineData("2 + 4 / 2", 2 + 4 / 2)]
		[InlineData("2 + 4 / 2 * 3", 2 + 4 / 2 * 3)]
		[InlineData("2 * (2 + 3)", 2 * (2 + 3))]
		[InlineData("2 * (2 + 3) << 1 - 1", 2 * (2 + 3) << 1 - 1)]
		[InlineData("2 * (2 + 3) << 1 + 1 ^ 7", 2 * (2 + 3) << 1 + 1 ^ 7)]
		[InlineData("2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10", 2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10)]
		public void IntArithmeticTests(string expression, int expected)
		{
			expression = CSharpExpression.Parse<int>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Evaluate<int>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\")", (2 * (2 + 3) << (1 - 1) & 7 | 25 ^ 10) + 10)]
		[InlineData("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\") + Math.Pow(100, 1)", (2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + 10 + 100.0)]
		[InlineData("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\") + Math.Pow(100, 1) + Math.E", (2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + 10 + 100.0 + Math.E)]
		public void ComplexExpressionTests(string expression, double expected)
		{
			expression = CSharpExpression.Parse<double>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
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
		[InlineData("\"\\x038\" + \"\\u0112\"+ \"\\112\"", "8Ēp")]
		[InlineData("\"a\" + 1", "a1")] // string concatenation with object
		[InlineData("1 + \"a\"", "1a")] // string concatenation with object
		[InlineData("\"1\" + 'a'", "1a")]
		[InlineData("\"1\" + '\t'", "1\t")]
		public void StringConcatenationTest(string expression, string expected)
		{
			expression = CSharpExpression.Parse<string>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
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
			expression = CSharpExpression.Parse<double>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Evaluate<double>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("default(Math)?.ToString()", null)]
		[InlineData("Math.E?.ToString()", "2.71828182845905")]
		[InlineData("default(Math[])?[0]?.ToString()", null)]
		public void NullResolveTest(string expression, object expected)
		{
			expression = CSharpExpression.Parse<object>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
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
			expression = CSharpExpression.Parse<Type>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Evaluate<Type>(expression);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("default(Int32)", default(int))]
		[InlineData("default(System.Int32)", default(int))]
		[InlineData("default(String)", default(string))]
		public void DefaultTest(string expression, object expected)
		{
			expression = CSharpExpression.Parse<object>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
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
			expression = CSharpExpression.Parse<bool>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
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
			expression = CSharpExpression.Parse<object, string>(expression, arg1Name: "arg1").Body.Render();
			output.WriteLine("Rendered: " + expression);
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
			expression = CSharpExpression.Parse<string>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Evaluate<string>(expression);

			Assert.NotNull(actual);
			Assert.Equal(expected, actual);
		}

		// convert enum

		[Fact]
		public void ArrayIndexTest()
		{
			Expression<Func<int[], int>> firstElementExpr = a => a[0];
			Expression<Func<int[], int>> tenthExpr = a => a[9];

			output.WriteLine("Rendered: " + firstElementExpr.Body.Render());
			output.WriteLine("Rendered: " + tenthExpr.Body.Render());

			Assert.NotNull(firstElementExpr.Body.Render());
			Assert.NotNull(tenthExpr.Body.Render());
		}

		[Fact]
		public void ArrayLengthTest()
		{
			Expression<Func<int[], int>> expression = a => a.Length;

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void QuoteTest()
		{
			Expression<Func<Expression<Func<int>>>> expression = () => (() => 1);

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void InvokeTest()
		{
			Expression<Func<Func<int>, int>> expression = a => a();

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void NewArrayInitTest()
		{
			Expression<Func<int[]>> expression = () => new int[] { 1, 2, 3, 4 };

			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void NewArrayBoundsTest()
		{
			Expression<Func<int[]>> expression = () => new int[10];

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void ListInitTest()
		{
			Expression<Func<List<int>>> expression = () => new List<int> { 1, 2, 3, 4 };

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void MemberAccessExpression()
		{
			Expression<Func<int>> staticFieldAccess = () => ExpressionExecutionTests.TestClass.StaticIntField;
			Expression<Func<int>> staticPropertyAccess = () => ExpressionExecutionTests.TestClass.StaticIntProperty;
			Expression<Func<ExpressionExecutionTests.TestClass, int>> instanceFieldAccess = t => t.IntField;
			Expression<Func<ExpressionExecutionTests.TestClass, int>> instancePropertyAccess = t => t.IntProperty;


			output.WriteLine("Rendered: " + staticFieldAccess.Body.Render());
			output.WriteLine("Rendered: " + staticPropertyAccess.Body.Render());
			output.WriteLine("Rendered: " + instanceFieldAccess.Body.Render());
			output.WriteLine("Rendered: " + instancePropertyAccess.Body.Render());

			Assert.NotNull(staticFieldAccess.Body.Render());
			Assert.NotNull(staticPropertyAccess.Body.Render());
			Assert.NotNull(instanceFieldAccess.Body.Render());
			Assert.NotNull(instancePropertyAccess.Body.Render());
		}

		[Fact]
		public void MemberInitTest()
		{
			Expression<Func<ExpressionExecutionTests.TestClass>> expression = () => new ExpressionExecutionTests.TestClass
			{
				IntField = 25,
				IntProperty = 10,
				TestClassField = new ExpressionExecutionTests.TestClass { { 1, 2 } },
				ListField = { 2, 3 },
				ListProperty = { 4, 5 }
			};

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void TypeAsTest()
		{
			Expression<Func<object, Delegate>> expression = a => a as Delegate;

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Fact]
		public void TypeIsTest()
		{
			Expression<Func<object, bool>> expression = a => a is Delegate;

			output.WriteLine("Rendered: " + expression.Body.Render());
			Assert.NotNull(expression.Body.Render());
		}

		[Theory]
		[InlineData("Math.Pow(1.0, 1.0)", 1.0)]
		[InlineData("Math.Pow(1.0, y: 1.0)", 1.0)]
		[InlineData("Math.Pow(x: 1.0, y: 1.0)", 1.0)]
		public void CallTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("1 > 2 ? 1 : 2", 1 > 2 ? 1 : 2)]
		[InlineData("true ? 1 : 2", true ? 1 : 2)]
		[InlineData("false ? 1 : 2", false ? 1 : 2)]
		[InlineData("true ? (false ? 3 : 4) : (true ? 5 : 6)", true ? (false ? 3 : 4) : (true ? 5 : 6))]
		[InlineData("1 != 1 || 1 == 1 ? 1 : 2", 1 != 1 || 1 == 1 ? 1 : 2)]
		[InlineData("1 < 2 && 3 >= 2 ? 1 : 2", 1 < 2 && 3 >= 2 ? 1 : 2)]
		public void ConditionalTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("-(1)", -(1))]
		[InlineData("+(-1)", +(-1))]
		[InlineData("!true", !true)]
		[InlineData("!false", !false)]
		[InlineData("~1", ~1)]
		public void UnaryTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("true && true", true)]
		[InlineData("true || false", true)]
		[InlineData("null ?? null", null)]
		// int8
		[InlineData("(SByte)2 + (SByte)2", (2 + 2))]
		[InlineData("unchecked((SByte)127 + (SByte)2)", unchecked((127 + 2)))]
		[InlineData("(SByte)2 - (SByte)2", (2 - 2))]
		[InlineData("unchecked(-(SByte)127 - (SByte)10)", unchecked((-127 - 10)))]
		[InlineData("(SByte)2 & (SByte)2", 2 & 2)]
		[InlineData("(SByte)2 | (SByte)2", 2 | 2)]
		[InlineData("(SByte)2 / (SByte)2", 2 / 2)]
		[InlineData("(SByte)2 == (SByte)2", 2 == 2)]
		[InlineData("(SByte)2 != (SByte)2", 2 != 2)]
		[InlineData("(SByte)2 ^ (SByte)2", 2 ^ 2)]
		[InlineData("(SByte)2 > (SByte)2", 2 > 2)]
		[InlineData("(SByte)2 >= (SByte)2", 2 >= 2)]
		[InlineData("(SByte)2 << 2", 2 << 2)]
		[InlineData("(SByte)2 >> 2", 2 >> 2)]
		[InlineData("(SByte)2 < (SByte)2", 2 < 2)]
		[InlineData("(SByte)2 <= (SByte)2", 2 <= 2)]
		[InlineData("(SByte)5 % (SByte)2", 5 % 2)]
		[InlineData("unchecked((SByte)127 * (SByte)2)", unchecked((127 * 2)))]
		[InlineData("(SByte)2 * (SByte)2", (2 * 2))]
		// uint8
		[InlineData("(Byte)2 + (Byte)2", (2 + 2))]
		[InlineData("unchecked((Byte)256 + (Byte)2)", unchecked(((byte)256 + 2)))]
		[InlineData("(Byte)2 - (Byte)2", (2 - 2))]
		[InlineData("unchecked((Byte)0 - (Byte)10)", unchecked((0 - 10)))]
		[InlineData("(Byte)2 & (Byte)2", 2 & 2)]
		[InlineData("(Byte)2 | (Byte)2", 2 | 2)]
		[InlineData("(Byte)2 / (Byte)2", 2 / 2)]
		[InlineData("(Byte)2 == (Byte)2", 2 == 2)]
		[InlineData("(Byte)2 != (Byte)2", 2 != 2)]
		[InlineData("(Byte)2 ^ (Byte)2", 2 ^ 2)]
		[InlineData("(Byte)2 > (Byte)2", 2 > 2)]
		[InlineData("(Byte)2 >= (Byte)2", 2 >= 2)]
		[InlineData("(Byte)2 << 2", 2 << 2)]
		[InlineData("(Byte)2 >> 2", 2 >> 2)]
		[InlineData("(Byte)2 < (Byte)2", 2 < 2)]
		[InlineData("(Byte)2 <= (Byte)2", 2 <= 2)]
		[InlineData("(Byte)5 % (Byte)2", 5 % 2)]
		[InlineData("unchecked((Byte)256 * (Byte)2)", unchecked(((byte)256 * 2)))]
		[InlineData("(Byte)2 * (Byte)2", (2 * 2))]
		// int16
		[InlineData("(Int16)2 + (Int16)2", (2 + 2))]
		[InlineData("unchecked((Int16)32767 + (Int16)2)", unchecked((32767 + 2)))]
		[InlineData("(Int16)2 - (Int16)2", (2 - 2))]
		[InlineData("unchecked(-(Int16)32766 - (Int16)10)", unchecked((-32766 - 10)))]
		[InlineData("(Int16)2 & (Int16)2", 2 & 2)]
		[InlineData("(Int16)2 | (Int16)2", 2 | 2)]
		[InlineData("(Int16)2 / (Int16)2", 2 / 2)]
		[InlineData("(Int16)2 == (Int16)2", 2 == 2)]
		[InlineData("(Int16)2 != (Int16)2", 2 != 2)]
		[InlineData("(Int16)2 ^ (Int16)2", 2 ^ 2)]
		[InlineData("(Int16)2 > (Int16)2", 2 > 2)]
		[InlineData("(Int16)2 >= (Int16)2", 2 >= 2)]
		[InlineData("(Int16)2 << 2", 2 << 2)]
		[InlineData("(Int16)2 >> 2", 2 >> 2)]
		[InlineData("(Int16)2 < (Int16)2", 2 < 2)]
		[InlineData("(Int16)2 <= (Int16)2", 2 <= 2)]
		[InlineData("(Int16)5 % (Int16)2", 5 % 2)]
		[InlineData("unchecked((Int16)32767 * (Int16)2)", unchecked((32767 * 2)))]
		[InlineData("(Int16)2 * (Int16)2", (2 * 2))]
		// int16
		[InlineData("(UInt16)2 + (UInt16)2", (2 + 2))]
		[InlineData("unchecked((UInt16)65535 + (UInt16)2)", unchecked((65535 + 2)))]
		[InlineData("(UInt16)2 - (UInt16)2", (2 - 2))]
		[InlineData("unchecked(-(UInt16)0 - (UInt16)10)", unchecked(-(UInt16)0 - (UInt16)10))]
		[InlineData("(UInt16)2 & (UInt16)2", 2 & 2)]
		[InlineData("(UInt16)2 | (UInt16)2", 2 | 2)]
		[InlineData("(UInt16)2 / (UInt16)2", 2 / 2)]
		[InlineData("(UInt16)2 == (UInt16)2", 2 == 2)]
		[InlineData("(UInt16)2 != (UInt16)2", 2 != 2)]
		[InlineData("(UInt16)2 ^ (UInt16)2", 2 ^ 2)]
		[InlineData("(UInt16)2 > (UInt16)2", 2 > 2)]
		[InlineData("(UInt16)2 >= (UInt16)2", 2 >= 2)]
		[InlineData("(UInt16)2 << 2", 2 << 2)]
		[InlineData("(UInt16)2 >> 2", 2 >> 2)]
		[InlineData("(UInt16)2 < (UInt16)2", 2 < 2)]
		[InlineData("(UInt16)2 <= (UInt16)2", 2 <= 2)]
		[InlineData("(UInt16)5 % (UInt16)2", 5 % 2)]
		[InlineData("unchecked((UInt16)65535 * (UInt16)2)", unchecked((65535 * 2)))]
		[InlineData("(UInt16)2 * (UInt16)2", (2 * 2))]
		// int32
		[InlineData("2 + 2", (2 + 2))]
		[InlineData("unchecked(2147483647 + 2)", unchecked(2147483647 + 2))]
		[InlineData("2 - 2", (2 - 2))]
		[InlineData("unchecked(-2147483646 - 10)", unchecked(-2147483646 - 10))]
		[InlineData("2 & 2", 2 & 2)]
		[InlineData("2 | 2", 2 | 2)]
		[InlineData("2 / 2", 2 / 2)]
		[InlineData("2 == 2", 2 == 2)]
		[InlineData("2 != 2", 2 != 2)]
		[InlineData("2 ^ 2", 2 ^ 2)]
		[InlineData("2 > 2", 2 > 2)]
		[InlineData("2 >= 2", 2 >= 2)]
		[InlineData("2 << 2", 2 << 2)]
		[InlineData("2 >> 2", 2 >> 2)]
		[InlineData("2 < 2", 2 < 2)]
		[InlineData("2 <= 2", 2 <= 2)]
		[InlineData("5 % 2", 5 % 2)]
		[InlineData("unchecked(2147483647 * 2)", unchecked(2147483647 * 2))]
		[InlineData("2 * 2", (2 * 2))]
		// uint
		[InlineData("2u + 2u", (2u + 2u))]
		[InlineData("unchecked(4294967295u + 2u)", unchecked(4294967295u + 2u))]
		[InlineData("2u - 2u", (2u - 2u))]
		[InlineData("unchecked(0u - 10u)", unchecked(0u - 10u))]
		[InlineData("2u & 2u", 2u & 2u)]
		[InlineData("2u | 2u", 2u | 2u)]
		[InlineData("2u / 2u", 2u / 2u)]
		[InlineData("2u == 2u", 2u == 2u)]
		[InlineData("2u != 2u", 2u != 2u)]
		[InlineData("2u ^ 2u", 2u ^ 2u)]
		[InlineData("2u > 2u", 2u > 2u)]
		[InlineData("2u >= 2u", 2u >= 2u)]
		[InlineData("2u << 2", 2u << 2)]
		[InlineData("2u >> 2", 2u >> 2)]
		[InlineData("2u < 2u", 2u < 2u)]
		[InlineData("2u <= 2u", 2u <= 2u)]
		[InlineData("5u % 2u", 5u % 2u)]
		[InlineData("unchecked(4294967295u * 2u)", unchecked(4294967295u * 2u))]
		[InlineData("2u * 2u", (2u * 2u))]
		// int64
		[InlineData("2L + 2L", (2L + 2L))]
		[InlineData("unchecked(9223372036854775807L + 2L)", unchecked(9223372036854775807L + 2L))]
		[InlineData("2L - 2L", (2L - 2L))]
		[InlineData("unchecked(-9223372036854775807L - 10L)", unchecked(-9223372036854775807L - 10L))]
		[InlineData("2L & 2L", 2L & 2L)]
		[InlineData("2L | 2L", 2L | 2L)]
		[InlineData("2L / 2L", 2L / 2L)]
		[InlineData("2L == 2L", 2L == 2L)]
		[InlineData("2L != 2L", 2L != 2L)]
		[InlineData("2L ^ 2L", 2L ^ 2L)]
		[InlineData("2L > 2L", 2L > 2L)]
		[InlineData("2L >= 2L", 2L >= 2L)]
		[InlineData("2L << 2", 2L << 2)]
		[InlineData("2L >> 2", 2L >> 2)]
		[InlineData("2L < 2L", 2L < 2L)]
		[InlineData("2L <= 2L", 2L <= 2L)]
		[InlineData("5L % 2L", 5L % 2L)]
		[InlineData("unchecked(9223372036854775807L * 2L)", unchecked(9223372036854775807L * 2L))]
		[InlineData("2L * 2L", (2L * 2L))]
		// uin64
		[InlineData("2UL + 2UL", (2UL + 2UL))]
		[InlineData("unchecked(18446744073709551615UL + 2UL)", unchecked(18446744073709551615UL + 2UL))]
		[InlineData("2UL - 2UL", (2UL - 2UL))]
		[InlineData("unchecked(0UL - 10UL)", unchecked(0UL - 10UL))]
		[InlineData("2UL & 2UL", 2UL & 2UL)]
		[InlineData("2UL | 2UL", 2UL | 2UL)]
		[InlineData("2UL / 2UL", 2UL / 2UL)]
		[InlineData("2UL == 2UL", 2UL == 2UL)]
		[InlineData("2UL != 2UL", 2UL != 2UL)]
		[InlineData("2UL ^ 2UL", 2UL ^ 2UL)]
		[InlineData("2UL > 2UL", 2UL > 2UL)]
		[InlineData("2UL >= 2UL", 2UL >= 2UL)]
		[InlineData("2UL << 2", 2UL << 2)]
		[InlineData("2UL >> 2", 2UL >> 2)]
		[InlineData("2UL < 2UL", 2UL < 2UL)]
		[InlineData("2UL <= 2UL", 2UL <= 2UL)]
		[InlineData("5UL % 2UL", 5UL % 2UL)]
		[InlineData("unchecked(18446744073709551615UL * 2UL)", unchecked(18446744073709551615UL * 2UL))]
		[InlineData("2UL * 2UL", (2UL * 2UL))]
		// single
		[InlineData("2f + 2f", (2f + 2f))]
		[InlineData("unchecked(18446744073709551615f + 2f)", unchecked(18446744073709551615f + 2f))]
		[InlineData("2f - 2f", (2f - 2f))]
		[InlineData("unchecked(0f - 10f)", unchecked(0f - 10f))]
		[InlineData("2f / 2f", 2f / 2f)]
		[InlineData("2f == 2f", 2f == 2f)]
		[InlineData("2f != 2f", 2f != 2f)]
		[InlineData("2f > 2f", 2f > 2f)]
		[InlineData("2f >= 2f", 2f >= 2f)]
		[InlineData("2f < 2f", 2f < 2f)]
		[InlineData("2f <= 2f", 2f <= 2f)]
		[InlineData("5f % 2f", 5f % 2f)]
		[InlineData("18446744073709551615f * 2f", unchecked(18446744073709551615f * 2f))]
		[InlineData("2f * 2f", (2f * 2f))]
		// double
		[InlineData("2d + 2d", (2d + 2d))]
		[InlineData("unchecked(18446744073709551615d + 2d)", unchecked(18446744073709551615d + 2d))]
		[InlineData("2d - 2d", (2d - 2d))]
		[InlineData("0d - 10d", unchecked(0d - 10d))]
		[InlineData("2d / 2d", 2d / 2d)]
		[InlineData("2d == 2d", 2d == 2d)]
		[InlineData("2d != 2d", 2d != 2d)]
		[InlineData("2d > 2d", 2d > 2d)]
		[InlineData("2d >= 2d", 2d >= 2d)]
		[InlineData("2d < 2d", 2d < 2d)]
		[InlineData("2d <= 2d", 2d <= 2d)]
		[InlineData("5d % 2d", 5d % 2d)]
		[InlineData("unchecked(18446744073709551615d * 2d)", unchecked(18446744073709551615d * 2d))]
		[InlineData("2d * 2d", (2d * 2d))]
		public void BinaryTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("2m + 2m", 2L + 2L)]
		[InlineData("unchecked(2147483647m + 2m)", unchecked(2147483647L + 2L))]
		[InlineData("2m - 2m", (2L - 2L))]
		[InlineData("0m - 10m", unchecked(0 - 10))]
		[InlineData("2m / 2m", 2L / 2L)]
		[InlineData("5m % 2m", 5L % 2L)]
		[InlineData("unchecked(2147483647m * 2m)", unchecked(2147483647L * 2L))]
		[InlineData("2m * 2m", (2L * 2L))]
		public void DecimalTest(string expression, object expectedInt64)
		{
			expression = CSharpExpression.Parse<decimal>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expressionFn = CSharpExpression.Parse<decimal>(expression).CompileAot(forceAot: true);
			var actual = expressionFn();
			var expected = Convert.ToDecimal(expectedInt64);
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("2m == 2m", 2 == 2)]
		[InlineData("2m != 2m", 2 != 2)]
		[InlineData("2m > 2m", 2 > 2)]
		[InlineData("2m >= 2m", 2 >= 2)]
		[InlineData("2m < 2m", 2 < 2)]
		[InlineData("2m <= 2m", 2 <= 2)]
		public void DecimalComparisonTest(string expression, bool expected)
		{
			expression = CSharpExpression.Parse<bool>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expressionFn = CSharpExpression.Parse<bool>(expression).CompileAot(forceAot: true);
			var actual = expressionFn();
			Assert.Equal(expected, actual);
		}

		[Theory]
		// default
		[InlineData("(Byte)1", (byte)1)]
		[InlineData("(SByte)1", (sbyte)1)]
		[InlineData("(Int16)1", (short)1)]
		[InlineData("(UInt16)1", (ushort)1)]
		[InlineData("(Int32)1", 1)]
		[InlineData("(UInt32)1", (uint)1)]
		[InlineData("(Int64)1", (long)1)]
		[InlineData("(UInt64)1", (ulong)1)]
		[InlineData("(Single)1", (float)1)]
		[InlineData("(Double)1", (double)1)]
		// unchecked
		[InlineData("unchecked((Byte)-1)", unchecked((byte)-1))]
		[InlineData("unchecked((UInt16)-1)", unchecked((ushort)-1))]
		[InlineData("unchecked((UInt32)-1)", unchecked((uint)-1))]
		[InlineData("unchecked((UInt64)-1)", unchecked((ulong)-1))]
		// byte
		[InlineData("unchecked((Byte)(Byte)-1000)", unchecked(((byte)-1000)))]
		[InlineData("unchecked((Byte)(SByte)-1000)", unchecked((byte)(sbyte)-1000))]
		[InlineData("unchecked((Byte)(Int16)-1000)", unchecked((byte)-1000))]
		[InlineData("unchecked((Byte)(UInt16)-1000)", unchecked((byte)(ushort)-1000))]
		[InlineData("unchecked((Byte)(Int32)-1000)", unchecked((byte)-1000))]
		[InlineData("unchecked((Byte)(UInt32)-1000)", unchecked((byte)(uint)-1000))]
		[InlineData("unchecked((Byte)(Int64)-1000)", unchecked((byte)-1000))]
		[InlineData("unchecked((Byte)(UInt64)-1000)", unchecked((byte)(ulong)-1000))]
		[InlineData("unchecked((Byte)(Single)-1000)", unchecked((byte)(float)-1000))]
		[InlineData("unchecked((Byte)(Double)-1000)", unchecked((byte)-1000))]
		// signed byte
		[InlineData("unchecked((SByte)(Byte)-1000)", unchecked((sbyte)(byte)-1000))]
		[InlineData("unchecked((SByte)(SByte)-1000)", unchecked(((sbyte)-1000)))]
		[InlineData("unchecked((SByte)(Int16)-1000)", unchecked((sbyte)-1000))]
		[InlineData("unchecked((SByte)(UInt16)-1000)", unchecked((sbyte)(ushort)-1000))]
		[InlineData("unchecked((SByte)(Int32)-1000)", unchecked((sbyte)-1000))]
		[InlineData("unchecked((SByte)(UInt32)-1000)", unchecked((sbyte)(uint)-1000))]
		[InlineData("unchecked((SByte)(Int64)-1000)", unchecked((sbyte)-1000))]
		[InlineData("unchecked((SByte)(UInt64)-1000)", unchecked((sbyte)(ulong)-1000))]
		[InlineData("unchecked((SByte)(Single)-1000)", unchecked((sbyte)(float)-1000))]
		[InlineData("unchecked((SByte)(Double)-1000)", unchecked((sbyte)-1000))]
		// int16
		[InlineData("unchecked((Int16)(Byte)-1000)", unchecked((short)(byte)-1000))]
		[InlineData("unchecked((Int16)(SByte)-1000)", unchecked((short)(sbyte)-1000))]
		[InlineData("unchecked((Int16)(Int16)-1000)", unchecked(((short)-1000)))]
		[InlineData("unchecked((Int16)(UInt16)-1000)", unchecked((short)(ushort)-1000))]
		[InlineData("unchecked((Int16)(Int32)-1000)", unchecked((short)-1000))]
		[InlineData("unchecked((Int16)(UInt32)-1000)", unchecked((short)(uint)-1000))]
		[InlineData("unchecked((Int16)(Int64)-1000)", unchecked((short)(long)-1000))]
		[InlineData("unchecked((Int16)(UInt64)-1000)", unchecked((short)(ulong)-1000))]
		[InlineData("unchecked((Int16)(Single)-1000)", unchecked((short)(float)-1000))]
		[InlineData("unchecked((Int16)(Double)-1000)", unchecked((short)(double)-1000))]
		// unsigned int16
		[InlineData("unchecked((UInt16)(Byte)-1000)", unchecked((ushort)(byte)-1000))]
		[InlineData("unchecked((UInt16)(SByte)-1000)", unchecked((ushort)(sbyte)-1000))]
		[InlineData("unchecked((UInt16)(Int16)-1000)", unchecked((ushort)-1000))]
		[InlineData("unchecked((UInt16)(UInt16)-1000)", unchecked(((ushort)-1000)))]
		[InlineData("unchecked((UInt16)(Int32)-1000)", unchecked((ushort)-1000))]
		[InlineData("unchecked((UInt16)(UInt32)-1000)", unchecked((ushort)(uint)-1000))]
		[InlineData("unchecked((UInt16)(Int64)-1000)", unchecked((ushort)-1000))]
		[InlineData("unchecked((UInt16)(UInt64)-1000)", unchecked((ushort)(ulong)-1000))]
		[InlineData("unchecked((UInt16)(Single)-1000)", unchecked((ushort)(float)-1000))]
		[InlineData("unchecked((UInt16)(Double)-1000)", unchecked((ushort)-1000))]
		// int32
		[InlineData("unchecked((Int32)(Byte)-1000)", unchecked((int)(byte)-1000))]
		[InlineData("unchecked((Int32)(SByte)-1000)", unchecked((int)(sbyte)-1000))]
		[InlineData("unchecked((Int32)(Int16)-1000)", unchecked((-1000)))]
		[InlineData("unchecked((Int32)(UInt16)-1000)", unchecked((int)(ushort)-1000))]
		[InlineData("unchecked((Int32)(Int32)-1000)", unchecked((-1000)))]
		[InlineData("unchecked((Int32)(UInt32)-1000)", unchecked((int)(uint)-1000))]
		[InlineData("unchecked((Int32)(Int64)-1000)", unchecked((-1000)))]
		[InlineData("unchecked((Int32)(UInt64)-1000)", unchecked((int)(ulong)-1000))]
		[InlineData("unchecked((Int32)(Single)-1000)", unchecked((-1000)))]
		[InlineData("unchecked((Int32)(Double)-1000)", unchecked((-1000)))]
		// unsigned int32
		[InlineData("unchecked((UInt32)(Byte)-1000)", unchecked((uint)(byte)-1000))]
		[InlineData("unchecked((UInt32)(SByte)-1000)", unchecked((uint)(sbyte)-1000))]
		[InlineData("unchecked((UInt32)(Int16)-1000)", unchecked((uint)-1000))]
		[InlineData("unchecked((UInt32)(UInt16)-1000)", unchecked((uint)(ushort)-1000))]
		[InlineData("unchecked((UInt32)(Int32)-1000)", unchecked((uint)-1000))]
		[InlineData("unchecked((UInt32)(UInt32)-1000)", unchecked(((uint)-1000)))]
		[InlineData("unchecked((UInt32)(Int64)-1000)", unchecked((uint)-1000))]
		[InlineData("unchecked((UInt32)(UInt64)-1000)", unchecked((uint)(ulong)-1000))]
		[InlineData("unchecked((UInt32)(Single)-1000)", unchecked((uint)(float)-1000))]
		[InlineData("unchecked((UInt32)(Double)-1000)", unchecked((uint)-1000))]
		// int64
		[InlineData("unchecked((Int64)(Byte)-1000)", unchecked((long)(byte)-1000))]
		[InlineData("unchecked((Int64)(SByte)-1000)", unchecked((long)(sbyte)-1000))]
		[InlineData("unchecked((Int64)(Int16)-1000)", unchecked((long)-1000))]
		[InlineData("unchecked((Int64)(UInt16)-1000)", unchecked((long)(ushort)-1000))]
		[InlineData("unchecked((Int64)(Int32)-1000)", unchecked((long)-1000))]
		[InlineData("unchecked((Int64)(UInt32)-1000)", unchecked((long)(uint)-1000))]
		[InlineData("unchecked((Int64)(Int64)-1000)", unchecked(((long)-1000)))]
		[InlineData("unchecked((Int64)(UInt64)-1000)", unchecked((long)(ulong)-1000))]
		[InlineData("unchecked((Int64)(Single)-1000)", unchecked((long)(float)-1000))]
		[InlineData("unchecked((Int64)(Double)-1000)", unchecked((long)(double)-1000))]
		// unsigned int64
		[InlineData("unchecked((UInt64)(Byte)-1000)", unchecked((ulong)(byte)-1000))]
		[InlineData("unchecked((UInt64)(SByte)-1000)", unchecked((ulong)(sbyte)-1000))]
		[InlineData("unchecked((UInt64)(Int16)-1000)", unchecked((ulong)-1000))]
		[InlineData("unchecked((UInt64)(UInt16)-1000)", unchecked((ulong)(ushort)-1000))]
		[InlineData("unchecked((UInt64)(Int32)-1000)", unchecked((ulong)-1000))]
		[InlineData("unchecked((UInt64)(UInt32)-1000)", unchecked((ulong)(uint)-1000))]
		[InlineData("unchecked((UInt64)(Int64)-1000)", unchecked((ulong)-1000))]
		[InlineData("unchecked((UInt64)(UInt64)-1000)", unchecked(((ulong)-1000)))]
		[InlineData("unchecked((UInt64)(Single)-1000)", unchecked((ulong)(float)-1000))]
		[InlineData("unchecked((UInt64)(Double)-1000)", unchecked((ulong)-1000))]
		// single
		[InlineData("unchecked((Single)(Byte)-1000)", unchecked((float)(byte)-1000))]
		[InlineData("unchecked((Single)(SByte)-1000)", unchecked((float)(sbyte)-1000))]
		[InlineData("unchecked((Single)(Int16)-1000)", unchecked((float)-1000))]
		[InlineData("unchecked((Single)(UInt16)-1000)", unchecked((float)(ushort)-1000))]
		[InlineData("unchecked((Single)(Int32)-1000)", unchecked((float)-1000))]
		[InlineData("unchecked((Single)(UInt32)-1000)", unchecked((float)(uint)-1000))]
		[InlineData("unchecked((Single)(Int64)-1000)", unchecked((float)-1000))]
		[InlineData("unchecked((Single)(UInt64)-1000)", unchecked((float)(ulong)-1000))]
		[InlineData("unchecked((Single)(Single)-1000)", unchecked(((float)-1000)))]
		[InlineData("unchecked((Single)(Double)-1000)", unchecked((float)(double)-1000))]
		// double
		[InlineData("unchecked((Double)(Byte)-1000)", unchecked((double)(byte)-1000))]
		[InlineData("unchecked((Double)(SByte)-1000)", unchecked((double)(sbyte)-1000))]
		[InlineData("unchecked((Double)(Int16)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Double)(UInt16)-1000)", unchecked((double)(ushort)-1000))]
		[InlineData("unchecked((Double)(Int32)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Double)(UInt32)-1000)", unchecked((double)(uint)-1000))]
		[InlineData("unchecked((Double)(Int64)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Double)(UInt64)-1000)", unchecked((double)(ulong)-1000))]
		[InlineData("unchecked((Double)(Single)-1000)", unchecked((double)(float)-1000))]
		[InlineData("unchecked((Double)(Double)-1000)", unchecked(((double)-1000)))]
		public void ConvertNumbers(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			Assert.Equal(expected, actual);
		}

		// decimal
		[InlineData("unchecked((Decimal)(Byte)-1000)", unchecked((double)(byte)-1000))]
		[InlineData("unchecked((Decimal)(SByte)-1000)", unchecked((double)(sbyte)-1000))]
		[InlineData("unchecked((Decimal)(Int16)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Decimal)(UInt16)-1000)", unchecked((double)(ushort)-1000))]
		[InlineData("unchecked((Decimal)(Int32)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Decimal)(UInt32)-1000)", unchecked((double)(uint)-1000))]
		[InlineData("unchecked((Decimal)(Int64)-1000)", unchecked((double)-1000))]
		[InlineData("unchecked((Decimal)(UInt64)-1000)", unchecked((double)(ulong)-1000))]
		[InlineData("unchecked((Decimal)(Single)-1000)", unchecked((double)(float)-1000))]
		[InlineData("unchecked((Decimal)(Double)-1000)", unchecked(((double)-1000)))]
		public void ConvertDecimal(string expression, double expectedDouble)
		{
			expression = CSharpExpression.Parse<decimal>(expression).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expressionFn = CSharpExpression.Parse<decimal>(expression).CompileAot(forceAot: true);
			var expected = (decimal)expectedDouble;
			var actual = expressionFn.DynamicInvoke();

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
		public void NullableBinaryTest(string expression, int? arg1, int? arg2, int? expected)
		{
			expression = CSharpExpression.Parse<int?, int?, int?>(expression, arg1Name: "a", arg2Name: "b").Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Parse<int?, int?, int?>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
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
		public void NullableEquationTest(string expression, int? arg1, int? arg2, bool expected)
		{
			expression = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaTest()
		{
			var expression = CSharpExpression.Parse<Func<int, int>>("a => a + 1").Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expected = 2;
			var lambda = CSharpExpression.Parse<Func<int, int>>(expression).CompileAot(forceAot: true).Invoke();
			var actual = lambda.Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaClosureTest()
		{
			var expression = CSharpExpression.Parse<int, Func<int, int>>("a => arg1 + a + 1", arg1Name: "arg1").Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expected = 3;
			var lambda = CSharpExpression.Parse<int, Func<int, int>>(expression, arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1);
			var actual = lambda.Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaBindingSubstitutionTest()
		{
			var expression = CSharpExpression.Parse<Func<int, int>>("a => a + 1").Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expected = 2;
			var actual = CSharpExpression.Parse<int, int>(expression, arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaConstructorTest()
		{
			var typeResolutionService = new KnownTypeResolver(typeof(Func<Type, object, bool>));
			var expression = CSharpExpression.Parse<Func<Type, object, bool>>("new Func<Type, object, bool>((t, c) => t != null)", typeResolutionService).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var expected = true;
			var lambda = CSharpExpression.Parse<Func<Type, object, bool>>(expression, typeResolutionService).CompileAot(forceAot: true).Invoke();
			var actual = lambda.Invoke(typeof(bool), null);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("typeof(Func<int>)", typeof(Func<int>), typeof(Type))]
		[InlineData("1 is Func<int>", false, typeof(bool))]
		[InlineData("new Func<int>(() => 1) as Array", null, typeof(object))]
		[InlineData("default(int?)", null, typeof(object))]
		public void GenericTypesInExpressionsTest(string expression, object expected, Type expectedType)
		{
			expression = ExpressionUtils.Parse(expression, new[] { expectedType }).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);

			if (expected != null)
			{
				Assert.NotNull(actual);
				Assert.IsAssignableFrom(expectedType, actual);
				Assert.Equal(expected, actual);
			}
			else
			{
				Assert.Null(actual);
			}
		}

		[Theory]
		[InlineData("ExpressionExecutionTests.TestGenericClass<int>.Field", 0)]
		[InlineData("ExpressionExecutionTests.TestGenericClass<int>.Property", 0)]
		[InlineData("new ExpressionExecutionTests.TestGenericClass<int>().InstanceMethod(10)", 10)]
		[InlineData("new ExpressionExecutionTests.TestGenericClass<int>().InstanceGenericMethod<int>(11)", 11)]
		[InlineData("ExpressionExecutionTests.TestGenericClass<int>.StaticGenericMethod<int>(12)", 12)]
		[InlineData("ExpressionExecutionTests.TestGenericClass<int>.StaticMethod()", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().Field1", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().Property1", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceMethod1()", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceGenericMethod1<int>(1,2,3,4)", 4)]
		[InlineData("GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>.StaticGenericMethod1<int>(13)", 13)]
		[InlineData("GameDevWare.Dynamic.Expressions.Tests.ExpressionExecutionTests.TestGenericClass<int>.TestSubClass<int,int>.StaticMethod1(14)", 14)]
		public void GenericInvocationTest(string expression, int expected)
		{
			var typeResolutionService = new KnownTypeResolver(typeof(ExpressionExecutionTests.TestGenericClass<>), typeof(ExpressionExecutionTests.TestGenericClass<>.TestSubClass<,>));
			expression = CSharpExpression.Parse<int>(expression, typeResolutionService).Body.Render();
			output.WriteLine("Rendered: " + expression);
			var actual = CSharpExpression.Parse<int>(expression, typeResolutionService).CompileAot(forceAot: true).Invoke();
			Assert.Equal(expected, actual);
		}
	}
}
