using System;
using System.Collections.Generic;
using Assets;
using Xunit;
using Xunit.Abstractions;

namespace GameDevWare.Dynamic.Expressions.Tests
{
	public class TypeResolverTests
	{
		public class InnerType
		{
			public class InnerInnerType
			{

			}

		}
		public class InnerGenType<T>
		{
			public class InnerInnerType
			{
				public T Prop1;
			}
			public class InnerInnerGenType<X>
			{
				public T Prop1;
				public X Prop2;
			}
			public class InnerInnerGenType<X, Y>
			{
				public T Prop1;
				public X Prop2;
				public Y Prop3;
			}
		}

		private readonly ITestOutputHelper output;
		public TypeResolverTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData(typeof(int))]
		[InlineData(typeof(Array))]
		[InlineData(typeof(TypeReference))]
		[InlineData(typeof(void))]
		[InlineData(typeof(Action))]
		[InlineData(typeof(Action<int>))]
		[InlineData(typeof(Func<int>))]
		[InlineData(typeof(Func<int, int>))]
		[InlineData(typeof(InnerType))]
		[InlineData(typeof(InnerType.InnerInnerType))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerType))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerGenType<int>))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerGenType<int, int>))]
		[InlineData(typeof(int?))]
		[InlineData(typeof(Guid?))]
		[InlineData(typeof(int[]))]
		[InlineData(typeof(string[]))]
		[InlineData(typeof(int[,]))]
		[InlineData(typeof(string[,]))]
		[InlineData(typeof(int[,,,]))]
		[InlineData(typeof(string[,,,]))]
		[InlineData(typeof(int*))]
		[InlineData(typeof(Guid*))]
		[InlineData(typeof(void*))]
		public void KnownTypeResolverConstructorTest(Type expected)
		{
			var knownTypeResolver = new KnownTypeResolver(expected);
			Assert.NotNull(knownTypeResolver);
		}

		[Theory]
		[InlineData(typeof(int))]
		[InlineData(typeof(Array))]
		[InlineData(typeof(TypeReference))]
		[InlineData(typeof(void))]
		[InlineData(typeof(InnerType))]
		[InlineData(typeof(InnerType.InnerInnerType))]
		public void SimpleTypeResolutionTest(Type expected)
		{
			var typeName = MakeTypeReference(expected, fullName: false);
			var typeFullName = MakeTypeReference(expected, fullName: true);
			output.WriteLine("Name: " + typeName);
			output.WriteLine("Full Name: " + typeFullName);

			var knownTypeResolver = new KnownTypeResolver(expected);

			var actual = default(Type);
			var nameFound = knownTypeResolver.TryGetType(typeName, out actual);
			Assert.True(nameFound, "Name is not found");
			Assert.Equal(expected, actual);

			var fullNameFound = knownTypeResolver.TryGetType(typeFullName, out actual);
			Assert.True(fullNameFound, "Full name is not found");
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(typeof(Action<int>))]
		[InlineData(typeof(Action<Action<int>>))]
		[InlineData(typeof(Func<int>))]
		[InlineData(typeof(Func<int, int>))]
		[InlineData(typeof(Func<int, int, int>))]
		[InlineData(typeof(Func<int, int, int, int>))]
		[InlineData(typeof(Func<int, int, int, int, int>))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerType))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerGenType<int>))]
		[InlineData(typeof(InnerGenType<int>.InnerInnerGenType<int, int>))]
		public void GenericTypeResolutionTest(Type expected)
		{
			var typeName = MakeTypeReference(expected, fullName: false);
			var typeFullName = MakeTypeReference(expected, fullName: true);
			output.WriteLine("Name: " + typeName);
			output.WriteLine("Full Name: " + typeFullName);

			var knownTypeResolver = new KnownTypeResolver(expected);

			var actual = default(Type);
			var nameFound = knownTypeResolver.TryGetType(typeName, out actual);
			Assert.True(nameFound, "Name is not found");
			Assert.Equal(expected, actual);

			var fullNameFound = knownTypeResolver.TryGetType(typeFullName, out actual);
			Assert.True(fullNameFound, "Full name is not found");
			Assert.Equal(expected, actual);
		}

		private static TypeReference MakeTypeReference(Type type, bool fullName)
		{
			var typeNameBuilder = fullName ?
				NameUtils.WriteFullName(type, writeGenericArguments: false) :
				NameUtils.WriteName(type, writeGenericArguments: false);

			NameUtils.RemoveGenericSuffix(typeNameBuilder, 0, typeNameBuilder.Length);

			var genericArguments = new List<TypeReference>();
			foreach (var genArgument in type.GetGenericArguments())
				genericArguments.Add(MakeTypeReference(genArgument, fullName));

			return new TypeReference(typeNameBuilder.ToString().Split('.'), genericArguments);
		}
	}
}
