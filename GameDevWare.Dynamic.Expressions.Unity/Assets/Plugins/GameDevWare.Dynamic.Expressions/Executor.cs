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
using System.Reflection;

// ReSharper disable UnusedParameter.Local

namespace GameDevWare.Dynamic.Expressions
{
	internal static partial class Executor
	{
		private const int LOCAL_OPERAND1 = 0;
		private const int LOCAL_OPERAND2 = 1;
		private const int LOCAL_SLOT1 = 2;
		private const int LOCAL_FIRST_PARAMETER = 3; // this is offset of first parameter in Closure locals

		private sealed class Closure
		{
			public readonly object[] Constants;
			public readonly object[] Locals; // first two locals is reserved, third and others is parameters

			public Closure(object[] constants, object[] locals)
			{
				if (constants == null) throw new ArgumentNullException("constants");
				if (locals == null) throw new ArgumentNullException("locals");
				this.Constants = constants;
				this.Locals = locals;
			}

			public object Box<T>(T value)
			{
				return value;
			}

			public T Unbox<T>(object boxed)
			{
				//if (boxed is StrongBox<T>)
				//	return ((StrongBox<T>)boxed).Value;
				//else if (boxed is IStrongBox)
				//	boxed = ((IStrongBox)boxed).Value;

				if (boxed is T)
					return (T)boxed;
				else
					return (T)System.Convert.ChangeType(boxed, typeof(T));
			}

			public bool Is<T>(object boxed)
			{
				return boxed is T;
			}
		}
		private sealed class ConstantsCollector : ExpressionVisitor
		{
			public readonly List<ConstantExpression> Constants = new List<ConstantExpression>();

			protected override Expression VisitConstant(ConstantExpression constantExpression)
			{
				this.Constants.Add(constantExpression);
				return constantExpression;
			}
		}
		private delegate object ExecuteFunc(Closure closure);

		static Executor()
		{
			// AOT
			if (typeof(Executor).Name == string.Empty)
			{
				Expression(default(Expression), default(ConstantExpression[]), default(ParameterExpression[]));
				Conditional(default(ConditionalExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Constant(default(ConstantExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Invocation(default(InvocationExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Lambda(default(LambdaExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				ListInit(default(ListInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberAccess(default(MemberExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberInit(default(MemberInitExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberAssignments(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberListBindings(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				MemberMemberBindings(default(IEnumerable<MemberBinding>), default(ConstantExpression[]), default(ParameterExpression[]));
				Call(default(MethodCallExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				New(default(NewExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				NewArray(default(NewArrayExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Parameter(default(ParameterExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				TypeIs(default(TypeBinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				TypeAs(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Convert(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Unary(default(UnaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				Binary(default(BinaryExpression), default(ConstantExpression[]), default(ParameterExpression[]));
				CreateUnaryOperationFn(default(MethodInfo));
				CreateBinaryOperationFn(default(MethodInfo));
				WrapUnaryOperation(default(Type), default(string));
				WrapUnaryOperation(default(MethodInfo));
				WrapBinaryOperation(default(Type), default(string));
				WrapBinaryOperation(default(MethodInfo));
			}
		}

		public static Func<ResultT> Prepare<ResultT>(Expression body, ReadOnlyCollection<ParameterExpression> parameters = null)
		{
			if (body == null) throw new ArgumentNullException("body");

			var collector = new ConstantsCollector();
			collector.Visit(body);

			var constExprs = collector.Constants.ToArray();
			var paramExprs = Constants.EmptyParameters;
			var compiledFn = Expression(body, constExprs, paramExprs);
			var constants = Array.ConvertAll(constExprs, c => c.Value);

			return () =>
			{
				var locals = new object[] { null, null, null };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledFn(closure);
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

			var constExprs = collector.Constants.ToArray();
			var paramExprs = parameters.ToArray();
			var compiledFn = Expression(body, constExprs, paramExprs);
			var constants = Array.ConvertAll(constExprs, c => c.Value);

			return arg1 =>
			{
				var locals = new object[] { null, null, null, arg1 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledFn(closure);
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

			var constExprs = collector.Constants.ToArray();
			var paramExprs = parameters.ToArray();
			var compiledFn = Expression(body, constExprs, paramExprs);
			var constants = Array.ConvertAll(constExprs, c => c.Value);

			return (arg1, arg2) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledFn(closure);
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

			var constExprs = collector.Constants.ToArray();
			var paramExprs = parameters.ToArray();
			var compiledFn = Expression(body, constExprs, paramExprs);
			var constants = Array.ConvertAll(constExprs, c => c.Value);

			return (arg1, arg2, arg3) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledFn(closure);
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

			var constExprs = collector.Constants.ToArray();
			var paramExprs = parameters.ToArray();
			var compiledFn = Expression(body, constExprs, paramExprs);
			var constants = Array.ConvertAll(constExprs, c => c.Value);

			return (arg1, arg2, arg3, arg4) =>
			{
				var locals = new object[] { null, null, null, arg1, arg2, arg3, arg4 };
				var closure = new Closure(constants, locals);

				var result = (ResultT)compiledFn(closure);
				Array.Clear(locals, 0, locals.Length);
				return result;
			};
		}

		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, Arg3T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, Arg2T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, Arg2T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, Arg1T, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, Arg1T, ResultT>();
		}
		public static void RegisterForFastCall<InstanceT, ResultT>()
		{
			MethodCall.RegisterInstanceMethod<InstanceT, ResultT>();
		}

		private static ExecuteFunc Expression(Expression expression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			if (expression == null)
				return (closure => null);

			switch (expression.NodeType)
			{
				case ExpressionType.ArrayIndex:
					return ArrayIndex(expression, constExprs, paramExprs);
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Coalesce:
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
					return Binary((BinaryExpression)expression, constExprs, paramExprs);
				case ExpressionType.ArrayLength:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
					return Unary((UnaryExpression)expression, constExprs, paramExprs);
				case ExpressionType.Quote:
					return closure => ((UnaryExpression)expression).Operand;
				case ExpressionType.Call:
					return Call((MethodCallExpression)expression, constExprs, paramExprs);

				case ExpressionType.Conditional:
					return Conditional((ConditionalExpression)expression, constExprs, paramExprs);

				case ExpressionType.Constant:
					return Constant((ConstantExpression)expression, constExprs, paramExprs);

				case ExpressionType.Invoke:
					return Invocation((InvocationExpression)expression, constExprs, paramExprs);

				case ExpressionType.Lambda:
					return Lambda((LambdaExpression)expression, constExprs, paramExprs);

				case ExpressionType.ListInit:
					return ListInit((ListInitExpression)expression, constExprs, paramExprs);

				case ExpressionType.MemberAccess:
					return MemberAccess((MemberExpression)expression, constExprs, paramExprs);

				case ExpressionType.MemberInit:
					return MemberInit((MemberInitExpression)expression, constExprs, paramExprs);

				case ExpressionType.New:
					return New((NewExpression)expression, constExprs, paramExprs);

				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					return NewArray((NewArrayExpression)expression, constExprs, paramExprs);

				case ExpressionType.Parameter:
					return Parameter((ParameterExpression)expression, constExprs, paramExprs);

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					return Convert((UnaryExpression)expression, constExprs, paramExprs);

				case ExpressionType.TypeAs:
					return TypeAs((UnaryExpression)expression, constExprs, paramExprs);

				case ExpressionType.TypeIs:
					return TypeIs((TypeBinaryExpression)expression, constExprs, paramExprs);
			}
			throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNEXPRTYPE, expression.Type));
		}

		private static ExecuteFunc Conditional(ConditionalExpression conditionalExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var baseExpression = default(Expression);
			var continuationExpression = default(Expression);
			// try to detect null-propagation operation
			if (ExpressionBuilder.ExtractNullPropagationExpression(conditionalExpression, out baseExpression, out continuationExpression))
			{
				var methodCallExpression = continuationExpression as MethodCallExpression;
				var memberExpression = continuationExpression as MemberExpression;
				var indexExpression = continuationExpression as BinaryExpression;

				if (indexExpression == null && methodCallExpression == null && memberExpression == null)
					throw new InvalidOperationException(string.Format("Unknown null-propagation pattern met: {0}?.{1}.", baseExpression.NodeType, continuationExpression.NodeType));

				var slot1ParameterExpression = ConstantExpression.Parameter(baseExpression.Type, "slot1");
				var baseFn = Expression(baseExpression, constExprs, paramExprs);
				var continueFn = Expression(methodCallExpression != null ? ConstantExpression.Call(slot1ParameterExpression, methodCallExpression.Method, methodCallExpression.Arguments) :
									memberExpression != null ? ConstantExpression.MakeMemberAccess(slot1ParameterExpression, memberExpression.Member) :
									(Expression)ConstantExpression.MakeBinary(ExpressionType.ArrayIndex, slot1ParameterExpression, indexExpression.Right), constExprs, paramExprs);

				return closure =>
				{
					var baseValue = baseFn(closure);
					if (baseValue == null)
						return null;

					closure.Locals[LOCAL_SLOT1] = baseValue;
					return continueFn(closure);
				};

			}
			else
			{
				var trueFn = Expression(conditionalExpression.IfTrue, constExprs, paramExprs);
				var falseFn = Expression(conditionalExpression.IfFalse, constExprs, paramExprs);
				var testFn = Expression(conditionalExpression.Test, constExprs, paramExprs);

				return closure => closure.Unbox<bool>(testFn(closure)) ? trueFn(closure) : falseFn(closure);
			}
		}

		private static ExecuteFunc Constant(ConstantExpression constantExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			return closure => closure.Constants[Array.IndexOf(constExprs, constantExpression)];
		}

		private static ExecuteFunc Invocation(InvocationExpression invocationExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var target = Expression(invocationExpression.Expression, constExprs, paramExprs);
			var argsFns = invocationExpression.Arguments.Select(e => Expression(e, constExprs, paramExprs)).ToArray();

			return closure =>
			{
				var targetDelegate = (Delegate)target(closure);
				var args = new object[argsFns.Length];
				for (var i = 0; i < args.Length; i++)
					args[i] = argsFns[i](closure);

				return targetDelegate.DynamicInvoke(args);
			};
		}

		private static ExecuteFunc Lambda(LambdaExpression lambdaExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			if (lambdaExpression.Type.IsGenericType == false)
				throw new NotSupportedException(Properties.Resources.EXCEPTION_COMPIL_ONLYFUNCLAMBDASISSUPPORTED);

			var funcDefinition = lambdaExpression.Type.GetGenericTypeDefinition();
			if (funcDefinition != typeof(Func<>) && funcDefinition != typeof(Func<,>) && funcDefinition != typeof(Func<,,>) && funcDefinition != typeof(Func<,,,>) && funcDefinition != typeof(Func<,,,,>))
				throw new NotSupportedException(Properties.Resources.EXCEPTION_COMPIL_ONLYFUNCLAMBDASISSUPPORTED);

			var funcArguments = lambdaExpression.Type.GetGenericArguments();
			var prepareMethodDefinition = typeof(Executor).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == Constants.EXECUTE_PREPARE_NAME && m.GetGenericArguments().Length == funcArguments.Length);
			var prepareMethod = prepareMethodDefinition.MakeGenericMethod(funcArguments);

			return closure =>
			{
				var body = lambdaExpression.Body;
				var parameters = lambdaExpression.Parameters;

				// substitute captured parameters
				if (paramExprs.Length > 0)
				{
					var substitutions = new Dictionary<Expression, Expression>(paramExprs.Length);
					foreach (var parameterExpr in paramExprs)
					{
						var parameterValue = closure.Locals[LOCAL_FIRST_PARAMETER + Array.IndexOf(paramExprs, parameterExpr)];
						substitutions.Add(parameterExpr, System.Linq.Expressions.Expression.Constant(parameterValue, parameterExpr.Type));
					}
					body = ExpressionSubstitutor.Visit(body, substitutions);
				}

				// prepare lambda
				return prepareMethod.Invoke(null, new object[] { body, parameters });
			};
		}

		private static ExecuteFunc ListInit(ListInitExpression listInitExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var newFn = New(listInitExpression.NewExpression, constExprs, paramExprs);
			var listInits =
			(
				from elemInit in listInitExpression.Initializers
				let argsFns = elemInit.Arguments.Select(e => Expression(e, constExprs, paramExprs)).ToArray()
				select new { addMethod = elemInit.AddMethod, argsFns }
			).ToArray();

			return closure =>
			{
				var list = newFn(closure);
				if (listInits.Length == 0) return list;

				foreach (var listInit in listInits)
				{
					var addMethod = listInit.addMethod;
					var addArgs = new object[listInit.argsFns.Length];

					for (var i = 0; i < listInit.argsFns.Length; i++)
						addArgs[i] = listInit.argsFns[i](closure);

					addMethod.Invoke(list, addArgs);
				}
				return list;
			};
		}

		private static ExecuteFunc MemberAccess(MemberExpression memberExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var valueFn = Expression(memberExpression.Expression, constExprs, paramExprs);

			return closure =>
			{
				var target = valueFn(closure);
				var member = memberExpression.Member;
				if (member is FieldInfo)
					return ((FieldInfo)member).GetValue(target);
				else
					return ((PropertyInfo)member).GetValue(target, null);
			};
		}

		private static ExecuteFunc MemberInit(MemberInitExpression memberInitExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var newFn = New(memberInitExpression.NewExpression, constExprs, paramExprs);
			var memberAssignments = MemberAssignments(memberInitExpression.Bindings, constExprs, paramExprs);
			var listBindings = MemberListBindings(memberInitExpression.Bindings, constExprs, paramExprs);
			var memberMemberBindings = MemberMemberBindings(memberInitExpression.Bindings, constExprs, paramExprs);

			return closure =>
			{

				var instance = newFn(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				memberAssignments(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				listBindings(closure);

				closure.Locals[LOCAL_OPERAND1] = instance;
				memberMemberBindings(closure);

				closure.Locals[LOCAL_OPERAND1] = null;
				return instance;
			};
		}

		private static ExecuteFunc MemberAssignments(IEnumerable<MemberBinding> bindings, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var assignFns = (
				from bind in bindings
				let assign = bind as MemberAssignment
				where assign != null
				select new { member = bind.Member, valueFn = Expression(assign.Expression, constExprs, paramExprs) }
			).ToArray();

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (assignFns.Length == 0) return instance;

				foreach (var assignFn in assignFns)
				{
					var member = assignFn.member;
					var valueFn = assignFn.valueFn;

					if (member is FieldInfo)
						((FieldInfo)member).SetValue(instance, valueFn(closure));
					else
						((PropertyInfo)member).SetValue(instance, valueFn(closure), null);
				}
				return instance;
			};
		}

		private static ExecuteFunc MemberListBindings(IEnumerable<MemberBinding> bindings, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var listBindGroups =
			(
				from bind in bindings
				let listBind = bind as MemberListBinding
				where listBind != null
				from elemInit in listBind.Initializers
				let argsFns = elemInit.Arguments.Select(e => Expression(e, constExprs, paramExprs)).ToArray()
				select new { member = bind.Member, addMethod = elemInit.AddMethod, argsFns }
			).ToLookup(m => m.member);

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (listBindGroups.Count == 0) return instance;

				foreach (var listBindGroup in listBindGroups)
				{
					var member = listBindGroup.Key;
					var addTarget = default(object);
					if (member is FieldInfo)
						addTarget = ((FieldInfo)member).GetValue(instance);
					else
						addTarget = ((PropertyInfo)member).GetValue(instance, null);
					if (addTarget == null) throw new NullReferenceException();

					foreach (var bindGroup in listBindGroup)
					{
						var addMethod = bindGroup.addMethod;
						var addArgs = new object[bindGroup.argsFns.Length];
						for (var i = 0; i < bindGroup.argsFns.Length; i++)
							addArgs[i] = bindGroup.argsFns[i](closure);
						addMethod.Invoke(addTarget, addArgs);
					}
				}
				return instance;
			};
		}

		private static ExecuteFunc MemberMemberBindings(IEnumerable<MemberBinding> bindings, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var bindGroups =
			(
				from bind in bindings
				let memberBinding = bind as MemberMemberBinding
				where memberBinding != null
				let memberAssignments = MemberAssignments(memberBinding.Bindings, constExprs, paramExprs)
				let listBindings = MemberListBindings(memberBinding.Bindings, constExprs, paramExprs)
				let memberMemberBindings = MemberMemberBindings(memberBinding.Bindings, constExprs, paramExprs)
				select new { member = memberBinding.Member, memberAssignments, listBindings, memberMemberBindings }
			).ToLookup(m => m.member);

			return closure =>
			{
				var instance = closure.Locals[LOCAL_OPERAND1];
				if (bindGroups.Count == 0) return instance;

				foreach (var bindGroup in bindGroups)
				{
					var member = bindGroup.Key;
					var bindTarget = default(object);
					if (member is FieldInfo)
						bindTarget = ((FieldInfo)member).GetValue(instance);
					else
						bindTarget = ((PropertyInfo)member).GetValue(instance, null);
					if (bindTarget == null) throw new NullReferenceException();

					foreach (var bind in bindGroup)
					{
						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.memberAssignments(closure);

						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.listBindings(closure);

						closure.Locals[LOCAL_OPERAND1] = bindTarget;
						bind.memberMemberBindings(closure);
					}
				}
				return instance;
			};
		}

		private static ExecuteFunc Call(MethodCallExpression methodCallExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var targetFn = Expression(methodCallExpression.Object, constExprs, paramExprs);
			var argsFns = methodCallExpression.Arguments.Select(e => Expression(e, constExprs, paramExprs)).ToArray();
			var invokeFn = MethodCall.TryCreate(methodCallExpression.Method);

			if (invokeFn != null)
			{
				return closure => { return invokeFn(closure, argsFns); };
			}
			else
			{
				return closure =>
				{
					var target = targetFn(closure);
					var args = new object[argsFns.Length];
					for (var i = 0; i < args.Length; i++)
						args[i] = argsFns[i](closure);

					return methodCallExpression.Method.Invoke(target, args);
				};
			}
		}

		private static ExecuteFunc New(NewExpression newExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var valuesFns = newExpression.Arguments.Select(e => Expression(e, constExprs, paramExprs)).ToArray();

			return closure =>
			{
				var source = new object[valuesFns.Length];
				for (var i = 0; i < source.Length; i++)
					source[i] = valuesFns[i](closure);

				var args = source.Take(newExpression.Constructor.GetParameters().Length).ToArray();
				var isNullableType = IsNullable(newExpression.Type);
				var instance = isNullableType ? null : Activator.CreateInstance(newExpression.Type, args);

				if (newExpression.Members != null)
				{
					for (var j = 0; j < newExpression.Members.Count; j++)
					{
						var member = newExpression.Members[j];
						if (member is FieldInfo)
							((FieldInfo)member).SetValue(instance, source[args.Length + j]);
						else
							((PropertyInfo)member).SetValue(instance, source[args.Length + j], null);
					}
				}
				return instance;
			};
		}

		private static ExecuteFunc NewArray(NewArrayExpression newArrayExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			if (newArrayExpression.NodeType == ExpressionType.NewArrayBounds)
			{
				var lengthFns = newArrayExpression.Expressions.Select(e => Expression(e, constExprs, paramExprs)).ToArray();

				return closure =>
				{
					var lengths = new int[lengthFns.Length];
					for (var i = 0; i < lengthFns.Length; i++)
						lengths[i] = closure.Unbox<int>(lengthFns[i](closure));

					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), lengths);
					return array;
				};
			}
			else
			{
				var valuesFns = newArrayExpression.Expressions.Select(e => Expression(e, constExprs, paramExprs)).ToArray();

				return closure =>
				{
					// ReSharper disable once AssignNullToNotNullAttribute
					var array = Array.CreateInstance(newArrayExpression.Type.GetElementType(), valuesFns.Length);
					for (var i = 0; i < valuesFns.Length; i++)
						array.SetValue(valuesFns[i](closure), i);

					return array;
				};
			}
		}

		private static ExecuteFunc Parameter(ParameterExpression parameterExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			return closure => closure.Locals[LOCAL_FIRST_PARAMETER + Array.IndexOf(paramExprs, parameterExpression)];
		}

		private static ExecuteFunc TypeIs(TypeBinaryExpression typeBinaryExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var valueFn = Expression(typeBinaryExpression.Expression, constExprs, paramExprs);

			return closure =>
			{
				var value = valueFn(closure);
				if (value == null) return false;

				return typeBinaryExpression.TypeOperand.IsAssignableFrom(value.GetType());
			};
		}

		private static ExecuteFunc TypeAs(UnaryExpression typeAsExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			if (typeAsExpression.Type.IsValueType)
				return Convert(typeAsExpression, constExprs, paramExprs);

			var valueFn = Expression(typeAsExpression.Operand, constExprs, paramExprs);
			return closure =>
			{
				var value = valueFn(closure);
				if (value == null)
					return null;

				if (typeAsExpression.Type.IsAssignableFrom(value.GetType()) == false)
					return null;

				return value;
			};
		}

		private static ExecuteFunc Convert(UnaryExpression convertExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var valueFn = Expression(convertExpression.Operand, constExprs, paramExprs);
			var convertOperator = WrapUnaryOperation(convertExpression.Method) ?? WrapUnaryOperation(
				convertExpression.Type
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(m =>
					(string.Equals(m.Name, "op_Explicit", StringComparison.Ordinal) || string.Equals(m.Name, "op_Implicit", StringComparison.Ordinal)) &&
					m.ReturnType == convertExpression.Type &&
					m.GetParameters().Length == 1 &&
					m.GetParameters()[0].ParameterType == convertExpression.Operand.Type)
			);
			var toType = Nullable.GetUnderlyingType(convertExpression.Type) ?? convertExpression.Type;
			var isToTypeNullable = IsNullable(convertExpression.Type);
			var fromType = Nullable.GetUnderlyingType(convertExpression.Operand.Type) ?? convertExpression.Operand.Type;
			var isFromTypeNullable = IsNullable(convertExpression.Operand);

			return closure =>
			{
				var value = closure.Unbox<object>(valueFn(closure));
				if (value == null && (convertExpression.Type.IsValueType == false || isToTypeNullable))
					return null;

				var convertType = convertExpression.NodeType;
				if (convertType != ExpressionType.Convert)
					convertType = ExpressionType.ConvertChecked;

				// un-box
				if ((fromType == typeof(object) || fromType == typeof(ValueType) || fromType.IsInterface) && toType.IsValueType)
				{
					// null un-box
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");
					// typecheck for un-box
					if (value.GetType() == toType)
						return value;
					throw new InvalidCastException();
				}
				// box
				else if (fromType.IsValueType && (toType == typeof(object) || toType == typeof(ValueType) || toType.IsInterface))
				{
					// typecheck for box
					return toType.IsInstanceOfType(value) ? value : null;
				}
				// to enum
				else if (toType.IsEnum && (fromType == typeof(byte) || fromType == typeof(sbyte) ||
					fromType == typeof(short) || fromType == typeof(ushort) ||
					fromType == typeof(int) || fromType == typeof(uint) ||
					fromType == typeof(long) || fromType == typeof(ulong)))
				{
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");

					value = Intrinsic.Convert(closure, value, Enum.GetUnderlyingType(toType), convertExpression.NodeType, null);
					return Enum.ToObject(toType, closure.Unbox<object>(value));
				}
				// from enum
				else if (fromType.IsEnum && (toType == typeof(byte) || toType == typeof(sbyte) ||
					toType == typeof(short) || toType == typeof(ushort) ||
					toType == typeof(int) || toType == typeof(uint) ||
					toType == typeof(long) || toType == typeof(ulong)))
				{
					if (value == null)
						throw new NullReferenceException("Attempt to unbox a null value.");

					value = System.Convert.ChangeType(value, Enum.GetUnderlyingType(fromType));
					value = Intrinsic.Convert(closure, value, toType, convertExpression.NodeType, null);
					return value;
				}
				// from nullable
				if (toType.IsValueType && isFromTypeNullable)
				{
					if (value == null) throw new NullReferenceException("Attempt to unbox a null value.");

					value = Intrinsic.Convert(closure, value, toType, convertExpression.NodeType, null);
				}
				else if (toType.IsInstanceOfType(value))
					return value;

				return Intrinsic.Convert(closure, value, toType, convertType, convertOperator);
			};
		}

		private static ExecuteFunc Unary(UnaryExpression unaryExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var valueFn = Expression(unaryExpression.Operand, constExprs, paramExprs);
			var opUnaryNegation = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryNegation");
			var opUnaryPlus = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_UnaryPlus");
			var opOnesComplement = WrapUnaryOperation(unaryExpression.Method) ?? WrapUnaryOperation(unaryExpression.Operand.Type, "op_OnesComplement");
			var isNullable = IsNullable(unaryExpression.Operand);

			return closure =>
			{
				var operand = valueFn(closure);

				if (isNullable && operand == null && unaryExpression.NodeType != ExpressionType.ArrayLength)
					return null;

				switch (unaryExpression.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
						return Intrinsic.UnaryOperation(closure, operand, unaryExpression.NodeType, opUnaryNegation);
					case ExpressionType.UnaryPlus:
						return Intrinsic.UnaryOperation(closure, operand, unaryExpression.NodeType, opUnaryPlus);
					case ExpressionType.Not:
						return Intrinsic.UnaryOperation(closure, operand, unaryExpression.NodeType, opOnesComplement);
					case ExpressionType.ArrayLength:
						return closure.Unbox<Array>(operand).Length;
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNUNARYEXPRTYPE, unaryExpression.Type));
			};
		}

		private static ExecuteFunc ArrayIndex(Expression expression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var binaryExpression = expression as BinaryExpression;
			var leftFn = binaryExpression != null ? Expression(binaryExpression.Left, constExprs, paramExprs) : null;
			var rightFn = binaryExpression != null ? Expression(binaryExpression.Right, constExprs, paramExprs) : null;
			var methodCallExpression = expression as MethodCallExpression;

			if (binaryExpression != null)
			{
				return closure =>
				{
					var left = leftFn(closure);
					var right = rightFn(closure);

					return closure.Is<int[]>(right)
						? closure.Unbox<Array>(left).GetValue(closure.Unbox<int[]>(right))
						: closure.Unbox<Array>(left).GetValue(closure.Unbox<int>(right));
				};
			}
			else
			{
				return Call(methodCallExpression, constExprs, paramExprs);
			}
		}

		private static ExecuteFunc Binary(BinaryExpression binaryExpression, ConstantExpression[] constExprs, ParameterExpression[] paramExprs)
		{
			var leftFn = Expression(binaryExpression.Left, constExprs, paramExprs);
			var rightFn = Expression(binaryExpression.Right, constExprs, paramExprs);
			var opAddition = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Addition");
			var opBitwiseAnd = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseAnd");
			var opDivision = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Division");
			var opEquality = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Equality");
			var opExclusiveOr = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_ExclusiveOr");
			var opGreaterThan = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThan");
			var opGreaterThanOrEqual = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_GreaterThanOrEqual");
			var opLessThan = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThan");
			var opLessThanOrEqual = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_LessThanOrEqual");
			var opModulus = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Modulus");
			var opMultiply = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Multiply");
			var opBitwiseOr = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_BitwiseOr");
			var opSubtraction = WrapBinaryOperation(binaryExpression.Method) ?? WrapBinaryOperation(binaryExpression.Left.Type, "op_Subtraction");
			var isNullable = IsNullable(binaryExpression.Left) || IsNullable(binaryExpression.Right);

			return closure =>
			{
				switch (binaryExpression.NodeType)
				{
					case ExpressionType.AndAlso:
						return closure.Unbox<bool>(leftFn(closure)) && closure.Unbox<bool>(rightFn(closure));
					case ExpressionType.OrElse:
						return closure.Unbox<bool>(leftFn(closure)) || closure.Unbox<bool>(rightFn(closure));
				}

				var left = leftFn(closure);
				var right = rightFn(closure);

				if
				(
					isNullable &&
					(left == null || right == null) &&
					binaryExpression.NodeType != ExpressionType.Coalesce &&
					binaryExpression.NodeType != ExpressionType.ArrayIndex
				)
				{
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (binaryExpression.NodeType)
					{
						case ExpressionType.Equal: return closure.Box(left == right);
						case ExpressionType.NotEqual: return closure.Box(left != right);
						case ExpressionType.GreaterThan:
						case ExpressionType.GreaterThanOrEqual:
						case ExpressionType.LessThan:
						case ExpressionType.LessThanOrEqual: return closure.Box(false);
						default: return null;
					}
				}

				switch (binaryExpression.NodeType)
				{
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opAddition);
					case ExpressionType.And:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opBitwiseAnd);
					case ExpressionType.Coalesce:
						return left ?? right;
					case ExpressionType.Divide:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opDivision);
					case ExpressionType.Equal:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opEquality);
					case ExpressionType.NotEqual:
						return closure.Box(closure.Unbox<bool>(Intrinsic.BinaryOperation(closure, left, right, ExpressionType.Equal, opEquality)) == false);
					case ExpressionType.ExclusiveOr:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opExclusiveOr);
					case ExpressionType.GreaterThan:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opGreaterThan);
					case ExpressionType.GreaterThanOrEqual:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opGreaterThanOrEqual);
					case ExpressionType.LeftShift:
					case ExpressionType.Power:
					case ExpressionType.RightShift:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, null);
					case ExpressionType.LessThan:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opLessThan);
					case ExpressionType.LessThanOrEqual:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opLessThanOrEqual);
					case ExpressionType.Modulo:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opModulus);
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opMultiply);
					case ExpressionType.Or:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opBitwiseOr);
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
						return Intrinsic.BinaryOperation(closure, left, right, binaryExpression.NodeType, opSubtraction);
				}

				throw new InvalidOperationException(string.Format(Properties.Resources.EXCEPTION_COMPIL_UNKNOWNBINARYEXPRTYPE, binaryExpression.Type));
			};
		}

		private static bool IsNullable(Expression expression)
		{
			if (expression == null) throw new ArgumentException("expression");

			var constantExpression = expression as ConstantExpression;
			if (constantExpression != null && constantExpression.Type == typeof(Object) && constantExpression.Value == null)
				return true;

			return IsNullable(expression.Type);
		}
		private static bool IsNullable(Type type)
		{
			if (type == null) throw new ArgumentException("type");

			return Nullable.GetUnderlyingType(type) != null;
		}
	}
}
