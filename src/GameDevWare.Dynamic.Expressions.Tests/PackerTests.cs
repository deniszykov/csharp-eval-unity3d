using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.CSharp;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable PreferConcreteValueOverDefault

namespace GameDevWare.Dynamic.Expressions.Tests;

public class PackerTests
{
	private class MyTestClass : IEnumerable
	{
		public int PublicField;
		public MyTestClass SubClassField;
		public int PublicProperty { get; set; }
		public MyTestClass(int param1)
		{
		}

		public int Add(int addParam1, string addParam2)
		{
			return 0;
		}
		public int Add(int addParam1)
		{
			return 0;
		}

		/// <inheritdoc />
		public IEnumerator GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}

	private readonly ITestOutputHelper output;

	public PackerTests(ITestOutputHelper output)
	{
		this.output = output;
	}

	public static IEnumerable<object[]> PackUnpackExpressionData()
	{
		var expressions = new Expression[] {
			(Expression<Func<bool>>)(() => !true),
			(Expression<Func<int>>)(() => +1),
			(Expression<Func<int>>)(() => -1),
			(Expression<Func<int>>)(() => ~1),
			(Expression<Func<int>>)(() => 1 << 2),
			(Expression<Func<int>>)(() => 1 >> 2),
			(Expression<Func<int>>)(() => 1 - 2),
			(Expression<Func<int>>)(() => 1 + 2),
			(Expression<Func<int>>)(() => 1 * 2),
			(Expression<Func<int>>)(() => 1 / 2),
			(Expression<Func<int>>)(() => 1 % 2),
			(Expression<Func<bool>>)(() => 1 != 2),
			(Expression<Func<bool>>)(() => 1 == 2),
			(Expression<Func<bool>>)(() => 1 > 2),
			(Expression<Func<bool>>)(() => 1 >= 2),
			(Expression<Func<bool>>)(() => 1 <= 2),
			(Expression<Func<bool>>)(() => 1 < 2),
			(Expression<Func<bool>>)(() => 1 < 2 || 1 > 2),
			(Expression<Func<bool>>)(() => (1 < 2) | (1 > 2)),
			(Expression<Func<bool>>)(() => (1 < 2) & (1 > 2)),
			(Expression<Func<bool>>)(() => 1 < 2 && 1 > 2),
			(Expression<Func<bool>>)(() => (1 < 2) ^ (1 > 2)),
			(Expression<Func<int>>)(() => default(int?) ?? 1),
			(Expression<Func<int>>)(() => 1 < 2 ? 1 : 2),
			Expression.Lambda<Func<int>>(Expression.ArrayLength(Expression.Constant(null, typeof(int[])))),
			(Expression<Func<int>>)(() => default(int[])[0]),

			//(Expression<Func<int>>)(() => default(int[,])[0,0]),
			(Expression<Func<int>>)(() => string.Empty.IndexOf('0')),
			(Expression<Func<int>>)(() => (int)Math.E),
			(Expression<Func<int>>)(() => default(Func<int>)()),
			(Expression<Func<List<int>>>)(() => new List<int> { 10, 100 }),
			(Expression<Func<MyTestClass>>)(() => new MyTestClass(10) {
				PublicField = 1,
				PublicProperty = 2,
				SubClassField = {
					PublicField = 1,
					SubClassField = {
						{ 10, string.Empty },
						10
					}
				}
			}),
			(Expression<Func<int[]>>)(() => new[] { 10 }),
			(Expression<Func<int[]>>)(() => new int[10]),
			(Expression<Func<int, int>>)(p => p),
			(Expression<Func<Expression>>)(() => default(Expression)),
			(Expression<Func<bool>>)(() => string.Empty != null)
		};
		return from expr in expressions
				select new object[] { CSharpExpression.Format(expr), expr };
	}

	[Theory, MemberData(nameof(PackUnpackExpressionData))]
	public void PackUnpackExpression(string expression, LambdaExpression lambdaExpression)
	{
		this.output.WriteLine("Original: " + lambdaExpression);

		var packedExpression = ExpressionPacker.Pack(lambdaExpression);

		this.output.WriteLine("Packed: " + JsonConvert.SerializeObject(packedExpression, Formatting.Indented));

		var syntaxTree = new SyntaxTreeNode(packedExpression);
		var binder = new Binder(lambdaExpression.Parameters, lambdaExpression.Body.Type);
		var unpackedExpression = binder.Bind(syntaxTree);

		this.output.WriteLine("Unpacked: " + unpackedExpression);

		Assert.NotNull(unpackedExpression);
		Assert.NotNull(expression);
		Assert.Equal(lambdaExpression.Body.NodeType, unpackedExpression.Body.NodeType);
	}
}
