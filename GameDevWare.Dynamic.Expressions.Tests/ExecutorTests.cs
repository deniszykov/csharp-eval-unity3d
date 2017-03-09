using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable EqualExpressionComparison

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class ExecutorTests
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
		public void ArrayIndex()
		{
			Expression<Func<int[], int>> firstElementExpr = a => a[0];
			Expression<Func<int[], int>> tenthExpr = a => a[9];

			var array = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200 };

			var expected = array.ElementAt(0);
			var actual = firstElementExpr.CompileAot(forceAot: true).Invoke(array);
			var expectedAlt = firstElementExpr.CompileAot(forceAot: false).Invoke(array);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = array.ElementAt(9);
			actual = tenthExpr.CompileAot(forceAot: true).Invoke(array);
			expectedAlt = tenthExpr.CompileAot(forceAot: false).Invoke(array);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ArrayLength()
		{
			var arrayParameter = Expression.Parameter(typeof(int[]), "array");
			Expression<Func<int[], int>> expression = Expression.Lambda<Func<int[], int>>(Expression.ArrayLength(arrayParameter), arrayParameter);

			var array = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200 };

			var expected = array.Length;
			var actual = expression.CompileAot(forceAot: true).Invoke(array);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(array);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void Quote()
		{
			Expression<Func<Expression<Func<int>>>> expression = () => (() => 1);

			var expected = ((UnaryExpression)expression.Body).Operand;
			var actual = expression.CompileAot(forceAot: true).Invoke();
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke();

			Assert.Same(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void Invoke()
		{
			Expression<Func<Func<int>, int>> expression = a => a();

			var expected = 10;
			var actual = expression.CompileAot(forceAot: true).Invoke(() => 10);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(() => 10);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void NewArrayInit()
		{
			Expression<Func<int[]>> expression = () => new int[] { 1, 2, 3, 4 };

			var expected = new int[] { 1, 2, 3, 4 };
			var actual = expression.CompileAot(forceAot: true).Invoke();
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void NewArrayBounds()
		{
			Expression<Func<int[]>> expression = () => new int[10];

			var expected = new int[10];
			var actual = expression.CompileAot(forceAot: true).Invoke();
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ListInit()
		{
			Expression<Func<List<int>>> expression = () => new List<int> { 1, 2, 3, 4 };

			var expected = new List<int> { 1, 2, 3, 4 };
			var actual = expression.CompileAot(forceAot: true).Invoke();
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void MemberAccess()
		{
			Expression<Func<int>> staticFieldAccess = () => TestClass.StaticIntField;
			Expression<Func<int>> staticPropertyAccess = () => TestClass.StaticIntProperty;
			Expression<Func<TestClass, int>> instanceFieldAccess = t => t.IntField;
			Expression<Func<TestClass, int>> instancePropertyAccess = t => t.IntProperty;


			var expected = TestClass.StaticIntField;
			var actual = staticFieldAccess.CompileAot(forceAot: true).Invoke();
			var expectedAlt = staticFieldAccess.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = TestClass.StaticIntProperty;
			actual = staticPropertyAccess.CompileAot(forceAot: true).Invoke();
			expectedAlt = staticPropertyAccess.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			var testClass = new TestClass();
			expected = testClass.IntField;
			actual = instanceFieldAccess.CompileAot(forceAot: true).Invoke(testClass);
			expectedAlt = instanceFieldAccess.CompileAot(forceAot: false).Invoke(testClass);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = testClass.IntProperty;
			actual = instancePropertyAccess.CompileAot(forceAot: true).Invoke(testClass);
			expectedAlt = instancePropertyAccess.CompileAot(forceAot: false).Invoke(testClass);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void MemberInit()
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
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected.IntField, actual.IntField);
			Assert.Equal(expected.IntProperty, actual.IntProperty);
			Assert.Equal(expected.TestClassField.ListField, actual.TestClassField.ListField);
			Assert.Equal(expected.ListField, actual.ListField);
			Assert.Equal(expected.ListProperty, actual.ListProperty);

			Assert.Equal(expectedAlt.IntField, actual.IntField);
			Assert.Equal(expectedAlt.IntProperty, actual.IntProperty);
			Assert.Equal(expectedAlt.TestClassField.ListField, actual.TestClassField.ListField);
			Assert.Equal(expectedAlt.ListField, actual.ListField);
			Assert.Equal(expectedAlt.ListProperty, actual.ListProperty);
		}

		[Fact]
		public void TypeAs()
		{
			Expression<Func<object, Delegate>> expression = a => a as Delegate;

			var expected = default(Delegate);
			var actual = expression.CompileAot(forceAot: true).Invoke(10);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(10);

			Assert.Same(expected, actual);
			Assert.Same(expectedAlt, actual);

			expected = (Predicate<int>)(p => true);
			actual = expression.CompileAot(forceAot: true).Invoke(expected);
			expectedAlt = expression.CompileAot(forceAot: false).Invoke(expected);

			Assert.Same(expected, actual);
			Assert.Same(expectedAlt, actual);
		}

		[Fact]
		public void TypeIs()
		{
			Expression<Func<object, bool>> expression = a => a is Delegate;

			var expected = false;
			var actual = expression.CompileAot(forceAot: true).Invoke(10);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(10);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = true;
			actual = expression.CompileAot(forceAot: true).Invoke((Predicate<int>)(p => true));
			expectedAlt = expression.CompileAot(forceAot: false).Invoke((Predicate<int>)(p => true));

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		[InlineData("arg1.ArrayField?[0].ToString()", "1")]
		[InlineData("arg1.TestClassField?.IntField", null)]
		[InlineData("arg1.TestClassField?[0]", null)]
		[InlineData("arg1.TestClassField?[0,1]", null)]
		[InlineData("arg1?.ListField?[1]?.ToString()", "2")]
		public void NullPropagation(string expression, object expected)
		{
			var testClass = new TestClass
			{
				ArrayField = new[] { 1, 2, 3 },
				ListField = new List<int> { 1, 2, 3 }
			};
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { testClass.GetType(), expectedType }, forceAot: true, arguments: new object[] { testClass });
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { testClass.GetType(), expectedType }, forceAot: false, arguments: new object[] { testClass });

			Assert.Equal(expectedAlt, actual);

			if (expected == null)
				Assert.Null(actual);
			else
				Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("Math.Pow(1.0, 1.0)", 1.0)]
		[InlineData("Math.Pow(1.0, y: 1.0)", 1.0)]
		[InlineData("Math.Pow(x: 1.0, y: 1.0)", 1.0)]
		public void MethodInvocation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		[InlineData("1 > 2 ? 1 : 2", 1 > 2 ? 1 : 2)]
		[InlineData("true ? 1 : 2", true ? 1 : 2)]
		[InlineData("false ? 1 : 2", false ? 1 : 2)]
		[InlineData("true ? (false ? 3 : 4) : (true ? 5 : 6)", true ? (false ? 3 : 4) : (true ? 5 : 6))]
		[InlineData("1 != 1 || 1 == 1 ? 1 : 2", 1 != 1 || 1 == 1 ? 1 : 2)]
		[InlineData("1 < 2 && 3 >= 2 ? 1 : 2", 1 < 2 && 3 >= 2 ? 1 : 2)]
		public void ConditionalOperation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		[InlineData("-(1)", -(1))]
		[InlineData("+((SByte)-1)", +-1)]
		[InlineData("!true", !true)]
		[InlineData("!false", !false)]
		[InlineData("~1", ~1)]
		public void UnaryOperation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
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
		[InlineData("unchecked(-(UInt16)0 - (UInt16)10)", unchecked(-(ushort)0 - (ushort)10))]
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
		public void NumberOperation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		// binary
		[InlineData("2m + 2m", 2L + 2L)]
		[InlineData("unchecked(2147483647m + 2m)", unchecked(2147483647L + 2L))]
		[InlineData("2m - 2m", (2L - 2L))]
		[InlineData("0m - 10m", unchecked(0 - 10))]
		[InlineData("2m / 2m", 2L / 2L)]
		[InlineData("5m % 2m", 5L % 2L)]
		[InlineData("unchecked(2147483647m * 2m)", unchecked(2147483647L * 2L))]
		[InlineData("2m * 2m", (2L * 2L))]
		[InlineData("2 ** 2", (2L * 2L))]
		// comparison
		[InlineData("2m == 2m", 2 == 2)]
		[InlineData("2m != 2m", 2 != 2)]
		[InlineData("2m > 2m", 2 > 2)]
		[InlineData("2m >= 2m", 2 >= 2)]
		[InlineData("2m < 2m", 2 < 2)]
		[InlineData("2m <= 2m", 2 <= 2)]
		public void DecimalOperation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			if (expectedType != typeof(bool))
				expectedType = typeof(decimal);

			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			expected = expected is bool ? expected : Convert.ToDecimal(expected);
			expectedAlt = expectedAlt is bool ? expectedAlt : Convert.ToDecimal(expected);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		// binary
		[InlineData("ConsoleColor.DarkCyan + 1", ConsoleColor.DarkCyan + 1)]
		[InlineData("ConsoleColor.DarkCyan - 1", ConsoleColor.DarkCyan - 1)]
		[InlineData("1 + ConsoleColor.DarkCyan", 1 + ConsoleColor.DarkCyan)]
		[InlineData("5 - ConsoleColor.DarkCyan", (ConsoleColor)2)]
		[InlineData("ConsoleColor.DarkCyan ^ ConsoleColor.DarkBlue", ConsoleColor.DarkCyan ^ ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.DarkCyan & ConsoleColor.DarkBlue", ConsoleColor.DarkCyan & ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.DarkCyan | ConsoleColor.DarkBlue", ConsoleColor.DarkCyan | ConsoleColor.DarkBlue)]
		// unary
		[InlineData("~ConsoleColor.DarkCyan", ~ConsoleColor.DarkCyan)]
		// comparison
		[InlineData("ConsoleColor.Black == ConsoleColor.DarkBlue", ConsoleColor.Black == ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.Black != ConsoleColor.DarkBlue", ConsoleColor.Black != ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.Black >  ConsoleColor.DarkBlue", ConsoleColor.Black > ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.Black >= ConsoleColor.DarkBlue", ConsoleColor.Black >= ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.Black <  ConsoleColor.DarkBlue", ConsoleColor.Black < ConsoleColor.DarkBlue)]
		[InlineData("ConsoleColor.Black <= ConsoleColor.DarkBlue", ConsoleColor.Black <= ConsoleColor.DarkBlue)]
		public void EnumOperation(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var typeResolver = new KnownTypeResolver(typeof(ConsoleColor));
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, typeResolver: typeResolver, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, typeResolver: typeResolver, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
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
		public void NumberConversion(string expression, object expected)
		{
			var expectedType = expected?.GetType() ?? typeof(object);
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
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
		public void DecimalConversion(string expression, double expectedDouble)
		{
			var expected = (decimal)expectedDouble;
			var actual = CSharpExpression.Parse<decimal>(expression).CompileAot(forceAot: true).DynamicInvoke();
			var expectedAlt = CSharpExpression.Parse<decimal>(expression).CompileAot(forceAot: false).DynamicInvoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertNullableToNullable()
		{
			Expression<Func<int?, double?>> expression = x => (double?)x;

			var expected = (double?)2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);
			expectedAlt = expression.CompileAot(forceAot: false).Invoke(null);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertFromNullable()
		{
			Expression<Func<int, double>> expression = x => (double)x;

			var expected = 2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertToNullable()
		{
			Expression<Func<int, double?>> expression = x => (double?)x;

			var expected = (double?)2.0;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertNullableToNullableEnum()
		{
			Expression<Func<int?, ConsoleColor?>> expression = x => (ConsoleColor?)x;

			var expected = (ConsoleColor?)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);
			expectedAlt = expression.CompileAot(forceAot: false).Invoke(null);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertFromNullableToEnum()
		{
			Expression<Func<int?, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertToNullableEnum()
		{
			Expression<Func<int, ConsoleColor?>> expression = x => (ConsoleColor?)x;

			var expected = (ConsoleColor?)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertFromIntToEnum()
		{
			Expression<Func<int, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void ConvertFromEnumToInt()
		{
			Expression<Func<ConsoleColor, int>> expression = x => (int)x;

			var expected = (int)ConsoleColor.DarkGreen;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGreen);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(ConsoleColor.DarkGreen);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void BoxNullable()
		{
			Expression<Func<int?, object>> expression = x => (object)x;

			var expected = (object)2;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);
			expectedAlt = expression.CompileAot(forceAot: false).Invoke(null);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void UnboxNullable()
		{
			Expression<Func<object, int?>> expression = x => (int?)x;

			var expected = (int?)2;
			var actual = expression.CompileAot(forceAot: true).Invoke(2);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);

			expected = null;
			actual = expression.CompileAot(forceAot: true).Invoke(null);
			expectedAlt = expression.CompileAot(forceAot: false).Invoke(null);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void BoxEnum()
		{
			Expression<Func<ConsoleColor, object>> expression = x => (object)x;

			var expected = (object)ConsoleColor.DarkGray;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGray);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(ConsoleColor.DarkGray);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void UnboxEnum()
		{
			Expression<Func<object, ConsoleColor>> expression = x => (ConsoleColor)x;

			var expected = (ConsoleColor)ConsoleColor.DarkGray;
			var actual = expression.CompileAot(forceAot: true).Invoke(ConsoleColor.DarkGray);
			var expectedAlt = expression.CompileAot(forceAot: false).Invoke(ConsoleColor.DarkGray);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		// C# Specs -> 7.3.7 Lifted operators -> For the binary operators
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
		// C# Specs -> 7.3.7 Lifted operators -> For the unary operators
		[InlineData("+b", null, null, null)]
		[InlineData("+b", null, 1, 1)]
		[InlineData("-b", null, null, null)]
		[InlineData("-b", null, 1, -1)]
		[InlineData("~b", null, null, null)]
		[InlineData("~b", null, 1, ~1)]
		public void LiftedOperation(string expression, int? arg1, int? arg2, int? expected)
		{
			var actual = CSharpExpression.Parse<int?, int?, int?>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
			var expectedAlt = CSharpExpression.Parse<int?, int?, int?>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: false).Invoke(arg1, arg2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		// C# Specs -> 7.3.7 Lifted operators -> For the relational operators
		[InlineData("a < b", 1, 2, true)]
		[InlineData("a < b", 1, null, false)]
		[InlineData("a > b", 1, null, false)]
		[InlineData("a == b", 1, null, false)]
		[InlineData("a >= b", 1, null, false)]
		[InlineData("a <= b", 1, null, false)]
		// C# Specs -> 7.3.7 Lifted operators -> For the equality operators
		[InlineData("null == a", null, null, true)]
		[InlineData("null == a", 1, null, false)]
		[InlineData("a != null", 1, null, true)]
		[InlineData("a != null", null, null, false)]
		[InlineData("a != b", 1, null, true)]
		[InlineData("a != b", null, null, false)]
		public void LiftedComparison(string expression, int? arg1, int? arg2, bool expected)
		{
			var actual = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
			var expectedAlt = CSharpExpression.Parse<int?, int?, bool>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: false).Invoke(arg1, arg2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		// C# Specs -> 7.11.4 Nullable boolean logical operators
		[InlineData("a & b", true, true, true)]
		[InlineData("a & b", true, false, false)]
		[InlineData("a & b", true, null, null)]
		[InlineData("a & b", false, true, false)]
		[InlineData("a & b", false, false, false)]
		[InlineData("a & b", false, null, false)]
		[InlineData("a | b", true, true, true)]
		[InlineData("a | b", true, false, true)]
		[InlineData("a | b", true, null, true)]
		[InlineData("a | b", false, true, true)]
		[InlineData("a | b", false, false, false)]
		[InlineData("a | b", false, null, null)]
		public void LiftedBoolOperation(string expression, bool? arg1, bool? arg2, bool? expected)
		{
			var actual = CSharpExpression.Parse<bool?, bool?, bool?>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: true).Invoke(arg1, arg2);
			var expectedAlt = CSharpExpression.Parse<bool?, bool?, bool?>(expression, arg1Name: "a", arg2Name: "b").CompileAot(forceAot: false).Invoke(arg1, arg2);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void LambdaBinding()
		{
			var expected = 2;
			var actual = CSharpExpression.Parse<Func<int, int>>("a => a + 1").CompileAot(forceAot: true).Invoke().Invoke(1);
			var expectedAlt = CSharpExpression.Parse<Func<int, int>>("a => a + 1").CompileAot(forceAot: false).Invoke().Invoke(1);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void LambdaClosureBinding()
		{
			var expected = 3;
			var actual = CSharpExpression.Parse<int, Func<int, int>>("a => arg1 + a + 1", arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1).Invoke(1);
			var expectedAlt = CSharpExpression.Parse<int, Func<int, int>>("a => arg1 + a + 1", arg1Name: "arg1").CompileAot(forceAot: false).Invoke(1).Invoke(1);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void LambdaBindingSubstitution()
		{
			var expected = 2;
			var actual = CSharpExpression.Parse<int, int>("a => a + 1", arg1Name: "arg1").CompileAot(forceAot: true).Invoke(1);
			var expectedAlt = CSharpExpression.Parse<int, int>("a => a + 1", arg1Name: "arg1").CompileAot(forceAot: false).Invoke(1);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Fact]
		public void LambdaConstructorBinding()
		{
			var expected = true;
			var typeResolutionService = new KnownTypeResolver(typeof(Func<Type, object, bool>));
			var actual = CSharpExpression.Parse<Func<Type, object, bool>>("new Func<Type, object, bool>((t, c) => t != null)", typeResolutionService).CompileAot(forceAot: true).Invoke().Invoke(typeof(bool), null);
			var expectedAlt = CSharpExpression.Parse<Func<Type, object, bool>>("new Func<Type, object, bool>((t, c) => t != null)", typeResolutionService).CompileAot(forceAot: false).Invoke().Invoke(typeof(bool), null);

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}

		[Theory]
		[InlineData("typeof(Func<int>)", typeof(Func<int>), typeof(Type))]
		[InlineData("1 is Func<int>", false, typeof(bool))]
		[InlineData("new Func<int>(() => 1) as Array", null, typeof(object))]
		[InlineData("default(int?)", null, typeof(object))]
		public void GenericTypesInExpressions(string expression, object expected, Type expectedType)
		{
			var actual = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: true);
			var expectedAlt = ExpressionUtils.Evaluate(expression, new[] { expectedType }, forceAot: false);

			Assert.Equal(expectedAlt, actual);

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
		[InlineData("ExecutorTests.TestGenericClass<int>.Field", 0)]
		[InlineData("ExecutorTests.TestGenericClass<int>.Property", 0)]
		[InlineData("new ExecutorTests.TestGenericClass<int>().InstanceMethod(10)", 10)]
		[InlineData("new ExecutorTests.TestGenericClass<int>().InstanceGenericMethod<int>(11)", 11)]
		[InlineData("ExecutorTests.TestGenericClass<int>.StaticGenericMethod<int>(12)", 12)]
		[InlineData("ExecutorTests.TestGenericClass<int>.StaticMethod()", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().Field1", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().Property1", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceMethod1()", 0)]
		[InlineData("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceGenericMethod1<int>(1,2,3,4)", 4)]
		[InlineData("GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>.StaticGenericMethod1<int>(13)", 13)]
		[InlineData("GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>.StaticMethod1(14)", 14)]
		public void GenericMemberInvocation(string expression, int expected)
		{
			var typeResolutionService = new KnownTypeResolver(typeof(TestGenericClass<>), typeof(TestGenericClass<>.TestSubClass<,>));
			var actual = CSharpExpression.Parse<int>(expression, typeResolutionService).CompileAot(forceAot: true).Invoke();
			var expectedAlt = CSharpExpression.Parse<int>(expression, typeResolutionService).CompileAot(forceAot: false).Invoke();

			Assert.Equal(expected, actual);
			Assert.Equal(expectedAlt, actual);
		}
	}
}
