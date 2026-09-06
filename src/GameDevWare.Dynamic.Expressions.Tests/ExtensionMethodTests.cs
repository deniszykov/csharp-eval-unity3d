using System;
using System.Collections.Generic;
using System.Linq;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class ExtensionMethodTests
{
	private static readonly KnownTypeResolver TestExtensionsResolver = new(typeof(TestExtensions), typeof(List<int>));

	[Fact]
	public void ExtensionMethodIsInvokedOnItsReceiverTest()
	{
		var actual = CSharpExpression.Evaluate<int, int>("a.Doubled()", 3, "a", TestExtensionsResolver);

		Assert.Equal(6, actual);
	}

	[Fact]
	public void ExtensionMethodAcceptsPositionalArgumentsTest()
	{
		var actual = CSharpExpression.Evaluate<int, int>("a.SumWith(4)", 3, "a", TestExtensionsResolver);

		Assert.Equal(7, actual);
	}

	[Fact]
	public void ExtensionMethodAcceptsNamedArgumentsTest()
	{
		var actual = CSharpExpression.Evaluate<int, int>("a.SumWith(other: 4)", 3, "a", TestExtensionsResolver);

		Assert.Equal(7, actual);
	}

	[Fact]
	public void ExtensionMethodIsCalledInStaticFormTest()
	{
		var actual = CSharpExpression.Evaluate<int, int>("TestExtensions.SumWith(a, 4)", 3, "a", TestExtensionsResolver);

		Assert.Equal(7, actual);
	}

	[Fact]
	public void InstanceMethodHidesExtensionMethodTest()
	{
		var actual = CSharpExpression.Evaluate<List<int>, bool>("a.Contains(2)", new List<int> { 1, 2, 3 }, "a", TestExtensionsResolver);

		Assert.True(actual); // the extension always returns false
	}

	[Fact]
	public void ExtensionMethodIsNotFoundWhenDeclaringTypeIsUnknownTest()
	{
		Assert.ThrowsAny<Exception>(() => CSharpExpression.Evaluate<int, int>("a.Doubled()", 3, "a", new KnownTypeResolver()));
	}

	[Fact]
	public void GenericExtensionMethodInfersTypeArgumentFromReceiverTest()
	{
		var actual = CSharpExpression.Evaluate<int, string>("a.Describe()", 3, "a", TestExtensionsResolver);

		Assert.Equal("Int32", actual);
	}

	[Fact]
	public void GenericExtensionMethodAcceptsExplicitTypeArgumentTest()
	{
		var actual = CSharpExpression.Evaluate<int, string>("a.Describe<object>()", 3, "a", TestExtensionsResolver);

		Assert.Equal("Object", actual);
	}

	[Fact]
	public void ExtensionMethodIsFoundByInterfaceReceiverTest()
	{
		var actual = CSharpExpression.Evaluate<int[], int>("a.Total()", new[] { 1, 2, 3 }, "a", TestExtensionsResolver);

		Assert.Equal(6, actual);
	}

	[Fact]
	public void GenericExtensionMethodIsFoundByOpenInterfaceReceiverTest()
	{
		var actual = CSharpExpression.Evaluate<List<int>, int>("a.FirstItem()", new List<int> { 7, 8 }, "a", TestExtensionsResolver);

		Assert.Equal(7, actual);
	}

	[Fact]
	public void DelegateMemberHidesExtensionMethodTest()
	{
		var typeResolver = new KnownTypeResolver(typeof(TestExtensions), typeof(Holder));

		var actual = CSharpExpression.Evaluate<Holder, int>("a.Named()", new Holder(), "a", typeResolver);

		Assert.Equal(42, actual); // the extension always returns 1
	}

	public class Holder
	{
		public Func<int> Named = () => 42;
	}
}

public class LinqMethodTests
{
	private static readonly int[] Numbers = { 1, 2, 3 };

	private static ResultT Eval<ResultT>(string expression)
	{
		return CSharpExpression.Evaluate<int[], ResultT>(expression, Numbers, "a");
	}

	[Fact]
	public void LinqIsAvailableWithoutDeclaringEnumerableAsKnownTypeTest()
	{
		Assert.Equal(3, Eval<int>("a.Count()"));
	}

	[Theory, InlineData("a.Count()", 3), InlineData("a.Count(x => x > 1)", 2), InlineData("a.Where(x => x > 1).Count()", 2), InlineData("a.Sum()", 6),
	InlineData("a.Sum(x => x * 2)", 12), InlineData("a.Min()", 1), InlineData("a.Max()", 3), InlineData("a.First()", 1), InlineData("a.First(x => x > 1)", 2),
	InlineData("a.Last()", 3), InlineData("a.FirstOrDefault(x => x > 5)", 0), InlineData("a.Skip(1).First()", 2), InlineData("a.Take(2).Count()", 2),
	InlineData("a.Reverse().First()", 3), InlineData("a.OrderByDescending(x => x).First()", 3), InlineData("a.OrderBy(x => 0 - x).First()", 3),
	InlineData("a.Concat(a).Count()", 6), InlineData("a.Distinct().Count()", 3), InlineData("a.ElementAt(1)", 2), InlineData("a.Select(x => x * 2).Sum()", 12),
	InlineData("a.SelectMany(x => a).Count()", 9), InlineData("a.Select((x, i) => x + i).Sum()", 9)]
	public void LinqQueryReturnsIntTest(string expression, int expected)
	{
		Assert.Equal(expected, Eval<int>(expression));
	}

	[Theory, InlineData("a.Any()", true), InlineData("a.Any(x => x > 2)", true), InlineData("a.Any(x => x > 5)", false), InlineData("a.All(x => x > 0)", true),
	InlineData("a.All(x => x > 1)", false), InlineData("a.Contains(2)", true), InlineData("a.Contains(9)", false)]
	public void LinqQueryReturnsBoolTest(string expression, bool expected)
	{
		Assert.Equal(expected, Eval<bool>(expression));
	}

	[Fact]
	public void LinqQueryOverListTest()
	{
		var actual = CSharpExpression.Evaluate<List<int>, int>("a.Where(x => x > 1).Sum()", new List<int> { 1, 2, 3 }, "a");

		Assert.Equal(5, actual);
	}

	[Fact]
	public void SelectInfersResultTypeFromLambdaBodyTest()
	{
		var actual = CSharpExpression.Evaluate<int[], IEnumerable<string>>("a.Select(x => x.ToString())", Numbers, "a");

		Assert.Equal(new[] { "1", "2", "3" }, actual);
	}

	[Fact]
	public void SelectKeepsElementTypeStronglyBoundTest()
	{
		var expression = CSharpExpression.ParseFunc<int[], IEnumerable<int>>("a.Select(x => x * 2)", "a");

		Assert.Equal(typeof(IEnumerable<int>), expression.Body.Type);
	}

	[Fact]
	public void ExplicitTypeArgumentsAreStillAcceptedTest()
	{
		var actual = CSharpExpression.Evaluate<int[], int>("a.Where<int>(x => x > 1).Count<int>()", Numbers, "a");

		Assert.Equal(2, actual);
	}

	[Fact]
	public void EnumerableIsCallableInStaticFormTest()
	{
		Assert.Equal(2, CSharpExpression.Evaluate<int[], int>("Enumerable.Where(a, x => x > 1).Count()", Numbers, "a"));
		Assert.Equal(3, CSharpExpression.Evaluate<int[], int>("System.Linq.Enumerable.Count(a)", Numbers, "a"));
	}

	[Fact]
	public void LinqQueryIsCompilableWithoutAotTest()
	{
		var actual = CSharpExpression.ParseFunc<int[], int>("a.Where(x => x > 1).Sum()", "a").Compile().Invoke(Numbers);

		Assert.Equal(5, actual);
	}

	[Fact]
	public void RegisterLinqFuncDoesNothingOnJitRuntimeTest()
	{
		AotCompilation.RegisterLinqFunc<int>();
		AotCompilation.RegisterLinqFunc<int, string>();
	}
}

public static class TestExtensions
{
	public static int Doubled(this int value)
	{
		return value * 2;
	}
	public static int SumWith(this int value, int other)
	{
		return value + other;
	}
	public static bool Contains(this List<int> list, int value)
	{
		return false;
	}
	public static string Describe<T>(this T value)
	{
		return typeof(T).Name;
	}
	public static int Total(this IEnumerable<int> values)
	{
		return values.Sum();
	}
	public static ItemT FirstItem<ItemT>(this IEnumerable<ItemT> values)
	{
		return values.First();
	}
	public static int Named(this ExtensionMethodTests.Holder holder)
	{
		return 1;
	}
}
