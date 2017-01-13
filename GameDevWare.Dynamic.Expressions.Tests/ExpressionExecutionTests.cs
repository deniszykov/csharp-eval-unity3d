using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable EqualExpressionComparison

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExpressionExecutionTests
	{
		public class TestClass : IEnumerable
		{
			public static int StaticIntField = 100500;
			public static int StaticIntProperty { get { return StaticIntField; } set { StaticIntField = value; } }

			public int[] ArrayField;
			public int IntField = 100500 * 2;
			public int IntProperty { get { return IntField; } set { IntField = value; } }
			public TestClass TestClassField;
			public TestClass TestClassProperty { get { return this.TestClassField; } set { TestClassField = value; } }
			public List<int> ListField = new List<int>();
			public List<int> ListProperty { get { return this.ListField; } set { this.ListField = value; } }
			public int this[int i] { get { return this.ArrayField[i]; } }
			public int this[int i, int k] { get { return this.ArrayField[i]; } }

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

		public class TestGenericClass<T1>
		{
			public class TestSubClass<T2, T3>
			{
				public T2 Field1;
				public T3 Property1 { get; set; }

				public static T2 StaticMethod1(T2 value)
				{
					return value;
				}
				public static T4 StaticGenericMethod1<T4>(T4 param4)
				{
					return param4;
				}
				public T4 InstanceGenericMethod1<T4>(T1 param1, T2 param2, T3 param3, T4 param4)
				{
					return param4;
				}
				public T3 InstanceMethod1()
				{
					return default(T3);
				}
			}

			public static T1 Field;
			public static T1 Property { get; set; }

			public static T5 StaticGenericMethod<T5>(T1 param1)
			{
				return (T5)(object)param1;
			}
			public static T1 StaticMethod()
			{
				return default(T1);
			}
			public T6 InstanceGenericMethod<T6>(T6 param1)
			{
				return param1;
			}
			public T1 InstanceMethod(T1 param1)
			{
				return param1;
			}
		}



		// convert enum
		[Fact]
		public void ArrayIndexTest()
		{
			Expression<Func<int[], int>> firstElementExpr = a => a[0];
			Expression<Func<int[], int>> tenthExpr = a => a[9];

			var array = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200 };

			var expected = array.ElementAt(0);
			var actual = firstElementExpr.CompileAot(forceAot: true).Invoke(array);

			Assert.Equal(expected, actual);

			expected = array.ElementAt(9);
			actual = tenthExpr.CompileAot(forceAot: true).Invoke(array);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ArrayLengthTest()
		{
			var arrayParameter = Expression.Parameter(typeof(int[]), "array");
			Expression<Func<int[], int>> expression = Expression.Lambda<Func<int[], int>>(Expression.ArrayLength(arrayParameter), arrayParameter);

			var array = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200 };

			var expected = array.Length;
			var actual = expression.CompileAot(forceAot: true).Invoke(array);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void QuoteTest()
		{
			Expression<Func<Expression<Func<int>>>> expression = () => (() => 1);

			var expected = ((UnaryExpression)expression.Body).Operand;
			var actual = expression.CompileAot(forceAot: true).Invoke();

			Assert.Same(expected, actual);
		}

		[Fact]
		public void InvokeTest()
		{
			Expression<Func<Func<int>, int>> expression = a => a();

			var expected = 10;
			var actual = expression.CompileAot(forceAot: true).Invoke(() => 10);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void NewArrayInitTest()
		{
			Expression<Func<int[]>> expression = () => new int[] { 1, 2, 3, 4 };

			var expected = new int[] { 1, 2, 3, 4 };
			var actual = expression.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void NewArrayBoundsTest()
		{
			Expression<Func<int[]>> expression = () => new int[10];

			var expected = new int[10];
			var actual = expression.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ListInitTest()
		{
			Expression<Func<List<int>>> expression = () => new List<int> { 1, 2, 3, 4 };

			var expected = new List<int> { 1, 2, 3, 4 };
			var actual = expression.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void MemberAccessExpression()
		{
			Expression<Func<int>> staticFieldAccess = () => TestClass.StaticIntField;
			Expression<Func<int>> staticPropertyAccess = () => TestClass.StaticIntProperty;
			Expression<Func<TestClass, int>> instanceFieldAccess = t => t.IntField;
			Expression<Func<TestClass, int>> instancePropertyAccess = t => t.IntProperty;


			var expected = TestClass.StaticIntField;
			var actual = staticFieldAccess.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected, actual);

			expected = TestClass.StaticIntProperty;
			actual = staticPropertyAccess.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected, actual);

			var testClass = new TestClass();
			expected = testClass.IntField;
			actual = instanceFieldAccess.CompileAot(forceAot: true).Invoke(testClass);

			Assert.Equal(expected, actual);

			expected = testClass.IntProperty;
			actual = instancePropertyAccess.CompileAot(forceAot: true).Invoke(testClass);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void MemberInitTest()
		{
			Expression<Func<TestClass>> expression = () => new TestClass
			{
				IntField = 25,
				IntProperty = 10,
				TestClassField = new TestClass { { 1, 2 } },
				ListField = { 2, 3 },
				ListProperty = { 4, 5 }
			};

			var expected = new TestClass
			{
				IntField = 25,
				IntProperty = 10,
				TestClassField = new TestClass { { 1, 2 } },
				ListField = { 2, 3 },
				ListProperty = { 4, 5 }
			};
			var actual = expression.CompileAot(forceAot: true).Invoke();

			Assert.Equal(expected.IntField, actual.IntField);
			Assert.Equal(expected.IntProperty, actual.IntProperty);
			Assert.Equal(expected.TestClassField.ListField, actual.TestClassField.ListField);
			Assert.Equal(expected.ListField, actual.ListField);
			Assert.Equal(expected.ListProperty, actual.ListProperty);
		}

		[Fact]
		public void TypeAsTest()
		{
			Expression<Func<object, Delegate>> expression = a => a as Delegate;

			var expected = default(Delegate);
			var actual = expression.CompileAot(forceAot: true).Invoke(10);

			Assert.Same(expected, actual);

			expected = (Predicate<int>)(p => true);
			actual = expression.CompileAot(forceAot: true).Invoke(expected);

			Assert.Same(expected, actual);
		}

		[Fact]
		public void TypeIsTest()
		{
			Expression<Func<object, bool>> expression = a => a is Delegate;

			var expected = false;
			var actual = expression.CompileAot(forceAot: true).Invoke(10);

			Assert.Equal(expected, actual);

			expected = true;
			actual = expression.CompileAot(forceAot: true).Invoke((Predicate<int>)(p => true));

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("arg1.ArrayField?[0].ToString()", "1")]
		[InlineData("arg1.TestClassField?.IntField", null)]
		[InlineData("arg1.TestClassField?[0]", null)]
		[InlineData("arg1.TestClassField?[0,1]", null)]
		[InlineData("arg1?.ListField?[1]?.ToString()", "2")]
		public void NullResolveTest(string expression, object expected)
		{
			var testClass = new TestClass
			{
				ArrayField = new[] { 1, 2, 3 },
				ListField = new List<int> { 1, 2, 3 }
			};
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { testClass.GetType(), expectedType }, forceAot: true, arguments: new object[] { testClass });

			if (expected == null)
				Assert.Null(actual);
			else
				Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("Math.Pow(1.0, 1.0)", 1.0)]
		[InlineData("Math.Pow(1.0, y: 1.0)", 1.0)]
		[InlineData("Math.Pow(x: 1.0, y: 1.0)", 1.0)]
		public void CallTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
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
		[InlineData("(SByte)2 ** (SByte)2", (2 * 2))]
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
		[InlineData("(Byte)2 ** (Byte)2", (2 * 2))]
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
		[InlineData("(Int16)2 ** (Int16)2", (2 * 2))]
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
		[InlineData("(UInt16)2 ** (UInt16)2", (2 * 2))]
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
		[InlineData("2 ** 2", (2 * 2))]
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
		[InlineData("2u ** 2u", (2u * 2u))]
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
		[InlineData("2L ** 2L", (2L * 2L))]
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
		[InlineData("2UL ** 2UL", (2UL * 2UL))]
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
		[InlineData("2f ** 2f", (2f * 2f))]
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
		[InlineData("2d ** 2d", (2d * 2d))]
		public void BinaryTest(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
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
		[InlineData("2 ** 2", (2L * 2L))]
		public void DecimalTest(string expression, object expectedInt64)
		{
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
		[InlineData("2m <= 2m", 2 <= 2m)]
		public void DecimalComparisonTest(string expression, bool expected)
		{
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
			var expressionFn = CSharpExpression.Parse<decimal>(expression).CompileAot(forceAot: true);
			var expected = (decimal)expectedDouble;
			var actual = expressionFn.DynamicInvoke();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertNullableToNullable()
		{
			Expression<Func<int?, double?>> expression = x => (double?)x;

			var expected = (double?)2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertFromNullable()
		{
			Expression<Func<int, double>> expression = x => (double)x;

			var expected = 2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertToNullable()
		{
			Expression<Func<int, double?>> expression = x => (double?)x;

			var expected = (double?)2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertNullableToNullableEnum()
		{
			Expression<Func<int?, ConsoleColor?>> expression = x => (ConsoleColor?)x;

			var expected = (ConsoleColor?)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertFromNullableToEnum()
		{
			Expression<Func<int?, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertToNullableEnum()
		{
			Expression<Func<int, ConsoleColor?>> expression = x => (ConsoleColor?)x;

			var expected = (ConsoleColor?)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertFromIntToEnum()
		{
			Expression<Func<int, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ConvertFromEnumToInt()
		{
			Expression<Func<ConsoleColor, int>> expression = x => (int)x;

			var expected = (int)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGreen);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void BoxNullable()
		{
			Expression<Func<int?, object>> expression = x => (object)x;

			var expected = (object)2;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void UnboxNullable()
		{
			Expression<Func<object, int?>> expression = x => (int?)x;

			var expected = (int?)2;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void BoxEnum()
		{
			Expression<Func<ConsoleColor, object>> expression = x => (object)x;

			var expected = (object)ConsoleColor.DarkGray;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGray);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void UnboxEnum()
		{
			Expression<Func<object, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = (ConsoleColor)ConsoleColor.DarkGray;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGray);

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
			var actual = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaBindingTest()
		{
			var expected = 2;
			var lambda = CSharpExpression.Parse<Func<int, int>>("a => a + 1").CompileAot(forceAot: true).Invoke();
			var actual = lambda.Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaClosureBindingTest()
		{
			var expected = 3;
			var lambda = CSharpExpression.Parse<int, Func<int, int>>("a => arg1 + a + 1", arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1);
			var actual = lambda.Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaBindingSubstitutionTest()
		{
			var expected = 2;
			var actual = CSharpExpression.Parse<int, int>("a => a + 1", arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1);

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void LambdaConstructorBindingTest()
		{
			var expected = true;
			var typeResolutionService = new KnownTypeResolver(typeof(Func<Type, object, bool>));
			var lambda = CSharpExpression.Parse<Func<Type, object, bool>>("new Func<Type, object, bool>((t, c) => t != null)", typeResolutionService).CompileAot(forceAot: true).Invoke();
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
		public void GenericMemberInvocationTest(string expression, int expected)
		{
			var typeResolutionService = new KnownTypeResolver(typeof(TestGenericClass<>), typeof(TestGenericClass<>.TestSubClass<,>));
			var actual = CSharpExpression.Parse<int>(expression, typeResolutionService).CompileAot(forceAot: true).Invoke();
			Assert.Equal(expected, actual);
		}
	}
}
