using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal static class AotCompiler
	{
		public static Func<ResultT> PrepareFunc<ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters = null)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = Constants.EmptyParameters;
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return () =>
			{
				var locals = new object[] { null, null, null };
				var closure = new Closure(constants, locals);

				var result = closure.Unbox<ResultT>(compiledBody.Run(closure));
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, ResultT> PrepareFunc<Arg1T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return arg1 =>
			{
				var locals = new object[] { null, null, null, arg1 };
				var closure = new Closure(constants, locals);

				var result = closure.Unbox<ResultT>(compiledBody.Run(closure));
				Array.Clear(locals, 0, locals.Length);

				return result;
			};
		}
		public static Func<Arg1T, Arg2T, ResultT> PrepareFunc<Arg1T, Arg2T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				var result = closure.Unbox<ResultT>(compiledBody.Run(closure));
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, Arg2T, Arg3T, ResultT> PrepareFunc<Arg1T, Arg2T, Arg3T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				var result = closure.Unbox<ResultT>(compiledBody.Run(closure));
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> PrepareFunc<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3, arg4) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				var result = closure.Unbox<ResultT>(compiledBody.Run(closure));
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}

		public static Action PrepareAction(Expression body, ReadOnlyCollection<ParameterExpression> parameters = null)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = Constants.EmptyParameters;
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return () =>
			{
				var locals = new object[] { null, null, null };
				var closure = new Closure(constants, locals);

				compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
			};
		}
		public static Action<Arg1T> PrepareAction<Arg1T>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return arg1 =>
			{
				var locals = new object[] { null, null, null, arg1 };
				var closure = new Closure(constants, locals);

				compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
			};
		}
		public static Action<Arg1T, Arg2T> PrepareAction<Arg1T, Arg2T>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
			};
		}
		public static Action<Arg1T, Arg2T, Arg3T> PrepareAction<Arg1T, Arg2T, Arg3T>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
			};
		}
		public static Action<Arg1T, Arg2T, Arg3T, Arg4T> PrepareAction<Arg1T, Arg2T, Arg3T, Arg4T>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = ArrayUtils.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3, arg4) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
			};
		}

		public static ExecutionNode Compile(Expression expression, ConstantExpression[] constExpressions, ParameterExpression[] parameterExpressions)
		{
			if (expression == null)
				return NullNode.Instance;

			if (constExpressions == null) throw new ArgumentNullException("constExpressions");
			if (parameterExpressions == null) throw new ArgumentNullException("parameterExpressions");

			switch (expression.NodeType)
			{
				case ExpressionType.ArrayIndex:
					return new ArrayIndexNode(expression, constExpressions, parameterExpressions);

				case ExpressionType.Coalesce:
					return new CoalesceNode((BinaryExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.Power:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
					return BinaryNode.Create((BinaryExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.ArrayLength:
					return new ArrayLengthNode((UnaryExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
					return UnaryNode.Create((UnaryExpression)expression, constExpressions, parameterExpressions);
				case ExpressionType.Quote:
					return new QuoteNode((UnaryExpression)expression);
				case ExpressionType.Call:
					return new CallNode((MethodCallExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Conditional:
					return new ConditionalNode((ConditionalExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Constant:
					return new ConstantNode((ConstantExpression)expression, constExpressions);

				case ExpressionType.Invoke:
					return new InvocationNode((InvocationExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Lambda:
					return new LambdaNode((LambdaExpression)expression, parameterExpressions);

				case ExpressionType.ListInit:
					return new ListInitNode((ListInitExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.MemberAccess:
					return new MemberAccessNode((MemberExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.MemberInit:
					return new MemberInitNode((MemberInitExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.New:
					return new NewNode((NewExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.NewArrayInit:
					return new NewArrayInitNode((NewArrayExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.NewArrayBounds:
					return new NewArrayBoundsNode((NewArrayExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.Parameter:
					return new ParameterNode((ParameterExpression)expression, parameterExpressions);

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					return new ConvertNode((UnaryExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.TypeAs:
					return new TypeAsNode((UnaryExpression)expression, constExpressions, parameterExpressions);

				case ExpressionType.TypeIs:
					return new TypeIsNode((TypeBinaryExpression)expression, constExpressions, parameterExpressions);

				default:
					throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, expression.Type));
			}
		}
	}
}
