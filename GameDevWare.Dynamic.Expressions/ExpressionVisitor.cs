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
using System.Linq.Expressions;

#pragma warning disable 1591

namespace GameDevWare.Dynamic.Expressions
{
	/// <summary>
	/// Represents a visitor or rewriter for expression trees.
	/// </summary>
	public abstract class ExpressionVisitor
	{
		// Methods
		private Exception UnhandledBindingType(MemberBindingType memberBindingType)
		{
			throw new InvalidOperationException(string.Format("Unknown binding type '{0}'.", memberBindingType));
		}

		private Exception UnhandledExpressionType(ExpressionType expressionType)
		{
			throw new InvalidOperationException(string.Format("Unknown expression type '{0}'.", expressionType));
		}

		/// <summary>
		/// Dispatches the expression to one of the more specialized visit methods in this class.
		/// </summary>
		public Expression Visit(Expression exp)
		{
			if (exp == null)
			{
				return exp;
			}
			switch (exp.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.ArrayIndex:
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
					return this.VisitBinary((BinaryExpression) exp);

				case ExpressionType.ArrayLength:
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Quote:
				case ExpressionType.TypeAs:
					return this.VisitUnary((UnaryExpression) exp);

				case ExpressionType.Call:
					return this.VisitMethodCall((MethodCallExpression) exp);

				case ExpressionType.Conditional:
					return this.VisitConditional((ConditionalExpression) exp);

				case ExpressionType.Constant:
					return this.VisitConstant((ConstantExpression) exp);

				case ExpressionType.Invoke:
					return this.VisitInvocation((InvocationExpression) exp);

				case ExpressionType.Lambda:
					return this.VisitLambda((LambdaExpression) exp);

				case ExpressionType.ListInit:
					return this.VisitListInit((ListInitExpression) exp);

				case ExpressionType.MemberAccess:
					return this.VisitMemberAccess((MemberExpression) exp);

				case ExpressionType.MemberInit:
					return this.VisitMemberInit((MemberInitExpression) exp);

				case ExpressionType.New:
					return this.VisitNew((NewExpression) exp);

				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					return this.VisitNewArray((NewArrayExpression) exp);

				case ExpressionType.Parameter:
					return this.VisitParameter((ParameterExpression) exp);

				case ExpressionType.TypeIs:
					return this.VisitTypeIs((TypeBinaryExpression) exp);
			}
			throw this.UnhandledExpressionType(exp.NodeType);
		}

		protected virtual Expression VisitBinary(BinaryExpression binaryExpression)
		{
			var left = this.Visit(binaryExpression.Left);
			var right = this.Visit(binaryExpression.Right);
			var expression3 = this.Visit(binaryExpression.Conversion);
			if (((left == binaryExpression.Left) && (right == binaryExpression.Right)) && (expression3 == binaryExpression.Conversion))
			{
				return binaryExpression;
			}
			if ((binaryExpression.NodeType == ExpressionType.Coalesce) && (binaryExpression.Conversion != null))
			{
				return Expression.Coalesce(left, right, expression3 as LambdaExpression);
			}
			return Expression.MakeBinary(binaryExpression.NodeType, left, right, binaryExpression.IsLiftedToNull, binaryExpression.Method);
		}

		protected virtual MemberBinding VisitBinding(MemberBinding binding)
		{
			switch (binding.BindingType)
			{
				case MemberBindingType.Assignment:
					return this.VisitMemberAssignment((MemberAssignment) binding);

				case MemberBindingType.MemberBinding:
					return this.VisitMemberMemberBinding((MemberMemberBinding) binding);

				case MemberBindingType.ListBinding:
					return this.VisitMemberListBinding((MemberListBinding) binding);
			}
			throw this.UnhandledBindingType(binding.BindingType);
		}

		protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			List<MemberBinding> list = null;
			var num = 0;
			var count = original.Count;
			while (num < count)
			{
				var item = this.VisitBinding(original[num]);
				if (list != null)
				{
					list.Add(item);
				}
				else if (item != original[num])
				{
					list = new List<MemberBinding>(count);
					for (var i = 0; i < num; i++)
					{
						list.Add(original[i]);
					}
					list.Add(item);
				}
				num++;
			}
			if (list != null)
			{
				return list;
			}
			return original;
		}

		protected virtual Expression VisitConditional(ConditionalExpression conditionalExpression)
		{
			var test = this.Visit(conditionalExpression.Test);
			var ifTrue = this.Visit(conditionalExpression.IfTrue);
			var ifFalse = this.Visit(conditionalExpression.IfFalse);
			if (((test == conditionalExpression.Test) && (ifTrue == conditionalExpression.IfTrue)) && (ifFalse == conditionalExpression.IfFalse))
			{
				return conditionalExpression;
			}
			return Expression.Condition(test, ifTrue, ifFalse);
		}

		protected virtual Expression VisitConstant(ConstantExpression constantExpression)
		{
			return constantExpression;
		}

		protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
		{
			var arguments = this.VisitExpressionList(initializer.Arguments);
			if (arguments != initializer.Arguments)
			{
				return Expression.ElementInit(initializer.AddMethod, arguments);
			}
			return initializer;
		}

		protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			List<ElementInit> list = null;
			var num = 0;
			var count = original.Count;
			while (num < count)
			{
				var item = this.VisitElementInitializer(original[num]);
				if (list != null)
				{
					list.Add(item);
				}
				else if (item != original[num])
				{
					list = new List<ElementInit>(count);
					for (var i = 0; i < num; i++)
					{
						list.Add(original[i]);
					}
					list.Add(item);
				}
				num++;
			}
			if (list != null)
			{
				return list;
			}
			return original;
		}

		protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			List<Expression> list = null;
			var num = 0;
			var count = original.Count;
			while (num < count)
			{
				var item = this.Visit(original[num]);
				if (list != null)
				{
					list.Add(item);
				}
				else if (item != original[num])
				{
					list = new List<Expression>(count);
					for (var i = 0; i < num; i++)
					{
						list.Add(original[i]);
					}
					list.Add(item);
				}
				num++;
			}
			if (list != null)
			{
				return new ReadOnlyCollection<Expression>(list);
			}
			return original;
		}

		protected virtual Expression VisitInvocation(InvocationExpression invocationExpression)
		{
			IEnumerable<Expression> arguments = this.VisitExpressionList(invocationExpression.Arguments);
			var expression = this.Visit(invocationExpression.Expression);
			if ((arguments == invocationExpression.Arguments) && (expression == invocationExpression.Expression))
			{
				return invocationExpression;
			}
			return Expression.Invoke(expression, arguments);
		}

		protected virtual Expression VisitLambda(LambdaExpression lambda)
		{
			var body = this.Visit(lambda.Body);
			if (body != lambda.Body)
			{
				return Expression.Lambda(lambda.Type, body, lambda.Parameters);
			}
			return lambda;
		}

		protected virtual Expression VisitListInit(ListInitExpression listInitExpression)
		{
			var newExpression = this.VisitNew(listInitExpression.NewExpression);
			var initializers = this.VisitElementInitializerList(listInitExpression.Initializers);
			if ((newExpression == listInitExpression.NewExpression) && (initializers == listInitExpression.Initializers))
			{
				return listInitExpression;
			}
			return Expression.ListInit(newExpression, initializers);
		}

		protected virtual Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			var expression = this.Visit(memberExpression.Expression);
			if (expression != memberExpression.Expression)
			{
				return Expression.MakeMemberAccess(expression, memberExpression.Member);
			}
			return memberExpression;
		}

		protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			var expression = this.Visit(assignment.Expression);
			if (expression != assignment.Expression)
			{
				return Expression.Bind(assignment.Member, expression);
			}
			return assignment;
		}

		protected virtual Expression VisitMemberInit(MemberInitExpression memberInitExpression)
		{
			var newExpression = this.VisitNew(memberInitExpression.NewExpression);
			var bindings = this.VisitBindingList(memberInitExpression.Bindings);
			if ((newExpression == memberInitExpression.NewExpression) && (bindings == memberInitExpression.Bindings))
			{
				return memberInitExpression;
			}
			return Expression.MemberInit(newExpression, bindings);
		}

		protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			var initializers = this.VisitElementInitializerList(binding.Initializers);
			if (initializers != binding.Initializers)
			{
				return Expression.ListBind(binding.Member, initializers);
			}
			return binding;
		}

		protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			var bindings = this.VisitBindingList(binding.Bindings);
			if (bindings != binding.Bindings)
			{
				return Expression.MemberBind(binding.Member, bindings);
			}
			return binding;
		}

		protected virtual Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			var instance = this.Visit(methodCallExpression.Object);
			IEnumerable<Expression> arguments = this.VisitExpressionList(methodCallExpression.Arguments);
			if ((instance == methodCallExpression.Object) && (arguments == methodCallExpression.Arguments))
			{
				return methodCallExpression;
			}
			return Expression.Call(instance, methodCallExpression.Method, arguments);
		}

		protected virtual NewExpression VisitNew(NewExpression newExpression)
		{
			IEnumerable<Expression> arguments = this.VisitExpressionList(newExpression.Arguments);
			if (arguments == newExpression.Arguments)
			{
				return newExpression;
			}
			if (newExpression.Members != null)
			{
				return Expression.New(newExpression.Constructor, arguments, newExpression.Members);
			}
			return Expression.New(newExpression.Constructor, arguments);
		}

		protected virtual Expression VisitNewArray(NewArrayExpression newArrayExpression)
		{
			IEnumerable<Expression> initializers = this.VisitExpressionList(newArrayExpression.Expressions);
			if (initializers == newArrayExpression.Expressions)
			{
				return newArrayExpression;
			}
			if (newArrayExpression.NodeType == ExpressionType.NewArrayInit)
			{
				return Expression.NewArrayInit(newArrayExpression.Type.GetElementType(), initializers);
			}
			return Expression.NewArrayBounds(newArrayExpression.Type.GetElementType(), initializers);
		}

		protected virtual Expression VisitParameter(ParameterExpression parameterExpression)
		{
			return parameterExpression;
		}

		protected virtual Expression VisitTypeIs(TypeBinaryExpression typeBinaryExpression)
		{
			var expression = this.Visit(typeBinaryExpression.Expression);
			if (expression != typeBinaryExpression.Expression)
			{
				return Expression.TypeIs(expression, typeBinaryExpression.TypeOperand);
			}
			return typeBinaryExpression;
		}

		protected virtual Expression VisitUnary(UnaryExpression unaryExpression)
		{
			var operand = this.Visit(unaryExpression.Operand);
			if (operand != unaryExpression.Operand)
			{
				return Expression.MakeUnary(unaryExpression.NodeType, operand, unaryExpression.Type, unaryExpression.Method);
			}
			return unaryExpression;
		}
	}
}
