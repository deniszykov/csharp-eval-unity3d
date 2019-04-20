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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using GameDevWare.Dynamic.Expressions.CSharp;

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// Abstract expression tree which is not bound to concrete types. Could be bound with <see cref="UnboundExpression.Bind{ResultT}"/> methods.
	/// </summary>
	public sealed class UnboundExpression
	{
		private readonly Dictionary<MethodCallSignature, Expression> compiledExpressions;

		private readonly SyntaxTreeNode syntaxTree;
		/// <summary>
		/// Syntax tree of this expression.
		/// </summary>
		public SyntaxTreeNode SyntaxTree { get { return this.syntaxTree; } }

		/// <summary>
		/// Creates new <see cref="UnboundExpression"/> from syntax tree.
		/// </summary>
		/// <param name="node"></param>
		public UnboundExpression(IDictionary<string, object> node)
		{
			if (node == null) throw new ArgumentNullException("node");

			this.compiledExpressions = new Dictionary<MethodCallSignature, Expression>();
			this.syntaxTree = node is SyntaxTreeNode ? (SyntaxTreeNode)node : new SyntaxTreeNode(node);
		}

		/// <summary>
		/// Binds expression to concrete types and compiles it afterward.
		/// </summary>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <returns>Bound and compiled into <see cref="Func{ResultT}"/> expression.</returns>
		public Func<ResultT> Bind<ResultT>()
		{
			var key = new MethodCallSignature(typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = new ReadOnlyCollection<ParameterExpression>(new ParameterExpression[0]);
					var builder = new Binder(parameters, resultType: typeof(ResultT));
					expression = builder.Bind(this.SyntaxTree);
					this.compiledExpressions.Add(key, expression);
				}
			}

			return ((Expression<Func<ResultT>>)expression).CompileAot();
		}
		/// <summary>
		/// Binds expression to concrete types and compiles it afterward.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="arg1Name">First argument name or <see cref="CSharpExpression.ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <returns>Bound and compiled into <see cref="Func{Arg1T, ResultT}"/> expression.</returns>
		public Func<Arg1T, ResultT> Bind<Arg1T, ResultT>(string arg1Name = null)
		{
			var key = new MethodCallSignature(typeof(Arg1T), arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME, typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T) }, new[] { arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME });
					var builder = new Binder(parameters, resultType: typeof(ResultT));
					expression = builder.Bind(this.SyntaxTree);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, ResultT>>)expression).CompileAot();
		}
		/// <summary>
		/// Binds expression to concrete types and compiles it afterward.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="arg1Name">First argument name or <see cref="CSharpExpression.ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="CSharpExpression.ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <returns>Bound and compiled into <see cref="Func{Arg1T, Arg2T, ResultT}"/> expression.</returns>
		public Func<Arg1T, Arg2T, ResultT> Bind<Arg1T, Arg2T, ResultT>(string arg1Name = null, string arg2Name = null)
		{
			var key = new MethodCallSignature(
				typeof(Arg1T),
				arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME, typeof(Arg2T),
				arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME, typeof(ResultT)
			);
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters
					(
						new[] { typeof(Arg1T), typeof(Arg2T) },
						new[] { arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME, arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME }
					);
					var builder = new Binder(parameters, resultType: typeof(ResultT));
					expression = builder.Bind(this.SyntaxTree);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, Arg2T, ResultT>>)expression).CompileAot();
		}
		/// <summary>
		/// Binds expression to concrete types and compiles it afterward.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="arg1Name">First argument name or <see cref="CSharpExpression.ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="CSharpExpression.ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="CSharpExpression.ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <returns>Bound and compiled into <see cref="Func{Arg1T, Arg2T, Arg3T, ResultT}"/> expression.</returns>
		public Func<Arg1T, Arg2T, Arg3T, ResultT> Bind<Arg1T, Arg2T, Arg3T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null)
		{
			var key = new MethodCallSignature
			(
				typeof(Arg1T),
				arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME, typeof(Arg2T),
				arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME, typeof(Arg3T),
				arg3Name ?? CSharpExpression.ARG3_DEFAULT_NAME, typeof(ResultT)
			);
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters
					(
						new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T) },
						new[]
						{
							arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME,
							arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME,
							arg3Name ?? CSharpExpression.ARG3_DEFAULT_NAME
						}
					);
					var builder = new Binder(parameters, resultType: typeof(ResultT));
					expression = builder.Bind(this.SyntaxTree);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, Arg2T, Arg3T, ResultT>>)expression).CompileAot();
		}
		/// <summary>
		/// Binds expression to concrete types and compiles it afterward.
		/// </summary>
		/// <typeparam name="Arg1T">First argument type.</typeparam>
		/// <typeparam name="Arg2T">Second argument type.</typeparam>
		/// <typeparam name="Arg3T">Third argument type.</typeparam>
		/// <typeparam name="Arg4T">Fourth argument type.</typeparam>
		/// <typeparam name="ResultT">Result type.</typeparam>
		/// <param name="arg1Name">First argument name or <see cref="CSharpExpression.ARG1_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg2Name">Second argument name or <see cref="CSharpExpression.ARG2_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg3Name">Third argument name or <see cref="CSharpExpression.ARG3_DEFAULT_NAME"/> if not specified.</param>
		/// <param name="arg4Name">Fourth argument name or <see cref="CSharpExpression.ARG4_DEFAULT_NAME"/> if not specified.</param>
		/// <returns>Bound and compiled into <see cref="Func{Arg1T, Arg2T, Arg3T, Arg4T, ResultT}"/> expression.</returns>
		public Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Bind<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null, string arg4Name = null)
		{
			var key = new MethodCallSignature
			(
				typeof(Arg1T),
				arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME, typeof(Arg2T),
				arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME, typeof(Arg3T),
				arg3Name ?? CSharpExpression.ARG3_DEFAULT_NAME, typeof(Arg4T),
				arg4Name ?? CSharpExpression.ARG4_DEFAULT_NAME, typeof(ResultT)
			);
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters
					(
						new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T), typeof(Arg4T) },
						new[]
						{
							arg1Name ?? CSharpExpression.ARG1_DEFAULT_NAME,
							arg2Name ?? CSharpExpression.ARG2_DEFAULT_NAME,
							arg3Name ?? CSharpExpression.ARG3_DEFAULT_NAME,
							arg4Name ?? CSharpExpression.ARG4_DEFAULT_NAME
						}
					);
					var builder = new Binder(parameters, resultType: typeof(ResultT));
					expression = builder.Bind(this.SyntaxTree);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>)expression).CompileAot();
		}

		private static ReadOnlyCollection<ParameterExpression> CreateParameters(Type[] types, string[] names)
		{
			if (types == null) throw new ArgumentNullException("types");
			if (names == null) throw new ArgumentNullException("names");

			if (types.Length != names.Length) throw new ArgumentException(Properties.Resources.EXCEPTION_UNBOUNDEXPR_TYPESDOESNTMATCHNAMES, "types");

			var parameters = new ParameterExpression[types.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				if (Array.IndexOf(names, names[i]) != i) throw new ArgumentException(string.Format(Properties.Resources.EXCEPTION_UNBOUNDEXPR_DUPLICATEPARAMNAME, names[i]), "names");

				parameters[i] = Expression.Parameter(types[i], names[i]);
			}
			return new ReadOnlyCollection<ParameterExpression>(parameters);
		}

		/// <summary>
		/// Compares to another unbound expression by reference.
		/// </summary>
		public override bool Equals(object obj)
		{
			var other = obj as UnboundExpression;
			if (other == null) return false;

			return this.syntaxTree.SequenceEqual(other.SyntaxTree);
		}
		/// <summary>
		/// Returns hash code of unbound expression.
		/// </summary>
		public override int GetHashCode()
		{
			return this.syntaxTree.GetHashCode();
		}

		/// <summary>
		/// Converts unbound expression to string representation for debug purpose.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder();
			lock (this.compiledExpressions)
			{
				foreach (var compiled in this.compiledExpressions)
					sb.Append(compiled.Key).Append(": ").Append(compiled.Value).AppendLine();
			}
			return sb.ToString();
		}
	}
}
