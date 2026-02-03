using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class TypeNameUtilsTests
{
	public class MyNestingClass
	{
		public class MyNestedClass
		{
			public class MyNestedNestedClass
			{
			}
		}
	}

	public class MyNestingClass<X>
	{
		public class MyNestedClass<Y>
		{
			public class MyNestedNestedClass<Z>
			{
			}
		}
	}

	public class MyNestingClass<X, Y>
	{
	}

	private readonly ITestOutputHelper outputHelper;

	public TypeNameUtilsTests(ITestOutputHelper output)
	{
		this.outputHelper = output;
	}

	[Theory, InlineData(typeof(TestStruct), "TestStruct"), InlineData(typeof(MyNestingClass), "MyNestingClass"),
	InlineData(typeof(MyNestingClass.MyNestedClass), "MyNestedClass"), InlineData(typeof(MyNestingClass.MyNestedClass.MyNestedNestedClass), "MyNestedNestedClass"),
	InlineData(typeof(MyNestingClass<TestStruct>), "MyNestingClass<TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>), "MyNestedClass<TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>.MyNestedNestedClass<TestStruct>), "MyNestedNestedClass<TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct, TestStruct>), "MyNestingClass<TestStruct,TestStruct>")]
	public void GetCSharpNameOnlyTest(Type type, string expectedName)
	{
		this.outputHelper.WriteLine("CLR Name: " + type.AssemblyQualifiedName);

		var actualName = type.GetCSharpNameOnly(options: TypeNameFormatOptions.IncludeGenericArguments).ToString();

		this.outputHelper.WriteLine("Actual name: " + actualName);

		Assert.Equal(expectedName, actualName);
	}

	[Theory, InlineData(typeof(TestStruct), "TestStruct"), InlineData(typeof(TestStruct[]), "TestStruct[]"),
	InlineData(typeof(MyNestingClass), "TypeNameUtilsTests.MyNestingClass"), InlineData(typeof(MyNestingClass[]), "TypeNameUtilsTests.MyNestingClass[]"),
	InlineData(typeof(MyNestingClass.MyNestedClass), "TypeNameUtilsTests.MyNestingClass.MyNestedClass"),
	InlineData(typeof(MyNestingClass.MyNestedClass.MyNestedNestedClass), "TypeNameUtilsTests.MyNestingClass.MyNestedClass.MyNestedNestedClass"),
	InlineData(typeof(MyNestingClass<TestStruct>), "TypeNameUtilsTests.MyNestingClass<TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct[]>[]), "TypeNameUtilsTests.MyNestingClass<TestStruct[]>[]"),
	InlineData(typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>), "TypeNameUtilsTests.MyNestingClass<TestStruct>.MyNestedClass<TestStruct>"), InlineData(
		typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>.MyNestedNestedClass<TestStruct>),
		"TypeNameUtilsTests.MyNestingClass<TestStruct>.MyNestedClass<TestStruct>.MyNestedNestedClass<TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct, TestStruct>), "TypeNameUtilsTests.MyNestingClass<TestStruct,TestStruct>")]
	public void GetCSharpName(Type type, string expectedName)
	{
		this.outputHelper.WriteLine("CLR Name: " + type.AssemblyQualifiedName);

		var actualName = type.GetCSharpName(options: TypeNameFormatOptions.IncludeGenericArguments).ToString();

		this.outputHelper.WriteLine("Actual name: " + actualName);

		Assert.Equal(expectedName, actualName);
	}

	[Theory, InlineData(typeof(TestStruct), "GameDevWare.Dynamic.Expressions.Tests.TestStruct"),
	InlineData(typeof(MyNestingClass), "GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass"),
	InlineData(typeof(MyNestingClass.MyNestedClass), "GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass.MyNestedClass"), InlineData(
		typeof(MyNestingClass.MyNestedClass.MyNestedNestedClass),
		"GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass.MyNestedClass.MyNestedNestedClass"), InlineData(typeof(MyNestingClass<TestStruct>),
		"GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>"), InlineData(
		typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>),
		"GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>.MyNestedClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct>.MyNestedClass<TestStruct>.MyNestedNestedClass<TestStruct>),
		"GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>.MyNestedClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>.MyNestedNestedClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct>"),
	InlineData(typeof(MyNestingClass<TestStruct, TestStruct>),
		"GameDevWare.Dynamic.Expressions.Tests.TypeNameUtilsTests.MyNestingClass<GameDevWare.Dynamic.Expressions.Tests.TestStruct,GameDevWare.Dynamic.Expressions.Tests.TestStruct>")]
	public void GetCSharpFullName(Type type, string expectedName)
	{
		this.outputHelper.WriteLine("CLR Name: " + type.AssemblyQualifiedName);

		var actualName = type.GetCSharpFullName(options: TypeNameFormatOptions.IncludeGenericArguments).ToString();

		this.outputHelper.WriteLine("Actual name: " + actualName);

		Assert.Equal(expectedName, actualName);
	}

	[Theory, InlineData(typeof(TestStruct), new[] { typeof(TestStruct) }),
	InlineData(typeof(MyNestingClass), new[] { typeof(TypeNameUtilsTests), typeof(MyNestingClass) }),
	InlineData(typeof(MyNestingClass.MyNestedClass), new[] { typeof(TypeNameUtilsTests), typeof(MyNestingClass), typeof(MyNestingClass.MyNestedClass) }), InlineData(
		typeof(MyNestingClass.MyNestedClass.MyNestedNestedClass),
		new[] {
			typeof(TypeNameUtilsTests), typeof(MyNestingClass), typeof(MyNestingClass.MyNestedClass), typeof(MyNestingClass.MyNestedClass.MyNestedNestedClass)
		})]
	public void GetDeclaringTypesTest(Type type, Type[] expectedTypes)
	{
		this.outputHelper.WriteLine("CLR Name: " + type.AssemblyQualifiedName);

		var actualTypes = type.GetDeclaringTypes().ToArray();

		Assert.Equal(expectedTypes, actualTypes);
	}
}
