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

namespace GameDevWare.Dynamic.Expressions
{
	public sealed class UnboundExpression
	{
		private readonly Dictionary<MethodCallSignature, Expression> compiledExpressions;

		private readonly ExpressionTree expressionTree;
		public ExpressionTree ExpressionTree { get { return this.expressionTree; } }

		public UnboundExpression(IDictionary<string, object> node)
		{
			if (node == null) throw new ArgumentNullException("node");

			this.compiledExpressions = new Dictionary<MethodCallSignature, Expression>();
			this.expressionTree = node is ExpressionTree ? (ExpressionTree)node : new ExpressionTree(node);
		}

		public Func<ResultT> Bind<ResultT>()
		{
			var key = new MethodCallSignature(typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = new ReadOnlyCollection<ParameterExpression>(new ParameterExpression[0]);
					var builder = new ExpressionBuilder(parameters, resultType: typeof(ResultT));
					expression = Expression.Lambda<Func<ResultT>>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}

			return ((Expression<Func<ResultT>>)expression).CompileAot();
		}
		public Func<Arg1T, ResultT> Bind<Arg1T, ResultT>(string arg1Name = null)
		{
			var key = new MethodCallSignature(typeof(Arg1T), arg1Name ?? "arg1", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T) }, new[] { arg1Name ?? "arg1" });
					var builder = new ExpressionBuilder(parameters, resultType: typeof(ResultT));
					expression = Expression.Lambda<Func<Arg1T, ResultT>>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, ResultT>>)expression).CompileAot();
		}
		public Func<Arg1T, Arg2T, ResultT> Bind<Arg1T, Arg2T, ResultT>(string arg1Name = null, string arg2Name = null)
		{
			var key = new MethodCallSignature(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2" });
					var builder = new ExpressionBuilder(parameters, resultType: typeof(ResultT));
					expression = Expression.Lambda<Func<Arg1T, Arg2T, ResultT>>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, Arg2T, ResultT>>)expression).CompileAot();
		}
		public Func<Arg1T, Arg2T, Arg3T, ResultT> Bind<Arg1T, Arg2T, Arg3T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null)
		{
			var key = new MethodCallSignature(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(Arg3T), arg3Name ?? "arg3", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2", arg3Name ?? "arg3" });
					var builder = new ExpressionBuilder(parameters, resultType: typeof(ResultT));
					expression = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, ResultT>>(builder.Build(this.ExpressionTree), parameters);
					this.compiledExpressions.Add(key, expression);
				}
			}
			return ((Expression<Func<Arg1T, Arg2T, Arg3T, ResultT>>)expression).CompileAot();
		}
		public Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Bind<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(string arg1Name = null, string arg2Name = null, string arg3Name = null, string arg4Name = null)
		{
			var key = new MethodCallSignature(typeof(Arg1T), arg1Name ?? "arg1", typeof(Arg2T), arg2Name ?? "arg2", typeof(Arg3T), arg3Name ?? "arg3", typeof(Arg4T), arg4Name ?? "arg4", typeof(ResultT));
			var expression = default(Expression);
			lock (this.compiledExpressions)
			{
				if (!this.compiledExpressions.TryGetValue(key, out expression))
				{
					var parameters = CreateParameters(new[] { typeof(Arg1T), typeof(Arg2T), typeof(Arg3T), typeof(Arg4T) }, new[] { arg1Name ?? "arg1", arg2Name ?? "arg2", arg3Name ?? "arg3", arg4Name ?? "arg4" });
					var builder = new ExpressionBuilder(parameters, resultType: typeof(ResultT));
					expression = Expression.Lambda<Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>>(builder.Build(this.ExpressionTree), parameters);
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

		public override bool Equals(object obj)
		{
			var other = obj as UnboundExpression;
			if (other == null) return false;

			return this.expressionTree.SequenceEqual(other.ExpressionTree);
		}
		public override int GetHashCode()
		{
			return this.expressionTree.GetHashCode();
		}

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
