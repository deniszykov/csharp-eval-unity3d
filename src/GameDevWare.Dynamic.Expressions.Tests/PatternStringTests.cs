using Assets;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class PatternStringTests
	{
		private class TestClass
		{
#pragma warning disable 414
			public int IntField;
			public string StringProperty;
			public TestClass Other;
#pragma warning restore 414
		}

		private readonly ITestOutputHelper output;
		public PatternStringTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData("a test string", "a test string")]
		[InlineData("a test {IntField} string", "a test 1 string")]
		[InlineData("a test {IntField} string {StringProperty}", "a test 1 string 2")]
		[InlineData("a test {IntField} string {StringProperty}{StringProperty}", "a test 1 string 22")]
		[InlineData("a test {IntField} string {StringProperty}{StringProperty}{StringProperty}", "a test 1 string 222")]
		[InlineData("a test {Other.IntField} string", "a test 3 string")]
		[InlineData("{Other.StringProperty}", "4")]
		[InlineData("{Other.Other}", "")]
		[InlineData("{Other.StringProperty} aaa", "4 aaa")]
		[InlineData("aaa{Other.StringProperty}", "aaa4")]
		public void GenericInvocationTest(string expression, string expected)
		{
			var actual = PatternString.TransformPattern(expression, new TestClass { IntField = 1, StringProperty = "2", Other = new TestClass { IntField = 3, StringProperty = "4"} });
			this.output.WriteLine("Transformed: " + actual);

			Assert.Equal(expected, actual);
		}
	}
}
