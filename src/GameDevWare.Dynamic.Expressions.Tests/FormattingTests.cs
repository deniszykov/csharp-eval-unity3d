using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GameDevWare.Dynamic.Expressions.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class FormattingTests
	{
		private static readonly ITypeResolver TypeResolver;

		private readonly ITestOutputHelper output;

		static FormattingTests()
		{
			TypeResolver = new KnownTypeResolver(typeof(Func<Type, object, bool>),
				typeof(ExecutorTests.TestGenericClass<>),
				typeof(ExecutorTests.TestGenericClass<>.TestSubClass<,>));
		}
		public FormattingTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		public static IEnumerable<object[]> ExpressionData()
		{
			var expressions = new Expression[] {
				// Add
				CSharpExpression.ParseFunc<int>("unchecked(2 + 2)"),
				CSharpExpression.ParseFunc<string>("\"\" + \"a\""),
				CSharpExpression.ParseFunc<string>("\"\" + \"\""),
				CSharpExpression.ParseFunc<string>("\"a\" + \"b\""),
				CSharpExpression.ParseFunc<string>("\"a\" + \"b\"+ \"c\""),
				CSharpExpression.ParseFunc<string>("\"\" + \"\"+ \"\""),
				CSharpExpression.ParseFunc<string>("\"\" + \"\"+ \"\""),
				CSharpExpression.ParseFunc<string>("\"\" + \"a\"+ \"\""),
				CSharpExpression.ParseFunc<string>("\"\\r\" + \"\\n\""),
				CSharpExpression.ParseFunc<string>("\"\\\\\" + \"\\\\\""),
				CSharpExpression.ParseFunc<string>("\"\\\\\" + \"\\\\\\\\\""),
				CSharpExpression.ParseFunc<string>("\"a\\r\" + \"\\nb\""),
				CSharpExpression.ParseFunc<string>("\"\\x038\" + \"\\u0112\"+ \"\\112\""),
				CSharpExpression.ParseFunc<string>("\"a\" + 1"),
				CSharpExpression.ParseFunc<string>("1 + \"a\""),
				CSharpExpression.ParseFunc<string>("\"1\" + 'a'"),
				CSharpExpression.ParseFunc<string>("\"1\" + '\t'"),
				// AddChecked
				CSharpExpression.ParseFunc<int>("checked(2 + 2)"),
				CSharpExpression.ParseFunc<int>("checked((SByte)2 + (SByte)2)"),
				// And
				CSharpExpression.ParseFunc<int>("(SByte)2 & (SByte)2"),
				// AndAlso
				CSharpExpression.ParseFunc<bool>("true && true"),
				// ArrayLength
				(Expression<Func<int[], int>>)(arg1 => arg1.Length),
				// ArrayIndex
				(Expression<Func<int[], int>>)(arg1 => arg1[0]),
				// Call
				CSharpExpression.ParseFunc<double>("Math.Max(1.0, 2.0)"),
				CSharpExpression.ParseFunc<double>("Math.Pow(3,4)"),
				CSharpExpression.ParseFunc<double>("System.Math.Pow(5,6)"),
				CSharpExpression.ParseFunc<double>("Math.Pow(7.0f, 8.0)"),
				CSharpExpression.ParseFunc<double>("Math.Pow(9.0, y: 10.0)"),
				CSharpExpression.ParseFunc<double>("Math.Pow(y: 11.0, x: 12.0)"),
				CSharpExpression.ParseFunc<string>("default(Math)?.ToString()"),
				CSharpExpression.ParseFunc<string>("Math.E?.ToString()"),
				CSharpExpression.ParseFunc<int[], string>("arg1?[0].ToString()?[0]?.ToString().Trim()"),
				// Coalesce
				CSharpExpression.ParseFunc<int?>("null ?? 2"),
				// Conditional
				CSharpExpression.ParseFunc<int>("1 > 2 ? 1 : 2"),
				CSharpExpression.ParseFunc<int>("true ? 1 : 2"),
				CSharpExpression.ParseFunc<int>("false ? 1 : 2"),
				CSharpExpression.ParseFunc<int>("true ? (false ? 3 : 4) : (true ? 5 : 6)"),
				// Constant
				CSharpExpression.ParseFunc<int>("10"),
				CSharpExpression.ParseFunc<int>("-11"),
				CSharpExpression.ParseFunc<uint>("12U"),
				CSharpExpression.ParseFunc<long>("13L"),
				CSharpExpression.ParseFunc<ulong>("14UL"),
				CSharpExpression.ParseFunc<uint>("15u"),
				CSharpExpression.ParseFunc<long>("16l"),
				CSharpExpression.ParseFunc<ulong>("17uL"),
				CSharpExpression.ParseFunc<ulong>("18Ul"),
				CSharpExpression.ParseFunc<long>("-19l"),
				CSharpExpression.ParseFunc<double>("20D"),
				CSharpExpression.ParseFunc<double>("21d"),
				CSharpExpression.ParseFunc<double>("22.01d"),
				CSharpExpression.ParseFunc<double>("-23.01d"),
				CSharpExpression.ParseFunc<float>("24f"),
				CSharpExpression.ParseFunc<float>("25F"),
				CSharpExpression.ParseFunc<float>("26.01F"),
				CSharpExpression.ParseFunc<float>("-27.01F"),
				CSharpExpression.ParseFunc<string>("\"a\""),
				CSharpExpression.ParseFunc<char>("'b'"),
				CSharpExpression.ParseFunc<bool>("true"),
				CSharpExpression.ParseFunc<bool>("false"),
				CSharpExpression.ParseFunc<object>("null"),
				CSharpExpression.ParseFunc<Type>("typeof(Int32)"),
				CSharpExpression.ParseFunc<Type>("typeof(short)"),
				CSharpExpression.ParseFunc<Type>("typeof(Math)"),
				// Convert
				CSharpExpression.ParseFunc<byte>("unchecked((byte)(2147483647 * 2))"),
				// ConvertChecked
				CSharpExpression.ParseFunc<byte>("checked((byte)(4 * 2))"),
				// Divide
				CSharpExpression.ParseFunc<int>("2 / 2"),
				CSharpExpression.ParseFunc<int>("(SByte)2 / (SByte)2"),
				// Equal
				CSharpExpression.ParseFunc<bool>("2 == 2"),
				CSharpExpression.ParseFunc<bool>("(SByte)2 == (SByte)2"),
				// ExclusiveOr
				CSharpExpression.ParseFunc<int>("2 ^ 2"),
				// GreaterThan
				CSharpExpression.ParseFunc<bool>("2 > 2"),
				CSharpExpression.ParseFunc<bool>("(SByte)2 > (SByte)2"),
				// GreaterThanOrEqual
				CSharpExpression.ParseFunc<bool>("2 >= 2"),
				CSharpExpression.ParseFunc<bool>("(SByte)2 >= (SByte)2"),
				// Invoke
				CSharpExpression.ParseFunc<Func<int, int>, int>("arg1(2)", arg1Name: "arg1"),
				// Lambda
				CSharpExpression.ParseFunc<Func<Type, object, bool>>("new Func<Type, object, bool>((t, c) => t != null)", TypeResolver),
				// LeftShift
				CSharpExpression.ParseFunc<int>("(SByte)2 << 2"),
				CSharpExpression.ParseFunc<int>("2 << 2"),
				// LessThan
				CSharpExpression.ParseFunc<bool>("(SByte)2 < (SByte)2"),
				CSharpExpression.ParseFunc<bool>("2 < 2"),
				// LessThanOrEqual
				CSharpExpression.ParseFunc<bool>("(SByte)2 <= (SByte)2"),
				CSharpExpression.ParseFunc<bool>("2 <= 2"),
				// TODO ListInit
				//(Expression<Func<List<int>>>)(() => new List<int> { 1, 2, 3, 4 }),
				// MemberAccess
				CSharpExpression.ParseFunc<object>("ExecutorTests.TestGenericClass<int>.Field", TypeResolver),
				CSharpExpression.ParseFunc<object>("ExecutorTests.TestGenericClass<int>.Property", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().Field1", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().Property1", TypeResolver),
				CSharpExpression.ParseFunc<double>("Math.E"),
				// TODO MemberInit
				//(Expression<Func<ExecutorTests.TestClass>>)(() => new ExecutorTests.TestClass
				//{
				//	IntField = 25,
				//	IntProperty = 10,
				//	TestClassField = new ExecutorTests.TestClass { { 1, 2 } },
				//	ListField = { 2, 3 },
				//	ListProperty = { 4, 5 }
				//}),
				// Modulo
				CSharpExpression.ParseFunc<int>("5 % 2"),
				CSharpExpression.ParseFunc<int>("(SByte)5 % (SByte)2"),
				// Multiply
				CSharpExpression.ParseFunc<int>("checked(2 * 2)"),
				CSharpExpression.ParseFunc<int>("checked((SByte)2 * (SByte)2)"),
				// MultiplyChecked
				CSharpExpression.ParseFunc<int>("unchecked(2 * 2)"),
				CSharpExpression.ParseFunc<int>("unchecked((SByte)2 * (SByte)2)"),
				// NegateChecked
				CSharpExpression.ParseFunc<int>("checked(-(101))"),
				// Negate
				CSharpExpression.ParseFunc<int>("unchecked(-(100))"),
				// UnaryPlus
				CSharpExpression.ParseFunc<int>("+(1)"),
				// New
				CSharpExpression.ParseFunc<object>("new ExecutorTests.TestGenericClass<int>()", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>()", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>()", TypeResolver),
				// TODO NewArrayInit
				//(Expression<Func<int[]>>)(() => new int[] { 1, 2, 3, 4 }),
				// NewArrayBounds
				(Expression<Func<int[]>>)(() => new int[8]),
				// Not
				CSharpExpression.ParseFunc<bool>("!true"),
				CSharpExpression.ParseFunc<bool>("!false"),
				// NotEqual
				CSharpExpression.ParseFunc<bool>("2 != 2"),
				CSharpExpression.ParseFunc<bool>("(SByte)2 != (SByte)2"),
				// Or
				CSharpExpression.ParseFunc<int>("(SByte)2 | (SByte)2"),
				CSharpExpression.ParseFunc<int>("2 | 2"),
				// OrElse
				CSharpExpression.ParseFunc<bool>("true || false"),
				// Parameter
				CSharpExpression.ParseFunc<int, int>("arg1", arg1Name: "arg1"),
				// Power
				CSharpExpression.ParseFunc<int>("2 ** 2"),
				CSharpExpression.ParseFunc<sbyte>("(SByte)2 ** (SByte)2"),
				CSharpExpression.ParseFunc<float>("2f ** 2f"),
				CSharpExpression.ParseFunc<double>("2d ** 2d"),
				// Quote
				// TODO Quote
				//(Expression<Func<Expression<Func<int>>>>)(() => (() => 1)),
				// RightShift
				CSharpExpression.ParseFunc<int>("(SByte)2 >> 2"),
				CSharpExpression.ParseFunc<int>("2 >> 2"),
				// Subtract
				CSharpExpression.ParseFunc<int>("unchecked((SByte)2 - (SByte)2)"),
				CSharpExpression.ParseFunc<int>("unchecked(2 - 2)"),
				CSharpExpression.ParseFunc<int>("unchecked(-(SByte)127 - (SByte)10)"),
				// SubtractChecked
				CSharpExpression.ParseFunc<int>("checked((SByte)2 - (SByte)2)"),
				CSharpExpression.ParseFunc<int>("checked(2 - 2)"),
				CSharpExpression.ParseFunc<int>("checked(-(SByte)127 - (SByte)10)"),
				// TypeAs
				CSharpExpression.ParseFunc<string>("'a' as String"),
				CSharpExpression.ParseFunc<string>("\"a\" as System.String"),
				CSharpExpression.ParseFunc<string>("1 as string"),
				// TypeIs
				CSharpExpression.ParseFunc<bool>("1 is Int32"),
				CSharpExpression.ParseFunc<bool>("1 is Int16"),
				// Default
				CSharpExpression.ParseFunc<int>("default(System.Int32)"),
				CSharpExpression.ParseFunc<string>("default(String)"),
				// Index
				CSharpExpression.ParseFunc<int[], int>("arg1[0]", arg1Name: "arg1"),
				CSharpExpression.ParseFunc<int[], int?>("(default(int[]))?[0]", arg1Name: "arg1"),
				// OnesComplement
				CSharpExpression.ParseFunc<int>("checked(~1203)"),
				// Other
				CSharpExpression.ParseFunc<int>("2 * 2 + 3"),
				CSharpExpression.ParseFunc<int>("2 + 2 * 3"),
				CSharpExpression.ParseFunc<int>("2 + 2 * 3"),
				CSharpExpression.ParseFunc<int>("2 + 4 / 2"),
				CSharpExpression.ParseFunc<int>("2 * (2 + 3) << 1 - 1"),
				CSharpExpression.ParseFunc<int>("2 * (2 + 3) << 1 + 1 ^ 7"),
				CSharpExpression.ParseFunc<int>("2 * (2 + 3) << 1 + 1 & 7 | 25 ^ 10"),
				CSharpExpression.ParseFunc<double>("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\")"),
				CSharpExpression.ParseFunc<double>("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\") + Math.Pow(100, 1)"),
				CSharpExpression.ParseFunc<double>("(2 * (2 + 3) << 1 - 1 & 7 | 25 ^ 10) + System.Int32.Parse(\"10\") + Math.Pow(100, 1) + Math.E"),
				CSharpExpression.ParseFunc<double>("10 *  (1 / (double)(1 * 1))"),
				CSharpExpression.ParseFunc<double>("10 *  (1 / (double)(1 * 1))"),
				CSharpExpression.ParseFunc<int>("1 != 1 || 1 == 1 ? 1 : 2"),
				CSharpExpression.ParseFunc<int>("1 < 2 && 3 >= 2 ? 1 : 2"),
				CSharpExpression.ParseFunc<int>("unchecked(2147483647 + 2)"),
				CSharpExpression.ParseFunc<int>("unchecked(-2147483646 - 10)"),
				CSharpExpression.ParseFunc<int>("unchecked(2147483647 * 2)"),
				CSharpExpression.ParseFunc<int>("unchecked((int)(Byte)-1000)"),
				CSharpExpression.ParseFunc<decimal>("unchecked((Decimal)(Byte)-1000)"),
				CSharpExpression.ParseFunc<object>("new ExecutorTests.TestGenericClass<int>().InstanceMethod(10)", TypeResolver),
				CSharpExpression.ParseFunc<object>("new ExecutorTests.TestGenericClass<int>().InstanceGenericMethod<int>(11)", TypeResolver),
				CSharpExpression.ParseFunc<object>("ExecutorTests.TestGenericClass<int>.StaticGenericMethod<int>(12)", TypeResolver),
				CSharpExpression.ParseFunc<object>("ExecutorTests.TestGenericClass<int>.StaticMethod()", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceMethod1()", TypeResolver),
				CSharpExpression.ParseFunc<object>("new GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>().InstanceGenericMethod1<int>(1,2,3,4)", TypeResolver),
				CSharpExpression.ParseFunc<object>("GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>.StaticGenericMethod1<int>(13)", TypeResolver),
				CSharpExpression.ParseFunc<object>("GameDevWare.Dynamic.Expressions.Tests.ExecutorTests.TestGenericClass<int>.TestSubClass<int,int>.StaticMethod1(14)", TypeResolver),
			};
			return (from expression in expressions
					let arguments = GetLambdaArguments((LambdaExpression)expression)
					select new object[] { expression }
				);
		}

		[Theory]
		[MemberData(nameof(ExpressionData))]
		public void FormatAndParseExpressionTest(LambdaExpression expression)
		{
			var arguments = GetLambdaArguments(expression);
			var expected = EvaluateLambda(expression, arguments);

			this.output.WriteLine("Lambda Type: " + expression.Type.GetTypeInfo().GetCSharpFullName(null, options: TypeNameFormatOptions.IncludeGenericArguments));
			this.output.WriteLine("Expression: " + expression);
			this.output.WriteLine("Arguments: " + string.Join(", ", arguments.Select(a => a == null ? "<null>" : Convert.ToString(a)).ToArray()));
			this.output.WriteLine("Expected: " + expected);

			var formattedExpression = CSharpExpression.Format(expression.Body);
			this.output.WriteLine("Formatted Expression: " + formattedExpression);

			var parsedExpression = ParseLambda(formattedExpression, expression.Type);
			var actual = EvaluateLambda(parsedExpression, arguments);

			if (expected is Delegate)
				Assert.IsType(expected.GetType(), actual);
			else
				Assert.Equal(actual, expected);
		}

		[Theory]
		[MemberData(nameof(ExpressionData))]
		public void FormatNoGrowTest(LambdaExpression expression)
		{
			this.output.WriteLine("Lambda Type: " + expression.Type.GetTypeInfo().GetCSharpFullName(null, options: TypeNameFormatOptions.IncludeGenericArguments));
			this.output.WriteLine("Expression: " + expression);

			var formattedExpression = CSharpExpression.Format(expression.Body);
			var iterations = 10;
			var iterationLength = new int[iterations];
			for (var i = 0; i < iterations; i++)
			{
				this.output.WriteLine("Formatted Expression #" + i + ": " + formattedExpression);
				expression = ParseLambda(formattedExpression, expression.Type);
				this.output.WriteLine("Expression #" + i + ": " + expression);

				iterationLength[i] = formattedExpression.Length;
			}

			Assert.Equal(iterationLength[0], iterationLength.Sum() / iterations);
		}

		[Theory]
		[MemberData(nameof(ExpressionData))]
		public void FormatAndParseSyntaxTreeTest(LambdaExpression expression)
		{
			var arguments = GetLambdaArguments(expression);
			var expected = EvaluateLambda(expression, arguments);

			this.output.WriteLine("Lambda Type: " + expression.Type.GetTypeInfo().GetCSharpFullName(null, options: TypeNameFormatOptions.IncludeGenericArguments));
			this.output.WriteLine("Expression: " + expression);
			this.output.WriteLine("Arguments: " + string.Join(", ", arguments.Select(a => a == null ? "<null>" : Convert.ToString(a)).ToArray()));
			this.output.WriteLine("Expected: " + expected);

			var formattedExpression = CSharpExpression.Format(expression.Body);
			this.output.WriteLine("Formatted Expression: " + formattedExpression);
			var formattedSyntaxTree = CSharpExpression.Format(Parse(formattedExpression));
			this.output.WriteLine("Formatted SyntaxTree: " + formattedSyntaxTree);

			var parsedExpression = ParseLambda(formattedSyntaxTree, expression.Type);
			var actual = EvaluateLambda(parsedExpression, arguments);

			if (expected is Delegate)
				Assert.IsType(expected.GetType(), actual);
			else
				Assert.Equal(actual, expected);
		}

		private static object[] GetLambdaArguments(LambdaExpression expression)
		{
			var lambdaTypes = expression.Type.GetGenericArguments();
			var parameterTypes = lambdaTypes.Take(lambdaTypes.Length - 1).ToArray();
			var arguments = new object[parameterTypes.Length];
			for (var i = 0; i < parameterTypes.Length; i++)
			{
				if (parameterTypes[i] == typeof(Func<int, int>))
					arguments[i] = new Func<int, int>(x => x + 100);
				else if (parameterTypes[i] == typeof(int[]))
					arguments[i] = Enumerable.Range(100, 100).ToArray();
				else
					arguments[i] = Activator.CreateInstance(parameterTypes[i]);
			}
			return arguments;
		}
		private static object EvaluateLambda(LambdaExpression expression, object[] arguments)
		{
			return expression.Compile().DynamicInvoke(arguments);
		}
		private static LambdaExpression ParseLambda(string formattedExpression, Type lambdaType)
		{
			var tokens = Tokenizer.Tokenize(formattedExpression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			var lambdaTypes = lambdaType.GetGenericArguments();
			var resultType = lambdaTypes[lambdaTypes.Length - 1];
			var parameters = new ParameterExpression[lambdaTypes.Length - 1];
			for (var i = 0; i < parameters.Length; i++)
			{
				parameters[i] = Expression.Parameter(lambdaTypes[i], "arg" + (i + 1));
			}
			var expressionBuilder = new Binder(parameters, resultType: resultType, typeResolver: TypeResolver);
			return expressionBuilder.Bind(expressionTree);
		}
		private static SyntaxTreeNode Parse(string formattedExpression)
		{
			var tokens = Tokenizer.Tokenize(formattedExpression);
			var parseTree = Parser.Parse(tokens);
			var expressionTree = parseTree.ToSyntaxTree();
			return expressionTree;
		}
	}
}
