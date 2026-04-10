using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions.Execution;
using Xunit;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class ExecutionExtendedTests
{
	private class Parent
	{
		public MyStruct StructField;
		public MyStruct StructProperty { get; set; }
		public Parent SubParent;
	}

	private struct MyStruct
	{
		public int Value;
	}

	[Fact]
	public void MemberInit_NestedStructField_ModifiesOriginal()
	{
		// () => new Parent { StructField = { Value = 1 } }
		var parentParam = Expression.Parameter(typeof(Parent), "p");
		var structField = typeof(Parent).GetField(nameof(Parent.StructField));
		var valueField = typeof(MyStruct).GetField(nameof(MyStruct.Value));

		var binding = Expression.MemberBind(structField, Expression.Bind(valueField, Expression.Constant(1)));
		var memberInit = Expression.MemberInit(Expression.New(typeof(Parent)), binding);
		var lambda = Expression.Lambda<Func<Parent>>(memberInit);

		var compiled = lambda.CompileAot(forceAot: true);
		var result = compiled();

		Assert.Equal(1, result.StructField.Value);
	}

	[Fact]
	public void MemberInit_NestedStructProperty_ModifiesOriginal()
	{
		// () => new Parent { StructProperty = { Value = 1 } }
		var structProp = typeof(Parent).GetProperty(nameof(Parent.StructProperty));
		var valueField = typeof(MyStruct).GetField(nameof(MyStruct.Value));

		var binding = Expression.MemberBind(structProp, Expression.Bind(valueField, Expression.Constant(1)));
		var memberInit = Expression.MemberInit(Expression.New(typeof(Parent)), binding);
		var lambda = Expression.Lambda<Func<Parent>>(memberInit);

		var compiled = lambda.CompileAot(forceAot: true);
		var result = compiled();

		Assert.Equal(1, result.StructProperty.Value);
	}

	[Fact]
	public void MemberInit_DeeplyNested()
	{
		// () => new Parent { SubParent = new Parent { StructField = { Value = 5 } } }
		var subParentField = typeof(Parent).GetField(nameof(Parent.SubParent));
		var structField = typeof(Parent).GetField(nameof(Parent.StructField));
		var valueField = typeof(MyStruct).GetField(nameof(MyStruct.Value));

		var innerBinding = Expression.MemberBind(structField, Expression.Bind(valueField, Expression.Constant(5)));
		var innerInit = Expression.MemberInit(Expression.New(typeof(Parent)), innerBinding);
		var outerBinding = Expression.Bind(subParentField, innerInit);
		var outerInit = Expression.MemberInit(Expression.New(typeof(Parent)), outerBinding);
		var lambda = Expression.Lambda<Func<Parent>>(outerInit);

		var compiled = lambda.CompileAot(forceAot: true);
		var result = compiled();

		Assert.NotNull(result.SubParent);
		Assert.Equal(5, result.SubParent.StructField.Value);
	}

	[Fact]
	public void ListInit_ComplexInitializers()
	{
		// () => new List<int> { 1, 2, 3 }
		var listInit = (Expression<Func<List<int>>>)(() => new List<int> { 1, 2, 3 });
		var compiled = listInit.CompileAot(forceAot: true);
		var result = compiled();

		Assert.Equal(new[] { 1, 2, 3 }, result);
	}

	[Fact]
	public void ListInit_Dictionary()
	{
		// () => new Dictionary<int, string> { { 1, "one" }, { 2, "two" } }
		var dictInit = (Expression<Func<Dictionary<int, string>>>)(() => new Dictionary<int, string> { { 1, "one" }, { 2, "two" } });
		var compiled = dictInit.CompileAot(forceAot: true);
		var result = compiled();

		Assert.Equal(2, result.Count);
		Assert.Equal("one", result[1]);
		Assert.Equal("two", result[2]);
	}

	[Fact]
	public void NewArrayBounds_Multidimensional()
	{
		// () => new int[2, 3]
		var expr = (Expression<Func<int[,]>>)(() => new int[2, 3]);
		var compiled = expr.CompileAot(forceAot: true);
		var result = compiled();

		Assert.Equal(2, result.GetLength(0));
		Assert.Equal(3, result.GetLength(1));
	}

	[Fact]
	public void ArrayIndex_Multidimensional()
	{
		// (arr) => arr[1, 2]
		var param = Expression.Parameter(typeof(int[,]), "arr");
		var arrayIndex = Expression.ArrayIndex(param, Expression.Constant(1), Expression.Constant(2));
		var lambda = Expression.Lambda<Func<int[,], int>>(arrayIndex, param);

		var compiled = lambda.CompileAot(forceAot: true);
		var arr = new int[2, 3];
		arr[1, 2] = 42;

		var result = compiled(arr);
		Assert.Equal(42, result);
	}

	[Fact]
	public void ArrayIndex_SingleDimensional()
	{
		// (arr) => arr[1]
		var param = Expression.Parameter(typeof(int[]), "arr");
		var arrayIndex = Expression.ArrayIndex(param, Expression.Constant(1));
		var lambda = Expression.Lambda<Func<int[], int>>(arrayIndex, param);

		var compiled = lambda.CompileAot(forceAot: true);
		var arr = new int[] { 10, 20, 30 };

		var result = compiled(arr);
		Assert.Equal(20, result);
	}
}
