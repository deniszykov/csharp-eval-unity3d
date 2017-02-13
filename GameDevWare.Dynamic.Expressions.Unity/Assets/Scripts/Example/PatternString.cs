/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "C# Eval()" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY,
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.

	This source code is distributed via Unity Asset Store,
	to use it in your project you should accept Terms of Service and EULA
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GameDevWare.Dynamic.Expressions;
using GameDevWare.Dynamic.Expressions.CSharp;

namespace Assets
{
	public sealed class PatternString<InstanceT>
	{
		private const int PART_TEXT = 0;
		private const int PART_EXPR = 1;

		private static readonly Binder ExpressionBinder = new Binder
		(
			parameters: new[] { Expression.Parameter(typeof(InstanceT), "p") },
			resultType: typeof(object),
			contextType: typeof(InstanceT)
		);
		private static readonly Func<object[], string> ConcatFunc = string.Concat;

		private readonly Func<InstanceT, string> transform;
		private readonly string pattern;

		public PatternString(string pattern)
		{
			if (pattern == null) throw new ArgumentNullException("pattern");

			this.pattern = pattern;
			this.transform = CreateTransformFn(pattern);
		}

		public string Tranform(InstanceT instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			return this.transform(instance);
		}

		private Func<InstanceT, string> CreateTransformFn(string pattern)
		{
			if (pattern == null) throw new ArgumentNullException("pattern");

			var concatArguments = new List<Expression>();
			foreach (var part in Split(pattern))
			{
				if (part.Key == PART_TEXT)
				{
					// add it as concat argument
					concatArguments.Add(Expression.Constant(part.Value, typeof(object)));
				}
				else
				{
					// tokenize expression
					var tokens = Tokenizer.Tokenize(part.Value);
					// build concrete tree
					var expressionTree = Parser.Parse(tokens).ToSyntaxTree(false);
					// build abstract tree
					var body = ExpressionBinder.Build(expressionTree, ExpressionBinder.Parameters[0]);
					// add it as argument for concat
					concatArguments.Add(body);
				}
			}

			var transformExpr = Expression.Lambda<Func<InstanceT, string>>
			(
				Expression.Call(ConcatFunc.Method, Expression.NewArrayInit(typeof(object), concatArguments)),
				ExpressionBinder.Parameters
			);

			return transformExpr.CompileAot();
		}
		private IEnumerable<KeyValuePair<int, string>> Split(string pattern)
		{
			if (pattern == null) throw new ArgumentNullException("pattern");

			var scanStart = 0;
			var bracersLevel = 0;

			for (var i = 0; i < pattern.Length; i++)
			{
				if (pattern[i] != '{' && pattern[i] != '}')
					continue;

				if (pattern[i] == '{' && bracersLevel > 0)
				{
					bracersLevel++;
					continue;
				}
				else if (pattern[i] == '}' && bracersLevel > 1)
				{
					bracersLevel--;
					continue;
				}
				else if (pattern[i] == '{')
				{
					if (i - scanStart != 0)
						yield return new KeyValuePair<int, string>(PART_TEXT, pattern.Substring(scanStart, i - scanStart));

					scanStart = i;
					bracersLevel++;
				}
				else if (pattern[i] == '}')
				{
					if (i - scanStart > 2)
						yield return new KeyValuePair<int, string>(PART_EXPR, pattern.Substring(scanStart + 1, i - scanStart - 1));

					scanStart = i + 1;
					bracersLevel--;
				}
			}

			if (bracersLevel > 0)
				throw new InvalidOperationException(string.Format("Unterminated expression at position {0} in '{1}'.", scanStart, pattern));

			if (scanStart < pattern.Length)
				yield return new KeyValuePair<int, string>(PART_TEXT, pattern.Substring(scanStart, pattern.Length - scanStart));
		}

		public override string ToString()
		{
			return this.pattern.ToString();
		}
	}

	public static class PatternString
	{
		public static string TransformPattern<InstanceT>(this string pattern, InstanceT instance)
		{
			if (pattern == null) throw new ArgumentNullException("pattern");
			if (instance == null) throw new ArgumentNullException("instance");

			return new PatternString<InstanceT>(pattern).Tranform(instance);
		}
	}

}
