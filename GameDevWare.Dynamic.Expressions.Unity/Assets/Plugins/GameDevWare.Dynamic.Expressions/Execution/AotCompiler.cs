using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace GameDevWare.Dynamic.Expressions.Execution
{
	internal static class AotCompiler
	{
		static AotCompiler()
		{
			// AOR
			if (typeof(AotCompiler).Name == string.Empty)
			{
				// ReSharper disable PossibleNullReferenceException
				// ReSharper disable ReturnValueOfPureMethodIsNotUsed
				default(ArrayIndexNode).Run(default(Closure));
				default(ArrayIndexNode).ToString();
				default(Closure).GetType(default(object));
				default(ArrayLengthNode).Run(default(Closure));
				default(ArrayLengthNode).ToString();
				default(BinaryNode).Run(default(Closure));
				BinaryNode.Create(default(BinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				default(BinaryNode).ToString();
				default(CallNode).Run(default(Closure));
				default(CallNode).ToString();
				default(CoalesceNode).Run(default(Closure));
				default(CoalesceNode).ToString();
				default(ConditionalNode).Run(default(Closure));
				default(ConditionalNode).ToString();
				default(ConstantNode).Run(default(Closure));
				default(ConstantNode).ToString();
				default(ExecutionNode).Run(default(Closure));
				FastCall.TryCreate(default(System.Reflection.MethodInfo));
				Intrinsic.InvokeBinaryOperation(default(Closure), default(object), default(object), default(ExpressionType), default(Intrinsic.BinaryOperation));
				Intrinsic.InvokeUnaryOperation(default(Closure), default(object), default(ExpressionType), default(Intrinsic.UnaryOperation));
				Intrinsic.InvokeConversion(default(Closure), default(object), default(Type), default(ExpressionType), default(Intrinsic.UnaryOperation));
				Intrinsic.CreateUnaryOperationFn(default(System.Reflection.MethodInfo));
				Intrinsic.CreateBinaryOperationFn(default(System.Reflection.MethodInfo));
				Intrinsic.WrapUnaryOperation(default(Type), default(string));
				Intrinsic.WrapUnaryOperation(default(System.Reflection.MethodInfo));
				Intrinsic.WrapBinaryOperation(default(Type), default(string));
				Intrinsic.WrapBinaryOperation(default(System.Reflection.MethodInfo));
				default(InvocationNode).Run(default(Closure));
				default(InvocationNode).ToString();
				default(LambdaNode).Run(default(Closure));
				default(LambdaNode).ToString();
				default(ListInitNode).Run(default(Closure));
				default(ListInitNode).ToString();
				default(LocalNode).Run(default(Closure));
				default(LocalNode).ToString();
				default(MemberAccessNode).Run(default(Closure));
				default(MemberAccessNode).ToString();
				default(MemberAssignmentsNode).Run(default(Closure));
				default(MemberAssignmentsNode).ToString();
				default(MemberInitNode).Run(default(Closure));
				default(MemberInitNode).ToString();
				default(MemberListBindingsNode).Run(default(Closure));
				default(MemberListBindingsNode).ToString();
				default(MemberMemberBindingsNode).Run(default(Closure));
				default(MemberMemberBindingsNode).ToString();
				default(NewArrayBoundsNode).Run(default(Closure));
				default(NewArrayBoundsNode).ToString();
				default(NewArrayInitNode).Run(default(Closure));
				default(NewArrayInitNode).ToString();
				default(NewNode).Run(default(Closure));
				default(NewNode).ToString();
				default(UnaryNode).Run(default(Closure));
				UnaryNode.Create(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				default(UnaryNode).ToString();
				default(NullNode).Run(default(Closure));
				default(NullNode).ToString();
				default(ParameterNode).Run(default(Closure));
				default(ParameterNode).ToString();
				default(QuoteNode).Run(default(Closure));
				default(QuoteNode).ToString();
				default(TypeAsNode).Run(default(Closure));
				default(TypeAsNode).ToString();
				default(TypeIsNode).Run(default(Closure));
				default(TypeIsNode).ToString();
				// ReSharper restore ReturnValueOfPureMethodIsNotUsed
				// ReSharper restore PossibleNullReferenceException
			}
		}

		public static Func<ResultT> Prepare<ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters = null)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = Constants.EmptyParameters;
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = Array.ConvertAll(constExpressions, c => c.Value);

			return () =>
			{
				var locals = new object[] { null, null, null };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, ResultT> Prepare<Arg1T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = Array.ConvertAll(constExpressions, c => c.Value);

			return arg1 =>
			{
				var locals = new object[] { null, null, null, arg1 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);

				return result;
			};
		}
		public static Func<Arg1T, Arg2T, ResultT> Prepare<Arg1T, Arg2T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = Array.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, Arg2T, Arg3T, ResultT> Prepare<Arg1T, Arg2T, Arg3T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = Array.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}
		public static Func<Arg1T, Arg2T, Arg3T, Arg4T, ResultT> Prepare<Arg1T, Arg2T, Arg3T, Arg4T, ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
		{
			if (body == null) throw new ArgumentNullException("body");
			if (parameters == null) throw new ArgumentNullException("parameters");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExpressions = collector.Constants.ToArray();
			var parameterExpressions = parameters.ToArray();
			var compiledBody = Compile(body, constExpressions, parameterExpressions);
			var constants = Array.ConvertAll(constExpressions, c => c.Value);

			return (arg1, arg2, arg3, arg4) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledBody.Run(closure);
				Array.Clear(locals, 0, locals.Length);
				return result;
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
