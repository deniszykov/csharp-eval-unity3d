using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.CSharp;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class PackerExtendedTests
{
	private class MyTestClass : IEnumerable
	{
		public int PublicField;
		public MyTestClass SubClassField;
		public int PublicProperty { get; set; }
		public MyTestClass() { }
		public MyTestClass(int param1) { }

		public int Add(int addParam1, string addParam2) { return 0; }
		public int Add(int addParam1) { return 0; }
		public IEnumerator GetEnumerator() { throw new NotImplementedException(); }
	}

	private class GenericClass<T>
	{
		public T Value;
		public T GetValue() => Value;
	}

	private readonly ITestOutputHelper output;

	public PackerExtendedTests(ITestOutputHelper output)
	{
		this.output = output;
	}

	public static IEnumerable<object[]> ExtendedPackUnpackExpressionData()
	{
		var expressions = new List<Expression> {
			// Multidimensional arrays
			(Expression<Func<int>>)(() => (new int[2, 2])[0, 0]),

			// TypeIs, TypeAs
			(Expression<Func<object, bool>>)(obj => obj is string),
			(Expression<Func<object, string>>)(obj => obj as string),

			// Coalesce with mixed types
			(Expression<Func<int?, int, int>>)((a, b) => a ?? b),

			// MemberInit with multiple bindings
			(Expression<Func<MyTestClass>>)(() => new MyTestClass { PublicField = 1, PublicProperty = 2 }),

			// NewArrayBounds
			Expression.Lambda<Func<int[,]>>(Expression.NewArrayBounds(typeof(int), Expression.Constant(2), Expression.Constant(3))),

			// Generic methods/classes
			(Expression<Func<GenericClass<int>, int>>)(c => c.GetValue()),
			(Expression<Func<List<int>, int>>)(list => list.Count),

			// Nested Lambda
			(Expression<Func<int, Func<int, int>>>)(x => y => x + y),

			// Quote
			(Expression<Func<Expression<Func<int>>>>)(() => () => 1),

			// ListInit with complex initializers
			(Expression<Func<List<int>>>)(() => new List<int> { 1, 2, 3 }),

			// Power (Math.Pow is a Call, but Expression.Power is different)
			Expression.Lambda<Func<double, double, double>>(Expression.Power(Expression.Parameter(typeof(double), "a"), Expression.Parameter(typeof(double), "b")), Expression.Parameter(typeof(double), "a"), Expression.Parameter(typeof(double), "b")),

			// MemberInit with constructor params and bindings
			(Expression<Func<MyTestClass>>)(() => new MyTestClass(1) { PublicField = 1 }),

			// ListInit with multiple-parameter Add method
			(Expression<Func<Dictionary<int, string>>>)(() => new Dictionary<int, string> { { 1, "one" }, { 2, "two" } }),

			// ArrayIndex with multiple indices
			Expression.Lambda<Func<int[,] ,int>>((Expression.ArrayIndex(Expression.Parameter(typeof(int[,]), "arr"), Expression.Constant(0), Expression.Constant(0))), Expression.Parameter(typeof(int[,]), "arr")),

			// Nested MemberInit
			(Expression<Func<MyTestClass>>)(() => new MyTestClass { SubClassField = new MyTestClass { PublicField = 5 } }),

			// New with parameters
			(Expression<Func<MyTestClass>>)(() => new MyTestClass(100)),

			// Unary expressions
			(Expression<Func<int, int>>)(x => -x),
			(Expression<Func<int, int>>)(x => +x),
			(Expression<Func<bool, bool>>)(x => !x),

			// Constants
			(Expression<Func<DateTime>>)(() => new DateTime(2026, 4, 10)),
			(Expression<Func<DayOfWeek>>)(() => DayOfWeek.Friday),
			(Expression<Func<int?>>)(() => null),
			(Expression<Func<long>>)(() => 123456789012345L),
			(Expression<Func<float>>)(() => 1.23f),
			(Expression<Func<decimal>>)(() => 1.23m),

			// Convert
			(Expression<Func<int, double>>)(x => (double)x),

			// NewArrayInit
			(Expression<Func<int[]>>)(() => new int[] { 1, 2, 3 }),
		};
		foreach (var expr in expressions)
		{
			string format;
			try { format = CSharpExpression.Format(expr); }
			catch { format = expr.ToString(); }
			yield return new object[] { format, expr };
		}
	}

	[Theory, MemberData(nameof(ExtendedPackUnpackExpressionData))]
	public void PackUnpackExpression(string expressionStr, LambdaExpression lambdaExpression)
	{
		this.output.WriteLine("Original: " + lambdaExpression);

		var packedExpression = ExpressionPacker.Pack(lambdaExpression);

		var json = JsonConvert.SerializeObject(packedExpression, Formatting.Indented);
		this.output.WriteLine("Packed: " + json);

		var unpackedExpression = ExpressionPacker.UnpackLambda(lambdaExpression.Type, packedExpression);

		this.output.WriteLine("Unpacked: " + unpackedExpression);

		Assert.NotNull(unpackedExpression);
		Assert.Equal(lambdaExpression.NodeType, unpackedExpression.NodeType);
		Assert.Equal(lambdaExpression.Type, unpackedExpression.Type);

		// Optionally check if they produce same results if they are executable
		// Some might have parameters, so we need to provide them.
	}
}
