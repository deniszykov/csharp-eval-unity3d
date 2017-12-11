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

namespace GameDevWare.Dynamic.Expressions
{
	internal sealed class ExpressionLookupVisitor : ExpressionVisitor
	{
		private readonly List<Expression> lookupList;
		private int found = 0;

		public ExpressionLookupVisitor(List<Expression> lookupList)
		{
			if (lookupList == null) throw new ArgumentNullException("lookupList");

			this.lookupList = lookupList;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if (this.lookupList.Contains(binaryExpression))
				this.found++;

			return base.VisitBinary(binaryExpression);
		}
		protected override Expression VisitConditional(ConditionalExpression conditionalExpression)
		{
			if (this.lookupList.Contains(conditionalExpression))
				this.found++;

			return base.VisitConditional(conditionalExpression);
		}
		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (this.lookupList.Contains(constantExpression))
				this.found++;

			return base.VisitConstant(constantExpression);
		}
		protected override Expression VisitInvocation(InvocationExpression invocationExpression)
		{
			if (this.lookupList.Contains(invocationExpression))
				this.found++;

			return base.VisitInvocation(invocationExpression);
		}
		protected override Expression VisitLambda(LambdaExpression lambda)
		{
			if (this.lookupList.Contains(lambda))
				this.found++;

			return base.VisitLambda(lambda);
		}
		protected override Expression VisitListInit(ListInitExpression listInitExpression)
		{
			if (this.lookupList.Contains(listInitExpression))
				this.found++;

			return base.VisitListInit(listInitExpression);

		}
		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			if (this.lookupList.Contains(memberExpression))
				this.found++;

			return base.VisitMemberAccess(memberExpression);
		}
		protected override Expression VisitMemberInit(MemberInitExpression memberInitExpression)
		{
			if (this.lookupList.Contains(memberInitExpression))
				this.found++;

			return base.VisitMemberInit(memberInitExpression);
		}
		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (this.lookupList.Contains(methodCallExpression))
				this.found++;

			return base.VisitMethodCall(methodCallExpression);
		}
		protected override NewExpression VisitNew(NewExpression newExpression)
		{
			if (this.lookupList.Contains(newExpression))
				this.found++;

			return base.VisitNew(newExpression);
		}
		protected override Expression VisitNewArray(NewArrayExpression newArrayExpression)
		{
			if (this.lookupList.Contains(newArrayExpression))
				this.found++;

			return base.VisitNewArray(newArrayExpression);
		}
		protected override Expression VisitParameter(ParameterExpression parameterExpression)
		{
			if (this.lookupList.Contains(parameterExpression))
				this.found++;

			return base.VisitParameter(parameterExpression);
		}
		protected override Expression VisitTypeIs(TypeBinaryExpression typeBinaryExpression)
		{
			if (this.lookupList.Contains(typeBinaryExpression))
				this.found++;

			return base.VisitTypeIs(typeBinaryExpression);
		}
		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			if (this.lookupList.Contains(unaryExpression))
				this.found++;

			return base.VisitUnary(unaryExpression);
		}

		public static bool Lookup(Expression expression, List<Expression> lookupList)
		{
			var visitor = new ExpressionLookupVisitor(lookupList);
			visitor.Visit(expression);
			return visitor.found == lookupList.Count;
		}
	}
}
