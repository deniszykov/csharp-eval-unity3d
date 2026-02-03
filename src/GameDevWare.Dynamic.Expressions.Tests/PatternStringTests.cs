using System;
using System.Linq.Expressions;
using Assets;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests;

public class PatternStringTests
{
	public PatternStringTests(ITestOutputHelper output)
	{
		this.output = output;
	}

	private class TestClass
	{
#pragma warning disable 414
		public int IntField;
		public string StringProperty;
		public TestClass Other;
#pragma warning restore 414
	}

	private readonly ITestOutputHelper output;

	[Theory, InlineData("a test string", "a test string"), InlineData("a test {IntField} string", "a test 1 string"),
	InlineData("a test {IntField} string {StringProperty}", "a test 1 string 2"),
	InlineData("a test {IntField} string {StringProperty}{StringProperty}", "a test 1 string 22"),
	InlineData("a test {IntField} string {StringProperty}{StringProperty}{StringProperty}", "a test 1 string 222"),
	InlineData("a test {Other.IntField} string", "a test 3 string"), InlineData("{Other.StringProperty}", "4"), InlineData("{Other.Other}", ""),
	InlineData("{Other.StringProperty} aaa", "4 aaa"), InlineData("aaa{Other.StringProperty}", "aaa4")]
	public void GenericInvocationTest(string expression, string expected)
	{
		var actual = expression.TransformPattern(new TestClass { IntField = 1, StringProperty = "2", Other = new TestClass { IntField = 3, StringProperty = "4" } });
		this.output.WriteLine("Transformed: " + actual);

		Assert.Equal(expected, actual);
	}

	public class InputParser
	{
		public void Parse()
		{
			var input = "Move(up,5)";
			var myGlobal = new MyGlobal();
			RunAction(myGlobal, input);
		}

		public static void RunAction(MyGlobal global, string expression)
		{
			var tokens = Tokenizer.Tokenize(expression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree(cSharpExpression: expression);
			var expressionBinder = new Binder(Array.Empty<ParameterExpression>(), typeof(void));
			var globalExpression = Expression.Constant(global);
			var boundExpression = (Expression<Action>)expressionBinder.Bind(expressionTree, globalExpression);
			boundExpression.CompileAot().Invoke();
		}
	}

	public class MyGlobal
	{
		public string up = "upward"; // just example

		public void Move(string direction, int distance)
		{
			Console.WriteLine($"player moves {direction} {distance} spaces");
		}
	}

	[Fact]
	public void Test()
	{
		var parser = new InputParser();
		parser.Parse();
	}
}
